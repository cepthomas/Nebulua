#ifndef NEBCOMMON_H
#define NEBCOMMON_H

#include <windows.h>
#include <stdio.h>
#include <stdlib.h>
#include <stdbool.h>
#include <string.h>
#include <time.h>
#include <unistd.h>
#include "lua.h"


//----------------------- Definitions -----------------------//

///// App errors start after internal lua errors so they can be handled harmoniously.
#define NEB_OK                  LUA_OK // synonym
#define NEB_ERR_INTERNAL        10
#define NEB_ERR_BAD_CLI_ARG     11
#define NEB_ERR_BAD_LUA_ARG     12
#define NEB_ERR_BAD_MIDI_CFG    13
#define NEB_ERR_SYNTAX          14
#define NEB_ERR_MIDI            15


//----------------------- Timing -----------------------------//

/// Only 4/4 time supported.
#define BEATS_PER_BAR 4

/// Internal/app resolution aka DeltaTicksPerQuarterNote or subbeats per beat.
#define INTERNAL_PPQ 32

/// Convenience.
#define SUBBEATS_PER_BEAT INTERNAL_PPQ

/// Convenience.
#define SUBEATS_PER_BAR SUBBEATS_PER_BEAT / BEATS_PER_BAR

/// Total.
#define TOTAL_BEATS(subbeat) subbeat / SUBBEATS_PER_BEAT

/// The bar number.
#define BAR(subbeat) subbeat / SUBEATS_PER_BAR

/// The beat number in the bar.
#define BEAT(subbeat) subbeat / SUBBEATS_PER_BEAT % BEATS_PER_BAR

/// The subbeat in the beat.
#define SUBBEAT(subbeat) subbeat % SUBBEATS_PER_BEAT

/// Calculate period for tempo.
/// @param[in] tempo
/// @return msec per subbeat
double common_InternalPeriod(int tempo);

/// Calculate integer period >= 1 for tempo.
/// @param[in] tempo
/// @return rounded msec per subbeat
int common_RoundedInternalPeriod(int tempo);

/// Convert subbeat to time.
/// @param[in] tempo
/// @param[in] subbeat
/// @return msec
double common_InternalToMsec(int tempo, int subbeat);


//----------------------- Utilities -----------------------------//

/// Convert a status to string.
/// @param[in] stat Status to examine
/// @return String or NULL if not valid
const char* common_FormatNebStatus(int stat);

/// Convert a status to string.
/// @param[in] mstat Midi status to examine
/// @return String or NULL if not valid
const char* common_FormatMidiStatus(int mstat);

/// Convert a string bar time to absolute position.
/// @param[in] sbt time string can be "1.2.3" or "1.2" or "1".
/// @return String or -1 if not valid.
int common_ParseBarTime(const char* sbt);

/// Convert a position to string bar time.
/// @param[in] position
/// @return string
const char* common_FormatBarTime(int position);

/// Safe convert a string to double with bounds checking.
/// @param[in] str to parse
/// @param[out] val answer
/// @param[in] min limit inclusive
/// @param[in] max limit inclusive
/// @return success
bool common_ParseDouble(const char* str, double* val, double min, double max);

/// Safe convert a string to int with bounds checking.
/// @param[in] str to parse
/// @param[out] val answer
/// @param[in] min limit inclusive
/// @param[in] max limit inclusive
/// @return success
bool common_ParseInt(const char* str, int* val, int min, int max);


#endif // NEBCOMMON_H
