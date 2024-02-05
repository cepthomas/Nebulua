// system
#include <time.h>
// lua
#include "lua.h"
#include "lualib.h"
#include "lauxlib.h"
// cbot
#include "cbot.h"
#include "logger.h"
// #include "cli.h"
#include "ftimer.h"
// application
#include "nebcommon.h"
#include "midi.h"
#include "devmgr.h"
#include "luainterop.h"
#include "luainteropwork.h"


//----------------------- Definitions -----------------------//


// Script lua_State access syncronization. https://learn.microsoft.com/en-us/windows/win32/sync/critical-section-objects
// TODO2 Performance? https://stackoverflow.com/questions/853316/is-critical-section-always-faster
static CRITICAL_SECTION _critical_section;
#define ENTER_CRITICAL_SECTION EnterCriticalSection(&_critical_section)
#define EXIT_CRITICAL_SECTION  LeaveCriticalSection(&_critical_section)

#define CLI_BUFF_LEN    128
#define MAX_CLI_ARGS     10
#define MAX_CLI_ARG_LEN  32


//----------------------- Types -----------------------//

// Cli command descriptor.
typedef struct cli_command_desc
{
    // If you like to type.
    const char* long_name;
    // If you don't.
    const char short_name;
    // Optional single char for immediate execution (no CR required). Can be ^(ctrl) or ~(alt) in conjunction with short_name.
    const char immediate_key;
    // Free text for command description.
    const char* info;
    // Free text for args description.
    const char* args;
} cli_command_desc_t;

// Cli command handler.
typedef int (* cli_command_handler_t)(const cli_command_desc_t* pcmd, int argc, char* argv[]);

// Bind a cli command to its handler.
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

// Cli commands forward reference.
static cli_command_t _commands[];

// CLI prompt.
static char* _prompt = "$";

// CLI buffer.
static char _cli_buff[CLI_BUFF_LEN];


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

// Tick corresponding to bpm. !!From Interrupt!!
static void _MidiClockHandler(double msec);

// Handle incoming messages. !!From Interrupt!!
static void _MidiInHandler(HMIDIIN hMidiIn, UINT wMsg, DWORD_PTR dwInstance, DWORD_PTR dwParam1, DWORD_PTR dwParam2);

// Top level error handler for nebulua status. Logs and calls luaL_error() which doesn't return.
static bool _EvalStatus(int stat, const char* format, ...);

// Notify the user.
static int _CliWriteLine(const char* format, ...);


//----------------------------------------------------//

/// Main entry for the application.
int exec_Main(int argc, char* argv[])
{
    int stat = NEB_OK;
    int cbot_stat;

    FILE* fp = fopen("nebulua_log.txt", "a");
    cbot_stat = logger_Init(fp);
    _EvalStatus(cbot_stat, "Failed to init logger");
    logger_SetFilters(LVL_DEBUG);

    // cbot_stat = cli_Open(_commands);
    // _EvalStatus(cbot_stat, "Failed to open cli");

    // Initialize the critical section. It is used to synchronize access to the lua context _l.
    InitializeCriticalSectionAndSpinCount(&_critical_section, 0x00000400);

    // Lock access to lua context during init.
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

    // Tempo timer and interrupt.
    cbot_stat = ftimer_Init(_MidiClockHandler, 1); // 1 msec resolution.
    _EvalStatus(cbot_stat, "Failed to init ftimer");
    luainteropwork_SetTempo(_l, 60);

    stat = devmgr_Init(_MidiInHandler);
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
    _CliWriteLine("Goodbye - come back soon!");
    ftimer_Run(0);
    ftimer_Destroy();
    // cli_Destroy();
    devmgr_Destroy();
    lua_close(_l);

    return 0;
}


//---------------------------------------------------//
int _Run(void)
{
    int stat = NEB_OK;

    // Loop forever doing cli requests.
    while (_app_running)
    {
        // Prompt.
        fputs(_prompt, stdout);
        fflush(stdout);
        char* res = fgets(_cli_buff, CLI_BUFF_LEN, stdin);

        if (res != NULL)
        {
            // Process the line.

            // TODO1 If this is a new line (len _cli_buff == 0)
            //   test the char against the immediate options
            //   if a match, return args[0] = short
            // char c = -1;
            // bool ctrl = false;
            // bool alt = false;
            // bool shift = false;
            // bool chars_done = false;
            // if (_kbhit())
            // {
            //     c = (char)_getch();
            //     ctrl = GetKeyState(VK_CONTROL) > 0;
            //     alt = GetKeyState(VK_MENU) > 0;
            //     shift = GetKeyState(VK_SHIFT) > 0;
            //     //?? _ungetch(int _Ch);
            // }
            // Sleep(100); // pace


            // Chop up the raw command line into args.
            int argc = 0;
            char argv[MAX_CLI_ARGS][MAX_CLI_ARG_LEN]; // The actual args.
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
                    if (pcmd->desc.short_name == argv[0][0] || strcmp(pcmd->desc.long_name, argv[0]))
                    {
                        // Execute the command. They handle any errors internally.
                        valid = true;
                        // Lock access to lua context.
                        ENTER_CRITICAL_SECTION;
                        stat = (*pcmd->handler)(&(pcmd->desc), argc, cmd_argv);
                        // _EvalStatus(stat, "handler failed: %s", pcmd->desc.long_name);
                        EXIT_CRITICAL_SECTION;
                        break;
                    }
                }

                if (!valid)
                {
                    _CliWriteLine("invalid command");
                }
            }
        }
        else
        {
            // Assume finished.
            _CliWriteLine("run loop finished");
            _app_running = false;
        }
    }

    return stat;
}


