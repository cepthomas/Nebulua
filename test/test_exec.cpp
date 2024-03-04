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
#include "scriptinfo.h"


extern lua_State* _l;

void _MidiInHandler(HMIDIIN hMidiIn, UINT wMsg, DWORD_PTR dwInstance, DWORD_PTR dwParam1, DWORD_PTR dwParam2);
void _MidiClockHandler(double msec);
int exec_Main(const char* script_fn);
}

// const char* _my_midi_in1  = "loopMIDI Port";
// const char* _my_midi_out1 = "Microsoft GS Wavetable Synth";


/////////////////////////////////////////////////////////////////////////////
UT_SUITE(EXEC_FUNCS, "Test exec functions. TODO2 need flesh")
{
    int stat = 0;

    _l = luaL_newstate();

    //////
    HMIDIIN hMidiIn = 0;
    UINT wMsg = 0;
    DWORD_PTR dwInstance = 0;
    DWORD_PTR dwParam1 = 0;
    DWORD_PTR dwParam2 = 0;
    _MidiInHandler(hMidiIn, wMsg, dwInstance, dwParam1, dwParam2);

    //////
    double msec = 12.34;
    _MidiClockHandler(msec);

    //////
    // stat = exec_Main(script_fn);
    // Needs luapath.

    lua_close(_l);

    return 0;
}


/////////////////////////////////////////////////////////////////////////////
UT_SUITE(EXEC_MAIN, "Test happy path.")
{
    int stat = 0;
    int iret = 0;

    lua_State* _l = luaL_newstate();

    // Load std libraries.
    luaL_openlibs(_l);

    // Load host funcs into lua space. This table gets pushed on the stack and into globals.
    luainterop_Load(_l);

    // Pop the table off the stack as it interferes with calling the module functions.
    lua_pop(_l, 1);

    // Load/compile the script file. Pushes the compiled chunk as a Lua function on top of the stack or pushes an error message.
    stat = luaL_loadfile(_l, "script_happy.lua");
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

    // Get some nebulator script globals.
    scriptinfo_Init(_l);

    const char* sect_name = scriptinfo_GetSectionName(0);
    int sect_start = scriptinfo_GetSectionStart(0);
    UT_NOT_NULL(sect_name);
    UT_STR_EQUAL(sect_name, "beginning");
    UT_EQUAL(sect_start, 0);

    sect_name = scriptinfo_GetSectionName(1);
    sect_start = scriptinfo_GetSectionStart(1);
    UT_NOT_NULL(sect_name);
    UT_STR_EQUAL(sect_name, "middle");
    UT_EQUAL(sect_start, 648);

    sect_name = scriptinfo_GetSectionName(2);
    sect_start = scriptinfo_GetSectionStart(2);
    UT_NOT_NULL(sect_name);
    UT_STR_EQUAL(sect_name, "ending");
    UT_EQUAL(sect_start, 2117);

    sect_name = scriptinfo_GetSectionName(3);
    sect_start = scriptinfo_GetSectionStart(3);
    UT_NULL(sect_name);
    UT_EQUAL(sect_start, -1);

    lua_close(_l);

    return 0;
}


