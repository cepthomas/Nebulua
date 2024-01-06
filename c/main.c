// system
#include <windows.h>
#include <stdio.h>
#include <string.h>
#include <time.h>
#include <stdlib.h>
#include <unistd.h>
#include <synchapi.h>
// lua
#include "lua.h"
#include "lualib.h"
#include "lauxlib.h"
// lbot
#include "diag.h"
#include "logger.h"
#include "ftimer.h"
#include "stopwatch.h"
// application
#include "nebcommon.h"
#include "midi.h"
#include "cli.h"
#include "devmgr.h"
#include "luainterop.h"
#include "luainteropwork.h"

// TODO1 need some unit tests.

//----------------------- Definitions -----------------------//

#define TEST

//----------------------- Types -----------------------------//

// Cli command descriptor.
typedef struct cli_command_desc
{
    const char* long_name;
    const char* short_name;
    const char* desc;
    const char* args;
} cli_command_desc_t;

// Cli command handler.
typedef int (*cli_command_handler_t)(cli_command_desc_t* pcmd, int argc, char* argv[]);

// Map a cli commands.
typedef struct cli_command
{
    cli_command_handler_t handler;
    cli_command_desc_t desc;
} cli_command_t;


//----------------------- Vars - app ---------------------------//

// The main Lua thread.
static lua_State* _l;

// The script execution state.
static bool _script_running = false;

// The app execution state.
static bool _app_running = true;

// Last tick time.
static double _last_msec = 0.0;

// CLI contents.
static char _cli_buf[CLI_BUFF_LEN];

// Forward reference.
static const cli_command_t _commands[];

// Script access syncronization. https://learn.microsoft.com/en-us/windows/win32/sync/critical-section-objects
static CRITICAL_SECTION _critical_section; 

static bool _mon_input = false;
static bool _mon_output = false;

//----------------------- Vars - script ------------------------//

// Current tempo in bpm.
static int _tempo = 100;

// Current subbeat.
static int _position = 0;

// Length of composition in subbeats.
static int _length = 0;


//---------------------- Functions ------------------------//

// Start forever loop.
static int _Run(void);

// Tick corresponding to bpm. !!Interrupt!!
static void _MidiClockHandler(double msec);

// Handle incoming messages. !!Interrupt!!
static void _MidiInHandler(HMIDIIN hMidiIn, UINT wMsg, DWORD_PTR dwInstance, DWORD_PTR dwParam1, DWORD_PTR dwParam2);

// Blocking sleep.
static void _Sleep(int msec);

// Top level error handler. Logs and calls luaL_error() which doesn't return.
static bool _EvalStatus(int stat, const char* format, ...);

// Safe convert a string to double.
static bool _StrToDouble(const char* str, double* val, double min, double max);

// Safe convert a string to integer.
static bool _StrToInt(const char* str, int* val, int min, int max);


//----------------------------------------------------//

/// Main entry for the application. Process args and start system.
int main(int argc, char* argv[])
{
    int stat = NEB_OK;

    FILE* fp = fopen(".\\nebulua_log.txt", "a");
    logger_Init(fp);
    logger_SetFilters(LVL_DEBUG);

    // Initialize the critical section one time only. Lock until init done.
    InitializeCriticalSectionAndSpinCount(&_critical_section, 0x00000400);
    EnterCriticalSection(&_critical_section); 

    cli_Open('s');

#ifdef TEST
    // Some test code.
    bool alive = true;
    cli_WriteLine("Tell 1");
    cli_WriteLine("Tell 2 %d", fp);
    do
    {
        bool ready = cli_ReadLine(_cli_buf, CLI_BUFF_LEN);
        if(ready)
        {
            cli_WriteLine("Got %s", _cli_buf);
        }
        _Sleep(100);
    } while (alive);
#endif

    // Get arg -> script filename.
    if(argc != 2)
    {
        cli_WriteLine("Bad cmd line. Use nebulua <file.lua>.");
        exit(1);
    }

    ///// Init internal stuff. /////
    _l = luaL_newstate();
    // diag_EvalStack(_l, 0);

    // Load std libraries.
    luaL_openlibs(_l);

    // Load host funcs into lua space. This table gets pushed on the stack and into globals.
    luainterop_Load(_l);

    // Pop the table off the stack as it interferes with calling the module functions.
    lua_pop(_l, 1);

    // Stopwatch.
    stopwatch_Init();
    _last_msec = stopwatch_TotalElapsedMsec();

    // Tempo timer and interrupt.
    ftimer_Init(_MidiClockHandler, 1); // 1 msec resolution.
    luainteropwork_SetTempo(_l, 60);

    stat = devmgr_Init((DWORD_PTR)_MidiInHandler);
    _EvalStatus(stat, "Failed to init device manager");

    ///// Load and run the application. /////

    // Load the script file. Pushes the compiled chunk as a Lua function on top of the stack - or pushes an error message.
    stat = luaL_loadfile(_l, argv[1]);
    _EvalStatus(stat, "luaL_loadfile() failed fn:%s", argv[1]);

    // Run the script to init everything.
    stat = lua_pcall(_l, 0, LUA_MULTRET, 0);
    _EvalStatus(stat, "lua_pcall() failed fn:%s", argv[1]);

    // Script setup.
    stat = luainterop_Setup(_l);
    _EvalStatus(stat, "luainterop_Setup() failed");

    // Good to go now.
    LeaveCriticalSection(&_critical_section);

    // Run blocks until done.
    stat = _Run();
    _EvalStatus(stat, "Run failed");

    ///// Finished. Clean up and go home. /////
    DeleteCriticalSection(&_critical_section);
    cli_WriteLine("Goodbye - come back soon!");
    ftimer_Run(0);
    ftimer_Destroy();
    cli_Destroy();
    devmgr_Destroy();
    lua_close(_l);

    return NEB_OK;
}


