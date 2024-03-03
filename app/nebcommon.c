// system
#include <windows.h>
#include <stdio.h>
#include <stdlib.h>
// #include <stdbool.h>
#include <math.h>
// lua
#include "lua.h"
#include "lualib.h"
#include "lauxlib.h"
// cbot
#include "cbot.h"
// lbot
#include "luautils.h"
// application
#include "luainterop.h"
#include "nebcommon.h"


#define BUFF_LEN 100


//--------------------------------------------------------//
const char* nebcommon_FormatMidiStatus(int mstat)
{
    static char buff[BUFF_LEN];
    buff[0] = 0;
    if (mstat != MMSYSERR_NOERROR)
    {
        // Get the lib supplied text from mmeapi.h.
        midiInGetErrorText(mstat, buff, BUFF_LEN);
        if (strlen(buff) == 0)
        {
            snprintf(buff, BUFF_LEN - 1, "MidiStatus:%d", mstat);
        }
    }

    return buff;
}


//--------------------------------------------------------//
const char* nebcommon_FormatBarTime(int tick)
{
    static char buff[BUFF_LEN];
    int bar = BAR(tick);
    int beat = BEAT(tick);
    int sub = SUB(tick);
    snprintf(buff, BUFF_LEN - 1, "%d:%d:%d", bar, beat, sub);

    return buff;
}


//--------------------------------------------------------//
int nebcommon_ParseBarTime(const char* sbt)
{
    int tick = 0;
    bool valid = false;
    int v;

    // Make writable copy and tokenize it.
    char cp[32];
    strncpy(cp, sbt, sizeof(cp) - 1);

    char* tok = strtok(cp, ":");
    if (tok != NULL)
    {
        valid = luautils_ParseInt(tok, &v, 0, 9999);
        if (!valid) goto nogood;
        tick += v * SUBS_PER_BAR;
    }

    tok = strtok(NULL, ":");
    if (tok != NULL)
    {
        valid = luautils_ParseInt(tok, &v, 0, BEATS_PER_BAR - 1);
        if (!valid) goto nogood;
        tick += v * SUBS_PER_BEAT;
    }

    tok = strtok(NULL, ":");
    if (tok != NULL)
    {
        valid = luautils_ParseInt(tok, &v, 0, SUBS_PER_BEAT - 1);
        if (!valid) goto nogood;
        tick += v;
    }

    return tick;

nogood:
    return -1;
}


#define ERR_BUFF_LEN 500

//--------------------------------------------------------//
const char* nebcommon_EvalStatus(lua_State* l, int stat, const char* format, ...)
{
    static char full_msg[ERR_BUFF_LEN];

    char* sret = NULL;

    if (stat >= LUA_ERRRUN)
    {
        // Format info string.
        char info[100];
        va_list args;
        va_start(args, format);
        vsnprintf(info, sizeof(info) - 1, format, args);
        va_end(args);

        // Get error number string.
        const char* sstat = NULL;
        switch (stat)
        {
            // generic
            case 0:                         sstat = "NO_ERR"; break;
            // lua 0-6
            case LUA_YIELD:                 sstat = "LUA_YIELD"; break;
            case LUA_ERRRUN:                sstat = "LUA_ERRRUN"; break;
            case LUA_ERRSYNTAX:             sstat = "LUA_ERRSYNTAX"; break; // syntax error during pre-compilation
            case LUA_ERRMEM:                sstat = "LUA_ERRMEM"; break; // memory allocation error
            case LUA_ERRERR:                sstat = "LUA_ERRERR"; break; // error while running the error handler function
            case LUA_ERRFILE:               sstat = "LUA_ERRFILE"; break; // couldn't open the given file
            // cbot 100-?
            case CBOT_ERR_INVALID_ARG:      sstat = "CBOT_ERR_INVALID_ARG"; break;
            case CBOT_ERR_ARG_NULL:         sstat = "CBOT_ERR_ARG_NULL"; break;
            case CBOT_ERR_NO_DATA:          sstat = "CBOT_ERR_NO_DATA"; break;
            case CBOT_ERR_INVALID_INDEX:    sstat = "CBOT_ERR_INVALID_INDX"; break;
            // app 10-?
            case NEB_ERR_INTERNAL:          sstat = "NEB_ERR_INTERNAL"; break;
            case NEB_ERR_BAD_CLI_ARG:       sstat = "NEB_ERR_BAD_CLI_ARG"; break;
            case NEB_ERR_BAD_LUA_ARG:       sstat = "NEB_ERR_BAD_LUA_ARG"; break;
            case NEB_ERR_BAD_MIDI_CFG:      sstat = "NEB_ERR_BAD_MIDI_CFG"; break;
            case NEB_ERR_SYNTAX:            sstat = "NEB_ERR_SYNTAX"; break;
            case NEB_ERR_MIDI_RX:           sstat = "NEB_ERR_MIDI_RX"; break;
            case NEB_ERR_MIDI_TX:           sstat = "NEB_ERR_MIDI_TX"; break;
            // Interop 200-?
            case INTEROP_BAD_FUNC_NAME:     sstat = "INTEROP_BAD_FUNC_NAME"; break;
            case INTEROP_BAD_RET_TYPE:      sstat = "INTEROP_BAD_RET_TYPE"; break;
            // default
            default:                        sstat = "UNKNOWN_ERROR"; LOG_DEBUG("Unknwon ret code:%d", stat); break;
        }

        // Additional error message.
        const char* smsg = "";
        if (stat <= LUA_ERRFILE && l != NULL && lua_gettop(l) > 0)
        {
            smsg = lua_tostring(l, -1);
            lua_pop(l, 1);
        }

        snprintf(full_msg, sizeof(full_msg) - 1, "%s %s\n%s", sstat, info, smsg);
        sret = full_msg;


        // // Log the error info.
        // if (errmsg == NULL)
        // {
        //     snprintf(_last_error, sizeof(_last_error) - 1, "%s %s", sstat, info);
        // }
        // else
        // {
        //     snprintf(_last_error, sizeof(_last_error) - 1, "%s %s\n%s", sstat, info, errmsg);
        // }
    }

    return sret;
}
