#include <stdarg.h>
#include <string.h>
#include "logger.h"
#include "diag.h"
#include "common.h"
#include "devmgr.h"


//--------------------- Defs -----------------------------//


// #define MAKE_HANDLE(dev_index, chan_num) ((dev_index << 8) | (chan_num))
// #define GET_DEV_INDEX(hndchan) ((hndchan >> 8) & 0xFF)
// #define GET_CHAN_NUM(hndchan) (hndchan & 0xFF)


int devmgr_GetChannelNumberFromChannelHandle(int hndchan)
{
    return hndchan & 0xFF;
}

int devmgr_GetDevIndexFromChannelHandle(int hndchan)
{
    return ((hndchan >> 8) & 0xFF);
}

int devmgr_MakeChannelHandle(int dev_index, int chan_num)
{
    return ((dev_index << 8) | (chan_num));
}


//------------------- Privates ---------------------------//

// Devices specified in the user script.
static midi_device_t _devices[NUM_MIDI_DEVICES];  //TODO1 global, refine/pointers




//------------------- Publics ----------------------------//

int devmgr_Init(DWORD_PTR midi_handler)
{
    assert(midi_handler);

    int stat = NEB_OK;
    MMRESULT res = 0;

    memset(_devices, 0, sizeof(_devices));
    int dev_index = 0;

    int num_in = midiInGetNumDevs();
    int num_out = midiOutGetNumDevs();

    if (num_in + num_out >= NUM_MIDI_DEVICES)
    {
//TODO1        common_EvalStatus(p_lmain, NEB_ERR_BAD_MIDI_CFG, "Too many midi devices");
    }

    // Inputs.
    for (int i = 0; i < num_in; i++, dev_index++)
    {
        MIDIINCAPS caps_in;
        res = midiInGetDevCaps(i, &caps_in, sizeof(caps_in));

        HMIDIIN hmidi_in;
        // dev_index => dwInstance;
        res = midiInOpen(&hmidi_in, i, midi_handler, (DWORD_PTR)dev_index, CALLBACK_FUNCTION);

        // Save the device info.
        _devices[dev_index].hnd_in = hmidi_in;
        _devices[dev_index].sys_dev_index = i;
        strncpy(_devices[dev_index].sys_dev_name, caps_in.szPname, MAXPNAMELEN - 1);

        // Fire it up.
        res = midiInStart(hmidi_in);
    }

    // Outputs.
    for (int i = 0; i < num_out; i++, dev_index++)
    {
        // http://msdn.microsoft.com/en-us/library/dd798469%28VS.85%29.aspx
        MIDIOUTCAPS caps_out;
        res = midiOutGetDevCaps(i, &caps_out, sizeof(caps_out));

        HMIDIOUT hmidi_out;
        // http://msdn.microsoft.com/en-us/library/dd798476%28VS.85%29.aspx
        res = midiOutOpen(&hmidi_out, i, 0, 0, 0);

        // Save the device info.
        _devices[dev_index].hnd_out = hmidi_out;
        _devices[dev_index].sys_dev_index = i;
        strncpy(_devices[dev_index].sys_dev_name, caps_out.szPname, MAXPNAMELEN);
    }

    return stat;

}

int devmgr_Destroy()
{
    int stat = NEB_OK;
    for (int i = 0; i < NUM_MIDI_DEVICES; i++)//devmgr_Destroy()
    {
        if (_devices[i].hnd_in > 0)
        {
            midiInStop(_devices[i].hnd_in);
            midiInClose(_devices[i].hnd_in); 
        }
        else if (_devices[i].hnd_out > 0)
        {
            midiOutClose(_devices[i].hnd_out);
        }
    }

    return stat;
}


int devmgr_GetChannelHandle(midi_device_t* pdev, int chan_num)
{
    assert(pdev);
    assert(chan_num >= 1 && chan_num <= NUM_MIDI_CHANNELS);

    int hndchan = 0; // default = invalid

    for (int i = 0; i < NUM_MIDI_DEVICES && hndchan == 0; i++)
    {
        if (_devices[i].hnd_in == pdev->hnd_in && _devices[i].channels[chan_num - 1]) // test for -1
        {
            hndchan = devmgr_MakeChannelHandle(i, chan_num);
        }
    }

    return hndchan;
}


