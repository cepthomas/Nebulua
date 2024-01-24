#include <cstdio>
#include <cstring>

#include "pnut.h"

extern "C"
{
#include "nebcommon.h"
#include "devmgr.h"
#include "logger.h"
}


/////////////////////////////////////////////////////////////////////////////
UT_SUITE(DEVMGR_MAIN, "Test device manager.")
{
    int stat = 0;

    stat = devmgr_Init();
    UT_EQUAL(stat, NEB_OK);

    /// Request for device using win midi handle.
    /// @param[in] hMidiIn System midi handle.
    /// @return midi_device_t The device or NULL if invalid.
    midi_device_t* pdev = devmgr_GetDeviceFromMidiHandle((HMIDIIN)999);
    UT_EQUAL(pdev, (void*)NULL);

    /// Request for device for channel handle.
    /// @param[in] chan_hnd Channel handle.
    /// @return midi_device_t The device or NULL if invalid.
    pdev = devmgr_GetOutputDeviceFromChannelHandle(999);
    UT_EQUAL(pdev, (void*)NULL);

    /// Request for device with name.
    /// @param[in] sys_dev_name Device name.
    /// @return midi_device_t The device or NULL if invalid.
    pdev = devmgr_GetDeviceFromName("aaaaaaa");
    UT_EQUAL(pdev, (void*)NULL);

    /// Request for channel number on the device.
    /// @param[in] pdev Device.
    /// @param[in] chan_num Chanel number 1-16.
    /// @return int Channel handle or 0 if invalid.
    int chan_hnd = devmgr_GetChannelHandle(pdev, 999);
    UT_EQUAL(chan_hnd, 999);

    /// Request for channel number for channel handle.
    /// @param[in] chan_hnd Channel handle.
    /// @return int Channel number 1-16 or 0 if invalid.
    int chan_num = devmgr_GetChannelNumber(999);
    UT_EQUAL(chan_num, 999);


    stat = devmgr_Destroy();
    UT_EQUAL(stat, NEB_OK);

    return 0;
}    
