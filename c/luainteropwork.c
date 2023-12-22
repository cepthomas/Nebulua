#include <windows.h>
#include <stdlib.h>
#include <stdio.h>
#include <stdbool.h>
#include <string.h>
#include "lua.h"
#include "lualib.h"
#include "lauxlib.h"
#include "luainterop.h"
#include "luainteropwork.h"
#include "logger.h"

// Definition of work functions.


//--------------------------------------------------------//
int luainteropwork_Log(int level, char* msg)
{
    switch (level)
    {
    case LVL_DEBUG:
        LOG_DEBUG(msg);
        break;
    case LVL_INFO:
        LOG_INFO(msg);
        break;
    case LVL_ERROR:
        LOG_ERROR(msg);
        break;
    }

    return 0;
}


//--------------------------------------------------------//
int luainteropwork_SetTempo(int bpm) // TODO1 content
{

    return 0;
}


//--------------------------------------------------------//
int luainteropwork_CreateChannel(const char* device, int channel, int patch)
{
    // TODO1 look through devices list for this device then...

    // local hdrums = create_device(0, 10, kit.Jazz)
    // local hinp1 = create_device(1, 2)

    int hnd = -1;


    return hnd;
}


//--------------------------------------------------------//
int luainteropwork_SendNote(int channel, int notenum, double volume, double dur) // TODO1 content
{
    int ret = 0;

    int dwMsg = 0;
    HMIDIOUT hmidi_out;

    // public virtual int GetAsShortMessage()
    // {
    //     return (channel - 1) + (int)commandCode; NoteOn etc
    // }


    // http://msdn.microsoft.com/en-us/library/dd798475%28VS.85%29.aspx
    ret = midiOutShortMsg(hmidi_out, dwMsg);

    return ret;
}


//--------------------------------------------------------//
int luainteropwork_SendController(int channel, int ctlr, int value) // TODO1 content
{
    int ret = 0;

    return ret;
}
