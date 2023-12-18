///// Warning - this file is created by gen_interop.lua, do not edit. /////

#include <stdlib.h>
#include <stdio.h>
#include <stdarg.h>
#include <stdbool.h>
#include <stdint.h>
#include <string.h>
#include <float.h>

#include "lua.h"
#include "lualib.h"
#include "lauxlib.h"

#include "luainterop.h"
#include "luainteropwork.h"

//---------------- Call lua functions from host -------------//

int luainterop_Setup(lua_State* l)
{
    int num_args = 0;
    int num_ret = 1;

    // Get function.
    int ltype = lua_getglobal(l, "setup");
    if (ltype != LUA_TFUNCTION) { luaL_error(l, "Bad lua function: setup"); };

    // Push arguments.

    // Do the actual call.
    int lstat = lua_pcall(l, num_args, num_ret, 0); // optionally throws
    if (lstat >= LUA_ERRRUN) { luaL_error(l, "lua_pcall() failed: %d", lstat); }

    // Get the results from the stack.
    int ret;
    if (lua_tointeger(l, -1)) { ret = lua_tointeger(l, -1); }
    else { luaL_error(l, "Return is not a int"); }
    lua_pop(l, num_ret); // Clean up results.
    return ret;
}

int luainterop_Step(lua_State* l, int bar, int beat, int subbeat)
{
    int num_args = 0;
    int num_ret = 1;

    // Get function.
    int ltype = lua_getglobal(l, "step");
    if (ltype != LUA_TFUNCTION) { luaL_error(l, "Bad lua function: step"); };

    // Push arguments.
    lua_pushinteger(l, bar);
    num_args++;
    lua_pushinteger(l, beat);
    num_args++;
    lua_pushinteger(l, subbeat);
    num_args++;

    // Do the actual call.
    int lstat = lua_pcall(l, num_args, num_ret, 0); // optionally throws
    if (lstat >= LUA_ERRRUN) { luaL_error(l, "lua_pcall() failed: %d", lstat); }

    // Get the results from the stack.
    int ret;
    if (lua_tointeger(l, -1)) { ret = lua_tointeger(l, -1); }
    else { luaL_error(l, "Return is not a int"); }
    lua_pop(l, num_ret); // Clean up results.
    return ret;
}

int luainterop_InputNote(lua_State* l, int channel, int notenum, double volume)
{
    int num_args = 0;
    int num_ret = 1;

    // Get function.
    int ltype = lua_getglobal(l, "input_note");
    if (ltype != LUA_TFUNCTION) { luaL_error(l, "Bad lua function: input_note"); };

    // Push arguments.
    lua_pushinteger(l, channel);
    num_args++;
    lua_pushinteger(l, notenum);
    num_args++;
    lua_pushnumber(l, volume);
    num_args++;

    // Do the actual call.
    int lstat = lua_pcall(l, num_args, num_ret, 0); // optionally throws
    if (lstat >= LUA_ERRRUN) { luaL_error(l, "lua_pcall() failed: %d", lstat); }

    // Get the results from the stack.
    int ret;
    if (lua_tointeger(l, -1)) { ret = lua_tointeger(l, -1); }
    else { luaL_error(l, "Return is not a int"); }
    lua_pop(l, num_ret); // Clean up results.
    return ret;
}

int luainterop_InputController(lua_State* l, int channel, int controller, int value)
{
    int num_args = 0;
    int num_ret = 1;

    // Get function.
    int ltype = lua_getglobal(l, "input_controller");
    if (ltype != LUA_TFUNCTION) { luaL_error(l, "Bad lua function: input_controller"); };

    // Push arguments.
    lua_pushinteger(l, channel);
    num_args++;
    lua_pushinteger(l, controller);
    num_args++;
    lua_pushinteger(l, value);
    num_args++;

    // Do the actual call.
    int lstat = lua_pcall(l, num_args, num_ret, 0); // optionally throws
    if (lstat >= LUA_ERRRUN) { luaL_error(l, "lua_pcall() failed: %d", lstat); }

    // Get the results from the stack.
    int ret;
    if (lua_tointeger(l, -1)) { ret = lua_tointeger(l, -1); }
    else { luaL_error(l, "Return is not a int"); }
    lua_pop(l, num_ret); // Clean up results.
    return ret;
}


