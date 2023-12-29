#ifndef LUAINTEROPWORK_H
#define LUAINTEROPWORK_H

#include "common.h"
#include "luainterop.h"

// Declaration of work functions for host functions called by lua.
// See interop_spec.lua for api. TODO1 autogen this and/or make an md of api.

#define VALIDATE(expr) luainterop_SyntaxError(#expr)

//--------------------------------------------------------//
int luainteropwork_Log(int level, char* msg);

//--------------------------------------------------------//
int luainteropwork_SetTempo(int bpm);

//--------------------------------------------------------//
int luainteropwork_CreateChannel(const char* device, int chan_num, int patch);

//--------------------------------------------------------//
int luainteropwork_SendNote(int hndchan, int notenum, double volume, double dur);

//--------------------------------------------------------//
int luainteropwork_SendController(int hndchan, int ctlr, int value);

#endif // LUAINTEROPWORK_H
