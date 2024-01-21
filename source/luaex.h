#ifndef LUAEX_H
#define LUAEX_H

#include "lua.h"

/// Extra/extended lua stuff to support this application.

/// Interface to lua_pcall() with error message function. Used to run all function chunks.
/// Modeled after docall(...).
/// @param[in] l Internal lua state.
/// @param[in] num_args Number of args.
/// @param[in] num_ret Number of return values.
/// @return LUA_STATUS
int luaex_docall(lua_State* l, int num_args, int num_ret);


#endif // LUAEX_H
