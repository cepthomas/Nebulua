#ifndef NEB_COMMON_H
#define NEB_COMMON_H

#include <windows.h>
#include <stdio.h>
#include <stdlib.h>
#include <stdbool.h>
#include <string.h>
#include <time.h>
#include <unistd.h>
#include "lua.h"


//----------------------- App defs -----------------------------//

// App errors start after internal lua errors so they can be handled harmoniously.
#define NEB_OK                  LUA_OK
#define NEB_ERR_INTERNAL        10
#define NEB_ERR_BAD_APP_ARG     11
#define NEB_ERR_BAD_LUA_ARG     12
#define NEB_ERR_BAD_MIDI_CFG    13
#define NEB_ERR_SYNTAX          14
// #define NEB_ERR_BAD_MIDI_IN     13
// #define NEB_ERR_BAD_MIDI_OUT    14

/////////////////////////////////////////////////////////////

/*

TODO1 main: init/run errors (fatal)  check all p_EvalStatus() msgs.


TODOX luainteropwork (mainly) syntax errors - fatal
#define assertS(expr) luainterop_SyntaxError(#expr);


TODO1   luainterop.* add luainterop_SyntaxError(const char *fmt, ...)
{
    const char* sstat = common_StatusToString(NEB_ERR_SYNTAX);
    snprintf(err_msg, sizeof(err_msg) - 1,  )
}
>>> then check after interop call.


TODO1   luainterop.* syntax errors (fatal)   --- improve the messages, call luainterop_SyntaxError()
//---------------- Call lua functions from host -------------//
int luainterop_Setup(lua_State* l)
{
    // Get function.
    int ltype = lua_getglobal(l, "setup");
    if (ltype != LUA_TFUNCTION) { luaL_error(l, "Bad lua function: setup"); };

    // Do the actual call.
    int lstat = luaex_docall(l, num_args, num_ret);
    if (lstat >= LUA_ERRRUN) { luaL_error(l, "luaex_docall() failed: %d", lstat); }

    // Get the results from the stack.
    if (lua_tointeger(l, -1)) { ret = lua_tointeger(l, -1); }
    else { luaL_error(l, "Return is not a int"); }
}
//---------------- Call host functions from Lua -------------//
static int luainterop_CreateChannel(lua_State* l)
{
    // Get arguments
    char* device;
    if (lua_isstring(l, 1)) { device = lua_tostring(l, 1); }
    else { luaL_error(l, "Bad arg type for device"); }

    // Do the work. One result.
    int ret = luainteropwork_CreateChannel(device, channum, patch); >>> may fail if !NEB_OK
    lua_pushinteger(l, ret);
    return 1;
}
*/

// User syntax error - fatal.
// #define assertS(expr) //luaL_error(lua_State* l, const char *fmt, ...)

// internal fatal error
// #define assertF(expr)

// return failure, client deals with it.
// #define assertR(expr, ret)

// int common_DoError(lua_State* l, const char *fmt, ...);

//----------------------- Midi defs -----------------------------//

// Only 4/4 time supported.
#define BEATS_PER_BAR 4

// This app internal resolution.
#define INTERNAL_PPQ 32

// Conveniences.
#define SUBBEATS_PER_BEAT INTERNAL_PPQ
#define SUBEATS_PER_BAR SUBBEATS_PER_BEAT * BEATS_PER_BAR

// Midi caps.
#define MIDI_VAL_MIN 0

// Midi caps.
#define MIDI_VAL_MAX 127

// Midi events.
typedef enum
{
    // Channel events 0x80-0x8F
    MIDI_NOTE_OFF = 0x80,               // 2 - 1 byte pitch, followed by 1 byte velocity
    MIDI_NOTE_ON = 0x90,                // 2 - 1 byte pitch, followed by 1 byte velocity
    MIDI_KEY_AFTER_TOUCH = 0xA0,        // 2 - 1 byte pitch, 1 byte pressure (after-touch)
    MIDI_CONTROL_CHANGE = 0xB0,         // 2 - 1 byte parameter number, 1 byte setting
    MIDI_PATCH_CHANGE = 0xC0,           // 1 byte program selected
    MIDI_CHANNEL_AFTER_TOUCH = 0xD0,    // 1 byte channel pressure (after-touch)
    MIDI_PITCH_WHEEL_CHANGE = 0xE0,     // 2 bytes gives a 14 bit value, least significant 7 bits first
    // System events - no channel.
    MIDI_SYSEX = 0xF0,
    MIDI_EOX = 0xF7,
    MIDI_TIMING_CLOCK = 0xF8,
    MIDI_START_SEQUENCE = 0xFA,
    MIDI_CONTINUE_SEQUENCE = 0xFB,
    MIDI_STOP_SEQUENCE = 0xFC,
    MIDI_AUTO_SENSING = 0xFE,
    MIDI_META_EVENT = 0xFF,
} midi_event_t;


//----------------------- Publics -----------------------------//


// Convert a status to string.
// @param[in] err Status to examine.
// @return String or NULL if not valid.
const char* common_StatusToString(int err);

/// Safe convert a string to double.
/// @param str The input.
/// @param val The output.
/// @return Valid conversion.
bool common_StrToDouble(const char* str, double* val);

/// Safe convert a string to integer.
/// @param str The input.
/// @param val The output.
/// @return Valid conversion.
bool common_StrToInt(const char* str, int* val);


#endif // NEB_COMMON_H
