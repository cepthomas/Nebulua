// system
#include <windows.h>
#include <stdio.h>
#include <time.h>
// lua
#include "lua.h"
#include "lualib.h"
#include "lauxlib.h"
// cbot
#include "cbot.h"
#include "logger.h"
#include "mathutils.h"
// lbot
#include "luautils.h"
#include "ftimer.h"
// application
#include "nebcommon.h"
#include "cli.h"
#include "devmgr.h"
#include "scriptinfo.h"
#include "luainterop.h"


//----------------------- Definitions -----------------------//

// Caps.
#define CLI_BUFF_LEN    128
#define ERR_BUFF_LEN    500
#define MAX_CLI_ARGS     10
#define MAX_CLI_ARG_LEN  32

// Midi defs.
#define ALL_NOTES_OFF     123

// Script lua_State access syncronization. 
HANDLE ghMutex; 
#define ENTER_CRITICAL_SECTION WaitForSingleObject(ghMutex, INFINITE)
#define EXIT_CRITICAL_SECTION ReleaseMutex(ghMutex)


//----------------------- Types -----------------------//

// Forward declaration for handler.
typedef struct cli_command cli_command_t;

// Cli command handler.
typedef int (* cli_command_handler_t)(const cli_command_t* cmd, int argc, char* argv[]);

// Cli command descriptor.
typedef struct cli_command
{
    // If you like to type.
    const char* long_name;
    // If you don't.
    const char short_name;
    // FUTURE Optional single char for immediate execution (no CR required). Can be ^(ctrl) or ~(alt) in conjunction with short_name.
    const char immediate_key;
    // Free text for command description.
    const char* info;
    // Free text for args description.
    const char* args;
    // The runtime handler.
    const cli_command_handler_t handler;
} cli_command_t;


//----------------------- Vars --------------------------------//

// The main Lua thread. FUTURE Global so unit test can see it.
lua_State* _l;

// Point this stream where you like.
static FILE* _fp_err = NULL;

// The script execution state.
static bool _script_running = false;

// The app execution state.
static bool _app_running = true;

// Last tick time.
static double _last_msec = 0.0;

// Current tempo in bpm.
static int _tempo = 100;

// Where are we in steps.
static int _current_tick = 0;

// Length of composition in ticks.
static int _length = 0;

// Keep going. TODO1 cli implementation for all these. set: start, end, section, reset/all.
static bool _do_loop = false;

// Loop start tick. -1 means start of composition.
static int _loop_start = -1;

// Loop end tick. -1 means end of composition.
static int _loop_end = -1;

// Monitor midi input. TODO1 implement all these monitors. Output to cli stream and/or log and/or ???
static bool _mon_input = false;

// Monitor midi output.
static bool _mon_output = false;

// Commands forward declaration. VS compiler needs size assigned, but not gcc.
static cli_command_t _commands[20];

// CLI prompt.
static char* _prompt = "$";

// CLI buffer.
static char _cli_buff[CLI_BUFF_LEN];


//---------------------- Functions ------------------------//

// Technically these should all be static but external visibility makes unit testing possible.

// Process user input.
int _DoCli(void);

// Clock tick corresponding to bpm. !!From Interrupt!!
void _MidiClockHandler(double msec);

// Handle incoming messages. !!From Interrupt!!
void _MidiInHandler(HMIDIIN hMidiIn, UINT wMsg, DWORD_PTR dwInstance, DWORD_PTR dwParam1, DWORD_PTR dwParam2);

/// General kill everything.
static int _Kill();


//----------------------- Main Functions ---------------------//

