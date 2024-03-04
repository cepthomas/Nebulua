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


//----------------------- Types -----------------------//


//----------------------- Vars --------------------------------//


//---------------------- Functions ------------------------//

/// Collect information from the loaded script.
/// @param[in] l lua state.
/// @return Status.
int scriptinfo_Init(lua_State* l);

/// Get script length.
/// @return length.
int scriptinfo_GetLength();

/// Get section name.
/// @param[in] index which.
/// @return name or NULL if invalid.
const char* scriptinfo_GetSectionName(int index);

/// Get section start.
/// @param[in] index which.
/// @return start or -1 if invalid.
int scriptinfo_GetSectionStart(int index);


#endif SECTINFO_H