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


//----------------------- Defs -----------------------------//

// Arbitrary cap.
#define NUM_MIDI_DEVICES 16

// Midi cap per device.
#define NUM_MIDI_CHANNELS 16


//----------------------- Types -----------------------------//

// Internal device management.
typedef struct
{
    char sys_dev_name[MAXPNAMELEN]; // from system enumeration
    int sys_dev_index; // from system enumeration
    bool channels[NUM_MIDI_CHANNELS]; // true if created by script, 0-based
    HMIDIIN hnd_in;
    HMIDIOUT hnd_out;
} midi_device_t;


// A handle is used to identify channels between lua and c. It's a unique packed int.


//----------------------- Publics -----------------------------//

// Initialize the component.
// @return Status.
int devmgr_Init();

// Clean up component resources.
// @return Status.
int devmgr_Destroy();

// Request.
// @param[in] hMidiIn System midi handle.
// @return midi_device_t The device or NULL if invalid.
midi_device_t* devmgr_GetDeviceFromMidiHandle(HMIDIIN hMidiIn);

// Request.
// @param[in] hndchan Channel handle.
// @return midi_device_t The device or NULL if invalid.
midi_device_t* devmgr_GetOutputDeviceFromChannelHandle(int hndchan);

// Request.
// @param[in] sys_dev_name Device name.
// @return midi_device_t The device or NULL if invalid.
midi_device_t* devmgr_GetDeviceFromName(const char* sys_dev_name);

// Request.
// @param[in] pdev Device.
// @param[in] chan_num Chanel number 1-16.
// @return int Channel handle.
int devmgr_GetChannelHandle(midi_device_t* pdev, int chan_num);

// Request.
// @param[in] hndchan Channel handle.
// @return int Channel number 1-16.
int devmgr_GetChannelNumber(int hndchan);

#endif // DEVMGR_H
