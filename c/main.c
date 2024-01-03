// system
#include <windows.h>
#include <stdio.h>
#include <string.h>
#include <time.h>
#include <stdlib.h>
#include <unistd.h>
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
#include "common.h"
#include "cli.h"
#include "devmgr.h"
#include "luainterop.h"
#include "luainteropwork.h"

//----------------------- Definitions -----------------------//

#define TEST

//----------------------- Types -----------------------------//

typedef int (*cli_command_handler_t)(int argc, char* argv[]);//, CliCommandData* pData);

// typedef struct cli_command_arg
// {
//     const char* name;               ///< Arg name cmd|alias|alias
//     const char* description;        ///< Brief arg description
//     // const char type;        ///< S|I|F/D|N
// } cli_command_arg_t;

typedef struct cli_command
{
    const char* name;               ///< Command name
    const char* description;        ///< Brief command description
    cli_command_handler_t handler;         ///< Command function pointer
    // int16_t minLevel;               ///< Minimum user level to see this command
    // int16_t minArgs;                ///< Minimum number of args
    // int16_t maxArgs;                ///< Maximum number of args
    const char* args;            ///< Brief args description string
    // const cli_command_arg arg_list[];
} cli_command_t;


//----------------------- Vars -----------------------------//

// The main Lua thread.
static lua_State* p_lmain;

// The script execution state.
static bool p_script_running = false;

// The app execution state.
static bool p_app_running = true;

// Last tick time.
static double p_last_msec = 0;

// CLI contents.
static char p_cli_buf[CLI_BUFF_LEN];


//---------------------- Private functions ------------------------//

// Start forever loop.
static int p_Run(const char* fn);

// Tick corresponding to bpm. !!Interrupt!!
static void p_MidiClockHandler(double msec);

// Handle incoming messages. !!Interrupt!!
static void p_MidiInHandler(HMIDIIN hMidiIn, UINT wMsg, DWORD_PTR dwInstance, DWORD_PTR dwParam1, DWORD_PTR dwParam2);

// Blocking sleep.
static void p_Sleep(int msec);

// Do what the cli says.
static bool p_ProcessCommand(const char* sin);

// Top level error handler. Logs and calls luaL_error() which doesn't return.
static bool p_EvalStatus(int stat, const char* format, ...);


///////////////////////////////////////////////////////////////////////////
////////////////////////// cli TODO1 ////////////////////////////////////////////
///////////////////////////////////////////////////////////////////////////

//??????
// print("Usage: gen_interop.lua (-d) (-t) [-ch|-cs] [your_spec.lua] [your_outpath]")
// print("  -ch generate c and h files")
// print("  -cs generate c# file")
// print("  -d enable debugger if available")
// print("  -t use debugger terminal color")


// t (123) - set tempo
// s|spacebar? - toggle script run
// x - exit
// monin (on|off) - monitor input
// monout (on|off) - monitor output
// k|kill - stop all midi
// r|rewind - set to 0
// c|compile - reload

///// original with getopt:
// case 'x':
//     p_app_running = false;
//     break;
// case 't':
//     int bpm = -1;
//     if(common_StrToInt(optarg, &bpm))
//     {
//         luainteropwork_SetTempo(p_lmain, bpm);
//     }
//     else
//     {
//         cli_WriteLine("Option -%c requires an integer argument.", c);
//         valid = false;
//     }
//     break;
// case '?':
//     // Error in cmd line.
//     if (optopt == 't')
//     {
//         cli_WriteLine("Option -%c missing argument.", optopt);
//     }
//     else if(isprint(optopt))
//     {
//         cli_WriteLine("Unknown option `-%c'.", optopt);
//     }
//     else
//     {
//         cli_WriteLine("Unknown option `\\x%x'.", optopt);
//     }
//     valid = false;
//     break;


void func_SetLevel(int l) { }
int func_GetLevel(void) { return 99; }
int user_command(int argc, char* argv[])
{
    if (argc >= 1) // set
    {
        if(argc == 2)
        {
            int level;
            if (common_StrToInt(argv[1], &level))
            {
                func_SetLevel(level);
            }
            else
            {
               cli_WriteLine("invalid value %d", level);
            }
        }

    }
    else // get
    {
        cli_WriteLine("Log output level = %d", func_GetLevel());
    }

    return 0;
}


