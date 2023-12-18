#ifndef LUAINTEROPWORK_H
#define LUAINTEROPWORK_H

// #include <stdlib.h>
// #include <stdio.h>
// #include <stdarg.h>
// #include <stdbool.h>
// #include <stdint.h>
// #include <string.h>
// #include <float.h>
// #include <errno.h>
// #include <math.h>
// #include "lua.h"
// #include "lualib.h"
// #include "lauxlib.h"
// #include "luaex.h"
// #include "luainterop.h"

// Declaration of work functions.


// Host export function: Script wants to log something.
// Lua arg: "level">Log level.
// Lua arg: "msg">Log message.
// Lua return: int Status.
int luainteropwork_Log(int level, char* msg);


// Host export function: If volume is 0 note_off else note_on. If dur is 0 dur = note_on with dur = 0.1 (for drum/hit).
// Lua arg: "channel">Output channel handle
// Lua arg: "notenum">Note number
// Lua arg: "volume">Volume between 0.0 and 1.0
// Lua arg: "dur">Duration as bar.beat
// Lua return: int Status.
int luainteropwork_SendNote(int channel, int notenum, double volume, double dur);


// Host export function: Send a controller immediately.
// Lua arg: "channel">Output channel handle
// Lua arg: "ctlr">Specific controller
// Lua arg: "value">Payload.
// Lua return: int Status
int luainteropwork_SendController(int channel, int ctlr, int value);


#endif // LUAINTEROPWORK_H
