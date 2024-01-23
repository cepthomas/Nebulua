#include <cstdio>
#include <cstring>

#include "pnut.h"

extern "C"
{
#include "devmgr.h"
#include "logger.h"
}

/* TODO1-TEST test these:

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

*/

/////////////////////////////////////////////////////////////////////////////
UT_SUITE(DEVMGR_SILLY, "Test stuff.")
{
    int x = 1;
    int y = 2;

    UT_EQUAL(x, y);

    LOG_INFO(CAT_INIT, "Hello!");

    return 0;
}    
