#include <cstdio>
#include <cstring>

#include "pnut.h"

extern "C"
{
#include "nebcommon.h"
#include "devmgr.h"
#include "logger.h"
}


const char* _my_midi_in1  = "loopMIDI Port";
const char* _my_midi_out1 = "Microsoft GS Wavetable Synth";


//--------------------------------------------------------//
static void _MidiInHandler(HMIDIIN hMidiIn, UINT wMsg, DWORD_PTR dwInstance, DWORD_PTR dwParam1, DWORD_PTR dwParam2)
{
    // Input midi event -- this is in an interrupt handler!
    // http://msdn.microsoft.com/en-us/library/dd798458%28VS.85%29.aspx

}


/////////////////////////////////////////////////////////////////////////////
UT_SUITE(DEVMGR_MAIN, "Test device manager. TODO1")
{
    int stat = 0;
    midi_device_t* pindev;
    midi_device_t* poutdev;
    midi_device_t* ptemp;
    int chan_hnd;
    int chan_num;

    stat = devmgr_Init(_MidiInHandler);
    UT_EQUAL(stat, NEB_OK);

    // devmgr_Dump();


    // create is:
    // midi_device_t* pdev = devmgr_GetDeviceFromName(sys_dev_name);
    // int stat = devmgr_OpenMidi(pdev);
    // chan_hnd = devmgr_GetChannelHandle(pdev, chan_num);


    ///// Inputs.
    pindev = devmgr_GetDeviceFromName(_my_midi_in1);
    UT_NOT_NULL(pindev);

    stat = devmgr_OpenMidi(pindev);
    UT_EQUAL(stat, NEB_OK);

    chan_hnd = devmgr_GetChannelHandle(pindev, 1);
    UT_EQUAL(chan_hnd, 1);//0



    ptemp = devmgr_GetDeviceFromMidiHandle((HMIDIIN)999); // invalid
    UT_NULL(ptemp);

    ptemp = devmgr_GetDeviceFromMidiHandle((HMIDIIN)1); // valid
    UT_EQUAL(ptemp, pindev);//X


    chan_num = devmgr_GetChannelNumber(0X1234);
    UT_EQUAL(chan_num, 0X34);//52



    ///// Outputs.
    poutdev = devmgr_GetDeviceFromName(_my_midi_out1);
    UT_GREATER(poutdev, INACTIVE_DEV);//0x7ff7627cf1a0

    stat = devmgr_OpenMidi(poutdev);
    UT_EQUAL(stat, NEB_OK);

    poutdev = devmgr_GetDeviceFromChannelHandle(999);
    UT_EQUAL(poutdev, (void*)NULL);

    chan_hnd = devmgr_GetChannelHandle(poutdev, 999);
    UT_EQUAL(chan_hnd, 999);//0

    chan_num = devmgr_GetChannelNumber(chan_hnd);
    UT_EQUAL(chan_num, 999);//73

    ///// Done.
    // devmgr_Dump();

    stat = devmgr_Destroy();
    UT_EQUAL(stat, NEB_OK);


    return 0;
}    
