#ifndef DEVMGR_H
#define DEVMGR_H

#include <windows.h>


//---------------- Public API ----------------------//

// Arbitrary cap each direction.
#define NUM_MIDI_DEVICES 8

// Midi cap per device.
#define NUM_MIDI_CHANNELS 16

/// Internal device management. TODO2 make these opaque?
typedef struct
{
    char sys_dev_name[MAXPNAMELEN];     // from system enumeration
    bool channels[NUM_MIDI_CHANNELS];   // true if created by script, 0-based
    HMIDIIN handle;                     // > 0 if valid and open
} midi_input_device_t;

/// Internal device management.
typedef struct
{
    char sys_dev_name[MAXPNAMELEN];     // from system enumeration
    bool channels[NUM_MIDI_CHANNELS];   // true if created by script, 0-based
    HMIDIOUT handle;                    // > 0 if valid and open
} midi_output_device_t;


/// Midi input handler.
typedef void (* midi_input_handler_t)(HMIDIIN hMidiIn, UINT wMsg, DWORD_PTR dwInstance, DWORD_PTR dwParam1, DWORD_PTR dwParam2);

/// Initialize the component.
/// @param[in] Midi input handler.
/// @return Status.
int devmgr_Init(midi_input_handler_t midi_input_handler);

/// Clean up component resources.
/// @return Status.
int devmgr_Destroy();

/// Request for device using win midi handle.
/// @param[in] hMidiIn System midi handle.
/// @return midi_input_device_t The device or NULL if invalid.
midi_input_device_t* devmgr_GetInputDeviceFromMidiHandle(HMIDIIN hMidiIn);

/// Request for device for channel handle.
/// @param[in] chan_hnd Channel handle.
/// @return midi_output_device_t The device or NULL if invalid.
midi_output_device_t* devmgr_GetOutputDeviceFromChannelHandle(int chan_hnd);

/// Request for device with name.
/// @param[in] sys_dev_name Device name.
/// @return midi_output_device_t The device or NULL if invalid.
midi_input_device_t* devmgr_GetInputDeviceFromName(const char* sys_dev_name);

/// Request for device with name.
/// @param[in] sys_dev_name Device name.
/// @return midi_output_device_t The device or NULL if invalid.
midi_output_device_t* devmgr_GetOutputDeviceFromName(const char* sys_dev_name);

/// Request for channel number on the device.
/// @param[in] pdev Device.
/// @param[in] chan_num Chanel number 1-16.
/// @return int Channel handle or 0 if invalid.
int devmgr_GetInputChannelHandle(midi_input_device_t* pdev, int chan_num);

/// Request for channel number on the device.
/// @param[in] pdev Device.
/// @param[in] chan_num Chanel number 1-16.
/// @return int Channel handle or 0 if invalid.
int devmgr_GetOutputChannelHandle(midi_output_device_t* pdev, int chan_num);

/// Request for channel number for channel handle.
/// @param[in] chan_hnd Channel handle.
/// @return int Channel number 1-16 or 0 if invalid.
int devmgr_GetChannelNumber(int chan_hnd);

/// Diagnostic. TODO2 remove.
void devmgr_Dump();

#endif // DEVMGR_H
