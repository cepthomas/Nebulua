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

#include "lua.h"
#include "lualib.h"
#include "lauxlib.h"

extern lua_State* _l;

void _MidiInHandler(HMIDIIN hMidiIn, UINT wMsg, DWORD_PTR dwInstance, DWORD_PTR dwParam1, DWORD_PTR dwParam2);
void _MidiClockHandler(double msec);
int exec_Main(const char* script_fn);
}

// const char* _my_midi_in1  = "loopMIDI Port";
// const char* _my_midi_out1 = "Microsoft GS Wavetable Synth";




/////////////////////////////////////////////////////////////////////////////
UT_SUITE(EXEC_MAIN, "Test exec functions.")
{
    int stat = 0;

    _l = luaL_newstate();


    HMIDIIN hMidiIn = 0;
    UINT wMsg = 0;
    DWORD_PTR dwInstance = 0;
    DWORD_PTR dwParam1 = 0;
    DWORD_PTR dwParam2 = 0;
    _MidiInHandler(hMidiIn, wMsg, dwInstance, dwParam1, dwParam2);


    double msec = 12.34;
    _MidiClockHandler(msec);

    // Needs luapath.
    //const char* script_fn = "script1.lua";
    //stat = exec_Main(script_fn);


    lua_close(_l);

    return 0;
}
