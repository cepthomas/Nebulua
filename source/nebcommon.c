#include <stdarg.h>
#include <string.h>
#include <stdio.h>
#include <errno.h>
#include <math.h>
#include "logger.h"
#include "luautils.h"
#include "nebcommon.h"


//--------------------- Defs -----------------------------//

#define BUFF_LEN 100

//--------------------------------------------------------//
double nebcommon_InternalPeriod(int tempo)
{
    double sec_per_beat = 60.0 / tempo;
    double msec_per_subbeat = 1000 * sec_per_beat / SUBEATS_PER_BAR;
    return msec_per_subbeat;
}

//--------------------------------------------------------//
int nebcommon_RoundedInternalPeriod(int tempo)
{
    double msec_per_subbeat = nebcommon_InternalPeriod(tempo);
    int period = msec_per_subbeat > 1.0 ? (int)round(msec_per_subbeat) : 1;
    return period;
}

//--------------------------------------------------------//
double nebcommon_InternalToMsec(int tempo, int subbeat)
{
    double msec = nebcommon_InternalPeriod(tempo) * subbeat;
    return msec;
}

//--------------------------------------------------------//
const char* nebcommon_FormatNebStatus(int stat)
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
        default: strncpy(buff, luautils_LuaStatusToString(stat), BUFF_LEN); break; // lua status?
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
const char* nebcommon_FormatMidiStatus(int mstat)
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
const char* nebcommon_FormatBarTime(int position)
{
    static char buff[BUFF_LEN];
    snprintf(buff, BUFF_LEN, "position: %d.%d.%d", BAR(position), BEAT(position), SUBBEAT(position));
    return buff;
}


//--------------------------------------------------------//
int nebcommon_ParseBarTime(const char* sbt)
{
    int position = 0;
    bool valid = false;
    int v;

    // Make writable copy and tokenize it.
    char cp[strlen(sbt) + 1];
    strcpy(cp, sbt);

    char* tok = strtok(cp, ".");
    if (tok != NULL)
    {
        valid = nebcommon_ParseInt(tok, &v, 0, 9999);
        if (!valid) goto nogood;
        position += v * SUBEATS_PER_BAR;
    }

    tok = strtok(NULL, ".");
    if (tok != NULL)
    {
        valid = nebcommon_ParseInt(tok, &v, 0, BEATS_PER_BAR-1);
        if (!valid) goto nogood;
        position += v * SUBBEATS_PER_BEAT;
    }

    tok = strtok(NULL, ".");
    if (tok != NULL)
    {
        valid = nebcommon_ParseInt(tok, &v, 0, SUBEATS_PER_BAR-1);
        if (!valid) goto nogood;
        position += v;
    }

    return position;
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