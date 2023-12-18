// #include <stdlib.h>
// #include <stdio.h>
// #include <stdarg.h>
// #include <stdbool.h>
// #include <stdint.h>
// #include <string.h>
// #include <float.h>
// #include <errno.h>
// #include "lua.h"
// #include "lualib.h"
// #include "lauxlib.h"

#include "luainterop.h"
#include "luainteropwork.h"
#include "logger.h"

// Definition of work functions.

//--------------------------------------------------------//
int luainteropwork_Log(int level, char* msg) // TODO content
{
    return 0;
}


//--------------------------------------------------------//
int luainteropwork_SendNote(int channel, int notenum, double volume, double dur) // TODO content
{
    return 0;
}


//--------------------------------------------------------//
int luainteropwork_SendController(int channel, int ctlr, int value) // TODO content
{
    return 0;
}
