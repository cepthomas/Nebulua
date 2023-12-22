#include <windows.h>
#include <stdio.h>
#include <string.h>
#include <time.h>
#include <stdlib.h>
#include <unistd.h>
#include "lua.h"
#include "lualib.h"
#include "lauxlib.h"
#include "luainterop.h"
#include "luainteropwork.h"
#include "common.h"
#include "logger.h"
#include "ftimer.h"
#include "stopwatch.h"


//----------------------------------------------------//
// The main Lua thread - C code incl interrupts (mm/fast timer, midi events)
static lua_State* p_lmain;

// The Lua thread where the script is running.
static lua_State* p_lscript;

// The script execution status.
static bool p_script_running = false;

// Processing loop status.
static bool p_loop_running;

// Last tick time.
// static unsigned long p_last_usec;
static double p_last_msec = 0;


static MIDI_DEVICE _devices[MAX_MIDI_DEVS];



//----------------------------------------------------//
//
void p_InitMidiDevices(void);

// Tick corresponding to bpm. Interrupt!
void p_MidiClockHandler(double msec);

// Handle incoming messages. Interrupt!
void p_MidiInHandler(HMIDIIN hMidiIn, UINT wMsg, DWORD_PTR dwInstance, DWORD_PTR dwParam1, DWORD_PTR dwParam2);

// Blocking sleep.
void p_Sleep(int msec);

//
int p_Init(void);

//
int p_Run(const char* fn);

void p_Fatal() { } // TODO2



//----------------------------------------------------//
// Main entry for the application. Process args and start system.
// @param argc How many args.
// @param argv The args.
// @return Standard exit code.
int main(int argc, char* argv[])
{
    int ret = 0;

    logger_Init(".\\nebulua_log.txt");
    logger_SetFilters(LVL_DEBUG);

    ret = p_Init();

    // Go.
    char* serr = NULL;
    char* sfn = NULL;

    if(argc == 2)
    {
        sfn = argv[1];
    }
    else
    {
        serr = "Invalid args";
    }

    if (serr == NULL && sfn != NULL)
    {
        if(p_Init() != 0)
        {
            serr = "Init failed";
        }
    }

    if (serr == NULL && sfn != NULL)
    {
        // Run the script file. Blocks forever. TODO1 need elegant way to stop - part of user interaction or ctrl-C?
        if(p_Run(argv[1]) != 0)
        {
            serr = "Run failed";
        }
    }

    // How did we do?
    if (serr != NULL)
    {
        LOG_ERROR(serr);
        printf("Epic fail!! %s\n", serr);
    }

    // Clean up and go home.
    // EVAL_STACK(p_lmain, 0);
    // EVAL_STACK(p_lscript, 0);
    ftimer_Run(0);
    ftimer_Destroy();
    lua_close(p_lscript);
    lua_close(p_lmain);

    return serr == NULL ? 0 : 1;
}


//-------------------------------------------------------//
void p_InitMidiDevices(void)
{
    MMRESULT res = 0;

    memset(_devices, 0, sizeof(_devices));
    int d = 0;

    {
        int num_in = midiInGetNumDevs();
        for (int i = 0; i < num_in; i++, d++)
        {
            if (d >= MAX_MIDI_DEVS)
            {
                p_Fatal();
            }

            // http://msdn.microsoft.com/en-us/library/dd798453%28VS.85%29.aspx
            MIDIINCAPS caps_in;
            res = midiInGetDevCaps(i, &caps_in, sizeof(caps_in));
            if (res > 0)
            {
                p_Fatal();
            }

            HMIDIIN hmidi_in;
            // http://msdn.microsoft.com/en-us/library/dd798458%28VS.85%29.aspx
            res = midiInOpen(&hmidi_in, i, (DWORD_PTR)p_MidiInHandler, (DWORD_PTR)0, CALLBACK_FUNCTION);
            if (res > 0 || hmidi_in < 0)
            {
                p_Fatal();
            }

            // Save the device info.
            _devices[d].hnd_in = hmidi_in;
            _devices[d].dev_index = i;
            strncpy(_devices[d].dev_name, caps_in.szPname, MAXPNAMELEN);

            res = midiInStart(hmidi_in);
        }
    }

    {
        int num_out = midiOutGetNumDevs();
        for (int i = 0; i < num_out; i++, d++)
        {
            if (d >= MAX_MIDI_DEVS)
            {
                p_Fatal();
            }

            // http://msdn.microsoft.com/en-us/library/dd798469%28VS.85%29.aspx
            MIDIOUTCAPS caps_out;
            res = midiOutGetDevCaps(i, &caps_out, sizeof(caps_out));
            if (res > 0)
            {
                p_Fatal();
            }

            HMIDIOUT hmidi_out;
            // http://msdn.microsoft.com/en-us/library/dd798476%28VS.85%29.aspx
            res = midiOutOpen(&hmidi_out, i, 0, 0, 0);
            if (res > 0 || hmidi_out < 0)
            {
                p_Fatal();
            }

            // Save the device info.
            _devices[d].hnd_out = hmidi_out;
            _devices[d].dev_index = i;
            strncpy(_devices[d].dev_name, caps_out.szPname, MAXPNAMELEN);
        }
    }
}


