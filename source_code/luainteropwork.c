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

// Macro used to handle user syntax errors in the interop work functions.
#define VALS(expr, s) if (!(expr)) { luaL_error(l, "%s: %s", #expr, s); }
#define VALI(expr, i) if (!(expr)) { luaL_error(l, "%s: %d", #expr, i); }
#define VALF(expr, f) if (!(expr)) { luaL_error(l, "%s: %f", #expr, f); }


//--------------------------------------------------------//
int luainteropwork_Log(lua_State* l, int level, const char* msg)
{
    logger_Log(level, -1, msg);
    return 0;
}


//--------------------------------------------------------//
int luainteropwork_SetTempo(lua_State* l, int bpm)
{
    VALI(bpm >= 30 && bpm <= 240, bpm);

    double sec_per_beat = 60.0 / bpm;
    double msec_per_sub = 1000 * sec_per_beat / SUBS_PER_BEAT;
    int period = msec_per_sub > 1.0 ? (int)round(msec_per_sub) : 1;

    ftimer_Run(period);

    return 0;
}


//--------------------------------------------------------//
int luainteropwork_CreateInputChannel(lua_State* l, const char* dev_name, int chan_num)
{
    int chan_hnd = 0;

    VALS(dev_name != NULL, dev_name);
    VALI(chan_num >= 1 && chan_num <= NUM_MIDI_CHANNELS, chan_num);

    midi_device_t* pdev = devmgr_GetDeviceFromName(dev_name);
    VALS(pdev != NULL, dev_name);

    int stat = devmgr_OpenMidi(pdev);
    VALI(stat == NEB_OK, 0);
    UNUSED(stat);
    
    chan_hnd = devmgr_GetChannelHandle(pdev, chan_num);
    VALI(chan_hnd > 0, chan_num);

    return chan_hnd;
}


//--------------------------------------------------------//
int luainteropwork_CreateOutputChannel(lua_State* l, const char* dev_name, int chan_num, int patch)
{
    int chan_hnd = 0;

    VALS(dev_name != NULL, dev_name);
    VALI(chan_num >= 1 && chan_num <= NUM_MIDI_CHANNELS, chan_num);
    VALI(patch >= MIDI_VAL_MIN && patch < MIDI_VAL_MAX, patch);

    midi_device_t* pdev = devmgr_GetDeviceFromName(dev_name);
    VALS(pdev != NULL, dev_name);

    int stat = devmgr_OpenMidi(pdev);
    VALI(stat == NEB_OK, 0);
    UNUSED(stat);

    chan_hnd = devmgr_GetChannelHandle(pdev, chan_num);
    VALI(chan_hnd > 0, chan_num);

    // Send patch now.
    if (pdev != NULL_PTR)
    {
        int short_msg = (chan_num - 1) + MIDI_PATCH_CHANGE + (patch << 8);
        int mstat = midiOutShortMsg(pdev->handle, short_msg);
        VALS(mstat == MMSYSERR_NOERROR, nebcommon_FormatMidiStatus(mstat));
    }

    return chan_hnd;
}


//--------------------------------------------------------//
int luainteropwork_SendNote(lua_State* l, int chan_hnd, int note_num, double volume)
{
    VALI(chan_hnd > 0, chan_hnd);
    VALI(note_num >= MIDI_VAL_MIN && note_num < MIDI_VAL_MAX, note_num);
    VALF(volume >= 0.0 && volume <= 1.0, volume);

    midi_device_t* pdev = devmgr_GetDeviceFromChannelHandle(chan_hnd);
    VALI(pdev != NULL, chan_hnd);

    int chan_num = devmgr_GetChannelNumber(chan_hnd);
    VALI(chan_num >= 1 && chan_num <= NUM_MIDI_CHANNELS, chan_num);

    if (pdev != NULL_PTR)
    {
        int cmd = volume == 0.0 ? MIDI_NOTE_OFF : MIDI_NOTE_ON;
        // Translate volume to velocity.
        int velocity = (int)(volume * MIDI_VAL_MAX);
        int short_msg = (chan_num - 1) + cmd + ((byte)note_num << 8) + ((byte)velocity << 16);
        int mstat = midiOutShortMsg(pdev->handle, short_msg);
        VALS(mstat == MMSYSERR_NOERROR, nebcommon_FormatMidiStatus(mstat));
    }

    return 0;
}


//--------------------------------------------------------//
int luainteropwork_SendController(lua_State* l, int chan_hnd, int controller, int value)
{
    VALI(chan_hnd > 0, chan_hnd);
    VALI(controller >= MIDI_VAL_MIN && controller < MIDI_VAL_MAX, controller);
    VALI(value >= MIDI_VAL_MIN && value < MIDI_VAL_MAX, value);

    midi_device_t* pdev = devmgr_GetDeviceFromChannelHandle(chan_hnd);
    VALI(pdev != NULL, chan_hnd);

    int chan_num = devmgr_GetChannelNumber(chan_hnd);
    VALI(chan_num >= 1 && chan_num <= NUM_MIDI_CHANNELS, chan_hnd);

    if (pdev != NULL_PTR)
    {
        int cmd = MIDI_CONTROL_CHANGE;
        int short_msg = (chan_num - 1) + cmd + ((byte)controller << 8) + ((byte)value << 16);
        int mstat = midiOutShortMsg(pdev->handle, short_msg);
        VALS(mstat == MMSYSERR_NOERROR, nebcommon_FormatMidiStatus(mstat));
    }

    return 0;
}
