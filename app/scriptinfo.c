// // system
// #include <windows.h>
#include <stdlib.h>
#include <string.h>
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
#include "nebcommon.h"
#include "scriptinfo.h"


//----------------------- Definitions -----------------------//

#define SECTION_NAME_LEN 32


//----------------------- Types -----------------------//

typedef struct { char name[SECTION_NAME_LEN]; int start; } section_desc_t;


//----------------------- Vars --------------------------------//

static section_desc_t _section_descs[MAX_SECTIONS];

static int _length;

//---------------------- Private Functions -------------------//

// qsort compare function.
static int _CompareSections(const void* elem1, const void* elem2)
{
    section_desc_t* f = (section_desc_t*)elem1;
    section_desc_t* s = (section_desc_t*)elem2;
    return (f->start > s->start) - (f->start < s->start);
}

//----------------------- Public Functions ---------------------//


//---------------------------------------------------//
int scriptinfo_Init(lua_State* l)
{
    int stat = NEB_OK;

    // Get length.
    int ltype = lua_getglobal(l, "_length");
    int length = (int)lua_tointeger(l, -1);
    lua_pop(l, 1); // Clean up stack.

    // Get section info.
    memset(_section_descs, 0, sizeof(_section_descs));
    ltype = lua_getglobal(l, "_section_names");
    lua_pushnil(l);
    int n = 0;
    while (lua_next(l, -2) != 0 && stat == NEB_OK) // TODO2 overflow
    {
        if (n < MAX_SECTIONS)
        {
            strncpy(_section_descs[n].name, lua_tostring(l, -2), SECTION_NAME_LEN-1);
            _section_descs[n].start = (int)lua_tointeger(l, -1);
            lua_pop(l, 1);
            n++;
        }
        else
        {
            stat = NEB_ERR_SYNTAX;
        }
    }
    qsort(_section_descs, n, sizeof(section_desc_t), _CompareSections);
    lua_pop(l, 1); // Clean up stack.

    return stat;
}

//---------------------------------------------------//
int scriptinfo_GetLength()
{
    return _length;
}

//---------------------------------------------------//
const char* scriptinfo_GetSectionName(int index)
{
    const char* ret = NULL; // default

    if (index < MAX_SECTIONS)
    {
        ret = strlen(_section_descs[index].name) > 0 ? _section_descs[index].name : NULL;
    }

    return ret;
}

//---------------------------------------------------//
int scriptinfo_GetSectionStart(int index)
{
    int ret = -1; // default

    if (index < MAX_SECTIONS)
    {
        ret = strlen(_section_descs[index].name) > 0 ? _section_descs[index].start : -1;
    }

    return ret;
}