//-------------------------------------------------------//
void p_MidiClockHandler(double msec)
{
    // TODO1 yield over to script thread and call its step function.
    //  See lua.c for a way to treat C signals, which you may adapt to your interrupts.

}


//--------------------------------------------------------//
void p_MidiInHandler(HMIDIIN hMidiIn, UINT wMsg, DWORD_PTR dwInstance, DWORD_PTR dwParam1, DWORD_PTR dwParam2) // TODO2 put in utility?
{
    // Input midi event. Note this is in an interrupt handler!
    // hMidiIn - Handle to the MIDI input device.
    // wMsg - MIDI input message.
    // dwInstance - Instance data supplied with the midiInOpen function.
    // dwParam1 - Message parameter.
    // dwParam2 - Message parameter.

    switch(wMsg)
    {
        case MIM_DATA:
            // parameter 1 is packed MIDI message
            // parameter 2 is milliseconds since MidiInStart
            // https://learn.microsoft.com/en-us/windows/win32/api/mmeapi/ns-mmeapi-midievent

            int ts = dwParam2;
            int raw_msg = dwParam1;
            int b = raw_msg & 0xFF;
            int data1 = (raw_msg >> 8) & 0xFF;
            int data2 = (raw_msg >> 16) & 0xFF;
            midi_event_t midi_evt;
            int channel = 1;

            if ((b & 0xF0) == 0xF0)
            {
                // Both bytes are used for command code in this case.
                midi_evt = (midi_event_t)b;
            }
            else
            {
                midi_evt = (midi_event_t)(b & 0xF0);
                channel = (b & 0x0F) + 1;
            }

            switch (midi_evt)
            {
                case NoteOn:
                case NoteOff:
                    if (data2 > 0 && midi_evt == NoteOn)
                    {
                        // me = new NoteOnEvent(ts, channel, data1, data2, 0);
                        // log.WriteInfo(String.Format("Time {0} Message 0x{1:X8} Event {2}", e.Timestamp, e.RawMessage, e.MidiEvent));
                    }
                    else
                    {
                        // me = new NoteEvent(ts, channel, midi_evt, data1, data2);
                    }
                    break;
                case ControlChange:
                    // me = new ControlChangeEvent(ts, channel, (MidiController)data1, data2);
                    break;
                case PitchWheelChange:
                    // me = new PitchWheelChangeEvent(ts, channel, data1 + (data2 << 7));
                    break;

                // Ignore other events for now.
                // case KeyAfterTouch:
                // case PatchChange:
                // case ChannelAfterTouch:
                // case TimingClock:
                // case StartSequence:
                // case ContinueSequence:
                // case StopSequence:
                // case AutoSensing:
                // case MetaEvent:
                // case Sysex:
                default:
                    // throw new FormatException(String.Format("Unsupported MIDI Command Code for Raw Message {0}", midi_evt));
                    break;
            }
            break;

        case MIM_ERROR:
            // parameter 1 is invalid MIDI message
            //TODO1 log.WriteError(String.Format("Time {0} Message 0x{1:X8} Event {2}", e.Timestamp, e.RawMessage, e.MidiEvent));
            break;

        // Others not implemented:
        case MIM_OPEN:
        case MIM_CLOSE:
        case MIM_LONGDATA:
        case MIM_LONGERROR:
        case MIM_MOREDATA:// MidiInterop.MidiInMessage.MoreData:
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


//----------------------------------------------------//
int p_Init(void)
{
    int ret = 0;

    // Init internal stuff.
    p_loop_running = false;
    p_lmain = luaL_newstate();
    EVAL_STACK(p_lmain, 0);

    // Load std libraries.
    luaL_openlibs(p_lmain);
    EVAL_STACK(p_lmain, 0);

    // Stopwatch.
    ret = stopwatch_Init();
    p_last_msec = stopwatch_TotalElapsedMsec();

    // Tempo timer and interrupt.
    ret = ftimer_Init(p_MidiClockHandler, 10);
    ret = ftimer_Run(17); // tempo from ???

    // Midi event interrupt.

    return ret;
}


//---------------------------------------------------//
int p_Run(const char* fn)
{
    int stat = 0;
    EVAL_STACK(p_lmain, 0);


    // Set up a second Lua thread so we can background execute the script.
    p_lscript = lua_newthread(p_lmain);
    EVAL_STACK(p_lscript, 0);
    lua_pop(p_lmain, 1); // from lua_newthread()
    EVAL_STACK(p_lmain, 0);

    // Open lua std libs.
    luaL_openlibs(p_lscript);

    // Load host funcs into lua space. This table gets pushed on the stack and into globals.
    luainterop_Load(p_lscript);
    EVAL_STACK(p_lscript, 1);

    // Pop the table off the stack as it interferes with calling the module functions.
    lua_pop(p_lscript, 1);
    EVAL_STACK(p_lscript, 0);

    // Load/run the script/file.
    stat = luaL_dofile(p_lscript, fn); // lua_load pushes the compiled chunk as a Lua function on top of the stack. Otherwise, it pushes an error message.
    // stat = luaL_loadfile(p_lscript, fn);
    CHK_LUA_ERROR(p_lscript, stat);
    EVAL_STACK(p_lscript, 0);

    // // Priming run of the loaded Lua script to create the script's global variables
    // stat = lua_pcall(p_lscript, 0, 0, 0);
    // CHK_LUA_ERROR(p_lscript, stat);
    // EVAL_STACK(p_lscript, 0);

    // Init the script. This also starts blocking execution.
    p_script_running = true;
    p_loop_running = true;

    int gtype = lua_getglobal(p_lscript, "do_it");
    EVAL_STACK(p_lscript, 1);

    ///// First do some yelding. /////
    do
    {
        stat = lua_resume(p_lscript, p_lmain, 0, 0);

        switch(stat)
        {
            case LUA_YIELD:
                // LOG_DEBUG("===LUA_YIELD.");
                break;

            case LUA_OK:
                // Script complete now.
                break;

            default:
                // Unexpected error.
                CHK_LUA_ERROR(p_lscript, stat);
                // CHK_LUA_ERROR(p_lscript, stat, "p_Run() error");
                break;
        }

        p_Sleep(200);
    }
    while (stat == LUA_YIELD);

    /// Then loop forever doing cli requests. /////
    do
    {
        // stat = board_CliReadLine(p_cli_buf, CLI_BUFF_LEN);
        // if(stat == 0 && strlen(p_cli_buf) > 0)
        // {
        //     // LOG_DEBUG("|||got:%s", p_cli_buf);
        //     stat = p_ProcessCommand_ex(p_cli_buf);
        // }
        p_Sleep(100);
    } while (p_script_running);

    ///// Script complete now. /////
    LOG_INFO("Finished script");

    return stat;
}


int p_ProcessCommand_ex(const char* sin)
{
    int stat = 0;

    // What are the command line options. First one should be the actual command.
    char* opts[88];
    memset(opts, 0x00, sizeof(opts));
    int oind = 0;

    // Make writable copy and tokenize it.
    char cp[strlen(sin) + 1];
    strcpy(cp, sin);
    char* token = strtok(cp, " ");

    while(token != NULL && oind < 22)
    {
        opts[oind++] = token;
        token = strtok(NULL, " ");
    }

    bool valid = false; // default

    // commands do things like:

    // p_script_running = false;
    //
    // interop_Calc(p_lscript, x, y, &res);
    // board_CliWriteLine("%g + %g = %g", x, y, res);

    return stat;
}
