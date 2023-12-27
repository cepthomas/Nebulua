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
#include "devmgr.h"
#include "luainterop.h"
#include "luainteropwork.h"

// Definition of work functions for host functions called by lua.


//--------------------------------------------------------//
int luainteropwork_Log(int level, char* msg)
{
    return logger_Log(level, msg);
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
int luainteropwork_CreateChannel(const char* sys_dev_name, int chan_num, int patch)
{
    int hndchan = 0;

    assert(sys_dev_name);
    assert(chan_num >= 1 && chan_num <= NUM_MIDI_CHANNELS);
    assert(patch >= 0 && patch < MIDI_VAL_MAX);

    midi_device_t* pdev = devmgr_GetDeviceFromName(sys_dev_name);
    assert(pdev);

    hndchan = devmgr_GetChannelHandle(pdev, chan_num);

    // Send patch now.
    int short_msg = (chan_num - 1) + MIDI_PATCH_CHANGE + (patch << 8);
    int ret = midiOutShortMsg(pdev->hnd_out, short_msg);

    return hndchan;
}


//--------------------------------------------------------//
int luainteropwork_SendNote(int hndchan, int note_num, double volume, double dur) // TODO1 if dur>0 add note off
{
    int ret = LUA_OK;

    assert(hndchan > 0);
    assert(note_num >= 0 && note_num < MIDI_VAL_MAX);
    assert(volume >= 0.0 && volume <= 1.0);
    assert(dur >= 0.0 && dur <= 100.0);

    midi_device_t* pdev = devmgr_GetOutputDeviceFromChannelHandle(hndchan);
    assert(pdev);

    int chan_num = devmgr_GetChannelNumber(hndchan);
    assert(chan_num >= 1 && chan_num <= NUM_MIDI_CHANNELS);


    int cmd = volume == 0.0 ? MIDI_NOTE_OFF : MIDI_NOTE_ON;
    int velocity = (int)(volume * MIDI_VAL_MAX);
    int short_msg = (chan_num - 1) + cmd + ((byte)note_num << 8) + ((byte)velocity << 16);
    ret = midiOutShortMsg(pdev->hnd_out, short_msg);

    return ret;
}


//--------------------------------------------------------//
int luainteropwork_SendController(int hndchan, int ctlr, int value)
{
    int ret = LUA_OK;

    assert(hndchan > 0);
    assert(ctlr >= 0 && ctlr < MIDI_VAL_MAX);
    assert(value >= 0 && value < MIDI_VAL_MAX);

    midi_device_t* pdev = devmgr_GetOutputDeviceFromChannelHandle(hndchan);
    assert(pdev);

    int chan_num = devmgr_GetChannelNumber(hndchan);
    assert(chan_num >= 1 && chan_num <= NUM_MIDI_CHANNELS);

    int cmd = MIDI_CONTROL_CHANGE;
    int short_msg = (chan_num - 1) + cmd + ((byte)ctlr << 8) + ((byte)value << 16);
    ret = midiOutShortMsg(pdev->hnd_out, short_msg);

    return ret;
}
