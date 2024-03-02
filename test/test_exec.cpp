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
#include "lua.h"
#include "lualib.h"
#include "lauxlib.h"

#include "luautils.h"
#include "luainterop.h"

#include "nebcommon.h"
#include "cbot.h"
#include "devmgr.h"
#include "cli.h"
#include "logger.h"

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


/////////////////////////////////////////////////////////////////////////////
UT_SUITE(EXEC_STUFF, "Test exec stuff.")
{
    int stat = 0;
    int iret = 0;

    ///// Need to load a real file for position stuff.
    lua_State* _l = luaL_newstate();

    // Load std libraries.
    luaL_openlibs(_l);

    // Load host funcs into lua space. This table gets pushed on the stack and into globals.
    luainterop_Load(_l);

    // Pop the table off the stack as it interferes with calling the module functions.
    lua_pop(_l, 1);

    // Load the script file. Pushes the compiled chunk as a Lua function on top of the stack or pushes an error message.
    stat = luaL_loadfile(_l, "script1.lua");
    UT_EQUAL(stat, NEB_OK);
    const char* e = nebcommon_EvalStatus(_l, stat, "load script");
    UT_NULL(e);

    // Execute the loaded script to init everything.
    stat = lua_pcall(_l, 0, LUA_MULTRET, 0);
    UT_EQUAL(stat, NEB_OK);
    e = nebcommon_EvalStatus(_l, stat, "run script");
    UT_NULL(e);

    // Script setup function.
    stat = luainterop_Setup(_l, &iret);
    UT_EQUAL(stat, NEB_OK);
    e = nebcommon_EvalStatus(_l, stat, "setup()");
    UT_NULL(e);

    ///// Good to go now.
    int ltype = lua_getglobal(_l, "_length");
    int length = lua_tointeger(_l, -1);

    ltype = lua_getglobal(_l, "_section_names"); //TTODO1 get these.

    
    //lua_Integer lua_tointegerx(lua_State * L, int index, int* isnum);
    lua_pop(_l, 1); // Clean up.

    //luautils_DumpStack(_l, stdout, "xxx");

    lua_close(_l);

    return 0;
}