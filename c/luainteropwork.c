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
// void p_DigInputHandler(unsigned int pin, bool value)
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
//     // no return value
// }


//--------------------------------------------------------//
int luainteropwork_Log(int level, char* msg) // TODOX content
{
    return 0;
}


//--------------------------------------------------------//
int luainteropwork_CreateChannel(const char* device, int channel, int patch)
{
    // TODOX look through devices list for this device then...

    // local hdrums = create_device(0, 10, kit.Jazz)
    // local hinp1 = create_device(1, 2)

    int hnd = -1;


    return hnd;
}


//--------------------------------------------------------//
int luainteropwork_SendNote(int channel, int notenum, double volume, double dur) // TODOX content
{
    int ret = 0;

    int dwMsg = 0;
    HMIDIOUT hmidi_out;
    // http://msdn.microsoft.com/en-us/library/dd798475%28VS.85%29.aspx
    ret = midiOutShortMsg(hmidi_out, dwMsg);

    return ret;
}


//--------------------------------------------------------//
int luainteropwork_SendController(int channel, int ctlr, int value) // TODOX content
{
    int ret = 0;

    return ret;
}
