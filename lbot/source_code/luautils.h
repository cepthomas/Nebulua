
#ifndef LUAUTILS_H
#define LUAUTILS_H

#include <stdbool.h>
#include "lua.h"
#include "lualib.h"
#include "lauxlib.h"


//---------------- Uilities --------------------------//

/// Dump the lua stack contents.
/// @param l Lua state.
/// @param fout where to boss.
/// @param info Extra info.
int luautils_DumpStack(lua_State *l, FILE* fout, const char* info);

/// Make a readable string.
/// @param status Specific Lua status.
/// @return the string.
const char* luautils_LuaStatusToString(int err);

/// Dump the table at the top.
/// @param l Lua state.
/// @param fout where to boss.
/// @param name visual.
int luautils_DumpTable(lua_State* l, FILE* fout, const char* name);

/// Dump the lua globals.
/// @param l Lua state.
/// @param fout where to boss.
int luautils_DumpGlobals(lua_State* l, FILE* fout);

 /// Check stack.
 void luautils_EvalStack(lua_State* l, FILE* fout, int expected);

/// Safe convert a string to double with bounds checking.
/// @param[in] str to parse
/// @param[out] val answer
/// @param[in] min limit inclusive
/// @param[in] max limit inclusive
/// @return success
bool luautils_ParseDouble(const char* str, double* val, double min, double max);

/// Safe convert a string to int with bounds checking.
/// @param[in] str to parse
/// @param[out] val answer
/// @param[in] min limit inclusive
/// @param[in] max limit inclusive
/// @return success
bool luautils_ParseInt(const char* str, int* val, int min, int max);

#endif // LUAUTILS_H
