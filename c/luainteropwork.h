#ifndef LUAINTEROPWORK_H
#define LUAINTEROPWORK_H

#include "common.h"
#include "luainterop.h"

// Declaration of work functions for host functions called by lua.
// See interop_spec.lua for api. TODO1 autogen this and/or make an md of api.


//--------------------------------------------------------//
int luainteropwork_Log(lua_State* l, int level, char* msg);

//--------------------------------------------------------//
int luainteropwork_SetTempo(lua_State* l, int bpm);

//--------------------------------------------------------//
int luainteropwork_CreateChannel(lua_State* l, const char* device, int chan_num, int patch);

//--------------------------------------------------------//
int luainteropwork_SendNote(lua_State* l, int hndchan, int notenum, double volume, double dur);

//--------------------------------------------------------//
int luainteropwork_SendController(lua_State* l, int hndchan, int ctlr, int value);

#endif // LUAINTEROPWORK_H
