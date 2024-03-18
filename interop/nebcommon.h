#ifndef NEBCOMMON_H
#define NEBCOMMON_H

#include "lua.hpp"

#define MAX_PATH 260 // borrowed from windows.h


//----------------------- Status -----------------------//

///// App errors start after internal lua errors so they can be handled harmoniously.
#define NEB_OK                   0  // synonym for LUA_OK and CBOT_ERR_NO_ERR
#define NEB_ERR_INTERNAL        10
#define NEB_ERR_BAD_CLI_ARG     11
#define NEB_ERR_BAD_LUA_ARG     12
#define NEB_ERR_BAD_MIDI_CFG    13
#define NEB_ERR_SYNTAX          14
#define NEB_ERR_MIDI_TX         15
#define NEB_ERR_MIDI_RX         16

#define INTEROP_BAD_FUNC_NAME 200
#define INTEROP_BAD_RET_TYPE  201


//----------------------- Timing -----------------------------//

///// Only 4/4 time supported.
//#define BEATS_PER_BAR 4
//
///// Our resolution = 32nd note. aka midi DeltaTicksPerQuarterNote.
//#define SUBS_PER_BEAT 8
//
///// Convenience.
//#define SUBS_PER_BAR (SUBS_PER_BEAT * BEATS_PER_BAR)
//
///// The bar number.
//#define BAR(tick) (tick / SUBS_PER_BAR)
//
///// The beat number in the bar.
//#define BEAT(tick) (tick / SUBS_PER_BEAT % BEATS_PER_BAR)
//
///// The sub in the beat.
//#define SUB(tick) (tick % SUBS_PER_BEAT)
//
///// Arbitrary setting.
//#define MAX_SECTIONS 32


//----------------------- Midi -----------------------------//

// Midi caps.
#define MIDI_VAL_MIN 0

// Midi caps.
#define MIDI_VAL_MAX 127

///// Midi events.
//typedef enum
//{
//    // Channel events 0x80-0x8F
//    MIDI_NOTE_OFF = 0x80,               // 2 - 1 byte pitch, followed by 1 byte velocity
//    MIDI_NOTE_ON = 0x90,                // 2 - 1 byte pitch, followed by 1 byte velocity
//    MIDI_KEY_AFTER_TOUCH = 0xA0,        // 2 - 1 byte pitch, 1 byte pressure (after-touch)
//    MIDI_CONTROL_CHANGE = 0xB0,         // 2 - 1 byte parameter number, 1 byte setting
//    MIDI_PATCH_CHANGE = 0xC0,           // 1 byte program selected
//    MIDI_CHANNEL_AFTER_TOUCH = 0xD0,    // 1 byte channel pressure (after-touch)
//    MIDI_PITCH_WHEEL_CHANGE = 0xE0,     // 2 bytes gives a 14 bit value, least significant 7 bits first
//    // System events - no channel.
//    MIDI_SYSEX = 0xF0,
//    MIDI_EOX = 0xF7,
//    MIDI_TIMING_CLOCK = 0xF8,
//    MIDI_START_SEQUENCE = 0xFA,
//    MIDI_CONTINUE_SEQUENCE = 0xFB,
//    MIDI_STOP_SEQUENCE = 0xFC,
//    MIDI_AUTO_SENSING = 0xFE,
//    MIDI_META_EVENT = 0xFF,
//} midi_event_t;
//

//----------------------- Utilities -----------------------------//

/// <summary>
/// 
/// </summary>
/// <param name="input"></param>
/// <param name="output"></param>
/// <param name="len"></param>
void ToCString(System::String^ input, char* output, int len);

/// <summary>
/// 
/// </summary>
/// <param name="input"></param>
/// <returns></returns>
System::String^ ToCliString(const char* input);



/// Convert a status to string.
/// @param[in] mstat Midi status to examine
/// @return String empty if status is ok
//const char* nebcommon_FormatMidiStatus(int mstat);

/// Convert a string bar time to absolute position.
/// @param[in] sbt time string can be "1.2.3" or "1.2" or "1".
/// @return int tick
//int nebcommon_ParseBarTime(const char* sbt);

/// Convert a position to string bar time.
/// @param[in] position
/// @return string
//const char* nebcommon_FormatBarTime(int position);

/// Checks stat and returns an error message if it failed.
/// @param[in] l lua context
/// @param[in] stat to be tested
/// @param[in] format extra info to add if fail
/// @return if fail the error string else NULL
System::String^ nebcommon_EvalStatus(lua_State* l, int stat, System::String^ msg);


#endif // NEBCOMMON_H