//---------------------------------------------------//
int exec_Main(const char* script_fn)
{
    int stat = NEB_OK;

    const char* e = NULL;
    int iret = 0;
    int exit_code = 0;

    #define EXEC_FAIL(code, msg) { LOG_ERROR(msg); fprintf(_fp_err, "ERROR %s\n", msg); exit_code = code; goto init_done; }

    // Init streams.
    _fp_err = stdout;
    cli_open();

    // Init logger.
    FILE* fp_log = fopen("_log.txt", "a");
    stat = logger_Init(fp_log);
    e = nebcommon_EvalStatus(_l, stat, "Failed to init logger");
    if (e != NULL) EXEC_FAIL(10, e);
    logger_SetFilters(LVL_DEBUG);
    LOG_INFO("Logger is alive");

    // Create a mutex with no initial owner.
    ghMutex = CreateMutex(NULL, FALSE, NULL);
    if (ghMutex == NULL) { EXEC_FAIL(11, "CreateMutex() failed."); }

    // Lock access to lua context during init.
    ENTER_CRITICAL_SECTION;

    ///// Init internal stuff. /////
    _l = luaL_newstate();

    // Load std libraries.
    luaL_openlibs(_l);

    // Load host funcs into lua space. This table gets pushed on the stack and into globals.
    luainterop_Load(_l);

    // Pop the table off the stack as it interferes with calling the module functions.
    lua_pop(_l, 1);

    // Tempo timer and interrupt.
    stat = ftimer_Init(_MidiClockHandler, 1); // 1 msec resolution.
    e = nebcommon_EvalStatus(_l, stat, "Failed to init ftimer.");
    if (e != NULL) EXEC_FAIL(12, e);

    // Device manager.
    stat = devmgr_Init(_MidiInHandler);
    e = nebcommon_EvalStatus(_l, stat, "Failed to init device manager.");
    if (e != NULL) EXEC_FAIL(13, e);

    ///// Load and run the application. /////

    // Load/compile the script file. Pushes the compiled chunk as a Lua function on top of the stack or pushes an error message.
    stat = luaL_loadfile(_l, script_fn);
    e = nebcommon_EvalStatus(_l, stat, "Load script file failed [%s].", script_fn);
    if (e != NULL) EXEC_FAIL(14, e);

    // Run the script to initialize it.
    stat = lua_pcall(_l, 0, LUA_MULTRET, 0);
    e = nebcommon_EvalStatus(_l, stat, "Execute script failed [%s].", script_fn);
    if (e != NULL) EXEC_FAIL(15, e);

    // Script nebulua setup.
    stat = luainterop_Setup(_l, &iret);
    e = nebcommon_EvalStatus(_l, stat, "Script setup() failed [%s].", script_fn);
    if (e != NULL) EXEC_FAIL(16, e);

    // Get script info.
    scriptinfo_Init(_l);
    

    ///// Good to go now. /////
    EXIT_CRITICAL_SECTION;

    // Loop forever doing cli requests.
    while (_app_running)
    {
        stat = _DoCli();
        e = nebcommon_EvalStatus(_l, stat, "_DoCli() failed [%s].", script_fn);
        if (e != NULL) EXEC_FAIL(17, e);
    }

/////
init_done:

    ///// Finished. Clean up resources and go home. /////
    if (ghMutex != NULL) { CloseHandle(ghMutex); }
    ftimer_Destroy();
    devmgr_Destroy();
    if (_fp_err != stdout) { fclose(_fp_err); }
    cli_close();
    fclose(fp_log);
    lua_close(_l);

    return exit_code;
}


//---------------------------------------------------//
int _DoCli(void)
{
    int stat = NEB_OK;

    // Prompt.
    cli_printf(_prompt);

    char* res = cli_gets(_cli_buff, CLI_BUFF_LEN);

    if (res != NULL)
    {
        // Process the line. Chop up the raw command line into args.
        int argc = 0;
        char argv[MAX_CLI_ARGS][MAX_CLI_ARG_LEN]; // The actual args.
        memset(argv, 0, sizeof(argv));
        char* cmd_argv[MAX_CLI_ARGS]; // For easy digestion by commands.
        memset(cmd_argv, 0, sizeof(cmd_argv));
        char* tok = strtok(_cli_buff, " ");
        while (tok != NULL && argc < MAX_CLI_ARGS)
        {
            strncpy(argv[argc], tok, MAX_CLI_ARG_LEN - 1);
            cmd_argv[argc] = tok;
            argc++;
            tok = strtok(NULL, " ");
        }

        // Process the command and its options.
        bool valid = false;
        if (argc > 0)
        {
            // Find and execute the command.
            const cli_command_t* pcmd = _commands;
            while (pcmd->handler != NULL)
            {
                if ((strlen(argv[0]) == 1 && pcmd->short_name == argv[0][0]) || strcmp(pcmd->long_name, argv[0]) == 0)
                {
                    // Execute the command. They handle any errors internally.
                    valid = true;
                    // Lock access to lua context.
                    ENTER_CRITICAL_SECTION;
                    stat = pcmd->handler(pcmd, argc, cmd_argv);
                    // ok = _EvalStatus(stat, "handler failed: %s", pcmd->desc.long_name);
                    EXIT_CRITICAL_SECTION;
                    break;
                }

                pcmd++;
            }

            if (!valid)
            {
                cli_printf("Invalid command\n");
            }
        }
    }
    else
    {
        // Assume finished.
        _app_running = false;
    }

    return stat;
}


