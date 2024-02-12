// system
#include <windows.h>
#include <stdio.h>
#include <stdlib.h>
#include <math.h>
// lua
#include "lua.h"
#include "lualib.h"
#include "lauxlib.h"
// cbot
#include "cbot.h"
// application
#include "nebcommon.h"


//--------------------- Defs -----------------------------//

#define BUFF_LEN 100


//--------------------------------------------------------//
bool nebcommon_EvalStatus(lua_State* l, int stat, const char* format, ...)
{
    static char buff[100];
    bool has_error = false;

    if (stat >= LUA_ERRRUN)
    {
        has_error = true;

        va_list args;
        va_start(args, format);
        vsnprintf(buff, sizeof(buff) - 1, format, args);
        va_end(args);

        const char* sstat = NULL;
        char err_buff[16];
        switch (stat)
        {
            // generic
            case 0:                         sstat = "NO_ERR"; break;
            // lua
            case LUA_YIELD:                 sstat = "LUA_YIELD"; break;
            case LUA_ERRRUN:                sstat = "LUA_ERRRUN"; break;
            case LUA_ERRSYNTAX:             sstat = "LUA_ERRSYNTAX"; break; // syntax error during pre-compilation
            case LUA_ERRMEM:                sstat = "LUA_ERRMEM"; break; // memory allocation error
            case LUA_ERRERR:                sstat = "LUA_ERRERR"; break; // error while running the error handler function
            case LUA_ERRFILE:               sstat = "LUA_ERRFILE"; break; // couldn't open the given file
            // cbot
            case CBOT_ERR_INVALID_ARG:      sstat = "CBOT_ERR_INVALID_ARG"; break;
            case CBOT_ERR_ARG_NULL:         sstat = "CBOT_ERR_ARG_NULL"; break;
            case CBOT_ERR_NO_DATA:          sstat = "CBOT_ERR_NO_DATA"; break;
            case CBOT_ERR_INVALID_INDEX:    sstat = "CBOT_ERR_INVALID_INDX"; break;
            // app
            case NEB_ERR_INTERNAL:          sstat = "NEB_ERR_INTERNAL"; break;
            case NEB_ERR_BAD_CLI_ARG:       sstat = "NEB_ERR_BAD_CLI_ARG"; break;
            case NEB_ERR_BAD_LUA_ARG:       sstat = "NEB_ERR_BAD_LUA_ARG"; break;
            case NEB_ERR_BAD_MIDI_CFG:      sstat = "NEB_ERR_BAD_MIDI_CFG"; break;
            case NEB_ERR_SYNTAX:            sstat = "NEB_ERR_SYNTAX"; break;
            case NEB_ERR_MIDI:              sstat = "NEB_ERR_MIDI"; break;
            // default
            default:                        snprintf(err_buff, sizeof(err_buff) - 1, "ERR_%d", stat); break;
        }

        sstat = (sstat == NULL) ? err_buff : sstat;

        if (stat <= LUA_ERRFILE) // internal lua error - get error message on stack if provided.
        {
            if (lua_gettop(l) > 0)
            {
                luaL_error(l, "Status:%s info:%s errmsg:%s", sstat, buff, lua_tostring(l, -1));
            }
            else
            {
                luaL_error(l, "Status:%s info:%s", sstat, buff);
            }
        }
        else // cbot or nebulua error
        {
            luaL_error(l, "Status:%s info:%s", sstat, buff);
        }

        //  maybe? const char* strerrorname_np(int errnum), const char* strerrordesc_np(int errnum);
    }

    return has_error;
}


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
            sprintf(buff, "MidiStatus:%d", mstat);
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
    int subbeat = SUBBEAT(tick);
    snprintf(buff, BUFF_LEN, "%d:%d:%d", bar, beat, subbeat);

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
    strncpy(cp, sbt, sizeof(cp));

    char* tok = strtok(cp, ":");
    if (tok != NULL)
    {
        valid = nebcommon_ParseInt(tok, &v, 0, 9999);
        if (!valid) goto nogood;
        tick += v * SUBBEATS_PER_BAR;
    }

    tok = strtok(NULL, ":");
    if (tok != NULL)
    {
        valid = nebcommon_ParseInt(tok, &v, 0, BEATS_PER_BAR-1);
        if (!valid) goto nogood;
        tick += v * SUBBEATS_PER_BEAT;
    }

    tok = strtok(NULL, ":");
    if (tok != NULL)
    {
        valid = nebcommon_ParseInt(tok, &v, 0, SUBBEATS_PER_BEAT-1);
        if (!valid) goto nogood;
        tick += v;
    }

    return tick;
    
nogood:
    return -1;
}


//--------------------------------------------------------//
bool nebcommon_ParseDouble(const char* str, double* val, double min, double max)
{
    bool valid = true;
    char* p;

    errno = 0;
    *val = strtof(str, &p);
    if (errno == ERANGE)
    {
        // Mag is too large.
        valid = false;
    }
    else if (p == str)
    {
        // Bad string.
        valid = false;
    }
    else if (*val < min || *val > max)
    {
        // Out of range.
        valid = false;
    }

    return valid;
}


//--------------------------------------------------------//
bool nebcommon_ParseInt(const char* str, int* val, int min, int max)
{
    bool valid = true;
    char* p;

    errno = 0;
    *val = strtol(str, &p, 10);
    if (errno == ERANGE)
    {
        // Mag is too large.
        valid = false;
    }
    else if (p == str)
    {
        // Bad string.
        valid = false;
    }
    else if (*val < min || *val > max)
    {
        // Out of range.
        valid = false;
    }

    return valid;
}
