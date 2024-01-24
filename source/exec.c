// system
#include <time.h>
// lua
#include "lua.h"
#include "lualib.h"
#include "lauxlib.h"
// cbot
#include "cbot.h"
#include "logger.h"
#include "cli.h"
#include "ftimer.h"
#include "timeanalyzer.h"
// application
#include "nebcommon.h"
#include "midi.h"
#include "devmgr.h"
#include "luainterop.h"
#include "luainteropwork.h"


//----------------------- Definitions -----------------------//

// Cli command descriptor.
typedef struct cli_command_desc
{
    const char* long_name;
    const char* short_name;
    const char* desc;
    const char* args;
} cli_command_desc_t;

// Cli command handler.
typedef int (* const cli_command_handler_t)(const cli_command_desc_t* pcmd, cli_args_t* args);

// Map a cli command.
typedef struct cli_command
{
    const cli_command_handler_t handler;
    const cli_command_desc_t desc;
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

// Monitor midi input.
static bool _mon_input = false;

// Monitor midi output.
static bool _mon_output = false;

// Forward reference.
static cli_command_t _commands[];

// Script lua_State access syncronization. https://learn.microsoft.com/en-us/windows/win32/sync/critical-section-objects
static CRITICAL_SECTION _critical_section; 
#define ENTER_CRITICAL_SECTION EnterCriticalSection(&_critical_section)
#define EXIT_CRITICAL_SECTION  LeaveCriticalSection(&_critical_section)


//----------------------- Vars - script ------------------------//

// Current tempo in bpm.
static int _tempo = 100;

// Current subbeat.
static int _position = 0;

// Length of composition in subbeats.
static int _length = 0;


//---------------------- Functions ------------------------//

// Forever loop.
static int _Run(void);

// Tick corresponding to bpm. !!Interrupt!!
static void _MidiClockHandler(double msec);

// Handle incoming messages. !!Interrupt!!
static void _MidiInHandler(HMIDIIN hMidiIn, UINT wMsg, DWORD_PTR dwInstance, DWORD_PTR dwParam1, DWORD_PTR dwParam2);

// Blocking sleep.
static void _Sleep(int msec);

// Top level error handler for nebulua status. Logs and calls luaL_error() which doesn't return.
static bool _EvalStatus(int stat, const char* format, ...);


//----------------------------------------------------//

/// Main entry for the application.
int exec_Main(int argc, char* argv[])
{
    int stat = NEB_OK;
    int cbot_stat;

    FILE* fp = fopen("nebulua_log.txt", "a");
    cbot_stat = logger_Init(fp);
    _EvalStatus(cbot_stat, "Failed to init logger");
    logger_SetFilters(LVL_DEBUG, CAT_ALL);

    cbot_stat = cli_OpenStdio();
    _EvalStatus(cbot_stat, "Failed to open cli");

    // Initialize the critical section. It is used to synchronize access to lua context.
    InitializeCriticalSectionAndSpinCount(&_critical_section, 0x00000400);

    ENTER_CRITICAL_SECTION; 

    ///// Init internal stuff. /////
    _l = luaL_newstate();
    // luautils_EvalStack(_l, 0);

    // Load std libraries.
    luaL_openlibs(_l);

    // Load host funcs into lua space. This table gets pushed on the stack and into globals.
    luainterop_Load(_l);

    // Pop the table off the stack as it interferes with calling the module functions.
    lua_pop(_l, 1);

    // Diagnostic.
    cbot_stat = timeanalyzer_Init(50); // TODO2 need to measure
    _EvalStatus(cbot_stat, "Failed to init timeanalyzer");

    // Tempo timer and interrupt.
    cbot_stat = ftimer_Init(_MidiClockHandler, 1); // 1 msec resolution.
    _EvalStatus(cbot_stat, "Failed to init ftimer");
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

    ///// Good to go now. /////
    EXIT_CRITICAL_SECTION;

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

    return 0;
}


//---------------------------------------------------//
int _Run(void)
{
    int stat = NEB_OK;
    int cbot_stat = CBOT_ERR_NO_ERR;
    cli_args_t cli_args;

    // Loop forever doing cli requests.
    while (_app_running)
    {
        cbot_stat = cli_ReadLine(&cli_args);
        _EvalStatus(cbot_stat, "Failed to cli_ReadLine");

        switch (cbot_stat)
        {
        case CBOT_ERR_NO_ERR: // something to do
            bool valid = false;

            // Process the command and its options.
            if (cli_args.arg_count > 0)
            {
                // Find and execute the command.
                const cli_command_t* pcmd = _commands;
                while (pcmd->handler != NULL)
                {
                    if (strcmp(pcmd->desc.short_name, cli_args.arg_values[0]) || strcmp(pcmd->desc.long_name, cli_args.arg_values[0]))
                    {
                        valid = true;
                        // Lock access to lua context.
                        ENTER_CRITICAL_SECTION; 
                        // Execute the command. They handle any errors internally.
                        stat = (*pcmd->handler)(&(pcmd->desc), &cli_args);
                        _EvalStatus(stat, "handler failed: %s", pcmd->desc.long_name);
                        EXIT_CRITICAL_SECTION; 
                        break;
                    }
                }

                if (!valid)
                {
                    cli_WriteLine("invalid command");
                }
            }
            break;

        case ENODATA: // nothing to do
            break;

        default: // error
            cbot_stat = cli_WriteLine(cli_args.arg_values[0]);
            _EvalStatus(cbot_stat, "cli_WriteLine() error");
            break;
        }

        _Sleep(100);
    }

    return stat;
}


//-------------------------------------------------------//
void _MidiClockHandler(double msec)
{
    // Process events -- this is in an interrupt handler!
    // msec is since last time.
    _last_msec = msec;

    // Lock access to lua context.
    ENTER_CRITICAL_SECTION;
    int stat = luainterop_Step(_l, BAR(_position), BEAT(_position), SUBBEAT(_position));
    if (stat != NEB_OK)
    {
        // TODO2 do something non-fatal?
    }
    _position++;
    EXIT_CRITICAL_SECTION;
}


//--------------------------------------------------------//
void _MidiInHandler(HMIDIIN hMidiIn, UINT wMsg, DWORD_PTR dwInstance, DWORD_PTR dwParam1, DWORD_PTR dwParam2)
{
    // Input midi event -- this is in an interrupt handler!
    // http://msdn.microsoft.com/en-us/library/dd798458%28VS.85%29.aspx

    int stat = NEB_OK;
    switch (wMsg)
    {
        case MIM_DATA:
            {
                int raw_msg = dwParam1;                // packed MIDI message
                // int timestamp = dwParam2;              // milliseconds since MidiInStart
                byte bstatus = raw_msg & 0xFF;         // MIDI status byte
                byte bdata1 = (raw_msg >> 8) & 0xFF;   // first MIDI data byte
                byte bdata2 = (raw_msg >> 16) & 0xFF;  // second MIDI data byte
                int channel = -1;
                int chan_hnd = 0;
                midi_event_t evt;

                if ((bstatus & 0xF0) == 0xF0)
                {
                    // System events.
                    evt = (midi_event_t)bstatus;
                }
                else
                {
                    // Specific channel events.
                    evt = (midi_event_t)(bstatus & 0xF0);
                    channel = (bstatus & 0x0F) + 1;
                }

                // Validate midiin device and channel number as registered by user.
                midi_device_t* pdev = devmgr_GetDeviceFromMidiHandle(hMidiIn);
                chan_hnd = devmgr_GetChannelHandle(pdev, channel);

                if (chan_hnd > 0)
                {
                    switch (evt)
                    {
                        case MIDI_NOTE_ON:
                        case MIDI_NOTE_OFF:
                            // Lock access to lua context.
                            ENTER_CRITICAL_SECTION; 
                            double volume = bdata2 > 0 && evt == MIDI_NOTE_ON ? (double)bdata1 / MIDI_VAL_MAX : 0.0;
                            stat = luainterop_InputNote(_l, chan_hnd, bdata1, volume);
                            _EvalStatus(stat, "luainterop_InputNote() failed");
                            EXIT_CRITICAL_SECTION;
                            break;

                        case MIDI_CONTROL_CHANGE:
                            // Lock access to lua context.
                            ENTER_CRITICAL_SECTION; 
                            stat = luainterop_InputController(_l, chan_hnd, bdata1, bdata2);
                            _EvalStatus(stat, "luainterop_InputController() failed");
                            EXIT_CRITICAL_SECTION;
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
            }
            break;

        // Ignore other messages for now.
        default:
            break;
    }
};


//--------------------------------------------------------//
int _TempoCmd(const cli_command_desc_t* pdesc, cli_args_t* args)
{
    int stat = NEB_ERR_BAD_CLI_ARG;

    if (args->arg_count == 1) // get
    {
        cli_WriteLine("tempo: %d", _tempo);
        stat = NEB_OK;
    }
    else if (args->arg_count == 2) // set
    {
        int t;
        if (nebcommon_ParseInt(args->arg_values[1], &t, 40, 240))
        {
            _tempo = t;
            luainteropwork_SetTempo(_l, _tempo);
        }
        else
        {
           cli_WriteLine("invalid tempo: %d", args->arg_values[1]);
           stat = NEB_ERR_BAD_CLI_ARG;
        }
        stat = NEB_OK;
    }

    return stat;
}


//--------------------------------------------------------//
int _RunCmd(const cli_command_desc_t* pdesc, cli_args_t* args)
{
    int stat = NEB_ERR_BAD_CLI_ARG;

    if (args->arg_count == 1) // no args
    {
        _script_running = !_script_running;
        stat = NEB_OK;
    }

    return stat;
}


//--------------------------------------------------------//
int _ExitCmd(const cli_command_desc_t* pdesc, cli_args_t* args)
{
    int stat = NEB_ERR_BAD_CLI_ARG;

    if (args->arg_count == 1) // no args
    {
        _app_running = false;
        stat = NEB_OK;
    }

    return stat;
}


//--------------------------------------------------------//
int _MonCmd(const cli_command_desc_t* pdesc, cli_args_t* args)
{
    int stat = NEB_ERR_BAD_CLI_ARG;

    if (args->arg_count == 2) // set
    {
        if (strcmp(args->arg_values[1], "in") == 0)
        {
            _mon_input = !_mon_input;
            stat = NEB_OK;
        }
        else if (strcmp(args->arg_values[1], "out") == 0)
        {
            _mon_output = !_mon_output;
            stat = NEB_OK;
        }
        else if (strcmp(args->arg_values[1], "off") == 0)
        {
            _mon_input = false;
            _mon_output = false;
            stat = NEB_OK;
        }
        else
        {
           cli_WriteLine("invalid option: %s", args->arg_values[1]);
        }
    }

    return stat;
}


//--------------------------------------------------------//
int _KillCmd(const cli_command_desc_t* pdesc, cli_args_t* args)
{
    int stat = NEB_ERR_BAD_CLI_ARG;

    if (args->arg_count == 1) // no args
    {
        // TODO2 send kill to all midi outputs. Need all output devices from devmgr. Or ask script to do it?
        // luainteropwork_SendController(_l, chan_hnd, AllNotesOff=123, 0);
        stat = NEB_OK;
    }

    return stat;
}


//--------------------------------------------------------//
int _PositionCmd(const cli_command_desc_t* pdesc, cli_args_t* args)
{
    int stat = NEB_ERR_BAD_CLI_ARG;

    if (args->arg_count == 1) // get
    {
        cli_WriteLine(nebcommon_FormatBarTime(_position));
        stat = NEB_OK;
    }
    else if (args->arg_count == 2)
    {
        int position = nebcommon_ParseBarTime(args->arg_values[1]);
        if (position < 0)
        {
           cli_WriteLine("invalid position: %s", args->arg_values[1]);
        }
        else
        {
            _position = position >= _length ? _length : position;
            cli_WriteLine(nebcommon_FormatBarTime(_position));
        }
    }

    return stat;
}


//--------------------------------------------------------//
int _ReloadCmd(const cli_command_desc_t* pdesc, cli_args_t* args)
{
    int stat = NEB_ERR_BAD_CLI_ARG;

    if (args->arg_count == 1) // no args
    {
        // TODO2 do something to reload script =>
        // - https://stackoverflow.com/questions/2812071/what-is-a-way-to-reload-lua-scripts-during-run-time
        // - https://stackoverflow.com/questions/9369318/hot-swap-code-in-lua
    }

    return stat;
}


//--------------------------------------------------------//
int _Usage(const cli_command_desc_t* pdesc, cli_args_t* args)
{
    int stat = NEB_OK;

    const cli_command_t* pcmditer = _commands;
    const cli_command_desc_t* pdesciter = &(pcmditer->desc);
    while (_commands->handler != (void*)NULL)
    {
        cli_WriteLine("%s|%s: %s", pdesciter->long_name, pdesciter->short_name, pdesciter->desc);
        if (strlen(pdesciter->args) > 0)
        {
            // Maybe multiline args. Make writable copy and tokenize it.
            char cp[strlen(pdesciter->args) + 1];
            strcpy(cp, pdesciter->args);
            char* tok = strtok(cp, "$");
            while(tok != NULL)
            {
                cli_WriteLine("    %s", tok);
                tok = strtok(NULL, "$");
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
        vsnprintf(buff, sizeof(buff)-1, format, args);
        va_end(args);

        const char* sstat = NULL;
        char err_buff[16];
        switch(stat)
        {
            // generic
            case 0:                         sstat = "NO_ERR"; break;
            // lua
            case LUA_YIELD:                 sstat = "LUA_YIELD"; break;
            case LUA_ERRRUN:                sstat = "LUA_ERRRUN"; break;
            case LUA_ERRSYNTAX:             sstat = "LUA_ERRSYNTAX"; break; // syntax error during pre-compilation
            case LUA_ERRMEM:                sstat = "LUA_ERRMEM"; break; // memory allocation error
            case LUA_ERRERR:                sstat = "LUA_ERRERR"; break; // error while running the error handler function
            case LUA_ERRFILE:               sstat = "LUA_ERRFILE"; break; // couldn't open the given file
            // cbot
            case CBOT_ERR_INVALID_ARG:      sstat = "CBOT_ERR_INVALID_ARG"; break;
            case CBOT_ERR_ARG_NULL:         sstat = "CBOT_ERR_ARG_NULL"; break;
            case CBOT_ERR_NO_DATA:          sstat = "CBOT_ERR_NO_DATA"; break;
            case CBOT_ERR_INVALID_INDEX:    sstat = "CBOT_ERR_INVALID_INDX"; break;
            // app
            case NEB_ERR_INTERNAL:          sstat = "NEB_ERR_INTERNAL"; break;
            case NEB_ERR_BAD_CLI_ARG:       sstat = "NEB_ERR_BAD_CLI_ARG"; break;
            case NEB_ERR_BAD_LUA_ARG:       sstat = "NEB_ERR_BAD_LUA_ARG"; break;
            case NEB_ERR_BAD_MIDI_CFG:      sstat = "NEB_ERR_BAD_MIDI_CFG"; break;
            case NEB_ERR_SYNTAX:            sstat = "NEB_ERR_SYNTAX"; break;
            case NEB_ERR_MIDI:              sstat = "NEB_ERR_MIDI"; break;
            default:                        snprintf(err_buff, sizeof(err_buff)-1, "ERR_%d", stat); break;
        }

        sstat = (sstat == NULL) ? err_buff : sstat;

        if (stat <= LUA_ERRFILE) // internal lua error - get error message on stack if provided.
        {
            if (lua_gettop(_l) > 0)
            {
                luaL_error(_l, "Status:%s info:%s errmsg:%s", sstat, buff, lua_tostring(_l, -1));
            }
            else
            {
                luaL_error(_l, "Status:%s info:%s", sstat, buff);
            }
        }
        else // cbot or nebulua error
        {
            luaL_error(_l, "Status:%s info:%s", sstat, buff);
        }

        //  maybe? const char* strerrorname_np(int errnum), const char* strerrordesc_np(int errnum);
    }

    return has_error;
}


//--------------------------------------------------------//
// Map commands to handlers.
static cli_command_t _commands[] =
{
    { _Usage,        { "help",       "?",   "explain it all",                         "" } },
    { _ExitCmd,      { "exit",       "x",   "exit the application",                   "" } },
    { _RunCmd,       { "run",        "r",   "toggle running the script",              "" } },
    { _TempoCmd,     { "tempo",      "t",   "get or set the tempo",                   "(bpm): tempo 40-240$[color]: blue" } },
    { _MonCmd,       { "monitor",    "m",   "toggle monitor midi traffic",            "(in|out|off): action" } },
    { _KillCmd,      { "kill",       "k",   "stop all midi",                          "" } },
    { _PositionCmd,  { "position",   "p",   "set position to where or tell current",  "(where): beat" } },
    { _ReloadCmd,    { "reload",     "l",   "re/load current script",                 "" } },
    { NULL,          { NULL, NULL, NULL, NULL } }
};
