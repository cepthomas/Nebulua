#include <cstdio>
#include <cstring>

#include "pnut.h"

extern "C"
{
#include "nebcommon.h"
#include "devmgr.h"
#include "logger.h"
}


// Midi Input:  name:"loopMIDI Port" index:0 handle:0000000000000000
// Midi Output:  name:"Microsoft GS Wavetable Synth" index:0 handle:0000000000000000 channels:


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

    stat = devmgr_Init(_MidiInHandler);
    UT_EQUAL(stat, NEB_OK);

    devmgr_Dump();

    midi_device_t* pindev = devmgr_GetDeviceFromMidiHandle((HMIDIIN)999);
    UT_EQUAL(pindev, (void*)NULL);

    pindev = devmgr_GetDeviceFromName("aaaaaaa");
    UT_EQUAL(pindev, (void*)NULL);

    int chan_hnd = devmgr_GetChannelHandle(pindev, 999);
    UT_EQUAL(chan_hnd, 999);

    midi_device_t* poutdev = devmgr_GetDeviceFromChannelHandle(999);
    UT_EQUAL(poutdev, (void*)NULL);

    poutdev = devmgr_GetDeviceFromName("aaaaaaa");
    UT_EQUAL(poutdev, (void*)NULL);

    chan_hnd = devmgr_GetChannelHandle(poutdev, 999);
    UT_EQUAL(chan_hnd, 999);

    int chan_num = devmgr_GetChannelNumber(999);
    UT_EQUAL(chan_num, 999);

    stat = devmgr_Destroy();
    UT_EQUAL(stat, NEB_OK);

    return 0;
}    
