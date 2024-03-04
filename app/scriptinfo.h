#ifndef SECTINFO_H
#define SECTINFO_H

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
// // application
// #include "nebcommon.h"
// #include "cli.h"
// #include "devmgr.h"
// #include "luainterop.h"


//----------------------- Definitions -----------------------//
#define SECTION_NAME_LEN 32
#define MAX_SECTIONS 32



//----------------------- Types -----------------------//

typedef struct { char name[SECTION_NAME_LEN]; int start; } section_desc_t;


//----------------------- Vars --------------------------------//


//---------------------- Functions ------------------------//


int scriptinfo_Init(lua_State* l);


int scriptinfo_GetLength();



#endif SECTINFO_H