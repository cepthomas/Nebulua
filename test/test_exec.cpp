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


// #define SECTION_NAME_LEN 32
// #define NUM_SECTIONS 32
// typedef struct { char name[SECTION_NAME_LEN]; int start; } section_name_t;

// int comp_sections(const void* elem1, const void* elem2)
// {
//     section_name_t* f = (section_name_t*)elem1;
//     section_name_t* s = (section_name_t*)elem2;
//     return (f->start > s->start) - (f->start < s->start);
// }


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

    // Load the script file. Pushes the compiled chunk as a Lua function on top of the stack or pushes an error message.
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
UT_SUITE(EXEC_ERR_XXX, "Test failure mode.")
{
    int stat = 0;
    int iret = 0;

    // Load lua.
    lua_State* _l = luaL_newstate();
    luaL_openlibs(_l);
    luainterop_Load(_l);
    lua_pop(_l, 1);

    // Load the bad script file. Pushes the compiled chunk as a Lua function on top of the stack or pushes an error message.
    stat = luaL_loadfile(_l, "xxxx.lua");
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

    lua_close(_l);

    return 0;
}
