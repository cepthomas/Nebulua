#include <cstdio>
#include <string>
#include <cstring>
#include <sstream>
#include <vector>
#include <iostream>

#include <windows.h>
#include "pnut.h"

extern "C"
{
#include "nebcommon.h"
#include "cbot.h"
#include "devmgr.h"
#include "cli.h"
#include "logger.h"

void _MidiInHandler(HMIDIIN hMidiIn, UINT wMsg, DWORD_PTR dwInstance, DWORD_PTR dwParam1, DWORD_PTR dwParam2);
void _MidiClockHandler(double msec);
int exec_Main(const char* script_fn);
}

// const char* _my_midi_in1  = "loopMIDI Port";
// const char* _my_midi_out1 = "Microsoft GS Wavetable Synth";


/////////////////////////////////////////////////////////////////////////////
UT_SUITE(EXEC_1111, "Test TODO2 any functions.")
{
    int stat = 0;

    // These need _l.

    HMIDIIN hMidiIn = 0;
    UINT wMsg = 0;
    DWORD_PTR dwInstance = 0;
    DWORD_PTR dwParam1 = 0;
    DWORD_PTR dwParam2 = 0;
    _MidiInHandler(hMidiIn, wMsg, dwInstance, dwParam1, dwParam2);


    double msec = 12.34;
    _MidiClockHandler(msec);


    const char* script_fn = "aaaaaa";
    //stat = exec_Main(script_fn);

    return 0;
}
