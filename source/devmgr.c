// system
#include <windows.h>
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


//------------------- Vars ---------------------------//

// Devices specified in the user script.
static midi_device_t _devices[NUM_MIDI_DEVICES];

// The common input event handler.
static midi_input_handler_t _midi_input_handler;


//------------------- Functions ---------------------------//

// Open a midi input.
// @param[in] dev_index place in our list.
// @return Status.
MMRESULT _OpenMidiIn(int dev_index);

// Open a midi output.
// @param[in] dev_index place in our list.
// @return Status.
MMRESULT _OpenMidiOut(int dev_index);


// C:\Gnu\mingw64\x86_64-w64-mingw32\include\windows.h
// intptr_t

void devmgr_Dump()
{
    // printf("")
    for (int i = 0; i < NUM_MIDI_DEVICES; i++)
    {
        midi_device_t* pdev = _devices + i;

        printf("Midi:  name:%s index:%d type:%d handle:%p channels: ", pdev->sys_dev_name, i, pdev->type, pdev->handle);
        for (int c = 0; c < NUM_MIDI_CHANNELS; c++)
        {
            if (pdev->channels[c])
            {
                printf("%2x ", c);
            }
        }
        printf("\n");

        // //if (pdev->hnd_in > 0)
        // {
        //     printf("Midi Input:  name:%s index:%d  hnd_in:%p channels: ", pdev->sys_dev_name, i, pdev->hnd_in);
        //     for (int c = 0; c < NUM_MIDI_CHANNELS; c++)
        //     {
        //         if (pdev->channels[c])
        //         {
        //             printf("%2x ", c);
        //         }
        //     }
        //     printf("\n");
        // }

        // //if (pdev->hnd_out > 0)
        // {
        //     printf("Midi Output: name:%s index:%d hnd_out:%p channels: ", pdev->sys_dev_name, i, pdev->hnd_out);
        //     for (int c = 0; c < NUM_MIDI_CHANNELS; c++)
        //     {
        //         if (pdev->channels[c])
        //         {
        //             printf("%2x ", c);
        //         }
        //     }
        //     printf("\n");
        // }
    }
}


