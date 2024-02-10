#ifndef LUAINTEROPWORK_H
#define LUAINTEROPWORK_H

///// Warning - this file is created by gen_interop.lua, do not edit. /////

#include "luainterop.h"

//---------------- Work functions for interop -------------//

/// Create an output midi channel.
/// @param[in] l Internal lua state.
/// @param[in] dev_name Midi device name
/// @param[in] chan_num Midi channel number 1-16
/// @param[in] patch Midi patch number
/// @return Channel handle or 0 if invalid
int luainteropwork_CreateOutputChannel(lua_State* l, const char* dev_name, int chan_num, int patch);

/// Create an input midi channel.
/// @param[in] l Internal lua state.
/// @param[in] dev_name Midi device name
/// @param[in] chan_num Midi channel number 1-16
/// @return Channel handle or 0 if invalid
int luainteropwork_CreateInputChannel(lua_State* l, const char* dev_name, int chan_num);

/// Script wants to log something.
/// @param[in] l Internal lua state.
/// @param[in] level Log level
/// @param[in] msg Log message
/// @return LUA_STATUS
int luainteropwork_Log(lua_State* l, int level, const char* msg);

/// Script wants to change tempo.
/// @param[in] l Internal lua state.
/// @param[in] bpm BPM
/// @return LUA_STATUS
int luainteropwork_SetTempo(lua_State* l, int bpm);

/// If volume is 0 note_off else note_on. If dur is 0 send note_on with dur = 1 (for drum/hit).
/// @param[in] l Internal lua state.
/// @param[in] chan_hnd Output channel handle
/// @param[in] note_num Note number
/// @param[in] volume Volume between 0.0 and 1.0
/// @param[in] dur Duration in subbeats
/// @return LUA_STATUS
int luainteropwork_SendNote(lua_State* l, int chan_hnd, int note_num, double volume, int dur);

/// Send a controller immediately.
/// @param[in] l Internal lua state.
/// @param[in] chan_hnd Output channel handle
/// @param[in] controller Specific controller
/// @param[in] value Payload.
/// @return LUA_STATUS
int luainteropwork_SendController(lua_State* l, int chan_hnd, int controller, int value);

#endif // LUAINTEROPWORK_H
