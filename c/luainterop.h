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

// Lua export function: Called to initialize Nebulator stuff.
// @param[in] l Internal lua state.
// @return int status
int luainterop_Setup(lua_State* l);

// Lua export function: Called every fast timer increment.
// @param[in] l Internal lua state.
// @param[in] bar Which bar
// @param[in] beat Which beat
// @param[in] subbeat Which subbeat
// @return int status
int luainterop_Step(lua_State* l, int bar, int beat, int subbeat);

// Lua export function: Called when input arrives.
// @param[in] l Internal lua state.
// @param[in] channel Input channel handle
// @param[in] notenum Note number
// @param[in] volume Volume between 0.0 and 1.0.
// @return int status
int luainterop_InputNote(lua_State* l, int channel, int notenum, double volume);

// Lua export function: Called when input arrives.
// @param[in] l Internal lua state.
// @param[in] channel Input channel handle
// @param[in] controller Specific controller id
// @param[in] value Payload
// @return int status
int luainterop_InputController(lua_State* l, int channel, int controller, int value);


///// Infrastructure.
void luainterop_Load(lua_State* l);

#endif // LUAINTEROP_H
