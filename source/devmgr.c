// system
#include <windows.h>
#include <stdlib.h>
#include <string.h>
// lua
// cbot
#include "logger.h"
// application
#include "nebcommon.h"
#include "devmgr.h"



//--------------------- Defs -----------------------------//

// An opaque handle is used to identify channels between lua and c.
// It's a unique int formed by packing the index (0-based) of the device in _devices with the channel number (1-based).
#define MAKE_HANDLE(dev_index, chan_num) ((dev_index << 8) | (chan_num))
#define GET_DEV_INDEX(chan_hnd) ((chan_hnd >> 8) & 0xFF)
#define GET_CHAN_NUM(chan_hnd) (chan_hnd & 0xFF)

// First half are input, second half are output.
#define NUM_MIDI_DEVICES 16

// Midi cap per device.
#define NUM_MIDI_CHANNELS 16


//------------------- Vars ---------------------------//

// Devices specified in the user script.
static midi_device_t _devices[NUM_MIDI_DEVICES];

// The common input event handler.
static midi_input_handler_t _midi_input_handler;


//------------------- Functions ---------------------------//

//--------------------------------------------------------//
int devmgr_Init(midi_input_handler_t midi_input_handler)
{
    int stat = NEB_OK;

    _midi_input_handler = midi_input_handler;

    memset(_devices, 0, sizeof(_devices));
    UINT num_in = midiInGetNumDevs();
    UINT num_out = midiOutGetNumDevs();

    // Inputs.
    if (midi_input_handler > 0 && num_in <= NUM_MIDI_DEVICES / 2)
    {
        for (UINT i = 0; i < num_in; i++)
        {
            midi_device_t* pdev = _devices + i;
            // https://learn.microsoft.com/en-us/windows/win32/api/mmeapi/nf-mmeapi-midiingetdevcaps
            MIDIINCAPS caps_in;
            MMRESULT mmres = midiInGetDevCaps(i, &caps_in, sizeof(caps_in));
            if (mmres == MMSYSERR_NOERROR)
            {
                // Save the device info.
                strncpy(pdev->sys_dev_name, caps_in.szPname, MAXPNAMELEN - 1);
                pdev->handle = INACTIVE_DEV; // exists but not opened
            }
        }
    }
    else
    {
        stat = NEB_ERR_INTERNAL;
    }

    // Outputs.
    if (num_out <= NUM_MIDI_DEVICES / 2)
    {
        for (UINT i = 0; i < num_out; i++, i++)
        {
            midi_device_t* pdev = _devices + NUM_MIDI_DEVICES / 2 + i;
            // https://learn.microsoft.com/en-us/windows/win32/api/mmeapi/nf-mmeapi-midioutgetdevcaps
            MIDIOUTCAPS caps_out;
            MMRESULT mmres = midiOutGetDevCaps(i, &caps_out, sizeof(caps_out));
            if (mmres == MMSYSERR_NOERROR)
            {
                // Save the device info.
                strncpy(pdev->sys_dev_name, caps_out.szPname, MAXPNAMELEN);
                pdev->handle = INACTIVE_DEV; // exists but not opened
            }
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
    
    for (int i = 0; i < NUM_MIDI_DEVICES / 2; i++)
    {
        if (_devices[i].handle > 0)
        {
            midiInStop(_devices[i].handle);
            midiInClose(_devices[i].handle);
        }
    }
    
    for (int i = NUM_MIDI_DEVICES / 2; i < NUM_MIDI_DEVICES; i++)
    {
        if (_devices[i].handle > 0)
        {
             midiOutClose(_devices[i].handle);
        }
    }

    // Clear.
    memset(_devices, 0, sizeof(_devices));

    return stat;
}

//--------------------------------------------------------//
int devmgr_RegisterChannel(midi_device_t* pdev, int chan_num)
{
    int chan_hnd = 0; // default = invalid

    if (pdev != NULL &&
        chan_num >= 1 &&
        chan_num <= NUM_MIDI_CHANNELS)
    {
        for (int i = 0; i < NUM_MIDI_DEVICES && chan_hnd == 0; i++)
        {
            if (_devices[i].handle == pdev->handle)
            {
                _devices[i].channels[chan_num - 1] = true;
                chan_hnd = MAKE_HANDLE(i, chan_num);
            }
        }
    }

    return chan_hnd;
}

//--------------------------------------------------------//
int devmgr_GetChannelHandle(midi_device_t* pdev, int chan_num)
{
    int chan_hnd = 0; // default = invalid

    // Search for a known handle - input or output.

    if (pdev != NULL &&
        chan_num >= 1 &&
        chan_num <= NUM_MIDI_CHANNELS)
    {
        for (int i = 0; i < NUM_MIDI_DEVICES && chan_hnd == 0; i++)
        {
            if (_devices[i].handle == pdev->handle && _devices[i].channels[chan_num - 1])
            {
                chan_hnd = MAKE_HANDLE(i, chan_num);
            }
        }
    }

    return chan_hnd;
}


//--------------------------------------------------------//
midi_device_t* devmgr_GetDeviceFromMidiHandle(HMIDIIN hMidiIn)
{
    midi_device_t* pdev = NULL;

    if (hMidiIn > 0)
    {
        for (int i = 0; i < NUM_MIDI_DEVICES / 2 && pdev == NULL; i++) // input only
        {
            if (_devices[i].handle == hMidiIn)
            {
                pdev = _devices + i;
            }
        }
    }

    return pdev;
}


//--------------------------------------------------------//
midi_device_t* devmgr_GetDeviceFromChannelHandle(int chan_hnd)
{
    midi_device_t* pdev = NULL;

    int chan_num = GET_CHAN_NUM(chan_hnd);
    int dev_index = GET_DEV_INDEX(chan_hnd);

    if (chan_hnd > 0 &&
        chan_num >= 1 &&
        chan_num <= NUM_MIDI_CHANNELS &&
        dev_index > NUM_MIDI_DEVICES / 2 &&  // output only
        dev_index < NUM_MIDI_DEVICES)
    {
        if( _devices[dev_index].handle > 0 && _devices[dev_index].channels[chan_num - 1])
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
int devmgr_GetChannelNumber(int chan_hnd)
{
    int chan_num = GET_CHAN_NUM(chan_hnd);
    return chan_num >= 1 && chan_num <= NUM_MIDI_CHANNELS ? chan_num : 0;
}


//--------------------------------------------------------//
int devmgr_OpenMidi(midi_device_t* pdev)
{
    int stat = NEB_ERR_BAD_MIDI_CFG;
    MMRESULT mmres = MMSYSERR_BADDEVICEID;

    if (pdev != NULL)
    {
        // Look through devices list for this device.
        for (int i = 0; i < NUM_MIDI_DEVICES; i++)
        {
            if (pdev == _devices + i && pdev->handle == INACTIVE_DEV) // Open it if not already.
            {
                if (i >= NUM_MIDI_DEVICES / 2) // output
                {
                    // https://learn.microsoft.com/en-us/windows/win32/api/mmeapi/nf-mmeapi-midioutopen
                    HMIDIOUT hmidi_out;
                    mmres = midiOutOpen(&hmidi_out, (i - NUM_MIDI_DEVICES / 2), 0, 0, 0);
                    if (mmres == MMSYSERR_NOERROR)
                    {
                        // Save the device info.
                        pdev->handle = hmidi_out;
                        stat = NEB_OK;
                    }
                }
                else // input
                {
                    HMIDIIN hmidi_in;
                    // https://learn.microsoft.com/en-us/windows/win32/api/mmeapi/nf-mmeapi-midiinopen
                    mmres = midiInOpen(&hmidi_in, i, (DWORD_PTR)_midi_input_handler, (DWORD_PTR)i, CALLBACK_FUNCTION);
                    if (mmres == MMSYSERR_NOERROR)
                    {
                        // Save the device info.
                        pdev->handle = hmidi_in;
                        // Fire it up.
                        mmres = midiInStart(hmidi_in);
                    }
                    if (mmres == MMSYSERR_NOERROR)
                    {
                        stat = NEB_OK;
                    }
                }
            }
        }
    }

    return stat;
}


//--------------------------------------------------------//
void devmgr_Dump()
{
    // Inputs.
    for (int i = 0; i < NUM_MIDI_DEVICES; i++)
    {
        midi_device_t* pdev = _devices + i;
        const char* dir = i >= NUM_MIDI_DEVICES / 2 ? "Output" : "Input";

        printf("Midi %s %d:  name:%s handle:%p channels: ", dir, i, pdev->sys_dev_name, pdev->handle);
        for (int c = 0; c < NUM_MIDI_CHANNELS; c++)
        {
            if (pdev->channels[c])
            {
                printf("%2x ", c);
            }
        }
        printf("\n");
    }
}