//-------------------------------------------------------//
void _MidiClockHandler(double msec)
{
    // Process events -- this is in an interrupt handler!

    // Update time.
    double elapsed = msec - _last_msec;
    _last_msec = msec;
    int stat;
    int ret = 0;

    if (_script_running)
    {
        // Do script.
        // FUTURE Process solo/mute like nebulator.

        // Lock access to lua context.
        ENTER_CRITICAL_SECTION;
        // Read stopwatch.
        stat = luainterop_Step(_l, _current_tick, &ret);
        // Read stopwatch and diff/stats.
        EXIT_CRITICAL_SECTION;

        const char* e = nebcommon_EvalStatus(_l, stat, "Step() failed");

        if (e != NULL)
        {
            // Stop everything.
            ftimer_Run(0);
            _script_running = false;
            _current_tick = 0;
            cli_printf("Stopped: %s\n", e);
        }
        else
        {
            // Bump time and check.
            int start = _loop_start == -1 ? 0 : _loop_start;
            int end = _loop_end == -1 ? _length : _loop_end;
            if (++_current_tick >= end) // done
            {
                // Keep going? else stop/rewind.
                _script_running = _do_loop;

                if (_do_loop)
                {
                    // Keep going.
                    _current_tick = start;
                }
                else
                {
                    // Stop and rewind.
                    _current_tick = start;

                    // just in case
                    _Kill();
                }
            }
        }
    }
}


//--------------------------------------------------------//
void _MidiInHandler(HMIDIIN hMidiIn, UINT wMsg, DWORD_PTR dwInstance, DWORD_PTR dwParam1, DWORD_PTR dwParam2)
{
    // Input midi event -- this is in an interrupt handler!
    // http://msdn.microsoft.com/en-us/library/dd798458%28VS.85%29.aspx.

    int stat = NEB_OK;
    int ret;

    switch (wMsg)
    {
    case MIM_DATA:
    {
        DWORD_PTR raw_msg = dwParam1;           // packed MIDI message
        // uint timestamp = dwParam2;           // milliseconds since MidiInStart
        byte bstatus = raw_msg & 0xFF;          // MIDI status byte
        byte bdata1 = (raw_msg >> 8) & 0xFF;    // first MIDI data byte
        byte bdata2 = (raw_msg >> 16) & 0xFF;   // second MIDI data byte
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
            const char* e = NULL;

            // Lock access to lua context.
            ENTER_CRITICAL_SECTION;

            switch (evt)
            {
            case MIDI_NOTE_ON:
            case MIDI_NOTE_OFF:
                // Translate velocity to volume.
                double volume = bdata2 > 0 && evt == MIDI_NOTE_ON ? (double)bdata1 / MIDI_VAL_MAX : 0.0;
                stat = luainterop_InputNote(_l, chan_hnd, bdata1, volume, &ret);
                e = nebcommon_EvalStatus(_l, stat, "luainterop_InputNote() failed");
                break;

            case MIDI_CONTROL_CHANGE:
                stat = luainterop_InputController(_l, chan_hnd, bdata1, bdata2, &ret);
                e = nebcommon_EvalStatus(_l, stat, "luainterop_InputController() failed");
                break;

                // Ignore other events for now.
            default:
                break;
            }
            break;

            EXIT_CRITICAL_SECTION;
        }
        // else ignore
    }
    break;

    // Ignore other messages for now.
    default:
        break;
    }
}


//--------------------- Commands -----------------------------//

//--------------------------------------------------------//
int _TempoCmd(const cli_command_t* pcmd, int argc, char* argv[])
{
    int stat = NEB_ERR_BAD_CLI_ARG;

    if (argc == 1) // get
    {
        cli_printf("%d\n", _tempo);
        stat = NEB_OK;
    }
    else if (argc == 2) // set
    {
        int t;
        if (luautils_ParseInt(argv[1], &t, 40, 240))
        {
            _tempo = t;
            luainteropwork_SetTempo(_tempo);
            stat = NEB_OK;
        }
        else
        {
            cli_printf("invalid tempo: %s\n", argv[1]);
        }
    }

    return stat;
}


//--------------------------------------------------------//
int _RunCmd(const cli_command_t* pcmd, int argc, char* argv[])
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
int _ExitCmd(const cli_command_t* pcmd, int argc, char* argv[])
{
    int stat = NEB_ERR_BAD_CLI_ARG;

    if (argc == 1) // no args
    {
        _script_running = false;
        _app_running = false;
        stat = NEB_OK;
    }

    return stat;
}


