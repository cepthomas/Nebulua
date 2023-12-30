#ifndef LUAINTEROPWORK_H
#define LUAINTEROPWORK_H

///// Warning - this file is created by gen_interop.lua, do not edit. /////

#include "common.h"
#include "luainterop.h"

//---------------- Work functions for interop -------------//

//--------------------------------------------------------//
int luainteropwork_CreateChannel(lua_State* l, char* device, int channum, int patch);

//--------------------------------------------------------//
int luainteropwork_Log(lua_State* l, int level, char* msg);

//--------------------------------------------------------//
int luainteropwork_SetTempo(lua_State* l, int bpm);

//--------------------------------------------------------//
int luainteropwork_SendNote(lua_State* l, int hndchan, int notenum, double volume, double dur);

//--------------------------------------------------------//
int luainteropwork_SendController(lua_State* l, int hndchan, int controller, int value);

#endif // LUAINTEROPWORK_H
