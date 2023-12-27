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



//----------------------- Vars -----------------------------//

// The main Lua thread - C code incl interrupts for mm/fast timer and midi events.
static lua_State* p_lmain;

// The script execution status.
static bool p_running = false;

// Processing loop status.
// static bool p_loop_running;

// Last tick time.
static double p_last_msec = 0;
// static unsigned long p_last_usec;

// CLI contents.
static char p_cli_buf[CLI_BUFF_LEN];

// // Devices specified in the user script.
// static midi_device_t _devices[NUM_MIDI_DEVICES];  //TODO1 global, refine/pointers



//---------------------- Private functions ------------------------//

//
// static int p_InitMidiDevices(void);

//
static int p_InitScript(void);

//
static int p_Run(const char* fn);

// Tick corresponding to bpm. Interrupt!
static void p_MidiClockHandler(double msec);

// Handle incoming messages. Interrupt!
static void p_MidiInHandler(HMIDIIN hMidiIn, UINT wMsg, DWORD_PTR dwInstance, DWORD_PTR dwParam1, DWORD_PTR dwParam2);

// Blocking sleep.
static void p_Sleep(int msec);

static int p_ProcessCommand(const char* sin);


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
    if(argc != 2)
    {
        stat = NEB_ERR_BAD_APP_ARG;
        common_EvalStatus(p_lmain, stat, "Invalid args");
    }

    stat = devmgr_Init((DWORD_PTR)p_MidiInHandler);
    // stat = p_InitMidiDevices();
    common_EvalStatus(p_lmain, stat, "Failed to init midi");

    stat = p_InitScript();
    common_EvalStatus(p_lmain, stat, "Failed to init script");

    ///// Run the application. Blocks forever.
    stat = p_Run(argv[1]);
    common_EvalStatus(p_lmain, stat, "Run failed");

    ///// Finished /////
    cli_WriteLine("Goodbye - come back soon!");
    // Clean up and go home.
    ftimer_Run(0);
    ftimer_Destroy();
    cli_Destroy();
    devmgr_Destroy();

    // for (int i = 0; i < NUM_MIDI_DEVICES; i++)//devmgr_Destroy()
    // {
    //     if (_devices[i].hnd_in > 0)
    //     {
    //         midiInStop(_devices[i].hnd_in);
    //         midiInClose(_devices[i].hnd_in); 
    //     }
    //     else if (_devices[i].hnd_out > 0)
    //     {
    //         midiOutClose(_devices[i].hnd_out);
    //     }
    // }

    lua_close(p_lmain);

    return NEB_OK;
}


//-------------------------------------------------------//
// int p_InitMidiDevices(void)//devmgr_Init()
// {
//     int stat = NEB_OK;
//     MMRESULT res = 0;

//     memset(_devices, 0, sizeof(_devices));
//     int dev_index = 0;

//     int num_in = midiInGetNumDevs();
//     int num_out = midiOutGetNumDevs();

//     if (num_in + num_out >= NUM_MIDI_DEVICES)
//     {
//         common_EvalStatus(p_lmain, NEB_ERR_BAD_MIDI_CFG, "Too many midi devices");
//     }

//     // Inputs.
//     for (int i = 0; i < num_in; i++, dev_index++)
//     {
//         MIDIINCAPS caps_in;
//         res = midiInGetDevCaps(i, &caps_in, sizeof(caps_in));

//         HMIDIIN hmidi_in;
//         // dev_index => dwInstance;
//         res = midiInOpen(&hmidi_in, i, (DWORD_PTR)p_MidiInHandler, (DWORD_PTR)dev_index, CALLBACK_FUNCTION);

//         // Save the device info.
//         _devices[dev_index].hnd_in = hmidi_in;
//         _devices[dev_index].sys_dev_index = i;
//         strncpy(_devices[dev_index].sys_dev_name, caps_in.szPname, MAXPNAMELEN - 1);

//         // Fire it up.
//         res = midiInStart(hmidi_in);
//     }

//     // Outputs.
//     for (int i = 0; i < num_out; i++, dev_index++)
//     {
//         // http://msdn.microsoft.com/en-us/library/dd798469%28VS.85%29.aspx
//         MIDIOUTCAPS caps_out;
//         res = midiOutGetDevCaps(i, &caps_out, sizeof(caps_out));

//         HMIDIOUT hmidi_out;
//         // http://msdn.microsoft.com/en-us/library/dd798476%28VS.85%29.aspx
//         res = midiOutOpen(&hmidi_out, i, 0, 0, 0);