// --------------------------------------------------------//
int _CliWriteLine(const char* format, ...)
{
    int stat = NEB_OK;

    static char buff[CLI_BUFF_LEN];

    va_list args;
    va_start(args, format);
    vsnprintf(buff, CLI_BUFF_LEN-1, format, args);
    va_end(args);

    printf("%s\r\n>", buff);

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
                double volume = bdata2 > 0 && evt == MIDI_NOTE_ON ? (double)bdata1 / MIDI_VAL_MAX : 0.0;
                stat = luainterop_InputNote(_l, chan_hnd, bdata1, volume);
                _EvalStatus(stat, "luainterop_InputNote() failed");
                break;

            case MIDI_CONTROL_CHANGE:
                stat = luainterop_InputController(_l, chan_hnd, bdata1, bdata2);
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


//--------------------------------------------------------//
int _TempoCmd(const cli_command_desc_t* pcmd, int argc, char* argv[])
{
    int stat = NEB_ERR_BAD_CLI_ARG;

    if (argc == 1) // get
    {
        _CliWriteLine("tempo: %d", _tempo);
        stat = NEB_OK;
    }
    else if (argc == 2) // set
    {
        int t;
        if (nebcommon_ParseInt(argv[1], &t, 40, 240))
        {
            _tempo = t;
            luainteropwork_SetTempo(_l, _tempo);
        }
        else
        {
            _CliWriteLine("invalid tempo: %d", argv[1]);
            stat = NEB_ERR_BAD_CLI_ARG;
        }
        stat = NEB_OK;
    }

    return stat;
}


//--------------------------------------------------------//
int _RunCmd(const cli_command_desc_t* pcmd, int argc, char* argv[])
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
int _ExitCmd(const cli_command_desc_t* pcmd, int argc, char* argv[])
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
int _MonCmd(const cli_command_desc_t* pcmd, int argc, char* argv[])
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
            _CliWriteLine("invalid option: %s", argv[1]);
        }
    }

    return stat;
}


//--------------------------------------------------------//
int _KillCmd(const cli_command_desc_t* pcmd, int argc, char* argv[])
{
    int stat = NEB_ERR_BAD_CLI_ARG;

    if (argc == 1) // no args
    {
        // TODO2 send kill to all midi outputs. Need all output devices from devmgr. Or ask script to do it?
        // luainteropwork_SendController(_l, chan_hnd, AllNotesOff=123, 0);
        stat = NEB_OK;
    }

    return stat;
}


//--------------------------------------------------------//
int _PositionCmd(const cli_command_desc_t* pcmd, int argc, char* argv[])
{
    int stat = NEB_ERR_BAD_CLI_ARG;

    if (argc == 1) // get
    {
        _CliWriteLine(nebcommon_FormatBarTime(_position));
        stat = NEB_OK;
    }
    else if (argc == 2)
    {
        int position = nebcommon_ParseBarTime(argv[1]);
        if (position < 0)
        {
            _CliWriteLine("invalid position: %s", argv[1]);
        }
        else
        {
            _position = position >= _length ? _length : position;
            _CliWriteLine(nebcommon_FormatBarTime(_position));
        }
    }

    return stat;
}


//--------------------------------------------------------//
int _ReloadCmd(const cli_command_desc_t* pcmd, int argc, char* argv[])
{
    int stat = NEB_ERR_BAD_CLI_ARG;

    if (argc == 1) // no args
    {
        // TODO2 do something to reload script =>
        // - https://stackoverflow.com/questions/2812071/what-is-a-way-to-reload-lua-scripts-during-run-time
        // - https://stackoverflow.com/questions/9369318/hot-swap-code-in-lua
    }

    return stat;
}


//--------------------------------------------------------//
int _Usage(const cli_command_desc_t* pcmd, int argc, char* argv[])
{
    int stat = NEB_OK;

    const cli_command_t* cmditer = _commands;
    while (_commands->handler != NULL_PTR)
    {
        const cli_command_desc_t* pdesc = &(cmditer->desc);
        _CliWriteLine("%s|%s: %s", pdesc->long_name, pdesc->short_name, pdesc->info);
        if (strlen(pdesc->args) > 0)
        {
            // Maybe multiline args. Make writable copy and tokenize it.
            char cp[128];
            strncpy(cp, pdesc->args, sizeof(cp));
            char* tok = strtok(cp, "$");
            while (tok != NULL)
            {
                _CliWriteLine("    %s", tok);
                tok = strtok(NULL, "$");
            }
        }
        cmditer++;
    }

    return stat;
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

        const char* sstat = NULL;
        char err_buff[16];
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
            case NEB_ERR_MIDI:              sstat = "NEB_ERR_MIDI"; break;
            // default
            default:                        snprintf(err_buff, sizeof(err_buff) - 1, "ERR_%d", stat); break;
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
    { _Usage,        { "help",       '?',   0,    "tell me everything",                     "" } },
    { _ExitCmd,      { "exit",       'x',   0,    "exit the application",                   "" } },
    { _RunCmd,       { "run",        'r',   ' ',  "toggle running the script",              "" } },
    { _TempoCmd,     { "tempo",      't',   0,    "get or set the tempo",                   "(bpm): 40-240" } },
    { _MonCmd,       { "monitor",    'm',   '^',  "toggle monitor midi traffic",            "(in|out|off): action" } },
    { _KillCmd,      { "kill",       'k',   '~',  "stop all midi",                          "" } },
    { _PositionCmd,  { "position",   'p',   0,    "set position to where or tell current",  "(where): bar.beat.subbeat" } },
    { _ReloadCmd,    { "reload",     'l',   0,    "re/load current script",                 "" } },
    { NULL,          { NULL,           0,   0,    NULL,                                     NULL } }
};
