#ifndef LUAINTEROP_H
#define LUAINTEROP_H

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

//---------------- Call lua functions from host -------------//

/// Lua export function: Called to initialize Nebulator stuff.
/// @param[in] l Internal lua state.
/// @return int LUA_STATUS
int luainterop_Setup(lua_State* l);

/// Lua export function: Called every fast timer increment aka tick.
/// @param[in] l Internal lua state.
/// @param[in] tick Current tick 0-N
/// @return int LUA_STATUS
int luainterop_Step(lua_State* l, int tick);

/// Lua export function: Called when input arrives.
/// @param[in] l Internal lua state.
/// @param[in] chan_hnd Input channel handle
/// @param[in] note_num Note number 0-MIDI_MAX
/// @param[in] volume Volume between 0.0 and 1.0
/// @return int LUA_STATUS
int luainterop_InputNote(lua_State* l, int chan_hnd, int note_num, double volume);

/// Lua export function: Called when input arrives.
/// @param[in] l Internal lua state.
/// @param[in] chan_hnd Input channel handle
/// @param[in] controller Specific controller id 0-MIDI_MAX
/// @param[in] value Payload 0-MIDI_MAX
/// @return int LUA_STATUS
int luainterop_InputController(lua_State* l, int chan_hnd, int controller, int value);


///// Infrastructure.
void luainterop_Load(lua_State* l);

#endif // LUAINTEROP_H