//
static cli_command_t p_commands[] =
{
    {
        .name        = "tempo",
        .description = "Set a key indicator 0-2 on hid 0-1",
        .handler    = user_command,
        .args     = "(hid) (row) (col) (indicator) (off|on|slow|med|fast)",
        // .argList     = 
        // {
        //     .name        = "bpm",
        //     // .opts        = "arg1",
        //     .description = "Aaaaaaarghhhh 40-240",
        // },
    },
    {
        .name        = "version",
        .description = "Show firmware and CLI version",
        .handler    = user_command,
        .args     =  "[set|ramp|pulse] [PWM num] [val] (0-1000)",
    },

    // List terminator
    { NULL,NULL, NULL, NULL}
};

///////////////////////////////////////////////////////////////////////////
///////////////////////////////////////////////////////////////////////////
///////////////////////////////////////////////////////////////////////////


//----------------------------------------------------//

/// Main entry for the application. Process args and start system.
/// @param argc How many args.
/// @param argv The args.
/// @return Standard exit code.
int main(int argc, char* argv[])
{
    int stat = NEB_OK;

    FILE* fp = fopen(".\\nebulua_log.txt", "a");
    logger_Init(fp);
    logger_SetFilters(LVL_DEBUG);

    cli_Open('s');

#ifdef TEST
    // Some test code.
    bool alive = true;
    cli_WriteLine("Tell 1");
    cli_WriteLine("Tell 2 %d", fp);
    do
    {
        bool ready = cli_ReadLine(p_cli_buf, CLI_BUFF_LEN);
        if(ready)
        {
            cli_WriteLine("Got %s", p_cli_buf);
        }
        p_Sleep(100);
    } while (alive);
#endif


    // Get arg -> script filename.
    if(argc != 2)
    {
        cli_WriteLine("Bad cmd line. Use nebulua <file.lua>.");
        exit(1);
    }

    // Init internal stuff.
    p_lmain = luaL_newstate();
    // diag_EvalStack(p_lmain, 0);

    // Load std libraries.
    luaL_openlibs(p_lmain);

    // Load host funcs into lua space. This table gets pushed on the stack and into globals.
    luainterop_Load(p_lmain);

    // Pop the table off the stack as it interferes with calling the module functions.
    lua_pop(p_lmain, 1);

    // Stopwatch.
    stopwatch_Init();
    p_last_msec = stopwatch_TotalElapsedMsec();

    // Tempo timer and interrupt.
    ftimer_Init(p_MidiClockHandler, 1); // 1 msec resolution.
    luainteropwork_SetTempo(p_lmain, 60);

    stat = devmgr_Init((DWORD_PTR)p_MidiInHandler);
    p_EvalStatus(stat, "Failed to init device manager");

    // Run the application - blocks until done.
    stat = p_Run(argv[1]);
    p_EvalStatus(stat, "Run failed");

    // Finished. Clean up and go home.
    cli_WriteLine("Goodbye - come back soon!");
    ftimer_Run(0);
    ftimer_Destroy();
    cli_Destroy();
    devmgr_Destroy();
    lua_close(p_lmain);

    return NEB_OK;
}


//---------------------------------------------------//
int p_Run(const char* fn)
{
    int stat = NEB_OK;

    // Load the script file. Pushes the compiled chunk as a Lua function on top of the stack - or pushes an error message.
    stat = luaL_loadfile(p_lmain, fn);
    p_EvalStatus(stat, "luaL_loadfile() failed fn:%s", fn);

    // Run the script to init everything.
    stat = lua_pcall(p_lmain, 0, LUA_MULTRET, 0);
    p_EvalStatus(stat, "lua_pcall() failed fn:%s", fn);

    // Script setup.
    stat = luainterop_Setup(p_lmain);
    p_EvalStatus(stat, "setup() failed");

    // Loop forever doing cli requests.
    do
    {
        bool ready = cli_ReadLine(p_cli_buf, CLI_BUFF_LEN);
        if(ready)
        {
            bool ok = p_ProcessCommand(p_cli_buf);
            if (!ok)
            {
                // cli_ProcessCommand() took care of error handling.
            }
        }
        p_Sleep(100);
    } while (p_app_running);

    return stat;
}