//---------------------------------------------------//
int _Run(void)
{
    int stat = NEB_OK;

    // Loop forever doing cli requests.
    do
    {
        bool ready = cli_ReadLine(_cli_buf, CLI_BUFF_LEN);
        if(ready)
        {
            bool valid = false;

            // Chop up the command line into args.
            #define MAX_NUM_ARGS 20
            char* argv[MAX_NUM_ARGS];
            int argc = 0;

            // Make writable copy and tokenize it.
            char cp[strlen(_cli_buf) + 1];
            strcpy(cp, _cli_buf);
            char* tok = strtok(cp, " ");
            while(tok != NULL && argc < MAX_NUM_ARGS)
            {
                argv[argc++] = tok;
                tok = strtok(NULL, " ");
            }

            // Process the command and its options.
            if (argc > 0)
            {
                // Find and execute the command.
                cli_command_t* pcmd = _commands;
                while (pcmd->handler != NULL)
                {
                    if (strcmp(pcmd->desc.short_name, argv[0]) || strcmp(pcmd->desc.long_name, argv[0]))
                    {
                        valid = true;
                        // Execute the command.
                        int stat = (*pcmd->handler)(&(pcmd->desc), argc, argv);
                        // cmd handles this _EvalStatus(stat, "CLI function failed: %s", pcmd->desc.name);
                        break;
                    }
                }

                if (!valid)
                {
                    cli_WriteLine("invalid command");
                }
            }
            // else assume fat fingers.
        }
        _Sleep(100);
    } while (_app_running);

    return stat;
}


//-------------------------------------------------------//
void _MidiClockHandler(double msec)
{
    // Process events -- this is in an interrupt handler!
    // msec is since last time.

    int stat = NEB_OK;

    EnterCriticalSection(&_critical_section);

    int bar = BAR(_position);
    int beat = BEAT(_position);
    int subbeat = SUBBEAT(_position);

    stat = luainterop_Step(_l, bar, beat, subbeat);
    
    _position++;

    LeaveCriticalSection(&_critical_section);
}


