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

#define INTEROP_BAD_FUNC_NAME 10
#define INTEROP_BAD_RET_TYPE  11
#define MAX_STRING 100

//---------------- Call lua functions from host -------------//

/// Lua export function: Called to initialize Nebulator stuff.
/// @param[in] l Internal lua state.
/// @param[out] int* Total length of composition - 0 means no composition/free-form
/// @return status
int luainterop_Setup(lua_State* l, int* ret);

/// Lua export function: Called every fast timer increment aka tick.
/// @param[in] l Internal lua state.
/// @param[in] tick Current tick 0 => N
/// @param[out] int* NEB_XX status
/// @return status
int luainterop_Step(lua_State* l, int tick, int* ret);

/// Lua export function: Called when input arrives.
/// @param[in] l Internal lua state.
/// @param[in] chan_hnd Input channel handle
/// @param[in] note_num Note number 0 => 127
/// @param[in] volume Volume 0.0 => 1.0
/// @param[out] int* NEB_XX status
/// @return status
int luainterop_InputNote(lua_State* l, int chan_hnd, int note_num, double volume, int* ret);

/// Lua export function: Called when input arrives.
/// @param[in] l Internal lua state.
/// @param[in] chan_hnd Input channel handle
/// @param[in] controller Specific controller id 0 => 127
/// @param[in] value Payload 0 => 127
/// @param[out] int* NEB_XX status
/// @return status
int luainterop_InputController(lua_State* l, int chan_hnd, int controller, int value, int* ret);


//---------------- Work functions for lua call host -------------//

/// Create an output midi channel.
/// @param[in] dev_name Midi device name
/// @param[in] chan_num Midi channel number 1 => 16
/// @param[in] patch Midi patch number 0 => 127
/// @return Channel handle or 0 if invalid
int luainteropwork_CreateOutputChannel(char* dev_name, int chan_num, int patch);

/// Create an input midi channel.
/// @param[in] dev_name Midi device name
/// @param[in] chan_num Midi channel number 1 => 16
/// @return Channel handle or 0 if invalid
int luainteropwork_CreateInputChannel(char* dev_name, int chan_num);

/// Script wants to log something.
/// @param[in] level Log level
/// @param[in] msg Log message
/// @return NEB_XX status
int luainteropwork_Log(int level, char* msg);

/// Script wants to change tempo.
/// @param[in] bpm BPM 40 => 240
/// @return NEB_XX status
int luainteropwork_SetTempo(int bpm);

/// If volume is 0 note_off else note_on. If dur is 0 send note_on with dur = 1 (for drum/hit).
/// @param[in] chan_hnd Output channel handle
/// @param[in] note_num Note number
/// @param[in] volume Volume 0.0 => 1.0
/// @return NEB_XX status
int luainteropwork_SendNote(int chan_hnd, int note_num, double volume);

/// Send a controller immediately.
/// @param[in] chan_hnd Output channel handle
/// @param[in] controller Specific controller 0 => 127
/// @param[in] value Payload 0 => 127
/// @return NEB_XX status
int luainteropwork_SendController(int chan_hnd, int controller, int value);

//---------------- Infrastructure ----------------------//

void luainterop_Load(lua_State* l);

#endif // LUAINTEROP_H
