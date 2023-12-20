#include <windows.h>
// #include <stdlib.h>
// #include <stdio.h>
// #include <stdarg.h>
// #include <stdbool.h>
// #include <stdint.h>
// #include <string.h>
// #include <float.h>
// #include <errno.h>
// #include "lua.h"
// #include "lualib.h"
// #include "lauxlib.h"

#include "luainterop.h"
#include "luainteropwork.h"
#include "logger.h"

// Definition of work functions.

//     stat = board_RegDigInterrupt(p_DigInputHandler);
// void p_DigInputHandler(unsigned int which, bool value)
// {
//     interop_Hinput(p_lscript, which, value);
// }
// void interop_Hinput(lua_State* L, unsigned int pin, bool value)
// {
//     int lstat = LUA_OK;

//     ///// Get the function to be called.
//     int gtype = lua_getglobal(L, "hinput");

//     ///// Push the arguments to the call.
//     lua_pushinteger(L, pin);
//     lua_pushboolean(L, value);

//     ///// Use lua_pcall to do the actual call.
//     lstat = lua_pcall(L, 2, 0, 0);

//     PROCESS_LUA_ERROR(L, lstat, "Call hinput() failed");

//     /////
//     // no return value
// }


extern void p_MidiInFunc(HMIDIIN, UINT, DWORD_PTR, DWORD_PTR, DWORD_PTR);


//--------------------------------------------------------//
int luainteropwork_Log(int level, char* msg) // TODO content
{
    return 0;
}


//--------------------------------------------------------//
int luainteropwork_CreateDevice(int dtype, int channel, int patch)
{
    int hnd = -1;

    ///// Configure midi.
    MMRESULT res = 0;
    int dev_in = 1; // from enumeration
    HMIDIIN hmidi_in = 0;
    MIDIINCAPS caps_in;

    int dev_out = 1; // from enumeration
    HMIDIOUT hmidi_out = 0;
    MIDIOUTCAPS caps_out;
    

    // local hdrums = create_device(0, 10, kit.Jazz)
    // local hinp1 = create_device(1, 2)

    // // IN:
    // int num_in = midiInGetNumDevs();
    // res = midiInGetDevCaps(dev_in, &caps_in, sizeof(caps_in));

    // res = midiInOpen(&hmidi_in, dev_in, p_MidiInProc, 0, CALLBACK_FUNCTION);
    // res = midiInStart(hmidi_in);

    // res = midiInReset(hmidi_in);
    // res = midiInStop(hmidi_in);
    // res = midiInClose(hmidi_in);

    // // OUT:
    // int num_out = midiOutGetNumDevs();
    // res = midiOutGetDevCaps(dev_out, &caps_out, sizeof(caps_out));

    // res = midiOutOpen(&hmidi_out, dev_out, 0, 0, 0);
    // int msg, dw1, dw2, dwMsg = 0;
    // res = midiOutMessage(hmidi_out, msg, dw1, dw2);
    // res = midiOutShortMsg(hmidi_out, dwMsg);

    // res = midiOutReset(hmidi_out);
    // res = midiOutClose(hmidi_out);

    return hnd;
}


//--------------------------------------------------------//
int luainteropwork_SendNote(int channel, int notenum, double volume, double dur) // TODO content
{
    return 0;
}


//--------------------------------------------------------//
int luainteropwork_SendController(int channel, int ctlr, int value) // TODO content
{
    return 0;
}
