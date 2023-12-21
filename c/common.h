#ifndef NEB_COMMON_H
#define NEB_COMMON_H

#include <windows.h>
#include <stdio.h>
#include <string.h>
#include <time.h>
#include <stdlib.h>
#include <unistd.h>
#include "lua.h"
#include "lualib.h"
#include "lauxlib.h"
#include "luainterop.h"
#include "luainteropwork.h"
#include "logger.h"


// Diagnostic utility.
int common_DumpStack(lua_State* L, const char* info);

// Diagnostic utility.
int common_DumpTable(lua_State* L, const char* tbl_name);

// Diagnostic utility.
void common_LuaError(lua_State* L, const char* fn, int line, int err, const char* format, ...);

// Helper macro to check/log stack size.
#define EVAL_STACK(L, expected)  { int num = lua_gettop(L); if (num != expected) { LOG_DEBUG("Expected %d stack but is %d", expected, num); } }

// Helper macro to check then handle error.
#define CHK_LUA_ERROR(L, err, fmt, ...)  if(err >= LUA_ERRRUN) { common_LuaError(L, __FILE__, __LINE__, err, fmt, ##__VA_ARGS__); }


// Internal device management.
typedef struct _MIDI_DEVICE
{
    char dev_name[MAXPNAMELEN];
    int dev_index; // from enumeration
    HMIDIIN hnd_in;
    HMIDIOUT hnd_out;
} MIDI_DEVICE;
#define MAX_MIDI_DEVS 16


#endif // NEB_COMMON_H
