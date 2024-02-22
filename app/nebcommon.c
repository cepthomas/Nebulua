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
// lbot
#include "luautils.h"
// application
#include "nebcommon.h"


//--------------------- Defs -----------------------------//

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
            snprintf(buff, BUFF_LEN, "MidiStatus:%d", mstat);
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
    snprintf(buff, BUFF_LEN, "%d:%d:%d", bar, beat, sub);

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
        valid = luautils_ParseInt(tok, &v, 0, 9999);
        if (!valid) goto nogood;
        tick += v * SUBS_PER_BAR;
    }

    tok = strtok(NULL, ":");
    if (tok != NULL)
    {
        valid = luautils_ParseInt(tok, &v, 0, BEATS_PER_BAR-1);
        if (!valid) goto nogood;
        tick += v * SUBS_PER_BEAT;
    }

    tok = strtok(NULL, ":");
    if (tok != NULL)
    {
        valid = luautils_ParseInt(tok, &v, 0, SUBS_PER_BEAT-1);
        if (!valid) goto nogood;
        tick += v;
    }

    return tick;
    
nogood:
    return -1;
}
