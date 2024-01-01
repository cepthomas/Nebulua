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


#define TEST


//----------------------- Vars -----------------------------//

// The main Lua thread - C code incl interrupts for mm/fast timer and midi events.
static lua_State* p_lmain;

// The execution status.
static bool p_running = false;

// Last tick time.
static double p_last_msec = 0;
// static unsigned long p_last_usec;

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
static int p_ProcessCommand(const char* sin);

// Top level error handler. Logs and calls luaL_error() which doesn't return.
static bool p_EvalStatus(int stat, const char* format, ...);


//----------------------------------------------------//
// Main entry for the application. Process args and start system.
// @param argc How many args.
// @param argv The args.
// @return Standard exit code.
int main(int argc, char* argv[])
{
    int stat = NEB_OK;

    FILE* fp = fopen(".\\nebulua_log.txt", "a");
    logger_Init(fp);
    // logger_SetFilters(LVL_DEBUG, CAT_ALL);

    // logger_Init(".\\nebulua_log.txt");
    logger_SetFilters(LVL_DEBUG);

    cli_Open();

#ifdef TEST
    // Some test code.
    p_running = true;
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
    } while (p_running);
#endif


    // Get args - just filename for now.
    if(argc != 2)
    {
        //printf("1 Bad cmd line. Use nebulua <filename>");
        cli_WriteLine("Bad cmd line. Use nebulua <file.lua>.");
        exit(1);
    }

    // Init internal stuff.
    p_lmain = luaL_newstate();
    // diag_EvalStack(p_lmain, 0);

    // Load std libraries.
    luaL_openlibs(p_lmain);
    // diag_EvalStack(p_lmain, 0);

    // Load host funcs into lua space. This table gets pushed on the stack and into globals.
    luainterop_Load(p_lmain);
    // diag_EvalStack(p_lmain, 1);

    // Pop the table off the stack as it interferes with calling the module functions.
    lua_pop(p_lmain, 1);
    // diag_EvalStack(p_lmain, 0);

    // Stopwatch.
    stopwatch_Init();
    p_last_msec = stopwatch_TotalElapsedMsec();

    // Tempo timer and interrupt - 1 msec resolution.
    ftimer_Init(p_MidiClockHandler, 1);
    luainteropwork_SetTempo(p_lmain, 60);  //no ret

    stat = devmgr_Init((DWORD_PTR)p_MidiInHandler);
    p_EvalStatus(stat, "Failed to init device manager");

    ///// Run the application. Blocks forever.
    stat = p_Run(argv[1]);
    p_EvalStatus(stat, "Run failed");

    ///// Finished /////
    cli_WriteLine("Goodbye - come back soon!");
    // Clean up and go home.
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

    // Load/run the script/file.
    stat = luaL_loadfile(p_lmain, fn);
    // or: stat = luaL_dofile(p_lmain, fn); // lua_load pushes the compiled chunk as a Lua function on top of the stack. Otherwise, it pushes an error message.
    p_EvalStatus(stat, "luaL_loadfile() failed fn:%s", fn);
    diag_EvalStack(p_lmain, 0);

    // Script setup.
    stat = luainterop_Setup(p_lmain);
    p_EvalStatus(stat, "luainterop_Setup failed");
    diag_EvalStack(p_lmain, 0);

    // Loop forever doing cli requests.
    p_running = true;
    do
    {
        bool ready = cli_ReadLine(p_cli_buf, CLI_BUFF_LEN);
        if(ready)
        {
            stat = p_ProcessCommand(p_cli_buf);
            if (stat != NEB_OK)
            {
                // Function took care of messages.
            }
        }
        p_Sleep(100);
    } while (p_running);

    return stat;
}


//---------------------------------------------------//
int p_ProcessCommand(const char* sin)
{
    int stat = NEB_OK;

    // Chop up the command line into something suitable for getopt(). sin is assumed to be destructible.
    #define MAX_NUM_OPTS 8
    char* opts[MAX_NUM_OPTS];
    int optcnt = 0;
    char* tok = strtok(sin, " ");
    while(tok != NULL && optcnt < MAX_NUM_OPTS)
    {
        opts[optcnt++] = tok;
        tok = strtok(NULL, " ");
    }

    // If opterr is set to nonzero, then getopt prints an error message to the standard error stream if it
    // encounters an unknown option character or an option with a missing required argument. This is the default behavior.
    // If you set this variable to zero, getopt does not print any messages, but it still returns the character ? to indicate an error.
    opterr = 0;

    bool done = false;
    bool valid = true;
    while (!done && valid)
    {
        int c = getopt(optcnt, opts, "xt:");
        switch (c)
        {
            case -1:
                done = true;
                break;

            case 'x':
                p_running = false;
                break;

            case 't':
                int bpm = -1;
                if(common_StrToInt(optarg, &bpm)) // optarg is set by getopt to point at the value of the option argument, for those options that accept arguments.
                {
                    luainteropwork_SetTempo(p_lmain, bpm);
                }
                else
                {
                    cli_WriteLine("Option -%c requires an integer argument.", c);
                    valid = false;
                }
                break;

            case '?':
                // Error in cmd line.
                // When getopt encounters an unknown option character or an option with a missing required argument, it stores that option
                // character in int optopt. You can use this for providing your own diagnostic messages.
                if (optopt == 't')
                {
                    cli_WriteLine("Option -%c requires an argument.", optopt);
                }
                else if(isprint(optopt))
                {
                    cli_WriteLine("Unknown option `-%c'.", optopt);
                }
                else
                {
                    cli_WriteLine("Unknown option character `\\x%x'.", optopt);
                }

                valid = false;
                break;

            default:
                abort();
        }
    }

    // Get non-opt args.
    // optind is set by getopt to the index of the next element of the argv array to be processed. Once getopt has
    // found all of the option arguments, you can use this variable to determine where the remaining non-option arguments begin.
    // The initial value of this variable is 1.
    if(valid)
    {
        for (int i = optind; i < optcnt; i++)
        {
            cli_WriteLine("Non-option argument: %s.", opts[i]);
        }
    }

    if(!valid)
    {
        // Usage.
        cli_WriteLine("x: exit");
        cli_WriteLine("t bpm: set tempo");
        stat = NEB_ERR_BAD_CLI_ARG;
    }

    return stat;
}


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
            int raw_msg = dwParam1; // packed MIDI message
            int timestamp = dwParam2; // milliseconds since MidiInStart
            BYTE bstatus = raw_msg & 0xFF;// MIDI status byte
            BYTE bdata1 = (raw_msg >> 8) & 0xFF;// first MIDI data byte
            BYTE bdata2 = (raw_msg >> 16) & 0xFF;// second MIDI data byte
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
