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
static midi_device_t _output_devices[NUM_MIDI_DEVICES];

// Devices specified in the user script.
static midi_device_t _input_devices[NUM_MIDI_DEVICES];

// The common input event handler.
static midi_input_handler_t _midi_input_handler;


//------------------- Functions ---------------------------//

// Open a midi input.
MMRESULT _OpenMidiIn(int dev_index);

// Open a midi output.
MMRESULT _OpenMidiOut(int dev_index);

// Diagnostics.
void devmgr_Dump();


//--------------------------------------------------------//
void devmgr_Dump()
{
    // Inputs.
    for (int i = 0; i < NUM_MIDI_DEVICES; i++)
    {
        midi_device_t* pdev = _input_devices + i;

        printf("Midi Input %d:  name:%s handle:%p channels: ", i, pdev->sys_dev_name, pdev->handle);
        for (int c = 0; c < NUM_MIDI_CHANNELS; c++)
        {
            if (pdev->channels[c])
            {
                printf("%2x ", c);
            }
        }
        printf("\n");
    }

    // Outputs.
    for (int i = 0; i < NUM_MIDI_DEVICES; i++)
    {
        midi_device_t* pdev = _output_devices + i;

        printf("Midi Output %d: name:%s handle:%p channels: ", i, pdev->sys_dev_name, pdev->handle);
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


//--------------------------------------------------------//
int devmgr_Init(midi_input_handler_t midi_input_handler)
{
    int stat = NEB_OK; //C:\Dev\repos\Lua\Nebulua\source\devmgr.c:31

    _midi_input_handler = midi_input_handler;

    memset(_input_devices, 0, sizeof(_input_devices));
    memset(_output_devices, 0, sizeof(_output_devices));
    UINT num_in = midiInGetNumDevs();
    UINT num_out = midiOutGetNumDevs();

    // Inputs.
    if (midi_input_handler > 0 && num_in <= NUM_MIDI_DEVICES)
    {
        for (UINT dev_index = 0; dev_index < num_in; dev_index++)
        {
            // https://learn.microsoft.com/en-us/windows/win32/api/mmeapi/nf-mmeapi-midiingetdevcaps
            MIDIINCAPS caps_in;
            MMRESULT mmres = midiInGetDevCaps(dev_index, &caps_in, sizeof(caps_in));
            if (mmres == MMSYSERR_NOERROR)
            {
                // Save the device info.
                strncpy(_input_devices[dev_index].sys_dev_name, caps_in.szPname, MAXPNAMELEN - 1);
                _input_devices[dev_index].handle = INACTIVE_DEV; // exists but not opened
            }
        }
    }
    else
    {
        stat = NEB_ERR_INTERNAL;
    }

    // Outputs.
    if (num_out <= NUM_MIDI_DEVICES)
    {
        for (int dev_index = 0; dev_index < num_out; dev_index++, dev_index++)
        {
            // https://learn.microsoft.com/en-us/windows/win32/api/mmeapi/nf-mmeapi-midioutgetdevcaps
            MIDIOUTCAPS caps_out;
            MMRESULT mmres = midiOutGetDevCaps(dev_index, &caps_out, sizeof(caps_out));
            if (mmres == MMSYSERR_NOERROR)
            {
                // Save the device info.
                strncpy(_output_devices[dev_index].sys_dev_name, caps_out.szPname, MAXPNAMELEN);
                _output_devices[dev_index].handle = INACTIVE_DEV; // exists but not opened
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
    
    for (int i = 0; i < NUM_MIDI_DEVICES; i++)
    {
        if (_input_devices[i].handle > 0)
        {
            midiInStop(_input_devices[i].handle);
            midiInClose(_input_devices[i].handle); 
        }
    }
    
    for (int i = 0; i < NUM_MIDI_DEVICES; i++)
    {
        if (_output_devices[i].handle > 0)
        {
             midiOutClose(_output_devices[i].handle);
        }
    }

    return stat;
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
        for (int index = 0; index < NUM_MIDI_DEVICES && chan_hnd == 0; index++)
        {
            if ((_input_devices[index].handle == pdev->handle &&
                _input_devices[index].channels[chan_num - 1]) ||
                (_output_devices[index].handle == pdev->handle &&
                _output_devices[index].channels[chan_num - 1]))
            {
                chan_hnd = MAKE_HANDLE(index, chan_num);
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
            if (_input_devices[i].handle == hMidiIn) // input only
            {
                pdev = _input_devices + i;
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
        dev_index >= 0 &&
        dev_index < NUM_MIDI_DEVICES)
    {
        if( _output_devices[dev_index].handle > 0 &&  // output only
            _output_devices[dev_index].channels[chan_num - 1])
        {
            pdev = _output_devices + dev_index;
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
        if (strcmp(sys_dev_name, _input_devices[i].sys_dev_name) == 0)
        {
            pdev = _input_devices + i;
            break; // done
        }
        else if (strcmp(sys_dev_name, _output_devices[i].sys_dev_name) == 0)
        {
            pdev = _output_devices + i;
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

    if (dev_index >= 0 &&
        dev_index < NUM_MIDI_DEVICES &&
        _input_devices[dev_index].handle == INACTIVE_DEV)
    {
        HMIDIIN hmidi_in;
        // https://learn.microsoft.com/en-us/windows/win32/api/mmeapi/nf-mmeapi-midiinopen
        mmres = midiInOpen(&hmidi_in, dev_index, (DWORD_PTR)_midi_input_handler, (DWORD_PTR)dev_index, CALLBACK_FUNCTION);
        if (mmres == MMSYSERR_NOERROR)
        {
            // Save the device info.
            _input_devices[dev_index].handle = hmidi_in;
            // Fire it up.
            mmres = midiInStart(hmidi_in);
        }
    }
    else
    {
        // Already open or doesn't exist.
    }

    return mmres;
}

//--------------------------------------------------------//
MMRESULT _OpenMidiOut(int dev_index)
{
    MMRESULT mmres = MMSYSERR_BADDEVICEID;

    if (dev_index >= 0 &&
        dev_index < NUM_MIDI_DEVICES &&
        _output_devices[dev_index].handle == INACTIVE_DEV)
    {
        // https://learn.microsoft.com/en-us/windows/win32/api/mmeapi/nf-mmeapi-midioutopen
        HMIDIOUT hmidi_out;
        mmres = midiOutOpen(&hmidi_out, dev_index, 0, 0, 0);
        if (mmres == MMSYSERR_NOERROR)
        {
            // Save the device info.
            _output_devices[dev_index].handle = hmidi_out;
        }
    }
    else
    {
        // Already open or doesn't exist.
    }

    return mmres;
}
