#include <stdarg.h>
#include <string.h>
#include <math.h>
#include "logger.h"
#include "nebcommon.h"
#include "diag.h"


//--------------------- Defs -----------------------------//

#define BUFF_LEN 100

//--------------------------------------------------------//
double common_InternalPeriod(int tempo)
{
    double sec_per_beat = 60.0 / tempo;
    double msec_per_subbeat = 1000 * sec_per_beat / SUBEATS_PER_BAR;
    return msec_per_subbeat;
}

//--------------------------------------------------------//
int common_RoundedInternalPeriod(int tempo)
{
    double msec_per_subbeat = common_InternalPeriod(tempo);
    int period = msec_per_subbeat > 1.0 ? (int)round(msec_per_subbeat) : 1;
    return period;
}

//--------------------------------------------------------//
double common_InternalToMsec(int tempo, int subbeat)
{
    double msec = common_InternalPeriod(tempo) * subbeat;
    return msec;
}

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