midi_device_t* devmgr_GetByIndex(int dev_index)
{
    assert(dev_index >= 0 && dev_index < NUM_MIDI_DEVICES);

    midi_device_t* pdev = NULL;
    //int dev_index = dwInstance;
    pdev = _devices + dev_index;//midi_device_t* devmgr_Get(dev_index);
    return pdev;
}


midi_device_t* devmgr_GetByMidiHandle(HMIDIIN hMidiIn)
{
    assert(hMidiIn >= 0);

    midi_device_t* pdev = NULL;

    // hndchan = 0;
    for (int i = 0; i < NUM_MIDI_DEVICES && pdev == NULL; i++)
    {
        if (_devices[i].hnd_in == hMidiIn)// && _devices[i].channels[chan_num - 1]) // test for -1
        {
            pdev = _devices + i;
            // hndchan = MAKE_HANDLE(i, chan_num);
        }
    }

    return pdev;
}

midi_device_t* devmgr_GetOutputByChannelHandle(int hndchan)
{
    assert(hndchan);
    midi_device_t* pdev = NULL;

    // Validate user lua args.
    int chan_num = devmgr_GetChannelNumberFromChannelHandle(hndchan);
    assert(chan_num >= 1 && chan_num <= NUM_MIDI_CHANNELS);
    int dev_index = devmgr_GetDevIndexFromChannelHandle(hndchan);
    assert(dev_index >= 0 && dev_index < NUM_MIDI_DEVICES);

    if(_devices[dev_index].hnd_out > 0 && _devices[dev_index].channels[chan_num - 1])
    {
        pdev = _devices + dev_index;
    }

    // if (chan_num >= 1 && chan_num <= NUM_MIDI_CHANNELS &&  //midi_device_t* devmgr_Get(hndchan)
    //     volume >= 0.0 && volume <= 1.0 &&
    //     dev_index >= 0 && dev_index < NUM_MIDI_DEVICES)
    // {
    //     if(_devices[dev_index].hnd_out > 0 && _devices[dev_index].channels[chan_num - 1])
    //     {
    //         int cmd = volume == 0.0 ? MIDI_NOTE_OFF : MIDI_NOTE_ON;
    //         int velocity = (int)(volume * MIDI_VAL_MAX);
    //         int short_msg = (chan_num - 1) + cmd + ((byte)note_num << 8) + ((byte)velocity << 16);
    //         ret = midiOutShortMsg(_devices[dev_index].hnd_out, short_msg);
    //     }
    // }
    // else
    // {
    //     ret = NEB_ERR_BAD_LUA_ARG;
    //     logger_Log(LVL_ERROR, "NEB_ERR_BAD_LUA_ARG %d", __LINE__);
    // }

    return pdev;
}

midi_device_t* devmgr_GetByName(const char* sys_dev_name)
{
    midi_device_t* pdev = NULL;

    int hndchan = 0; // default = invalid

    // Look through devices list for this device.//midi_device_t* devmgr_Get(sys_dev_name, channel)
    for (int i = 0; i < NUM_MIDI_DEVICES && pdev == NULL; i++)
    {
        if (strcmp(sys_dev_name, _devices[i].sys_dev_name) == 0)
        {
            pdev = _devices + i;

            // // Valid device. Make a simple handle from the index and the channel number.
            // hndchan = MAKE_HANDLE(i, chan_num);
            // _devices[i].channels[chan_num - 1] = true;

            // if (_devices[i].hnd_out > 0)
            // {
            //     // Send patch now.
            //     int short_msg = (chan_num - 1) + MIDI_PATCH_CHANGE + (patch << 8);
            //     int ret = midiOutShortMsg(_devices[i].hnd_out, short_msg);
            // }

            break; // done
        }
    }

    return pdev;
}
