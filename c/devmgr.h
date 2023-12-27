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
// #include "lualib.h"
// #include "lauxlib.h"
// #include "luainterop.h"
// #include "luainteropwork.h"
// #include "logger.h"

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
// Macros to do the pack/unpack.


// Validate user lua args. TODO1 refactor?
    // if (chan_num >= 1 && chan_num <= NUM_MIDI_CHANNELS &&
    //     devi >= 0 && devi < NUM_MIDI_DEVICES &&
// && _devices[i].channels[c]...


//----------------------- Publics -----------------------------//

int devmgr_Init();

int devmgr_Destroy();

midi_device_t* devmgr_GetByIndex(int dev_index);


midi_device_t* devmgr_GetByMidiHandle(HMIDIIN hMidiIn);

midi_device_t* devmgr_GetOutputByChannelHandle(int hndchan);


midi_device_t* devmgr_GetByName(const char* sys_dev_name);

int devmgr_GetChannelHandle(midi_device_t* pdev, int chan_num);

// int devmgr_MakeChannelHandle(int dev_index, int chan_num)
int devmgr_GetChannelNumberFromChannelHandle(int hndchan);
// int devmgr_GetDevIndexFromChannelHandle(int hndchan);

#endif // DEVMGR_H