//---------------------------------------------------//
bool p_ProcessCommand(const char* sin)
{
    bool done = false;
    bool valid = false;

    // Chop up the command line into something suitable for getopt().
    #define MAX_NUM_ARGS 20
    char* argv[MAX_NUM_ARGS];
    int argc = 0;

    // Make writable copy and tokenize it.
    char cp[strlen(sin) + 1];
    strcpy(cp, sin);
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
        cli_command_t* pcmd = p_commands;
        while (p_commands->name != NULL)
        {
            if (strcmp(p_commands->name, argv[0]))
            {
                valid = true;
                int stat = (*p_commands->handler)(argc, argv);
                p_EvalStatus(stat, "CLI function failed: %s", p_commands->name);
                break;
            }
        }

        if (!valid)
        {
            cli_WriteLine("Bad command: %s.", argv[0]);
//>>>            p_Usage();
        }
    }
    // else ignore?

    // if(!valid)
    // {
    //     // Usage.
    //     cli_WriteLine("x: exit");
    //     cli_WriteLine("t bpm: set tempo");
    // }

    return valid;
}



// //---------------------------------------------------//
// int p_ProcessCommand(const char* sin)
// {
//     int stat = NEB_OK;
//     // TODO2 make this generic. Some spec with list of entries:
//     //   - cmd string (aliases?)
//     //   - descr string
//     //   - optlist:
//     //      - opt string
//     //      - descr string
//     //      - type: S|I|F/D|N
//     //      - handlerDef  typedef void (*handlerDef)(?);
//     // ? C:\Dev\AL\harvester\xib-firmware\src\cli\cli_command_list.c
//     // ? C:\Dev\AL\caldwell\gen3procfirmware\src-application\cli\cli_command_list.c
// // typedef struct CliCommandInfo
// // {
// //     const char* name;               ///< Command name (all lower case)
// //     CliCommandPtr pCommand;         ///< Command function pointer
// //     int16_t minLevel;               ///< Minimum user level to see this command
// //     int16_t minArgs;                ///< Minimum number of args
// //     int16_t maxArgs;                ///< Maximum number of args
// //     const char* argList;            ///< Brief args description string
// //     const char* description;        ///< Brief command description
// // } CliCommandInfo;
// // typedef bool (*CliCommandPtr)(uint16_t argc, char* argv[], CliCommandData* pData);
// // bool cliLogLevelCmd(uint16_t argc, char* argv[], CliCommandData* pData)
// // {
// //     int16_t cliPort = pData->cliPort;
// //     if (argc >= 1)
// //     {
// //         if(argc == 2)
// //         {
// //             uint16_t level = toUInt16(argv[1], 0, 10);
// //             if (conversionError())
// //             {
// //                 serialWriteLine("invalid value", (uint16_t)cliPort);
// //             }
// //             else
// //             {
// //                 debugLogSetLevel(level);
// //             }
// //         }
// //         snprintf(pData->printBuf, CLI_PRINT_BUF_LENGTH, "Log output level = %d", debugLogGetLevel());
// //         serialWriteLine(pData->printBuf, (uint16_t)cliPort);
// //     }
// //     return true;
// // }




//-------------------------------------------------------//
void p_MidiClockHandler(double msec)
{
    // TODO1 process events
    //  See lua.c for a way to treat C signals, which you may adapt to your interrupts.

}


//--------------------------------------------------------//
void p_MidiInHandler(HMIDIIN hMidiIn, UINT wMsg, DWORD_PTR dwInstance, DWORD_PTR dwParam1, DWORD_PTR dwParam2)
{
    // Input midi event -- this is in an interrupt handler! It is inited in devmgr_Init().
    // http://msdn.microsoft.com/en-us/library/dd798458%28VS.85%29.aspx

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
                        luainterop_InputNote(p_lmain, hndchan, bdata1, volume);                            
                        break;

                    case MIDI_CONTROL_CHANGE:
                        luainterop_InputController(p_lmain, hndchan, bdata1, bdata2);
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
};


//--------------------------------------------------------//
// Blocking sleep.
void p_Sleep(int msec)
{
    struct timespec ts;
    ts.tv_sec = msec / 1000;
    ts.tv_nsec = (msec % 1000) * 1000000;
    nanosleep(&ts, NULL);
}


//--------------------------------------------------------//
bool p_EvalStatus(int stat, const char* format, ...)
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
            if (lua_gettop(p_lmain) > 0)
            {
                luaL_error(p_lmain, "Status:%s info:%s errmsg:%s", sstat, buff, lua_tostring(p_lmain, -1));
            }
            else
            {
                luaL_error(p_lmain, "Status:%s info:%s", sstat, buff);
            }
        }
        else // assume nebulua error
        {
            luaL_error(p_lmain, "Status:%s info:%s", sstat, buff);
        }
    }

    return has_error;
}