//---------------- Call host functions from Lua -------------//

// Host export function: Script wants to log something.
// Lua arg: "level">Log level.
// Lua arg: "msg">Log message.
// Lua return: int Status.
// @param[in] l Internal lua state.
// @return Number of lua return values.
static int luainterop_Log(lua_State* l)
{
    // Get arguments
    int level;
    if (lua_isinteger(l, 1)) { level = lua_tointeger(l, 1); }
    else { luaL_error(l, "Bad arg type for level"); }
    char* msg;
    if (lua_isstring(l, 2)) { msg = lua_tostring(l, 2); }
    else { luaL_error(l, "Bad arg type for msg"); }

    // Do the work. One result.
    int ret = luainteropwork_Log(level, msg);
    lua_pushinteger(l, ret);
    return 1;
}

// Host export function: If volume is 0 note_off else note_on. If dur is 0 dur = note_on with dur = 0.1 (for drum/hit).
// Lua arg: "channel">Output channel handle
// Lua arg: "notenum">Note number
// Lua arg: "volume">Volume between 0.0 and 1.0
// Lua arg: "dur">Duration as bar.beat
// Lua return: int Status.
// @param[in] l Internal lua state.
// @return Number of lua return values.
static int luainterop_SendNote(lua_State* l)
{
    // Get arguments
    int channel;
    if (lua_isinteger(l, 1)) { channel = lua_tointeger(l, 1); }
    else { luaL_error(l, "Bad arg type for channel"); }
    int notenum;
    if (lua_isinteger(l, 2)) { notenum = lua_tointeger(l, 2); }
    else { luaL_error(l, "Bad arg type for notenum"); }
    double volume;
    if (lua_isnumber(l, 3)) { volume = lua_tonumber(l, 3); }
    else { luaL_error(l, "Bad arg type for volume"); }
    double dur;
    if (lua_isnumber(l, 4)) { dur = lua_tonumber(l, 4); }
    else { luaL_error(l, "Bad arg type for dur"); }

    // Do the work. One result.
    int ret = luainteropwork_SendNote(channel, notenum, volume, dur);
    lua_pushinteger(l, ret);
    return 1;
}

// Host export function: Send a controller immediately.
// Lua arg: "channel">Output channel handle
// Lua arg: "ctlr">Specific controller
// Lua arg: "value">Payload.
// Lua return: int Status
// @param[in] l Internal lua state.
// @return Number of lua return values.
static int luainterop_SendController(lua_State* l)
{
    // Get arguments
    int channel;
    if (lua_isinteger(l, 1)) { channel = lua_tointeger(l, 1); }
    else { luaL_error(l, "Bad arg type for channel"); }
    int ctlr;
    if (lua_isinteger(l, 2)) { ctlr = lua_tointeger(l, 2); }
    else { luaL_error(l, "Bad arg type for ctlr"); }
    int value;
    if (lua_isinteger(l, 3)) { value = lua_tointeger(l, 3); }
    else { luaL_error(l, "Bad arg type for value"); }

    // Do the work. One result.
    int ret = luainteropwork_SendController(channel, ctlr, value);
    lua_pushinteger(l, ret);
    return 1;
}


//---------------- Infrastructure -------------//

static const luaL_Reg function_map[] =
{
    { "log", luainterop_Log },
    { "send_note", luainterop_SendNote },
    { "send_controller", luainterop_SendController },
    { NULL, NULL }
};

static int luainterop_Open(lua_State* l)
{
    luaL_newlib(l, function_map);
    return 1;
}

void luainterop_Load(lua_State* l)
{
    luaL_requiref(l, "nebulua_api", luainterop_Open, true);
}
