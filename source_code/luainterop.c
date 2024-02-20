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
#include "luaex.h"


#if defined(_MSC_VER)
// Ignore some generated code warnings
#pragma warning( push )
#pragma warning( disable : 6001 4244 4703 )
#endif

//---------------- Call lua functions from host -------------//

//--------------------------------------------------------//
int luainterop_Setup(lua_State* l, int* ret)
{
    int stat = LUA_OK;
    int num_args = 0;
    int num_ret = 1;

    // Get function.
    int ltype = lua_getglobal(l, "setup");
    if (ltype != LUA_TFUNCTION) { stat = INTEROP_BAD_FUNC_NAME; }

    if (stat == LUA_OK)
    {
        // Push arguments. No error checking required.

        // Do the actual call. If script fails, luaex_docall adds the script stack to the error object.
        stat = luaex_docall(l, num_args, num_ret);
    }

    if (stat == LUA_OK)
    {
        // Get the results from the stack.
        if (lua_tointeger(l, -1)) { *ret = lua_tointeger(l, -1); }
        else { stat = INTEROP_BAD_RET_TYPE; }
        lua_pop(l, num_ret); // Clean up results.
    }

    return stat;
}

//--------------------------------------------------------//
int luainterop_Step(lua_State* l, int tick, int* ret)
{
    int stat = LUA_OK;
    int num_args = 0;
    int num_ret = 1;

    // Get function.
    int ltype = lua_getglobal(l, "step");
    if (ltype != LUA_TFUNCTION) { stat = INTEROP_BAD_FUNC_NAME; }

    if (stat == LUA_OK)
    {
        // Push arguments. No error checking required.
        lua_pushinteger(l, tick);
        num_args++;

        // Do the actual call. If script fails, luaex_docall adds the script stack to the error object.
        stat = luaex_docall(l, num_args, num_ret);
    }

    if (stat == LUA_OK)
    {
        // Get the results from the stack.
        if (lua_tointeger(l, -1)) { *ret = lua_tointeger(l, -1); }
        else { stat = INTEROP_BAD_RET_TYPE; }
        lua_pop(l, num_ret); // Clean up results.
    }

    return stat;
}

//--------------------------------------------------------//
int luainterop_InputNote(lua_State* l, int chan_hnd, int note_num, double volume, int* ret)
{
    int stat = LUA_OK;
    int num_args = 0;
    int num_ret = 1;

    // Get function.
    int ltype = lua_getglobal(l, "input_note");
    if (ltype != LUA_TFUNCTION) { stat = INTEROP_BAD_FUNC_NAME; }

    if (stat == LUA_OK)
    {
        // Push arguments. No error checking required.
        lua_pushinteger(l, chan_hnd);
        num_args++;
        lua_pushinteger(l, note_num);
        num_args++;
        lua_pushnumber(l, volume);
        num_args++;

        // Do the actual call. If script fails, luaex_docall adds the script stack to the error object.
        stat = luaex_docall(l, num_args, num_ret);
    }

    if (stat == LUA_OK)
    {
        // Get the results from the stack.
        if (lua_tointeger(l, -1)) { *ret = lua_tointeger(l, -1); }
        else { stat = INTEROP_BAD_RET_TYPE; }
        lua_pop(l, num_ret); // Clean up results.
    }

    return stat;
}

//--------------------------------------------------------//
int luainterop_InputController(lua_State* l, int chan_hnd, int controller, int value, int* ret)
{
    int stat = LUA_OK;
    int num_args = 0;
    int num_ret = 1;

    // Get function.
    int ltype = lua_getglobal(l, "input_controller");
    if (ltype != LUA_TFUNCTION) { stat = INTEROP_BAD_FUNC_NAME; }

    if (stat == LUA_OK)
    {
        // Push arguments. No error checking required.
        lua_pushinteger(l, chan_hnd);
        num_args++;
        lua_pushinteger(l, controller);
        num_args++;
        lua_pushinteger(l, value);
        num_args++;

        // Do the actual call. If script fails, luaex_docall adds the script stack to the error object.
        stat = luaex_docall(l, num_args, num_ret);
    }

    if (stat == LUA_OK)
    {
        // Get the results from the stack.
        if (lua_tointeger(l, -1)) { *ret = lua_tointeger(l, -1); }
        else { stat = INTEROP_BAD_RET_TYPE; }
        lua_pop(l, num_ret); // Clean up results.
    }

    return stat;
}


//---------------- Call host functions from Lua -------------//

//--------------------------------------------------------//
// Host export function: Create an output midi channel.
// @param[in] l Internal lua state.
// @return Number of lua return values.
// Lua arg: dev_name Midi device name
// Lua arg: chan_num Midi channel number 1 => 16
// Lua arg: patch Midi patch number 0 => 127
// Lua return: int Channel handle or 0 if invalid
static int luainterop_CreateOutputChannel(lua_State* l)
{
    // Get arguments
    const char* dev_name;
    if (lua_isstring(l, 1)) { dev_name = lua_tostring(l, 1); }
    else { luaL_error(l, "Bad arg type for dev_name"); }
    int chan_num;
    if (lua_isinteger(l, 2)) { chan_num = lua_tointeger(l, 2); }
    else { luaL_error(l, "Bad arg type for chan_num"); }
    int patch;
    if (lua_isinteger(l, 3)) { patch = lua_tointeger(l, 3); }
    else { luaL_error(l, "Bad arg type for patch"); }

    // Do the work. One result.
    int ret = luainteropwork_CreateOutputChannel(dev_name, chan_num, patch);
    lua_pushinteger(l, ret);
    return 1;
}

