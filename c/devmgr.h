#ifndef DEVMGR_H
#define DEVMGR_H

#include <windows.h>
#include <stdio.h>
#include <stdlib.h>
#include <stdbool.h>
#include <string.h>
#include <time.h>
#include <unistd.h>
#include "lua.h"


//---------------- Public API ----------------------//

// Arbitrary cap.
#define NUM_MIDI_DEVICES 16

// Midi cap per device.
#define NUM_MIDI_CHANNELS 16


/// Internal device management.
typedef struct
{
    char sys_dev_name[MAXPNAMELEN]; // from system enumeration
    int sys_dev_index; // from system enumeration
    bool channels[NUM_MIDI_CHANNELS]; // true if created by script, 0-based
    HMIDIIN hnd_in;
    HMIDIOUT hnd_out;
} midi_device_t;


/// Initialize the component.
/// @return Status.
int devmgr_Init();

/// Clean up component resources.
/// @return Status.
int devmgr_Destroy();

/// Request for device using win midi handle.
/// @param[in] hMidiIn System midi handle.
/// @return midi_device_t The device or NULL if invalid.
midi_device_t* devmgr_GetDeviceFromMidiHandle(HMIDIIN hMidiIn);

/// Request for device for channel handle.
/// @param[in] hndchan Channel handle.
/// @return midi_device_t The device or NULL if invalid.
midi_device_t* devmgr_GetOutputDeviceFromChannelHandle(int hndchan);

/// Request for device with name.
/// @param[in] sys_dev_name Device name.
/// @return midi_device_t The device or NULL if invalid.
midi_device_t* devmgr_GetDeviceFromName(const char* sys_dev_name);

/// Request for channel number on the device.
/// @param[in] pdev Device.
/// @param[in] chan_num Chanel number 1-16.
/// @return int Channel handle or 0 if invalid.
int devmgr_GetChannelHandle(midi_device_t* pdev, int chan_num);

/// Request for channel number for channel handle.
/// @param[in] hndchan Channel handle.
/// @return int Channel number 1-16 or 0 if invalid.
int devmgr_GetChannelNumber(int hndchan);

#endif // DEVMGR_H
