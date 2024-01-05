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
#include "nebcommon.h"
#include "devmgr.h"
#include "luainterop.h"
#include "luainteropwork.h"


// Definition of work functions for host functions called by lua.

// Macro used to handle user syntax errors in the interop work functions. TODO3 put these somewhere else?
#define VALS(expr, s) luaL_error(l, "%s: %s", #expr, s)
#define VALI(expr, i) luaL_error(l, "%s: %d", #expr, i)
#define VALF(expr, f) luaL_error(l, "%s: %f", #expr, f)


//--------------------------------------------------------//
int luainteropwork_Log(lua_State* l, int level, char* msg)
{
    logger_Log(level, msg);
    return 0;
}


//--------------------------------------------------------//
int luainteropwork_SetTempo(lua_State* l, int bpm)
{
    VALI(bpm >= 30 && bpm <= 240, bpm);

    double sec_per_beat = 60.0 / bpm;
    double msec_per_subbeat = 1000 * sec_per_beat / SUBEATS_PER_BAR;
    int period = msec_per_subbeat > 1.0 ? round(msec_per_subbeat) : 1;

    ftimer_Run(period);

    return 0;
}


//--------------------------------------------------------//
int luainteropwork_CreateChannel(lua_State* l, char* sys_dev_name, int chan_num, int patch)
{
    int hndchan = 0;

    VALS(sys_dev_name != NULL, sys_dev_name);
    VALI(chan_num >= 1 && chan_num <= NUM_MIDI_CHANNELS, chan_num);
    VALI(patch >= 0 && patch < MIDI_VAL_MAX, patch);

    midi_device_t* pdev = devmgr_GetDeviceFromName(sys_dev_name);
    VALS(pdev != NULL, sys_dev_name);

    hndchan = devmgr_GetChannelHandle(pdev, chan_num);
    VALI(hndchan > 0, chan_num);

    // Send patch now.
    int short_msg = (chan_num - 1) + MIDI_PATCH_CHANGE + (patch << 8);
    int mstat = midiOutShortMsg(pdev->hnd_out, short_msg);
    VALS(mstat == MMSYSERR_NOERROR, common_MidiStatusToString(mstat));

    return hndchan;
}


//--------------------------------------------------------//
int luainteropwork_SendNote(lua_State* l, int hndchan, int note_num, double volume, double dur) // TODO2 if dur>0 add note off
{
    VALI(hndchan > 0, hndchan);
    VALI(note_num >= 0 && note_num < MIDI_VAL_MAX, note_num);
    VALF(volume >= 0.0 && volume <= 1.0, volume);
    VALF(dur >= 0.0 && dur <= 100.0, dur);

    midi_device_t* pdev = devmgr_GetOutputDeviceFromChannelHandle(hndchan);
    VALI(pdev != NULL, hndchan);

    int chan_num = devmgr_GetChannelNumber(hndchan);
    VALI(chan_num >= 1 && chan_num <= NUM_MIDI_CHANNELS, chan_num);

    int cmd = volume == 0.0 ? MIDI_NOTE_OFF : MIDI_NOTE_ON;
    int velocity = (int)(volume * MIDI_VAL_MAX);
    int short_msg = (chan_num - 1) + cmd + ((byte)note_num << 8) + ((byte)velocity << 16);
    int mstat = midiOutShortMsg(pdev->hnd_out, short_msg);
    VALS(mstat == MMSYSERR_NOERROR, common_MidiStatusToString(mstat));

    return 0;
}


//--------------------------------------------------------//
int luainteropwork_SendController(lua_State* l, int hndchan, int ctlr, int value)
{
    VALI(hndchan > 0, hndchan);
    VALI(ctlr >= 0 && ctlr < MIDI_VAL_MAX, ctlr);
    VALI(value >= 0 && value < MIDI_VAL_MAX, value);

    midi_device_t* pdev = devmgr_GetOutputDeviceFromChannelHandle(hndchan);
    VALI(pdev != NULL, hndchan);

    int chan_num = devmgr_GetChannelNumber(hndchan);
    VALI(chan_num >= 1 && chan_num <= NUM_MIDI_CHANNELS, hndchan);

    int cmd = MIDI_CONTROL_CHANGE;
    int short_msg = (chan_num - 1) + cmd + ((byte)ctlr << 8) + ((byte)value << 16);
    int mstat = midiOutShortMsg(pdev->hnd_out, short_msg);
    VALS(mstat == MMSYSERR_NOERROR, common_MidiStatusToString(mstat));

    return 0;
}
