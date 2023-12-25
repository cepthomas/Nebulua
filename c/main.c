#include <windows.h>
#include <stdio.h>
#include <string.h>
#include <time.h>
#include <stdlib.h>
#include <unistd.h>
#include "lua.h"
#include "lualib.h"
#include "lauxlib.h"
#include "common.h"
#include "diag.h"
#include "logger.h"
#include "ftimer.h"
#include "stopwatch.h"
#include "luainterop.h"
#include "luainteropwork.h"


//----------------------- Vars -----------------------------//

// The main Lua thread - C code incl interrupts (mm/fast timer, midi events)
static lua_State* p_lmain;

// The script execution status.
static bool p_script_running = false;

// Processing loop status.
static bool p_loop_running;

// Last tick time.
static double p_last_msec = 0;
// static unsigned long p_last_usec;

// Devices specified in the user script.
static MIDI_DEVICE _devices[MIDI_DEVICES];



//---------------------- Private functions ------------------------//

//
int p_InitMidiDevices(void);

// Tick corresponding to bpm. Interrupt!
void p_MidiClockHandler(double msec);

// Handle incoming messages. Interrupt!
void p_MidiInHandler(HMIDIIN hMidiIn, UINT wMsg, DWORD_PTR dwInstance, DWORD_PTR dwParam1, DWORD_PTR dwParam2);

// Blocking sleep.
void p_Sleep(int msec);

//
int p_InitScript(void);

//
int p_Run(const char* fn);

//
void p_Fatal() { } // TODOE => errors



//----------------------------------------------------//
// Main entry for the application. Process args and start system.
// @param argc How many args.
// @param argv The args.
// @return Standard exit code.
int main(int argc, char* argv[])
{
    int stat = NEB_OK;

    ///// Initialize /////
    logger_Init(".\\nebulua_log.txt");
    logger_SetFilters(LVL_DEBUG);

    ///// Get args /////
    char* serr = NULL;
    char* sfn = NULL;

    if(argc == 2)
    {
        sfn = argv[1];
    }
    else
    {
        serr = "Invalid args"; NEB_ERR_BAD_APP_ARG
    }


    stat = p_InitMidiDevices();
    common_EvalStatus(p_lmain, stat, "Failed to init midi");

    stat = p_InitScript();
    common_EvalStatus(p_lmain, stat, "Failed to init script");



    if (serr == NULL && sfn != NULL)
    {
        if(p_Init() != 0)
        {
            serr = "Init failed";
        }
    }

    ///// Run the application. Blocks forever. ///// TODO1 need elegant way to stop - part of user interaction or CLI/ctrl-C?
    if (serr == NULL && sfn != NULL)
    {
        if(p_Run(argv[1]) != 0)
        {
            serr = "Run failed";
        }
    }

    ///// Finished /////
    if (serr != NULL)
    {
        logger_Log(LVL_ERROR, serr);
        printf("Epic fail!! %s\n", serr);
    }

    // Clean up and go home.
    ftimer_Run(0);
    ftimer_Destroy();

    for (int i = 0; i < MIDI_DEVICES; i++)
    {
        if (_devices[i].hnd_in > 0)
        {
            midiInStop(_devices[i].hnd_in);
            midiInClose(_devices[i].hnd_in); 
        }
        else if (_devices[i].hnd_out > 0)
        {
            midiOutClose(_devices[i].hnd_out);
        }
    }

    lua_close(p_lmain);

    return serr == NULL ? 0 : 1;
}