//--------------------------------------------------------//
int _MonCmd(const cli_command_t* pcmd, int argc, char* argv[])
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
            cli_printf("invalid option: %s\n", argv[1]);
        }
    }

    return stat;
}


//--------------------------------------------------------//
int _KillCmd(const cli_command_t* pcmd, int argc, char* argv[])
{
    int stat = NEB_ERR_BAD_CLI_ARG;

    if (argc == 1) // no args
    {
        stat = _Kill();
    }

    return stat;
}


//--------------------------------------------------------//
int _PositionCmd(const cli_command_t* pcmd, int argc, char* argv[])
{
    int stat = NEB_ERR_BAD_CLI_ARG;

    if (argc == 1) // get
    {
        cli_printf("%s\n", nebcommon_FormatBarTime(_current_tick));
        stat = NEB_OK;
    }
    else if (argc == 2) // set
    {
        int position = nebcommon_ParseBarTime(argv[1]);
        if (position < 0)
        {
            cli_printf("invalid position: %s\n", argv[1]);
        }
        else
        {
            // Limit range maybe.
            int start = _loop_start == -1 ? 0 : _loop_start;
            int end = _loop_end == -1 ? _length : _loop_end;
            position = mathutils_Constrain(position, start, end);

            cli_printf("%s\n", nebcommon_FormatBarTime(_current_tick)); // echo
            stat = NEB_OK;
        }
    }

    return stat;
}


//--------------------------------------------------------//
int _ReloadCmd(const cli_command_t* pcmd, int argc, char* argv[])
{
    int stat = NEB_ERR_BAD_CLI_ARG;

    if (argc == 1) // no args
    {
        // TODO2 do something to reload script =>
        // - https://stackoverflow.com/questions/2812071/what-is-a-way-to-reload-lua-scripts-during-run-time
        // - https://stackoverflow.com/questions/9369318/hot-swap-code-in-lua
        stat = NEB_OK;
    }

    return stat;
}


//--------------------------------------------------------//
int _Usage(const cli_command_t* pcmd, int argc, char* argv[])
{
    int stat = NEB_OK;

    const cli_command_t* cmditer = _commands;
    while (cmditer->handler != NULL)
    {
        //const cli_command_t* pdesc = &(cmditer->desc);
        cli_printf("%s|%c: %s\n", cmditer->long_name, cmditer->short_name, cmditer->info);
        if (strlen(cmditer->args) > 0)
        {
            // Maybe multiline args. Make writable copy and tokenize it.
            char cp[128];
            strncpy(cp, cmditer->args, sizeof(cp) - 1);
            char* tok = strtok(cp, "$");
            while (tok != NULL)
            {
                cli_printf("    %s\n", tok);
                tok = strtok(NULL, "$");
            }
        }
        cmditer++;
    }

    return stat;
}


//--------------------------------------------------------//
static cli_command_t _commands[] =
{
    { "help",       '?',   0,    "tell me everything",                     "",                          _Usage },
    { "exit",       'x',   0,    "exit the application",                   "",                          _ExitCmd },
    { "run",        'r',   ' ',  "toggle running the script",              "",                          _RunCmd },
    { "tempo",      't',   0,    "get or set the tempo",                   "(bpm): 40-240",             _TempoCmd },
    { "monitor",    'm',   '^',  "toggle monitor midi traffic",            "(in|out|off): action",      _MonCmd },
    { "kill",       'k',   '~',  "stop all midi",                          "",                          _KillCmd },
    { "position",   'p',   0,    "set position to where or tell current",  "(where): bar:beat:sub",     _PositionCmd },
    { "reload",     'l',   0,    "re/load current script",                 "",                          _ReloadCmd },
    { NULL,          0,    0,    NULL,                                     NULL,                        NULL }
};


//-------------------- Private Functions -----------------//

//--------------------------------------------------------//
int _Kill()
{
    int stat = NEB_OK;
    int iret;

    // Send kill to all midi outputs.
    midi_device_t* dev = devmgr_GetOutputDevices(NULL);
    while (dev != NULL)
    {
        for (int i = 0; i < NUM_MIDI_CHANNELS; i++)
        {
            int chan_hnd = devmgr_GetChannelHandle(dev, i);
            luainteropwork_SendController(chan_hnd, ALL_NOTES_OFF, 0);
        }
        // next
        dev = devmgr_GetOutputDevices(dev);
    }

    // Hard reset.
    _script_running = false;
    stat = luainterop_Setup(_l, &iret);
    const char* e = nebcommon_EvalStatus(_l, stat, "Script setup() failed in kill");
    if (e != NULL)
    {
        LOG_ERROR(e);
    }

    return stat;
}
