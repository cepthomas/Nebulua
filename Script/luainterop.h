#pragma once
///// Warning - this file is created by gen_interop.lua - do not edit. /////

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

//============= C => Lua functions .h =============//

// Call to initialize Nebulator and composition.
// @param[in] l Internal lua state.
// @return const char* Script meta info if composition
const char* luainterop_Setup(lua_State* l);

// Called every fast timer increment aka tick.
// @param[in] l Internal lua state.
// @param[in] tick Current tick 0 => N
// @return int Unused
int luainterop_Step(lua_State* l, int tick);

// Called when midi input arrives.
// @param[in] l Internal lua state.
// @param[in] chan_hnd Input channel handle
// @param[in] note_num Note number 0 => 127
// @param[in] volume Volume 0.0 => 1.0
// @return int Unused
int luainterop_RcvNote(lua_State* l, int chan_hnd, int note_num, double volume);

// Called when midi input arrives.
// @param[in] l Internal lua state.
// @param[in] chan_hnd Input channel handle
// @param[in] controller Specific controller id 0 => 127
// @param[in] value Payload 0 => 127
// @return int Unused
int luainterop_RcvController(lua_State* l, int chan_hnd, int controller, int value);

// Host calls arbitrary lua function. For internal use only.
// @param[in] l Internal lua state.
// @param[in] cmd Specific command
// @param[in] arg Optional argument
// @return const char* Script return
const char* luainterop_NebCommand(lua_State* l, const char* cmd, const char* arg);


//============= Lua => C callback functions .h =============//

// Create an output midi channel.
// @param[in] l Internal lua state.
// @param[in] dev_name Midi device name
// @param[in] chan_num Midi channel number 1 => 16
// @param[in] patch Midi patch number 0 => 127
// @return Channel handle or 0 if invalid
int luainteropcb_CreateOutputChannel(lua_State* l, const char* dev_name, int chan_num, int patch);

// Create an input midi channel.
// @param[in] l Internal lua state.
// @param[in] dev_name Midi device name
// @param[in] chan_num Midi channel number 1 => 16 or 0 => all
// @return Channel handle or 0 if invalid
int luainteropcb_CreateInputChannel(lua_State* l, const char* dev_name, int chan_num);

// If volume is 0 note_off else note_on. If dur is 0 send note_on with dur = 1 (min for drum/hit).
// @param[in] l Internal lua state.
// @param[in] chan_hnd Output channel handle
// @param[in] note_num Note number
// @param[in] volume Volume 0.0 => 1.0
// @return Unused
int luainteropcb_SendNote(lua_State* l, int chan_hnd, int note_num, double volume);

// Send a controller immediately.
// @param[in] l Internal lua state.
// @param[in] chan_hnd Output channel handle
// @param[in] controller Specific controller 0 => 127
// @param[in] value Payload 0 => 127
// @return Unused
int luainteropcb_SendController(lua_State* l, int chan_hnd, int controller, int value);

// Script wants to log something.
// @param[in] l Internal lua state.
// @param[in] level Log level
// @param[in] msg Log message
// @return Unused
int luainteropcb_Log(lua_State* l, int level, const char* msg);

// Script wants to change tempo.
// @param[in] l Internal lua state.
// @param[in] bpm BPM 40 => 240
// @return Unused
int luainteropcb_SetTempo(lua_State* l, int bpm);

//============= Infrastructure .h =============//

/// Load Lua C lib.
void luainterop_Load(lua_State* l);

/// Return operation error or NULL if ok.
const char* luainterop_Error();
