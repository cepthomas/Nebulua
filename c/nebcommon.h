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



///// App errors start after internal lua errors so they can be handled harmoniously.
#define NEB_OK                  LUA_OK // synonym
#define NEB_ERR_INTERNAL        10
#define NEB_ERR_BAD_CLI_ARG     11
#define NEB_ERR_BAD_LUA_ARG     12
#define NEB_ERR_BAD_MIDI_CFG    13
#define NEB_ERR_SYNTAX          14
#define NEB_ERR_MIDI            15


/// Convert a status to string.
/// @param[in] err Status to examine.
/// @return String or NULL if not valid.
const char* common_StatusToString(int err);

/// Convert a status to string.
/// @param[in] err Midi status to examine.
/// @return String or NULL if not valid.
const char* common_MidiStatusToString(int mstat);


#endif // NEBCOMMON_H
