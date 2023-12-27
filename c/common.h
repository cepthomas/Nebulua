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
#define NEB_ERR_START           10
#define NEB_ERR_BAD_APP_ARG     11
#define NEB_ERR_BAD_LUA_ARG     12
#define NEB_ERR_BAD_MIDI_CFG    13
// #define NEB_ERR_BAD_MIDI_IN     13
// #define NEB_ERR_BAD_MIDI_OUT    14

#define assert(expr) // TODO1


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

// Examine status and log message if failed. Calls lua error function which doesn't return!
// @param[in] l Internal lua state.
// @param[in] stat Status to look at.
// @param[in] msg Info to add if not internal lua error.
// @return bool Pointless pass.
bool common_EvalStatus(lua_State* l, int stat, const char* msg);

// Convert a status to string.
// @param[in] err Status to examine.
// @return String or NULL if not valid.
const char* common_StatusToString(int err);

// TODO3 put these somewhere generic:
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
