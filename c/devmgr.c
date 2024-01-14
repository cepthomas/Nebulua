#include <stdarg.h>
#include <string.h>
#include "logger.h"
#include "nebcommon.h"
#include "devmgr.h"


//--------------------- Defs -----------------------------//

// An opaque handle is used to identify channels between lua and c.
// It's a unique int formed by packing the index (0-based) of the device in _devices
// with the channel number (1-based).
#define MAKE_HANDLE(dev_index, chan_num) ((dev_index << 8) | (chan_num))
#define GET_DEV_INDEX(hndchan) ((hndchan >> 8) & 0xFF)
#define GET_CHAN_NUM(hndchan) (hndchan & 0xFF)


//------------------- Vars ---------------------------//

// Devices specified in the user script.
static midi_device_t _devices[NUM_MIDI_DEVICES];


//--------------------------------------------------------//
int devmgr_Init(DWORD_PTR midi_handler)
{
    int stat = NEB_OK;

    memset(_devices, 0, sizeof(_devices));
    int dev_index = 0;
    int num_in = midiInGetNumDevs();
    int num_out = midiOutGetNumDevs();

    if (midi_handler > 0 && (num_in + num_out <= NUM_MIDI_DEVICES))
    {
        // Inputs.
        for (int i = 0; i < num_in; i++, dev_index++)
        {
            MIDIINCAPS caps_in;
            MMRESULT res = midiInGetDevCaps(i, &caps_in, sizeof(caps_in));
            if (res != 0) { }
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
            MMRESULT res = midiOutGetDevCaps(i, &caps_out, sizeof(caps_out));
            if (res != 0) { }

            HMIDIOUT hmidi_out;
            // http://msdn.microsoft.com/en-us/library/dd798476%28VS.85%29.aspx
            res = midiOutOpen(&hmidi_out, i, 0, 0, 0);

            // Save the device info.
            _devices[dev_index].hnd_out = hmidi_out;
            _devices[dev_index].sys_dev_index = i;
            strncpy(_devices[dev_index].sys_dev_name, caps_out.szPname, MAXPNAMELEN);
        }
    }
    else
    {
        stat = NEB_ERR_INTERNAL;
    }

    return stat;
}


//--------------------------------------------------------//
int devmgr_Destroy()
{
    int stat = NEB_OK;
    for (int i = 0; i < NUM_MIDI_DEVICES; i++)
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


//--------------------------------------------------------//
int devmgr_GetChannelHandle(midi_device_t* pdev, int chan_num)
{
    int hndchan = 0; // default = invalid

    if (pdev != NULL &&
        chan_num >= 1 && chan_num <= NUM_MIDI_CHANNELS)
    {
        for (int i = 0; i < NUM_MIDI_DEVICES && hndchan == 0; i++)
        {
            if (_devices[i].hnd_in == pdev->hnd_in && _devices[i].channels[chan_num - 1]) // test for -1
            {
                hndchan = MAKE_HANDLE(i, chan_num);
            }
        }
    }

    return hndchan;
}


//--------------------------------------------------------//
midi_device_t* devmgr_GetDeviceFromMidiHandle(HMIDIIN hMidiIn)
{
    midi_device_t* pdev = NULL;

    if (hMidiIn > 0)
    {
        for (int i = 0; i < NUM_MIDI_DEVICES && pdev == NULL; i++)
        {
            if (_devices[i].hnd_in == hMidiIn)// && _devices[i].channels[chan_num - 1]) // test for -1
            {
                pdev = _devices + i;
            }
        }
    }

    return pdev;
}


//--------------------------------------------------------//
midi_device_t* devmgr_GetOutputDeviceFromChannelHandle(int hndchan)
{
    midi_device_t* pdev = NULL;

    int chan_num = GET_CHAN_NUM(hndchan);
    int dev_index = GET_DEV_INDEX(hndchan);

    if (hndchan > 0 &&
        chan_num >= 1 && chan_num <= NUM_MIDI_CHANNELS &&
        dev_index >= 0 && dev_index < NUM_MIDI_DEVICES)
    {
        if(_devices[dev_index].hnd_out > 0 && _devices[dev_index].channels[chan_num - 1])
        {
            pdev = _devices + dev_index;
        }
    }

    return pdev;
}


//--------------------------------------------------------//
midi_device_t* devmgr_GetDeviceFromName(const char* sys_dev_name)
{
    midi_device_t* pdev = NULL; // default = invalid

    // Look through devices list for this device.
    for (int i = 0; i < NUM_MIDI_DEVICES && pdev == NULL; i++)
    {
        if (strcmp(sys_dev_name, _devices[i].sys_dev_name) == 0)
        {
            pdev = _devices + i;
            break; // done
        }
    }

    return pdev;
}


//--------------------------------------------------------//
int devmgr_GetChannelNumber(int hndchan)
{
    int chan_num = GET_CHAN_NUM(hndchan);
    return chan_num >= 1 && chan_num <= NUM_MIDI_CHANNELS ? chan_num : 0;
}