//--------------------------------------------------------//
void _MidiInHandler(HMIDIIN hMidiIn, UINT wMsg, DWORD_PTR dwInstance, DWORD_PTR dwParam1, DWORD_PTR dwParam2)
{
    // Input midi event -- this is in an interrupt handler.
    // http://msdn.microsoft.com/en-us/library/dd798458%28VS.85%29.aspx

    int stat = NEB_OK;

    EnterCriticalSection(&_critical_section); 

    switch(wMsg)
    {
        case MIM_DATA:
            int raw_msg = dwParam1;  // packed MIDI message
            int timestamp = dwParam2;  // milliseconds since MidiInStart
            BYTE bstatus = raw_msg & 0xFF;  // MIDI status byte
            BYTE bdata1 = (raw_msg >> 8) & 0xFF;  // first MIDI data byte
            BYTE bdata2 = (raw_msg >> 16) & 0xFF;  // second MIDI data byte
            int channel = -1;
            int hndchan = 0;
            midi_event_t evt;

            if ((bstatus & 0xF0) == 0xF0)
            {
                // System events.
                evt = (midi_event_t)bstatus;
            }
            else
            {
                // Channel events.
                evt = (midi_event_t)(bstatus & 0xF0);
                channel = (bstatus & 0x0F) + 1;
            }

            // Validate midiin device and channel number as registered by user.
            midi_device_t* pdev = devmgr_GetDeviceFromMidiHandle(hMidiIn);
            hndchan = devmgr_GetChannelHandle(pdev, channel);

            if (hndchan > 0)
            {
                switch (evt)
                {
                    case MIDI_NOTE_ON:
                    case MIDI_NOTE_OFF:
                        double volume = bdata2 > 0 && evt == MIDI_NOTE_ON ? (double)bdata1 / MIDI_VAL_MAX : 0.0;
                        stat = luainterop_InputNote(_l, hndchan, bdata1, volume);
                        _EvalStatus(stat, "luainterop_InputNote() failed");
                        break;

                    case MIDI_CONTROL_CHANGE:
                        stat = luainterop_InputController(_l, hndchan, bdata1, bdata2);
                        _EvalStatus(stat, "luainterop_InputController() failed");
                        break;

                    case MIDI_PITCH_WHEEL_CHANGE:
                        // PitchWheelChangeEvent(ts, channel, data1 + (data2 << 7));
                        break;

                    // Ignore other events for now.
                    default:
                        break;
                }
                break;
            }
            // else ignore

        // Ignore other messages for now.
        default:
            break;
    }

    LeaveCriticalSection(&_critical_section);
};


//--------------------------------------------------------//
int _TempoCmd(cli_command_desc_t* pdesc, int argc, char* argv[])
{
    int stat = NEB_ERR_BAD_CLI_ARG;

    if (argc == 1) // get
    {
        cli_WriteLine("tempo: %d", _tempo);
        stat = NEB_OK;
    }
    else if (argc == 2) // set
    {
        int t;
        if(_StrToInt(argv[1], &t, 40, 240))
        {
            _tempo = t;
            luainteropwork_SetTempo(_l, _tempo);
        }
        else
        {
           cli_WriteLine("invalid tempo: %d", argv[1]);
           stat = NEB_ERR_BAD_CLI_ARG;
        }
        stat = NEB_OK;
    }

    return stat;
}

//--------------------------------------------------------//
int _RunCmd(cli_command_desc_t* pdesc, int argc, char* argv[]) // TODO3 also single space bar?
{
    int stat = NEB_ERR_BAD_CLI_ARG;

    if (argc == 1) // no args
    {
        _script_running = !_script_running;
        stat = NEB_OK;
    }

    return stat;
}


//--------------------------------------------------------//
int _ExitCmd(cli_command_desc_t* pdesc, int argc, char* argv[])
{
    int stat = NEB_ERR_BAD_CLI_ARG;

    if (argc == 1) // no args
    {
        _app_running = false;
        stat = NEB_OK;
    }

    return stat;
}


//--------------------------------------------------------//
int _MonCmd(cli_command_desc_t* pdesc, int argc, char* argv[])
{
    int stat = NEB_ERR_BAD_CLI_ARG;

    if (argc == 2) // set
    {
        if (strcmp(argv[1], "in") == 0)
        {
            _mon_input = !_mon_input;
            stat = NEB_OK;
        }
        else if (strcmp(argv[1], "out") == 0)
        {
            _mon_output = !_mon_output;
            stat = NEB_OK;
        }
        else if (strcmp(argv[1], "off") == 0)
        {
            _mon_input = false;
            _mon_output = false;
            stat = NEB_OK;
        }
        else
        {
           cli_WriteLine("invalid option: %s", argv[1]);
        }
    }

    return stat;
}


//--------------------------------------------------------//
int _KillCmd(cli_command_desc_t* pdesc, int argc, char* argv[])
{
    int stat = NEB_ERR_BAD_CLI_ARG;

    if (argc == 1) // no args
    {
        // TODO1 send kill to all midi outputs
        stat = NEB_OK;
    }

    return stat;
}


//--------------------------------------------------------//
int _PositionCmd(cli_command_desc_t* pdesc, int argc, char* argv[])
{
    // p|position (where) - 
    int stat = NEB_ERR_BAD_CLI_ARG;

    if (argc == 1) // get
    {
        int bar = BAR(_position);
        int beat = BEAT(_position);
        int subbeat = SUBBEAT(_position);
        cli_WriteLine("position: %d.%d.%d", bar, beat, subbeat);
    }
    else if (argc == 2) // set TODO1 can be 1.2.3 or 1.2 or 1
    {
        
    }

    return stat;
}


