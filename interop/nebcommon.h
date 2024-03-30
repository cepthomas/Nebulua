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
#define NEB_ERR_API             17
#define NEB_ERR_RUN             18
#define NEB_ERR_FILE            19


//----------------------- Midi -----------------------------//

// Midi caps.
#define MIDI_VAL_MIN 0

// Midi caps.
#define MIDI_VAL_MAX 127


//----------------------- Utilities -----------------------------//

/// <summary>
/// Convert managed string to unmanaged.
/// </summary>
/// <param name="input"></param>
/// <returns>C string.</returns>
const char* ToCString(System::String^ input);

/// <summary>
/// Convert unmanaged string to managed.
/// </summary>
/// <param name="input"></param>
/// <returns>Converted string.</returns>
System::String^ ToCliString(const char* input);

/// Checks stat and returns an error message if it failed.
/// @param[in] l lua context
/// @param[in] stat to be tested
/// @param[in] format extra info to add if fail
/// @return if fail the error string else NULL
System::String^ EvalStatus(lua_State* l, int stat, System::String^ msg);


#endif // NEBCOMMON_H
