#ifndef DEVMGR_H
#define DEVMGR_H

#include <windows.h>


//---------------- Public API ----------------------//

// Midi cap.
#define NUM_MIDI_DEVICES 16

// Midi cap per device.
#define NUM_MIDI_CHANNELS 16

// Device not valid.
#define INVALID_DEV (HANDLE)0

// Device valid but not open.
#define INACTIVE_DEV (HANDLE)1

/// All midi device management.
typedef struct
{
    char sys_dev_name[MAXPNAMELEN];     // from system enumeration
    bool channels[NUM_MIDI_CHANNELS];   // true if registered by script, 0-based
    HANDLE handle;                      // HMIDIIN or HMIDIOUT or INVALID_DEV or INACTIVE_DEV
} midi_device_t;

/// Midi input handler.
typedef void (* midi_input_handler_t)(HMIDIIN hMidiIn, UINT wMsg, DWORD_PTR dwInstance, DWORD_PTR dwParam1, DWORD_PTR dwParam2);


/// Initialize the component.
/// @param[in] Midi input handler.
/// @return Status.
int devmgr_Init(midi_input_handler_t midi_input_handler);

/// Clean up component resources.
/// @return Status.
int devmgr_Destroy();

/// Lazy open a midi device. If it's already open this is ignored. Input/output devices.
/// @param[in] pdev midi_device_t.
/// @return Status.
int devmgr_OpenMidi(midi_device_t* pdev);

/// Get device by name. Input/output devices.
/// @param[in] sys_dev_name Device name.
/// @return midi_device_t The device or NULL if invalid.
midi_device_t* devmgr_GetDeviceFromName(const char* sys_dev_name);

/// Get device using win32 midi handle. Input devices only.
/// @param[in] hMidiIn System midi handle.
/// @return midi_device_t The device or NULL if invalid.
midi_device_t* devmgr_GetDeviceFromMidiHandle(HMIDIIN hMidiIn);

/// Get device for channel handle. Output devices only.
/// @param[in] chan_hnd Channel handle.
/// @return midi_device_t The device or NULL if invalid.
midi_device_t* devmgr_GetDeviceFromChannelHandle(int chan_hnd);

/// Register channel for the device. Input/output devices.
/// @param[in] pdev Device.
/// @param[in] chan_num Channel number 1-16.
/// @return int Channel handle or 0 if invalid.
int devmgr_RegisterChannel(midi_device_t* pdev, int chan_num);

/// Get channel handle for the device. Input/output devices.
/// @param[in] pdev Device.
/// @param[in] chan_num Channel number 1-16.
/// @return int Channel handle or 0 if invalid.
int devmgr_GetChannelHandle(midi_device_t* pdev, int chan_num);

/// Get channel number for channel handle. Input/output devices.
/// @param[in] chan_hnd Channel handle.
/// @return int Channel number 1-16 or 0 if invalid.
int devmgr_GetChannelNumber(int chan_hnd);

/// Diagnostic.
void devmgr_Dump();

#endif // DEVMGR_H
