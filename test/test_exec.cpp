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


#define SECTION_NAME_LEN 32
#define NUM_SECTIONS 32
typedef struct { char name[SECTION_NAME_LEN]; int start; } section_name_t;

int comp_sections(const void* elem1, const void* elem2)
{
    section_name_t* f = (section_name_t*)elem1;
    section_name_t* s = (section_name_t*)elem2;
    return (f->start > s->start) - (f->start < s->start);
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

    // Get some nebulator script globals. TODO2 error checking?
    // => length
    int ltype = lua_getglobal(_l, "_length");
    if (ltype != LUA_TNUMBER) { stat = 9999; }
    int length = (int)lua_tointeger(_l, -1);
    lua_pop(_l, 1); // Clean up.

    section_name_t sections[NUM_SECTIONS];
    memset(sections, 0, sizeof(sections));
    section_name_t* ps = sections;

    ltype = lua_getglobal(_l, "_section_names");
    lua_pushnil(_l);
    while (lua_next(_l, -2) != 0)
    {
        strncpy(ps->name, lua_tostring(_l, -2), SECTION_NAME_LEN-1);
        ps->start = (int)lua_tointeger(_l, -1);
        lua_pop(_l, 1);
        ps++;
    }

    qsort(sections, ps - sections, sizeof(section_name_t), comp_sections);

    lua_pop(_l, 1); // Clean up.

    lua_close(_l);

    return 0;
}