//--------------------------------------------------------//
int devmgr_Init(midi_input_handler_t midi_input_handler)
{
    int stat = NEB_OK; //C:\Dev\repos\Lua\Nebulua\source\devmgr.c:31

    _midi_input_handler = midi_input_handler;

    memset(_devices, 0, sizeof(_devices));
    int dev_index = 0;
    UINT num_in = midiInGetNumDevs();
    UINT num_out = midiOutGetNumDevs();

    if (midi_input_handler > 0 && (num_in + num_out <= NUM_MIDI_DEVICES))
    {
        // Inputs.
        for (UINT i = 0; i < num_in; i++, dev_index++)
        {
            // https://learn.microsoft.com/en-us/windows/win32/api/mmeapi/nf-mmeapi-midiingetdevcaps
            MIDIINCAPS caps_in;
            MMRESULT mmres = midiInGetDevCaps(i, &caps_in, sizeof(caps_in));
            if (mmres != 0) { } // TODO2 error checking for win32 calls.

            // // https://learn.microsoft.com/en-us/windows/win32/api/mmeapi/nf-mmeapi-midiinopen
            // HMIDIIN hmidi_in;
            // mmres = midiInOpen(&hmidi_in, i, (DWORD_PTR)midi_input_handler, (DWORD_PTR)dev_index, CALLBACK_FUNCTION);

            // Save the device info.
            // _devices[dev_index].hnd_in = hmidi_in;
            _devices[dev_index].sys_dev_index = i;
            strncpy(_devices[dev_index].sys_dev_name, caps_in.szPname, MAXPNAMELEN - 1);
        }

        // Outputs.
        for (int i = 0; i < num_out; i++, dev_index++)
        {
            // https://learn.microsoft.com/en-us/windows/win32/api/mmeapi/nf-mmeapi-midioutgetdevcaps
            MIDIOUTCAPS caps_out;
            MMRESULT mmres = midiOutGetDevCaps(i, &caps_out, sizeof(caps_out));
            if (mmres != 0) { }

            // HMIDIOUT hmidi_out;
            // // https://learn.microsoft.com/en-us/windows/win32/api/mmeapi/nf-mmeapi-midioutopen
            // mmres = midiOutOpen(&hmidi_out, i, 0, 0, 0);

            // Save the device info.
            // _devices[dev_index].hnd_out = hmidi_out;
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
        if (_devices[i].handle > 0)
        {
            if (_devices[i].type == MIDI_INPUT)
            {
                midiInStop(_devices[i].handle);
                midiInClose(_devices[i].handle); 
            }
            else if (_devices[i].type == MIDI_OUTPUT)
            {
                midiOutClose(_devices[i].handle);
            }
        }
    }

    return stat;
}


//--------------------------------------------------------//
int devmgr_GetChannelHandle(midi_device_t* pdev, int chan_num)
{
    int chan_hnd = 0; // default = invalid

    if (pdev != NULL &&
        chan_num >= 1 &&
        chan_num <= NUM_MIDI_CHANNELS)
    {
        for (int i = 0; i < NUM_MIDI_DEVICES && chan_hnd == 0; i++)
        {
            if (_devices[i].type == MIDI_OUTPUT &&
                _devices[i].handle == pdev->handle &&
                _devices[i].channels[chan_num - 1]) // test for -1
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
        for (int i = 0; i < NUM_MIDI_DEVICES && pdev == NULL; i++)
        {
            if (_devices[i].type == MIDI_INPUT &&
                _devices[i].handle == hMidiIn)
                // && _devices[i].channels[chan_num - 1]) // test for -1
            {
                pdev = _devices + i;
            }
        }
    }

    return pdev;
}


//--------------------------------------------------------//
midi_device_t* devmgr_GetOutputDeviceFromChannelHandle(int chan_hnd)
{
    midi_device_t* pdev = NULL;

    int chan_num = GET_CHAN_NUM(chan_hnd);
    int dev_index = GET_DEV_INDEX(chan_hnd);

    if (chan_hnd > 0 &&
        chan_num >= 1 &&
        chan_num <= NUM_MIDI_CHANNELS &&
        dev_index >= 0 &&
        dev_index < NUM_MIDI_DEVICES)
    {
        if( _devices[dev_index].type == MIDI_OUTPUT &&
            _devices[dev_index].handle > 0 &&
            _devices[dev_index].channels[chan_num - 1])
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
MMRESULT _OpenMidiIn(int dev_index)
{
    MMRESULT mmres = MMSYSERR_BADDEVICEID;

    if (_devices[dev_index].type == MIDI_INPUT)
    {
        HMIDIIN hmidi_in;
        // https://learn.microsoft.com/en-us/windows/win32/api/mmeapi/nf-mmeapi-midiinopen
        mmres = midiInOpen(&hmidi_in, _devices[dev_index].sys_dev_index, (DWORD_PTR)_midi_input_handler, (DWORD_PTR)dev_index, CALLBACK_FUNCTION);
        if (mmres != 0) { }

        // Save the device info.
        _devices[dev_index].handle = hmidi_in;

        // Fire it up.
        mmres = midiInStart(hmidi_in);
    }

    return mmres;
}

//--------------------------------------------------------//
MMRESULT _OpenMidiOut(int dev_index)
{
    MMRESULT mmres = MMSYSERR_BADDEVICEID;

    if (_devices[dev_index].type == MIDI_OUTPUT)
    {
        // https://learn.microsoft.com/en-us/windows/win32/api/mmeapi/nf-mmeapi-midioutopen
        HMIDIOUT hmidi_out;
        MMRESULT mmres = midiOutOpen(&hmidi_out, _devices[dev_index].sys_dev_index, 0, 0, 0);
        if (mmres != 0) { }

        // Save the device info.
        _devices[dev_index].handle = hmidi_out;
    }

    return mmres;
}
