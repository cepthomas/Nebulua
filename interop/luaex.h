#ifndef LUAEX_H
#define LUAEX_H

#include "lua.h"
#include "lualib.h"
#include "lauxlib.h"


//---------------- Public API ----------------------//

/// TODO Add tableex type support similar to LuaEx.cs/TableEx.cs (see structinator).
//  Also consider arrays of scalars or tableex.

typedef struct tableex
{
    int something;
    char* other;
} tableex_t;


/// Push a table onto lua stack.
/// @param[in] l Internal lua state.
/// @param[in] tbl The table.
void luaex_pushtableex(lua_State* l, tableex_t* tbl);

/// Make a TableEx from the lua table on the top of the stack.
/// Note: Like other "to" functions except also does the pop.
/// @param[in] l Internal lua state.
/// @param[in] ind Where it is on the stack. Not implemented yet.
/// @return The new table or NULL if invalid.
tableex_t* luaex_totableex(lua_State* l, int ind);

/// Interface to lua_pcall() with error message function. Used to run all function chunks.
/// Modeled after docall(...).
/// @param[in] l Internal lua state.
/// @param[in] num_args Number of args.
/// @param[in] num_ret Number of return values.
/// @return LUA_STATUS
int luaex_docall(lua_State* l, int num_args, int num_ret);


#endif // LUAEX_H
