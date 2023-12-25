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

//----------------------- Defs -----------------------------//

// App errors start after internal lua errors so they can be handled harmoniously.
#define NEB_OK                  LUA_OK
#define NEB_ERR_START           10
#define NEB_ERR_BAD_APP_ARG     11
#define NEB_ERR_BAD_LUA_ARG     12
#define NEB_ERR_BAD_MIDI_IN     13
#define NEB_ERR_BAD_MIDI_OUT    14


// Only 4/4 time supported.
#define BEATS_PER_BAR 4

// This app internal resolution.
#define INTERNAL_PPQ 32

// Conveniences.
#define SUBBEATS_PER_BEAT INTERNAL_PPQ
#define SUBEATS_PER_BAR SUBBEATS_PER_BEAT * BEATS_PER_BAR

// Arbitrary cap.
#define MIDI_DEVICES 4

// Midi cap per device. Note midi is 1-based.
#define MIDI_CHANNELS 16

// Midi caps.
#define MIDI_MIN 0

// Midi caps.
#define MIDI_MAX 127


//----------------------- Types -----------------------------//

// Internal device management.
typedef struct _MIDI_DEVICE
{
    char dev_name[MAXPNAMELEN];
    int dev_index; // from system enumeration
    bool channels[MIDI_CHANNELS]; // 0-based
    HMIDIIN hnd_in;
    HMIDIOUT hnd_out;
} MIDI_DEVICE;

// Midi events.
typedef enum
{
    MIDI_NOTE_OFF = 0X80,
    MIDI_NOTE_ON = 0X90,
    MIDI_KEY_AFTER_TOUCH = 0XA0,
    MIDI_CONTROL_CHANGE = 0XB0,
    MIDI_PATCH_CHANGE = 0XC0,
    MIDI_CHANNEL_AFTER_TOUCH = 0XD0,
    MIDI_PITCH_WHEEL_CHANGE = 0XE0,
    MIDI_SYSEX = 0XF0,
    MIDI_EOX = 0XF7,
    MIDI_TIMING_CLOCK = 0XF8,
    MIDI_START_SEQUENCE = 0XFA,
    MIDI_CONTINUE_SEQUENCE = 0XFB,
    MIDI_STOP_SEQUENCE = 0XFC,
    MIDI_AUTO_SENSING = 0XFE,
    MIDI_META_EVENT = 0XFF,
} midi_event_t;


//----------------------- Publics -----------------------------//

// Examine status and log message if failed. Calls lua error function which doesn't return!
// @param[in] l Internal lua state.
// @param[in] stat Status to look at.
// @param[in] msg Info to add if not internal lua error.
// @return bool Pontless pass.
bool common_EvalStatus(lua_State* l, int stat, const char* msg);

// Convert a status to string.
// @param[in] err Status to examine.
// @return String or NULL if not valid.
const char* common_StatusToString(int err);


#endif // NEB_COMMON_H
