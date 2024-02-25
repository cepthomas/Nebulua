// system
#include <time.h>
// lua
#include "lua.h"
#include "lualib.h"
#include "lauxlib.h"
// cbot
#include "cbot.h"
#include "logger.h"
// lbot
#include "luautils.h"
#include "ftimer.h"
// application
#include "nebcommon.h"
#include "cli.h"
#include "devmgr.h"
#include "luainterop.h"


//----------------------- Definitions -----------------------//


// TODO1 Script lua_State access syncronization. 
// CRITICAL_SECTION Not working, try:
//https://learn.microsoft.com/en-us/windows/win32/sync/event-objects
//https://learn.microsoft.com/en-us/windows/win32/sync/mutex-objects
// https://learn.microsoft.com/en-us/windows/win32/sync/critical-section-objects
// Performance? https://stackoverflow.com/questions/853316/is-critical-section-always-faster
//static CRITICAL_SECTION _critical_section;
#define ENTER_CRITICAL_SECTION ;//EnterCriticalSection(&_critical_section)
#define EXIT_CRITICAL_SECTION  ;//LeaveCriticalSection(&_critical_section)


#define CLI_BUFF_LEN    128
#define MAX_CLI_ARGS     10
#define MAX_CLI_ARG_LEN  32


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
    // TODO2 Optional single char for immediate execution (no CR required). Can be ^(ctrl) or ~(alt) in conjunction with short_name.
    const char immediate_key;
    // Free text for command description.
    const char* info;
    // Free text for args description.
    const char* args;
    // The runtime handler.
    const cli_command_handler_t handler;
} cli_command_t;


//----------------------- Vars - app ---------------------------//

// The main Lua thread.
static lua_State* _l;

// Point this stream where you like.
static FILE* _fperr = NULL;

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

// Commands forward declaration. VS compiler needs size assigned, but not gcc.
static cli_command_t _commands[20];

// CLI prompt.
static char* _prompt = "$";

// CLI buffer.
static char _cli_buff[CLI_BUFF_LEN];

//static char _last_log[500];
static char _last_error[500];

//----------------------- Vars - script ------------------------//

// Current tempo in bpm.
static int _tempo = 100;

// Current sub.
static int _position = 0;

// Length of composition in ticks.
static int _length = 0;


//---------------------- Functions TODO2 restore static------------------------//

// Process user input.
int _DoCli(void);

// Clock tick corresponding to bpm. !!From Interrupt!!
void _MidiClockHandler(double msec);

// Handle incoming messages. !!From Interrupt!!
void _MidiInHandler(HMIDIIN hMidiIn, UINT wMsg, DWORD_PTR dwInstance, DWORD_PTR dwParam1, DWORD_PTR dwParam2);

/// Top level error handler for nebulua status.
static bool _EvalStatus(int stat, int line, const char* format, ...);


//----------------------------------------------------//