//--------------------------------------------------------//
int _ReloadCmd(cli_command_desc_t* pdesc, int argc, char* argv[])// TODO2 do something
{
    // l|re/load - script
    int stat = NEB_ERR_BAD_CLI_ARG;

    if (argc == 1) // no args
    {

    }

    return stat;
}


//--------------------------------------------------------//
int _Usage(cli_command_desc_t* pdesc, int argc, char* argv[])
{
    int stat = NEB_OK;

    cli_command_t* pcmditer = _commands;
    cli_command_desc_t* pdesciter = &(pcmditer->desc);
    while (_commands->handler != (void*)NULL)
    {
        cli_WriteLine("%s|%s: %s", pdesciter->long_name, pdesciter->short_name, pdesciter->desc);
        if (strlen(pdesciter->args) > 0)
        {
            // Multiline args.
            char* tok = strtok(pdesciter->args, "\0");
            while(tok != NULL)
            {
                cli_WriteLine("    %s", tok);
                tok = strtok(NULL, "\0");
            }
        }
        pcmditer++;
    }

    return stat;
}


//--------------------------------------------------------//
void _Sleep(int msec)
{
    struct timespec ts;
    ts.tv_sec = msec / 1000;
    ts.tv_nsec = (msec % 1000) * 1000000;
    nanosleep(&ts, NULL);
}


//--------------------------------------------------------//
bool _EvalStatus(int stat, const char* format, ...)
{
    static char buff[100];
    bool has_error = false;
    if (stat >= LUA_ERRRUN)
    {
        has_error = true;

        va_list args;
        va_start(args, format);
        vsnprintf(buff, sizeof(buff) - 1, format, args);
        va_end(args);

        const char* sstat = common_StatusToString(stat);

        if (stat <= LUA_ERRFILE) // internal lua error
        {
            // Get error message on stack if provided.
            if (lua_gettop(_l) > 0)
            {
                luaL_error(_l, "Status:%s info:%s errmsg:%s", sstat, buff, lua_tostring(_l, -1));
            }
            else
            {
                luaL_error(_l, "Status:%s info:%s", sstat, buff);
            }
        }
        else // assume nebulua error
        {
            luaL_error(_l, "Status:%s info:%s", sstat, buff);
        }
    }

    return has_error;
}


//--------------------------------------------------------//
bool _StrToDouble(const char* str, double* val, double min, double max)
{
    bool valid = true;
    char* p;

    errno = 0;
    *val = strtof(str, &p);
    if (errno == ERANGE)
    {
        // Mag is too large.
        valid = false;
    }
    else if (p == str)
    {
        // Bad string.
        valid = false;
    }
    else if (*val < min || *val > max)
    {
        // Out of range.
        valid = false;
    }

    return valid;
}


//--------------------------------------------------------//
bool _StrToInt(const char* str, int* val, int min, int max)
{
    bool valid = true;
    char* p;

    errno = 0;
    *val = strtol(str, &p, 10);
    if (errno == ERANGE)
    {
        // Mag is too large.
        valid = false;
    }
    else if (p == str)
    {
        // Bad string.
        valid = false;
    }
    else if (*val < min || *val > max)
    {
        // Out of range.
        valid = false;
    }

    return valid;
}

//--------------------------------------------------------//
// Map commands to handlers.
static const cli_command_t _commands[] =
{
    { _Usage,        { "help",       "?",   "explain it all",                         "" } },
    { _ExitCmd,      { "exit",       "x",   "exit the application",                   "" } },
    { _RunCmd,       { "run",        "r",   "toggle running the script",              "" } },
    { _TempoCmd,     { "tempo",      "t",   "get or set the tempo",                   "(bpm): tempo 40-240" } },
    { _MonCmd,       { "monitor",    "m",   "toggle monitor midi traffic",            "(in|out|off): action" } },
    { _KillCmd,      { "kill",       "k",   "stop all midi",                          "" } },
    { _PositionCmd,  { "position",   "p",   "set position to where or tell current",  "(where): beat" } },
    { _ReloadCmd,    { "reload",     "l",   "re/load current script",                 "" } },
    { NULL,          { NULL, NULL, NULL, NULL } }
};