/////////////////////////////////////////////////////////////////////////////
UT_SUITE(EXEC_ERR1, "Test basic failure modes.")
{
    int stat = 0;
    int iret = 0;

    // Load lua.
    lua_State* _l = luaL_newstate();
    luaL_openlibs(_l);
    luainterop_Load(_l);
    lua_pop(_l, 1);

    ///// General syntax error during load.
    const char* s1 =
        "local neb = require(\"nebulua\")\n"
        "this is a bad statement\n";
    stat = luaL_loadstring(_l, s1);
    UT_EQUAL(stat, LUA_ERRSYNTAX);
    const char* e = nebcommon_EvalStatus(_l, stat, "ERR1");
    UT_STR_CONTAINS(e, "syntax error near 'is'");

    ///// General syntax error - lua_pcall(_l, 0, LUA_MULTRET, 0);
    s1 =
        "local neb = require(\"nebulua\")\n"
        "res1 = 345 + nil_value\n";
    stat = luaL_loadstring(_l, s1);
    UT_EQUAL(stat, LUA_OK);
    stat = lua_pcall(_l, 0, LUA_MULTRET, 0);
    UT_EQUAL(stat, LUA_ERRRUN); // runtime error
    e = nebcommon_EvalStatus(_l, stat, "ERR2");
    UT_STR_CONTAINS(e, "attempt to perform arithmetic on a nil value");


    ///// Missing required C2L api element - luainterop_Setup(_l, &iret);
    s1 =
        "local neb = require(\"nebulua\")\n"
        "resx = 345 + 456\n";
    stat = luaL_loadstring(_l, s1);
    UT_EQUAL(stat, LUA_OK);
    stat = lua_pcall(_l, 0, LUA_MULTRET, 0);
    UT_EQUAL(stat, LUA_OK);
    stat = luainterop_Setup(_l, &iret);
    UT_EQUAL(stat, INTEROP_BAD_FUNC_NAME);
    e = nebcommon_EvalStatus(_l, stat, "ERR3");
    UT_STR_CONTAINS(e, "INTEROP_BAD_FUNC_NAME");


    ///// Bad L2C api function
    s1 =
        "local neb = require(\"nebulua\")\n"
        "function setup()\n"
        "    neb.no_good(95)\n"
        "    return 0\n"
        "end\n";
    stat = luaL_loadstring(_l, s1);
    UT_EQUAL(stat, LUA_OK);
    stat = lua_pcall(_l, 0, LUA_MULTRET, 0);
    UT_EQUAL(stat, LUA_OK);
    stat = luainterop_Setup(_l, &iret);
    UT_EQUAL(stat, LUA_ERRRUN);
    e = nebcommon_EvalStatus(_l, stat, "ERR4");
    UT_STR_CONTAINS(e, "attempt to call a nil value (field 'no_good')");

    lua_close(_l);

    return 0;
}


/////////////////////////////////////////////////////////////////////////////
UT_SUITE(EXEC_ERR2, "Test error() failure modes.")
{
    int stat = 0;
    int iret = 0;

    // Load lua.
    lua_State* _l = luaL_newstate();
    luaL_openlibs(_l);
    luainterop_Load(_l);
    lua_pop(_l, 1);

    ///// General explicit error.
    const char* s1 =
        "local neb = require(\"nebulua\")\n"
        "function setup()\n"
        "    error(\"setup() raises error()\")\n"
        "    return 0\n"
        "end\n";
    stat = luaL_loadstring(_l, s1);
    UT_EQUAL(stat, LUA_OK);
    stat = lua_pcall(_l, 0, LUA_MULTRET, 0);
    UT_EQUAL(stat, LUA_OK);
    stat = luainterop_Setup(_l, &iret);
    UT_EQUAL(stat, LUA_ERRRUN);
    const char* e = nebcommon_EvalStatus(_l, stat, "ERR5");
    UT_STR_CONTAINS(e, "setup() raises error()");

    lua_close(_l);

    return 0;
}


/////////////////////////////////////////////////////////////////////////////
UT_SUITE(EXEC_ERR3, "Test fatal internal failure modes.")
{
    int stat = 0;
    int iret = 0;

    // Load lua.
    lua_State* _l = luaL_newstate();
    luaL_openlibs(_l);
    luainterop_Load(_l);
    lua_pop(_l, 1);

    ///// Runtime error.
    const char* s1 =
        "local neb = require(\"nebulua\")\n"
        "function setup()\n" 
        "    local bad = 123 + ng\n"
        "    return 0\n"
        "end\n";
     stat = luaL_loadstring(_l, s1);
    UT_EQUAL(stat, LUA_OK);
    stat = lua_pcall(_l, 0, LUA_MULTRET, 0);
    UT_EQUAL(stat, LUA_OK);
    stat = luainterop_Setup(_l, &iret);
    UT_EQUAL(stat, LUA_ERRRUN);
    const char* e = nebcommon_EvalStatus(_l, stat, "ERR6");
    UT_STR_CONTAINS(e, "attempt to perform arithmetic on a nil value (global 'ng')");

    lua_close(_l);

    return 0;
}