//--------------------------------------------------------//
// Host export function: Create an input midi channel.
// @param[in] l Internal lua state.
// @return Number of lua return values.
// Lua arg: dev_name Midi device name
// Lua arg: chan_num Midi channel number 1 => 16
// Lua return: int Channel handle or 0 if invalid
static int luainterop_CreateInputChannel(lua_State* l)
{
    // Get arguments
    const char* dev_name;
    if (lua_isstring(l, 1)) { dev_name = lua_tostring(l, 1); }
    else { luaL_error(l, "Bad arg type for dev_name"); }
    int chan_num;
    if (lua_isinteger(l, 2)) { chan_num = lua_tointeger(l, 2); }
    else { luaL_error(l, "Bad arg type for chan_num"); }

    // Do the work. One result.
    int ret = luainteropwork_CreateInputChannel(dev_name, chan_num);
    lua_pushinteger(l, ret);
    return 1;
}

//--------------------------------------------------------//
// Host export function: Script wants to log something.
// @param[in] l Internal lua state.
// @return Number of lua return values.
// Lua arg: level Log level
// Lua arg: msg Log message
// Lua return: int LUA_STATUS
static int luainterop_Log(lua_State* l)
{
    // Get arguments
    int level;
    if (lua_isinteger(l, 1)) { level = lua_tointeger(l, 1); }
    else { luaL_error(l, "Bad arg type for level"); }
    const char* msg;
    if (lua_isstring(l, 2)) { msg = lua_tostring(l, 2); }
    else { luaL_error(l, "Bad arg type for msg"); }

    // Do the work. One result.
    int ret = luainteropwork_Log(level, msg);
    lua_pushinteger(l, ret);
    return 1;
}

//--------------------------------------------------------//
// Host export function: Script wants to change tempo.
// @param[in] l Internal lua state.
// @return Number of lua return values.
// Lua arg: bpm BPM 40 => 240
// Lua return: int LUA_STATUS
static int luainterop_SetTempo(lua_State* l)
{
    // Get arguments
    int bpm;
    if (lua_isinteger(l, 1)) { bpm = lua_tointeger(l, 1); }
    else { luaL_error(l, "Bad arg type for bpm"); }

    // Do the work. One result.
    int ret = luainteropwork_SetTempo(bpm);
    lua_pushinteger(l, ret);
    return 1;
}

//--------------------------------------------------------//
// Host export function: If volume is 0 note_off else note_on. If dur is 0 send note_on with dur = 1 (for drum/hit).
// @param[in] l Internal lua state.
// @return Number of lua return values.
// Lua arg: chan_hnd Output channel handle
// Lua arg: note_num Note number
// Lua arg: volume Volume 0.0 => 1.0
// Lua return: int LUA_STATUS
static int luainterop_SendNote(lua_State* l)
{
    // Get arguments
    int chan_hnd;
    if (lua_isinteger(l, 1)) { chan_hnd = lua_tointeger(l, 1); }
    else { luaL_error(l, "Bad arg type for chan_hnd"); }
    int note_num;
    if (lua_isinteger(l, 2)) { note_num = lua_tointeger(l, 2); }
    else { luaL_error(l, "Bad arg type for note_num"); }
    double volume;
    if (lua_isnumber(l, 3)) { volume = lua_tonumber(l, 3); }
    else { luaL_error(l, "Bad arg type for volume"); }

    // Do the work. One result.
    int ret = luainteropwork_SendNote(chan_hnd, note_num, volume);
    lua_pushinteger(l, ret);
    return 1;
}

//--------------------------------------------------------//
// Host export function: Send a controller immediately.
// @param[in] l Internal lua state.
// @return Number of lua return values.
// Lua arg: chan_hnd Output channel handle
// Lua arg: controller Specific controller 0 => 127
// Lua arg: value Payload 0 => 127
// Lua return: int LUA_STATUS
static int luainterop_SendController(lua_State* l)
{
    // Get arguments
    int chan_hnd;
    if (lua_isinteger(l, 1)) { chan_hnd = lua_tointeger(l, 1); }
    else { luaL_error(l, "Bad arg type for chan_hnd"); }
    int controller;
    if (lua_isinteger(l, 2)) { controller = lua_tointeger(l, 2); }
    else { luaL_error(l, "Bad arg type for controller"); }
    int value;
    if (lua_isinteger(l, 3)) { value = lua_tointeger(l, 3); }
    else { luaL_error(l, "Bad arg type for value"); }

    // Do the work. One result.
    int ret = luainteropwork_SendController(chan_hnd, controller, value);
    lua_pushinteger(l, ret);
    return 1;
}


//---------------- Infrastructure -------------//

static const luaL_Reg function_map[] =
{
    { "create_output_channel", luainterop_CreateOutputChannel },
    { "create_input_channel", luainterop_CreateInputChannel },
    { "log", luainterop_Log },
    { "set_tempo", luainterop_SetTempo },
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
    luaL_requiref(l, "host_api", luainterop_Open, true);
}

#if defined(_MSC_VER)
#pragma warning( pop )
#endif
