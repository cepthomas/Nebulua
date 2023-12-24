#include <windows.h>
#include <stdlib.h>
#include <stdio.h>
#include <stdbool.h>
#include <string.h>
#include <math.h>
#include "lua.h"
#include "lualib.h"
#include "lauxlib.h"
#include "logger.h"
#include "ftimer.h"
#include "common.h"
#include "luainterop.h"
#include "luainteropwork.h"

// Definition of work functions for host functions called by lua.



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
int luainteropwork_SetTempo(int bpm)
{
    double sec_per_beat = 60.0 / bpm;
    double msec_per_subbeat = 1000 * sec_per_beat / SUBEATS_PER_BAR;
    int period = msec_per_subbeat > 1.0 ? round(msec_per_subbeat) : 1;

    ftimer_Run(period);

    return 0;
}


//--------------------------------------------------------//
int luainteropwork_CreateChannel(const char* device, int channum, int patch)
{
    // Handle is
    int hnd = 0; // default = invalid

    if (channum >= 1 && channum <= MIDI_CHANNELS)
    {
        // Look through devices list for this device.
        for (int i = 0; i < MIDI_DEVICES; i++)
        {
            if (strcmp(device, _devices[i].dev_name) == 0)
            {
                // Valid device. Make a simple handle from the index and the channel number.
                _devices[i].channels[channum - 1] = true;
                hnd = (i << 8) | channum;

                if (_devices[i].hmidi_out > 0)
                {
                    // Send patch now.
                    int short_msg = (channum - 1) + MIDI_PATCH_CHANGE + (patch << 8);
                    int ret = midiOutShortMsg(_devices[i].hmidi_out, short_msg);
                }

                break; // done
            }
        }
    }

    return hnd;
}

int p_Constrain(int val)
{
    val = max(val, MIDI_MIN);
    val = min(val, MIDI_MAX);
    return val;
}


//--------------------------------------------------------//
int luainteropwork_SendNote(int hndchan, int note_num, double volume, double dur) // TODO1 dur -> note off
{
    int ret = LUA_OK;

    // Validate user args.
    int channum = hndchan & 0xFF;
    int devi = (hndchan >> 8) & 0xFF;

    if (channum >= 1 && channum <= MIDI_CHANNELS && devi >= 0 && devi < MIDI_DEVICES)
    {
        if(_devices[devi].hmidi_out > 0 && _devices[devi].channels[channum - 1])
        {
            int cmd = volume == 0.0 ? MIDI_NOTE_OFF : MIDI_NOTE_ON;
            int velocity = p_Constrain((int)(volume * MIDI_MAX));
            int short_msg = (channum - 1) + cmd + ((byte)note_num << 8) + ((byte)velocity << 16);
            ret = midiOutShortMsg(_devices[devi].hmidi_out, short_msg);
        }
    }
    else
    {
        ret = -99; // TODO1 errors notify/log?
    }

    return ret;
}


//--------------------------------------------------------//
int luainteropwork_SendController(int hndchan, int ctlr, int value)
{
    int ret = LUA_OK;

    // Validate user args. TODO3 refactor?
    int channum = hndchan & 0xFF;
    int devi = (hndchan >> 8) & 0xFF;

    if (channum >= 1 && channum <= MIDI_CHANNELS &&
        devi >= 0 && devi < MIDI_DEVICES &&
        ctlr >= 0 && ctlr < MIDI_MAX &&
        value >= 0 && value < MIDI_MAX)
    {
        if(_devices[devi].hmidi_out > 0 && _devices[devi].channels[channum - 1])
        {
            int cmd = MIDI_CONTROL_CHANGE;
            int short_msg = (channum - 1) + cmd + ((byte)ctlr << 8) + ((byte)value << 16);
            ret = midiOutShortMsg(_devices[devi].hmidi_out, short_msg);
        }
    }
    else
    {
        ret = -99;
    }


    return ret;
}