/// Main entry for the application.
int exec_Main(const char* script_fn)
{
    int stat = NEB_OK;

    bool ok = false;
    int iret = 0;
    double dret = 0;
    bool bret = false;
    const char* sret = NULL;
    int exit_code = 0;

    #define EXEC_FAIL() fprintf(_fperr, "ERROR %s\n", _last_error); exit_code = 1; goto init_done;

    // Init streams.
    _fperr = stdout;
    cli_open();

    // Init logger.
    FILE* fplog = fopen("_log.txt", "a");
    stat = logger_Init(fplog);
    ok = _EvalStatus(stat, __LINE__, "Failed to init logger");
    if (!ok) EXEC_FAIL();
    logger_SetFilters(LVL_DEBUG);
    LOG_INFO("Logger is alive");

    // Initialize the critical section. It is used to synchronize access to the lua context _l.
    // ok = InitializeCriticalSectionAndSpinCount(&_critical_section, 0x00000400);

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
    ok = _EvalStatus(stat, __LINE__, "Failed to init ftimer.");
    if (!ok) EXEC_FAIL();
    luainteropwork_SetTempo(60);

    stat = devmgr_Init(_MidiInHandler);
    ok = _EvalStatus(stat, __LINE__, "Failed to init device manager.");
    if (!ok) EXEC_FAIL();

    ///// Load and run the application. /////

    // Load the script file. Pushes the compiled chunk as a Lua function on top of the stack or pushes an error message.
    stat = luaL_loadfile(_l, script_fn);
    ok = _EvalStatus(stat, __LINE__, "Load script file failed [%s].", script_fn);
    if (!ok) EXEC_FAIL();

    // Run the script to init everything.
    stat = lua_pcall(_l, 0, LUA_MULTRET, 0);
    ok = _EvalStatus(stat, __LINE__, "Execute script failed [%s].", script_fn);
    if (!ok) EXEC_FAIL();

    // Script setup.
    stat = luainterop_Setup(_l, &iret);
    ok = _EvalStatus(stat, __LINE__, "Script setup() failed [%s].", script_fn);
    if (!ok) EXEC_FAIL();

    ///// Good to go now. /////
    EXIT_CRITICAL_SECTION;

    // Loop forever doing cli requests.
    while (_app_running)
    {
        stat = _DoCli();
    }

    ok = _EvalStatus(stat, __LINE__, "Run failed [%s].", script_fn);
    if (!ok) EXEC_FAIL();


init_done:
    if (exit_code != 0)
    {
        LOG_ERROR("FATAL init error: %s", _last_error);
    }

    ///// Finished. Clean up and go home. /////
    // DeleteCriticalSection(&_critical_section);
    ftimer_Run(0);
    ftimer_Destroy();
    devmgr_Destroy();

    if (_fperr != stdout) { fclose(_fperr); }
    cli_close();
    fclose(fplog);
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
                    // ok = _EvalStatus(stat, __LINE__, "handler failed: %s", pcmd->desc.long_name);
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
    // msec is since last time.
    _last_msec = msec;
    int iret;

    // Lock access to lua context.
    ENTER_CRITICAL_SECTION;

    int stat = luainterop_Step(_l, _position, &iret);
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
    bool ok = true;
    int iret;

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
            // Lock access to lua context.
            ENTER_CRITICAL_SECTION;

            switch (evt)
            {
            case MIDI_NOTE_ON:
            case MIDI_NOTE_OFF:
                // Translate velocity to volume.
                double volume = bdata2 > 0 && evt == MIDI_NOTE_ON ? (double)bdata1 / MIDI_VAL_MAX : 0.0;
                stat = luainterop_InputNote(_l, chan_hnd, bdata1, volume, &iret);
                ok = _EvalStatus(stat, __LINE__, "luainterop_InputNote() failed");
                break;

            case MIDI_CONTROL_CHANGE:
                stat = luainterop_InputController(_l, chan_hnd, bdata1, bdata2, &iret);
                ok = _EvalStatus(stat, __LINE__, "luainterop_InputController() failed");
                break;

            case MIDI_PITCH_WHEEL_CHANGE:
                // PitchWheelChangeEvent(ts, channel, data1 + (data2 << 7));
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


//---------------------------------------------------------------//
//--------------------- all the commands ------------------------//
//---------------------------------------------------------------//

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
            stat = NEB_ERR_BAD_CLI_ARG;
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
        // TODO2 send kill to all midi outputs. Need all output devices from devmgr. Or ask script to do it?
        // luainteropwork_SendController(chan_hnd, AllNotesOff=123, 0);
        stat = NEB_OK;
    }

    return stat;
}


//--------------------------------------------------------//
int _PositionCmd(const cli_command_t* pcmd, int argc, char* argv[])
{
    int stat = NEB_ERR_BAD_CLI_ARG;

    if (argc == 1) // get
    {
        cli_printf("%s\n", nebcommon_FormatBarTime(_position));
        stat = NEB_OK;
    }
    else if (argc == 2)
    {
        int position = nebcommon_ParseBarTime(argv[1]);
        if (position < 0)
        {
            cli_printf("invalid position: %s\n", argv[1]);
        }
        else
        {
            _position = position >= _length ? _length - 1 : position;
            cli_printf("%s\n", nebcommon_FormatBarTime(_position)); // echo
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
            strncpy(cp, cmditer->args, sizeof(cp));
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


//--------------------------------------------------------//
bool _EvalStatus(int stat, int line, const char* format, ...)
{
    bool ok = true;
    strcpy(_last_error, "No error");

    if (stat >= LUA_ERRRUN)
    {
        ok = false;

        // Format info string.
        char info[100];
        va_list args;
        va_start(args, format);
        vsnprintf(info, sizeof(info) - 1, format, args);
        va_end(args);

        const char* sstat = NULL;
        switch (stat)
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
            case NEB_ERR_MIDI_RX:           sstat = "NEB_ERR_MIDI_RX"; break;
            case NEB_ERR_MIDI_TX:           sstat = "NEB_ERR_MIDI_TX"; break;
            // default
            default:                        sstat = "UNKNOWN_ERROR"; LOG_DEBUG("Unknwon ret code:%d", stat); break;
        }

        // Additional error message.
        const char* errmsg = NULL;
        if (stat <= LUA_ERRFILE && _l != NULL && lua_gettop(_l) > 0) // internal lua error - get error message on stack if provided.
        {
            errmsg = lua_tostring(_l, -1);
        }
        // else app error

        // Log the error info.
        if (errmsg == NULL)
        {
            snprintf(_last_error, sizeof(_last_error), "%s %s", sstat, info);
        }
        else
        {
            snprintf(_last_error, sizeof(_last_error), "%s %s\n%s", sstat, info, errmsg);
        }
    }

    return ok;
}
