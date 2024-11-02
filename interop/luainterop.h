#ifndef LUAINTEROP_H
#define LUAINTEROP_H

///// Warning - this file is created by gen_interop.lua, do not edit. /////

#include <stdbool.h>

#ifdef __cplusplus
#include "lua.hpp"
extern "C" {
#include "luaex.h"
};
#else
#include "lua.h"
#include "luaex.h"
#endif

//---------------- Call lua functions from host -------------//

/// Host call lua: Call to initialize Nebulator and composition.
/// @param[in] l Internal lua state.
/// @return int Unused
int luainterop_Setup(lua_State* l);

/// Host call lua: Called every fast timer increment aka tick.
/// @param[in] l Internal lua state.
/// @param[in] tick Current tick 0 => N
/// @return int Unused
int luainterop_Step(lua_State* l, int tick);

/// Host call lua: Called when midi input arrives.
/// @param[in] l Internal lua state.
/// @param[in] chan_hnd Input channel handle
/// @param[in] note_num Note number 0 => 127
/// @param[in] volume Volume 0.0 => 1.0
/// @return int Unused
int luainterop_RcvNote(lua_State* l, int chan_hnd, int note_num, double volume);

/// Host call lua: Called when midi input arrives.
/// @param[in] l Internal lua state.
/// @param[in] chan_hnd Input channel handle
/// @param[in] controller Specific controller id 0 => 127
/// @param[in] value Payload 0 => 127
/// @return int Unused
int luainterop_RcvController(lua_State* l, int chan_hnd, int controller, int value);

/// Host call lua: Host calls arbitrary lua function. For internal use only.
/// @param[in] l Internal lua state.
/// @param[in] cmd Specific command
/// @param[in] arg Optional argument
/// @return const char* Script return
const char* luainterop_NebCommand(lua_State* l, const char* cmd, const char* arg);


//---------------- Work functions for lua call host -------------//

/// Create an output midi channel.
/// @param[in] l Internal lua state.
/// @param[in] dev_name Midi device name
/// @param[in] chan_num Midi channel number 1 => 16
/// @param[in] patch Midi patch number 0 => 127
/// @return Channel handle or 0 if invalid
int luainteropwork_CreateOutputChannel(lua_State* l, const char* dev_name, int chan_num, int patch);

/// Create an input midi channel.
/// @param[in] l Internal lua state.
/// @param[in] dev_name Midi device name
/// @param[in] chan_num Midi channel number 1 => 16
/// @return Channel handle or 0 if invalid
int luainteropwork_CreateInputChannel(lua_State* l, const char* dev_name, int chan_num);

/// Script wants to log something.
/// @param[in] l Internal lua state.
/// @param[in] level Log level
/// @param[in] msg Log message
/// @return Unused
int luainteropwork_Log(lua_State* l, int level, const char* msg);

/// Script wants to change tempo.
/// @param[in] l Internal lua state.
/// @param[in] bpm BPM 40 => 240
/// @return Unused
int luainteropwork_SetTempo(lua_State* l, int bpm);

/// If volume is 0 note_off else note_on. If dur is 0 send note_on with dur = 1 (min for drum/hit).
/// @param[in] l Internal lua state.
/// @param[in] chan_hnd Output channel handle
/// @param[in] note_num Note number
/// @param[in] volume Volume 0.0 => 1.0
/// @return Unused
int luainteropwork_SendNote(lua_State* l, int chan_hnd, int note_num, double volume);

/// Send a controller immediately.
/// @param[in] l Internal lua state.
/// @param[in] chan_hnd Output channel handle
/// @param[in] controller Specific controller 0 => 127
/// @param[in] value Payload 0 => 127
/// @return Unused
int luainteropwork_SendController(lua_State* l, int chan_hnd, int controller, int value);

//---------------- Infrastructure ----------------------//

/// Load Lua C lib.
void luainterop_Load(lua_State* l);

/// Return operation error or NULL if ok.
const char* luainterop_Error();

#endif // LUAINTEROP_H