//         // Save the device info.
//         _devices[dev_index].hnd_out = hmidi_out;
//         _devices[dev_index].sys_dev_index = i;
//         strncpy(_devices[dev_index].sys_dev_name, caps_out.szPname, MAXPNAMELEN);
//     }

//     return stat;
// }


//-------------------------------------------------------//
void p_MidiClockHandler(double msec)
{
    // TODO2 process
    //  See lua.c for a way to treat C signals, which you may adapt to your interrupts.

}


// bool p_IsValid()

//--------------------------------------------------------//
void p_MidiInHandler(HMIDIIN hMidiIn, UINT wMsg, DWORD_PTR dwInstance, DWORD_PTR dwParam1, DWORD_PTR dwParam2) // TODO2 put in midi utility?
{
    // Input midi event. Note this is in an interrupt handler!
    // http://msdn.microsoft.com/en-us/library/dd798458%28VS.85%29.aspx

    // int dev_index = dwInstance;
    // midi_device_t* pdev = _devices + dev_index;//midi_device_t* devmgr_Get(dev_index);
//    midi_device_t* pdev = devmgr_GetByIndex(dwInstance);

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

            // Validate midiin device and channel number as registered by user.//midi_device_t* devmgr_Get(hMidiIn, channel)
            midi_device_t* pdev = devmgr_GetByMidiHandle(hMidiIn);
            hndchan = devmgr_GetChannelHandle(pdev, channel);

            // for (int i = 0; i < NUM_MIDI_DEVICES && hndchan == 0; i++)
            // {
            //     if (_devices[i].hnd_in == hMidiIn && _devices[i].channels[channel - 1]) // test for -1
            //     {
            //         hndchan = MAKE_HANDLE(i, channel);
            //     }
            // }


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


//----------------------------------------------------//
int p_InitScript(void)
{
    int stat = NEB_OK;

    // Init internal stuff.
    p_running = false;
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

    // Load/run the script/file.
    stat = luaL_loadfile(p_lmain, fn);
    // or: stat = luaL_dofile(p_lmain, fn); // lua_load pushes the compiled chunk as a Lua function on top of the stack. Otherwise, it pushes an error message.
    common_EvalStatus(p_lmain, stat, "luaL_loadfile");
    diag_EvalStack(p_lmain, 0);

    // Script setup.
    stat = luainterop_Setup(p_lmain);
    common_EvalStatus(p_lmain, stat, "setup");
    diag_EvalStack(p_lmain, 0);

    // Loop forever doing cli requests.
    p_running = true;
    do
    {
        stat = cli_ReadLine(p_cli_buf, CLI_BUFF_LEN);
        if(stat == 0 && strlen(p_cli_buf) > 0)
        {
            // logger_Log(LVL_DEBUG, "|||got:%s", p_cli_buf);
            stat = p_ProcessCommand(p_cli_buf);
        }
        p_Sleep(100);
    } while (p_running);

    return stat;
}


//---------------------------------------------------//
int p_ProcessCommand(const char* sin)
{
    int stat = NEB_OK;
#define MAX_NUM_OPTS 8
    // What are the command line options. First one should be the actual command.
    char* opts[MAX_NUM_OPTS];
    memset(opts, 0x00, sizeof(opts));
    int oind = 0;

    // Make writable copy and tokenize it.
    char cp[strlen(sin) + 1];
    strcpy(cp, sin);
    char* token = strtok(cp, " ");

    while(token != NULL && oind < MAX_NUM_OPTS)
    {
        opts[oind++] = token;
        token = strtok(NULL, " ");
    }

    bool valid = false; // default
    if(oind > 0)
    {
        switch(opts[0][0])
        {
            case 'x':
                valid = true;
                p_running = false;
                break;

            case 'c':
                if(oind == 3)
                {
                    double x = -1;
                    double y = -1;
                    double res = -1;
                    if(common_StrToDouble(opts[1], &x) && common_StrToDouble(opts[2], &y))
                    {
//>>>                        interop_Calc(p_lscript, x, y, &res);
                        cli_WriteLine("%g + %g = %g", x, y, res);
                        valid = true;
                    }
                }
                break;
        }
    }

    if(!valid)
    {
        // usage
        cli_WriteLine("Invalid cmd:%s ", sin);
        cli_WriteLine("Try:");
        cli_WriteLine("  exit: x");
        cli_WriteLine("  tell script to blabla: c op1 op2");
        stat = NEB_ERR_BAD_APP_ARG;
    }

    return stat;
}
