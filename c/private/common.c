#include <stdarg.h>
#include <string.h>
#include "logger.h"
#include "common.h"
#include "diag.h"


//--------------------- Defs -----------------------------//

#define BUFF_LEN 100

//----------------------- Functions -----------------------------//



//--------------------------------------------------------//
const char* common_StatusToString(int stat)
{
    const char* sstat = NULL;
    static char buff[BUFF_LEN];

    switch(stat)
    {
        case NEB_OK: sstat = "NEB_OK"; break;
        case NEB_ERR_INTERNAL: sstat = "NEB_ERR_INTERNAL"; break;
        case NEB_ERR_BAD_CLI_ARG: sstat = "NEB_ERR_BAD_CLI_ARG"; break;
        case NEB_ERR_BAD_LUA_ARG: sstat = "NEB_ERR_BAD_LUA_ARG"; break;
        case NEB_ERR_BAD_MIDI_CFG: sstat = "NEB_ERR_BAD_MIDI_CFG"; break;
        case NEB_ERR_SYNTAX: sstat = "NEB_ERR_SYNTAX"; break;
        case NEB_ERR_MIDI: sstat = "NEB_ERR_MIDI"; break;
        default: sstat = diag_LuaStatusToString(stat); break; // lua status?
    }

    if (sstat == NULL)
    {
        snprintf(buff, BUFF_LEN - 1, "STAT%d", stat);
        return buff;
    }
    else
    {
        return sstat;
    }
}


//--------------------------------------------------------//
const char* common_MidiStatusToString(int mstat)
{
    static char buff[BUFF_LEN];
    if (mstat != MMSYSERR_NOERROR)
    {
        // Get the lib supplied text.
        midiInGetErrorText(mstat, buff, BUFF_LEN);
        return buff;
    }
    else
    {
        return NULL;
    }
}


//--------------------------------------------------------//
bool common_StrToDouble(const char* str, double* val)// TODO1 put these formatters/parsers somewhere else?
{
    bool valid = true;
    char* p;

    errno = 0;
    *val = strtof(str, &p);
    if(errno == ERANGE)
    {
        // Mag is too large.
        valid = false;
    }
    else if(p == str)
    {
        // Bad string.
        valid = false;
    }

    return valid;
}


//--------------------------------------------------------//
bool common_StrToInt(const char* str, int* val)// TODO1 put these formatters/parsers somewhere else?
{
    bool valid = true;
    char* p;

    errno = 0;
    *val = strtol(str, &p, 10);
    if(errno == ERANGE)
    {
        // Mag is too large.
        valid = false;
    }
    else if(p == str)
    {
        // Bad string.
        valid = false;
    }

    return valid;
}
