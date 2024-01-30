// system
#include <windows.h>
#include <stdio.h>
#include <stdlib.h>
#include <math.h>
// lua
#include "lua.h"
// cbot
// application
#include "nebcommon.h"


//--------------------- Defs -----------------------------//

#define BUFF_LEN 100

// //--------------------------------------------------------//
// double nebcommon_InternalPeriod(int tempo)
// {
//     double sec_per_beat = 60.0 / tempo;
//     double msec_per_subbeat = 1000 * sec_per_beat / SUBBEATS_PER_BAR;
//     return msec_per_subbeat;
// }


// //--------------------------------------------------------//
// int nebcommon_RoundedInternalPeriod(int tempo)
// {
//     double msec_per_subbeat = nebcommon_InternalPeriod(tempo);
//     int period = msec_per_subbeat > 1.0 ? (int)round(msec_per_subbeat) : 1;
//     return period;
// }


// //--------------------------------------------------------//
// double nebcommon_InternalToMsec(int tempo, int subbeats)
// {
//     double msec = nebcommon_InternalPeriod(tempo) * subbeats;
//     return msec;
// }


//--------------------------------------------------------//
const char* nebcommon_FormatMidiStatus(int mstat)
{
    static char buff[BUFF_LEN];
    buff[0] = 0;
    if (mstat != MMSYSERR_NOERROR)
    {
        // Get the lib supplied text.
        midiInGetErrorText(mstat, buff, BUFF_LEN);
        if (strlen(buff) == 0)
        {
            sprintf(buff, "MidiStatus:%d", mstat);
        }
    }

    return buff;
}


//--------------------------------------------------------//
const char* nebcommon_FormatBarTime(int subbeats)
{
    static char buff[BUFF_LEN];
    int bar = BAR(subbeats);
    int beat = BEAT(subbeats);
    int subbeat = SUBBEAT(subbeats);
    snprintf(buff, BUFF_LEN, "%d.%d.%d", bar, beat, subbeat);

    return buff;
}


//--------------------------------------------------------//
int nebcommon_ParseBarTime(const char* sbt)
{
    int subbeats = 0;
    bool valid = false;
    int v;

    // Make writable copy and tokenize it.
    char cp[32];
    strncpy(cp, sbt, sizeof(cp));

    char* tok = strtok(cp, ".");
    if (tok != NULL)
    {
        valid = nebcommon_ParseInt(tok, &v, 0, 9999);
        if (!valid) goto nogood;
        subbeats += v * SUBBEATS_PER_BAR;
    }

    tok = strtok(NULL, ".");
    if (tok != NULL)
    {
        valid = nebcommon_ParseInt(tok, &v, 0, BEATS_PER_BAR-1);
        if (!valid) goto nogood;
        subbeats += v * SUBBEATS_PER_BEAT;
    }

    tok = strtok(NULL, ".");
    if (tok != NULL)
    {
        valid = nebcommon_ParseInt(tok, &v, 0, SUBBEATS_PER_BEAT-1);
        if (!valid) goto nogood;
        subbeats += v;
    }

    return subbeats;
    
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
