// // system
// #include <windows.h>
// #include <stdio.h>
// #include <time.h>
// // lua
// #include "lua.h"
// #include "lualib.h"
// #include "lauxlib.h"
// // cbot
// #include "cbot.h"
// #include "logger.h"
// #include "mathutils.h"
// // lbot
// #include "luautils.h"
// #include "ftimer.h"
// application
#include "scriptinfo.h"


//----------------------- Definitions -----------------------//


//----------------------- Types -----------------------//


//----------------------- Vars --------------------------------//

static section_desc_t _section_descs[MAX_SECTIONS];

static int _length;

//---------------------- Functions ------------------------//

//private
int _CompareSections(const void* elem1, const void* elem2)
{
    section_desc_t* f = (section_desc_t*)elem1;
    section_desc_t* s = (section_desc_t*)elem2;
    return (f->start > s->start) - (f->start < s->start);
}

//----------------------- Main Functions ---------------------//

//---------------------------------------------------//
int scriptinfo_Init(lua_State* l)
{
    int stat = NEB_OK;

    // Get length and section info. TODO2 error checking? it's in my lib...
    int ltype = lua_getglobal(_l, "_length");
    int length = (int)lua_tointeger(_l, -1);
    lua_pop(_l, 1); // Clean up stack.

    memset(_section_descs, 0, sizeof(_section_descs));
    section_desc_t* ps = _section_descs;
    ltype = lua_getglobal(_l, "_section_names");
    lua_pushnil(_l);
    while (lua_next(_l, -2) != 0) // TODO2 overflow
    {
        strncpy(ps->name, lua_tostring(_l, -2), SECTION_NAME_LEN-1);
        ps->start = (int)lua_tointeger(_l, -1);
        lua_pop(_l, 1);
        ps++;
    }
    qsort(_section_descs, ps - _section_descs, sizeof(section_desc_t), _CompareSections);
    lua_pop(_l, 1); // Clean up stack.

    return stat;
}

//---------------------------------------------------//
int scriptinfo_GetLength()
{
    return _length;
}

// need name() or names() for cli; start for name;