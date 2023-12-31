#ifndef LUAINTEROPWORK_H
#define LUAINTEROPWORK_H

///// Warning - this file is created by gen_interop.lua, do not edit. /////

#include "luainterop.h"

//---------------- Work functions for interop -------------//

/// Create an in or out midi channel.
/// @param[in] l Internal lua state.
/// @param[in] device Midi device name
/// @param[in] channum Midi channel number 1-16
/// @param[in] patch Midi patch number (output channel only)
/// @return Channel handle or 0 if invalid
int luainteropwork_CreateChannel(lua_State* l, char* device, int channum, int patch);

/// Script wants to log something.
/// @param[in] l Internal lua state.
/// @param[in] level Log level
/// @param[in] msg Log message
/// @return LUA_STATUS
int luainteropwork_Log(lua_State* l, int level, char* msg);

/// Script wants to change tempo.
/// @param[in] l Internal lua state.
/// @param[in] bpm BPM
/// @return LUA_STATUS
int luainteropwork_SetTempo(lua_State* l, int bpm);

/// If volume is 0 note_off else note_on. If dur is 0 send note_on with dur = 0.1 (for drum/hit).
/// @param[in] l Internal lua state.
/// @param[in] hndchan Output channel handle
/// @param[in] notenum Note number
/// @param[in] volume Volume between 0.0 and 1.0
/// @param[in] dur Duration as bar.beat
/// @return LUA_STATUS
int luainteropwork_SendNote(lua_State* l, int hndchan, int notenum, double volume, double dur);

/// Send a controller immediately.
/// @param[in] l Internal lua state.
/// @param[in] hndchan Output channel handle
/// @param[in] controller Specific controller
/// @param[in] value Payload.
/// @return LUA_STATUS
int luainteropwork_SendController(lua_State* l, int hndchan, int controller, int value);

#endif // LUAINTEROPWORK_H
