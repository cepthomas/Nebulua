#ifndef NEBCOMMON_H
#define NEBCOMMON_H

// system
#include <stdbool.h>
// lua
#include "lua.h"
// cbot
// application


//----------------------- Definitions -----------------------//

///// App errors start after internal lua errors so they can be handled harmoniously.
#define NEB_OK                   0  // synonym for LUA_OK and CBOT_ERR_NO_ERR
#define NEB_ERR_INTERNAL        10
#define NEB_ERR_BAD_CLI_ARG     11
#define NEB_ERR_BAD_LUA_ARG     12
#define NEB_ERR_BAD_MIDI_CFG    13
#define NEB_ERR_SYNTAX          14
#define NEB_ERR_MIDI            15


//----------------------- Timing -----------------------------//

/// Only 4/4 time supported.
#define BEATS_PER_BAR 4

/// Internal/app resolution aka DeltaTicksPerQuarterNote. was 32?
#define INTERNAL_PPQ 8

/// Convenience.
#define SUBBEATS_PER_BEAT INTERNAL_PPQ

/// Convenience.
#define SUBBEATS_PER_BAR (SUBBEATS_PER_BEAT * BEATS_PER_BAR)

/// Total.
#define TOTAL_BEATS(subbeats) (subbeats / SUBBEATS_PER_BEAT)

/// The bar number.
#define BAR(subbeats) (subbeats / SUBBEATS_PER_BAR)

/// The beat number in the bar.
#define BEAT(subbeats) (subbeats / SUBBEATS_PER_BEAT % BEATS_PER_BAR)

/// The subbeat in the beat.
#define SUBBEAT(subbeats) (subbeats % SUBBEATS_PER_BEAT)


//----------------------- Utilities -----------------------------//

/// Top level error handler for nebulua status. Logs and calls luaL_error() which doesn't return.
/// @param[in] l Lua context
/// @param[in] stat Status to examine
/// @param[in] format Standard printf
/// @return String empty if status is ok
bool nebcommon_EvalStatus(lua_State* l, int stat, const char* format, ...);

/// Convert a status to string.
/// @param[in] mstat Midi status to examine
/// @return String empty if status is ok
const char* nebcommon_FormatMidiStatus(int mstat);

/// Convert a string bar time to absolute position.
/// @param[in] sbt time string can be "1.2.3" or "1.2" or "1".
/// @return int subbeats
int nebcommon_ParseBarTime(const char* sbt);

/// Convert a position to string bar time.
/// @param[in] position
/// @return string
const char* nebcommon_FormatBarTime(int position);

/// Safe convert a string to double with bounds checking.
/// @param[in] str to parse
/// @param[out] val answer
/// @param[in] min limit inclusive
/// @param[in] max limit inclusive
/// @return success
bool nebcommon_ParseDouble(const char* str, double* val, double min, double max);

/// Safe convert a string to int with bounds checking.
/// @param[in] str to parse
/// @param[out] val answer
/// @param[in] min limit inclusive
/// @param[in] max limit inclusive
/// @return success
bool nebcommon_ParseInt(const char* str, int* val, int min, int max);


#endif // NEBCOMMON_H
