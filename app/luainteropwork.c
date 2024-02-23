// system
#include <stdlib.h>
#include <math.h>
// lua
#include "lua.h"
#include "lualib.h"
#include "lauxlib.h"
// cbot
#include "logger.h"
#include "ftimer.h"
#include "cbot.h"
// application
#include "nebcommon.h"
#include "devmgr.h"


// Definition of work functions for host functions called by lua.


//--------------------------------------------------------//
int luainteropwork_Log(int level, const char* msg)
{
    logger_Log(level, -1, msg); // does arg checking.
    return NEB_OK;
}


//--------------------------------------------------------//
int luainteropwork_SetTempo(int bpm)
{
    int stat = NEB_ERR_BAD_LUA_ARG;

    if (bpm >= 30 && bpm <= 240)
    {
        double sec_per_beat = 60.0 / bpm;
        double msec_per_sub = 1000 * sec_per_beat / SUBS_PER_BEAT;
        int period = msec_per_sub > 1.0 ? (int)round(msec_per_sub) : 1;

        ftimer_Run(period);
        stat = NEB_OK;
    }

    return stat;
}

//--------------------------------------------------------//
int luainteropwork_CreateInputChannel(const char* dev_name, int chan_num)
{
    int chan_hnd = 0; // default is invalid
    midi_device_t* pdev = NULL;

    if (dev_name != NULL && chan_num >= 1 && chan_num <= NUM_MIDI_CHANNELS)
    {
        pdev = devmgr_GetDeviceFromName(dev_name);
    }

    if (pdev != NULL)
    {
        int stat = devmgr_OpenMidi(pdev);
        if (stat != NEB_OK)
        {
            pdev = NULL;
        }
    }

    if (pdev != NULL)
    {
        chan_hnd = devmgr_GetChannelHandle(pdev, chan_num);
    }

    return chan_hnd;
}


//--------------------------------------------------------//
int luainteropwork_CreateOutputChannel(const char* dev_name, int chan_num, int patch)
{
    int chan_hnd = 0; // default is invalid
    midi_device_t* pdev = NULL;

    if (dev_name != NULL && chan_num >= 1 && chan_num <= NUM_MIDI_CHANNELS)
    {
        pdev = devmgr_GetDeviceFromName(dev_name);
    }

    if (pdev != NULL)
    {
        int stat = devmgr_OpenMidi(pdev);
        if (stat != NEB_OK)
        {
            pdev = NULL;
        }
    }

    if (pdev != NULL)
    {
        chan_hnd = devmgr_GetChannelHandle(pdev, chan_num);
    }

    // Send patch now.
    if (pdev != NULL)
    {
        int short_msg = (chan_num - 1) + MIDI_PATCH_CHANGE + (patch << 8);
        int mstat = midiOutShortMsg(pdev->handle, short_msg);
        if (mstat != MMSYSERR_NOERROR)
        {
            chan_hnd = 0;
        }
    }

    return chan_hnd;
}


//--------------------------------------------------------//
int luainteropwork_SendNote(int chan_hnd, int note_num, double volume)
{
    int stat = NEB_OK;
    midi_device_t* pdev = NULL;

    if (chan_hnd > 0 && note_num >= MIDI_VAL_MIN && note_num < MIDI_VAL_MAX && volume >= 0.0 && volume <= 1.0)
    {
        pdev = devmgr_GetDeviceFromChannelHandle(chan_hnd);
    }

    if (pdev != NULL)
    {
        int chan_num = devmgr_GetChannelNumber(chan_hnd);
        int cmd = volume == 0.0 ? MIDI_NOTE_OFF : MIDI_NOTE_ON;
        // Translate volume to velocity.
        int velocity = (int)(volume * MIDI_VAL_MAX);
        int short_msg = (chan_num - 1) + cmd + ((byte)note_num << 8) + ((byte)velocity << 16);
        int mstat = midiOutShortMsg(pdev->handle, short_msg);
        if (mstat != MMSYSERR_NOERROR)
        {
            stat = NEB_ERR_MIDI_TX;
        }
    }
    else
    {
        stat = NEB_ERR_BAD_CLI_ARG;
    }

    return stat;
}


//--------------------------------------------------------//
int luainteropwork_SendController(int chan_hnd, int controller, int value)
{
    int stat = NEB_OK;
    midi_device_t* pdev = NULL;

    if (chan_hnd > 0 && controller >= MIDI_VAL_MIN && controller < MIDI_VAL_MAX && value >= MIDI_VAL_MIN && value < MIDI_VAL_MAX)
    {
        pdev = devmgr_GetDeviceFromChannelHandle(chan_hnd);
    }

    if (pdev != NULL)
    {
        int chan_num = devmgr_GetChannelNumber(chan_hnd);
        int cmd = MIDI_CONTROL_CHANGE;
        int short_msg = (chan_num - 1) + cmd + ((byte)controller << 8) + ((byte)value << 16);
        int mstat = midiOutShortMsg(pdev->handle, short_msg);
        if (mstat != MMSYSERR_NOERROR)
        {
            stat = NEB_ERR_MIDI_TX;
        }
    }
    else
    {
        stat = NEB_ERR_BAD_CLI_ARG;
    }

    return stat;
}