//-------------------------------------------------------//
int p_InitMidiDevices(void)
{
    int stat = NEB_OK    ;
    MMRESULT res = 0;

    memset(_devices, 0, sizeof(_devices));
    int d = 0;

    // Inputs.
    {
        int num_in = midiInGetNumDevs();
        for (int i = 0; i < num_in; i++, d++)
        {
            if (d >= MIDI_DEVICES)
            {
                p_Fatal();
            }

            MIDIINCAPS caps_in;
            res = midiInGetDevCaps(i, &caps_in, sizeof(caps_in));
            if (res > 0)
            {
                p_Fatal();
            }

            HMIDIIN hmidi_in;
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

    // Outputs.
    {
        int num_out = midiOutGetNumDevs();
        for (int i = 0; i < num_out; i++, d++)
        {
            if (d >= MIDI_DEVICES)
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
    // TODO2 process
    //  See lua.c for a way to treat C signals, which you may adapt to your interrupts.

}


//--------------------------------------------------------//
void p_MidiInHandler(HMIDIIN hMidiIn, UINT wMsg, DWORD_PTR dwInstance, DWORD_PTR dwParam1, DWORD_PTR dwParam2) // TODO2 put in midi utility?
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

            int raw_msg = dwParam1;
            int timestamp = dwParam2;
            int b = raw_msg & 0xFF;
            int data1 = (raw_msg >> 8) & 0xFF;
            int data2 = (raw_msg >> 16) & 0xFF;
            int channel = 1;
            midi_event_t evt;

            if ((b & 0xF0) == 0xF0)
            {
                // Both bytes are used for command code in this case.
                evt = (midi_event_t)b;
            }
            else
            {
                evt = (midi_event_t)(b & 0xF0);
                channel = (b & 0x0F) + 1;
            }


            //TODOE validate midiin device and channel number as registered by user.

            int hndchan = 0;
            


            switch (evt)
            {
                case MIDI_NOTE_ON: // => luainterop_InputNote(lua_State* l, int hndchan, int notenum, double volume)
                case MIDI_NOTE_OFF:
                    if (data2 > 0 && evt == MIDI_NOTE_ON)
                    {
                        // me = new NoteOnEvent(ts, channel, data1, data2, 0);
                        // NoteOnEvent(long absoluteTime, int channel, int noteNumber, int velocity, int duration)

                        // log.WriteInfo(String.Format("Time {0} Message 0x{1:X8} Event {2}", e.Timestamp, e.RawMessage, e.MidiEvent));
                    }
                    else
                    {
                        // me = new NoteEvent(ts, channel, evt, data1, data2);
                    }
                    break;

                case MIDI_CONTROL_CHANGE: // => luainterop_InputController(lua_State* l, int hndchan, int controller, int value)
                    // me = new ControlChangeEvent(ts, channel, (MidiController)data1, data2);
                    break;

                // case PitchWheelChange:
                //     // me = new PitchWheelChangeEvent(ts, channel, data1 + (data2 << 7));
                //     break;
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
                    // throw new FormatException(String.Format("Unsupported MIDI Command Code for Raw Message {0}", evt));
                    break;
            }
            break;

        case MIM_ERROR:
            // parameter 1 is invalid MIDI message
            //TODOE log.WriteError(String.Format("Time {0} Message 0x{1:X8} Event {2}", e.Timestamp, e.RawMessage, e.MidiEvent));
            break;

        // Others not implemented:
        case MIM_OPEN:
        case MIM_CLOSE:
        case MIM_LONGDATA:
        case MIM_LONGERROR:
        case MIM_MOREDATA:
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
int p_InitScript(void)
{
    int stat = NEB_OK;

    // Init internal stuff.
    p_loop_running = false;
    p_lmain = luaL_newstate();
    diag_EvalStack(p_lmain, 0);

    // Load std libraries.
    luaL_openlibs(p_lmain);
    diag_EvalStack(p_lmain, 0);

    // Load host funcs into lua space. This table gets pushed on the stack and into globals.
    luainterop_Load(p_lmain);
    diag_EvalStack(p_lmain, 1);

    // Pop the table off the stack as it interferes with calling the module functions.
    lua_pop(p_lmain, 1);
    diag_EvalStack(p_lmain, 0);

    // Stopwatch.
    stat = stopwatch_Init();
    p_last_msec = stopwatch_TotalElapsedMsec();

    // Tempo timer and interrupt - 1 msec resolution.
    stat = ftimer_Init(p_MidiClockHandler, 1);
    luainteropwork_SetTempo(60);

    // Midi event interrupt inited in p_InitMidiDevices(void).

    return stat;
}


//---------------------------------------------------//
int p_Run(const char* fn)
{
    int stat = NEB_OK;

    stat = luaL_loadfile(p_lmain, fn);
    // TODOE if err - do like common_LuaError or EvalLuaStatus
    diag_EvalStack(p_lmain, 0);

    // Load/run the script/file. >>>>>>>>>>>> use new style
    // stat = luaL_dofile(p_lmain, fn); // lua_load pushes the compiled chunk as a Lua function on top of the stack. Otherwise, it pushes an error message.
    // or...
    stat = luaL_loadfile(p_lmain, fn);
    //if err - do like common_LuaError or EvalLuaStatus
    diag_EvalStack(p_lmain, 0);

    // Call script setup.
    stat = luainterop_Setup(p_lmain);
    //if err - do like common_LuaError or EvalLuaStatus
    diag_EvalStack(p_lmain, 0);


//    CHK_LUA_ERROR(p_lmain, stat, fn);
// >>>>
// LuaStatus lstat = LoadFile(file); /LoadString
// err = EvalLuaStatus(lstat);
// lstat = DoCall(0, LUA_MULTRET);
// err |= EvalLuaStatus(lstat);



    /// Then loop forever doing cli requests. TODO2
    bool _run = true;
    do
    {
        // stat = board_CliReadLine(p_cli_buf, CLI_BUFF_LEN);
        // if(stat == 0 && strlen(p_cli_buf) > 0)
        // {
        //     // logger_Log(LVL_DEBUG, "|||got:%s", p_cli_buf);
        //     stat = p_ProcessCommand_ex(p_cli_buf);
        // }
        p_Sleep(100);
    } while (_run);

    return stat;
}
