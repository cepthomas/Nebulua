#ifndef MIDI_H
#define MIDI_H

// Midi caps.
#define MIDI_VAL_MIN 0

// Midi caps.
#define MIDI_VAL_MAX 127

/// Midi events.
typedef enum
{
    // Channel events 0x80-0x8F
    MIDI_NOTE_OFF = 0x80,               // 2 - 1 byte pitch, followed by 1 byte velocity
    MIDI_NOTE_ON = 0x90,                // 2 - 1 byte pitch, followed by 1 byte velocity
    MIDI_KEY_AFTER_TOUCH = 0xA0,        // 2 - 1 byte pitch, 1 byte pressure (after-touch)
    MIDI_CONTROL_CHANGE = 0xB0,         // 2 - 1 byte parameter number, 1 byte setting
    MIDI_PATCH_CHANGE = 0xC0,           // 1 byte program selected
    MIDI_CHANNEL_AFTER_TOUCH = 0xD0,    // 1 byte channel pressure (after-touch)
    MIDI_PITCH_WHEEL_CHANGE = 0xE0,     // 2 bytes gives a 14 bit value, least significant 7 bits first
    // System events - no channel.
    MIDI_SYSEX = 0xF0,
    MIDI_EOX = 0xF7,
    MIDI_TIMING_CLOCK = 0xF8,
    MIDI_START_SEQUENCE = 0xFA,
    MIDI_CONTINUE_SEQUENCE = 0xFB,
    MIDI_STOP_SEQUENCE = 0xFC,
    MIDI_AUTO_SENSING = 0xFE,
    MIDI_META_EVENT = 0xFF,
} midi_event_t;


#endif // MIDI_H
