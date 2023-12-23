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


//----------------------- Types -----------------------------//

// Internal device management.
typedef struct _MIDI_DEVICE
{
    char dev_name[MAXPNAMELEN];
    int dev_index; // from enumeration
    HMIDIIN hnd_in;
    HMIDIOUT hnd_out;
} MIDI_DEVICE;
#define MAX_MIDI_DEVS 16

// Midi events. TODO3 these should come from somewhere else?
typedef enum
{
    // Note Off
    NoteOff = 0x80,
    // Note On
    NoteOn = 0x90,
    // Key After-touch
    KeyAfterTouch = 0xA0,
    // Control change
    ControlChange = 0xB0,
    // Patch change
    PatchChange = 0xC0,
    // Channel after-touch
    ChannelAfterTouch = 0xD0,
    // Pitch wheel change
    PitchWheelChange = 0xE0,
    // Sysex message
    Sysex = 0xF0,
    // Eox (comes at end of a sysex message)
    Eox = 0xF7,
    // Timing clock (used when synchronization is required)
    TimingClock = 0xF8,
    // Start sequence
    StartSequence = 0xFA,
    // Continue sequence
    ContinueSequence = 0xFB,
    // Stop sequence
    StopSequence = 0xFC,
    // Auto-Sensing
    AutoSensing = 0xFE,
    // Meta-event
    MetaEvent = 0xFF,
} midi_event_t;


// //----------------------- Diagnostics -----------------------------//

// // Diagnostic utility.
// int common_DumpStack(lua_State* L, const char* info);

// // Diagnostic utility.
// int common_DumpTable(lua_State* L, const char* tbl_name);

// // // Diagnostic utility.
// // void common_LuaError(lua_State* L, const char* fn, int line, int err, const char* msg);

// // Check/log stack size.
// void common_EvalStack(lua_State* L, int expected);

// // #define EVAL_STACK(L, expected) {     int num = lua_gettop(L);     if (num != expected)     {         LOG_DEBUG("Expected %d stack but is %d", expected, num);     } }

// // // Helper macro to check then handle error.
// // #define CHK_LUA_ERROR(L, err, msg)  if(err >= LUA_ERRRUN) { common_LuaError(L, __FILE__, __LINE__, err, msg); }

// //----------------------- Utils -----------------------------//

// // // Interface to lua_pcall, but sets appropriate message function and C-signal handler. Used to run all chunks.
// // int common_DoCall(lua_State* L, int narg, int nres);



#endif // NEB_COMMON_H
