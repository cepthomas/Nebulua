#ifndef MIDI_H
#define MIDI_H

// #include <windows.h>
// #include <stdio.h>
// #include <stdlib.h>
// #include <stdbool.h>
// #include <string.h>
// #include <time.h>
// #include <unistd.h>
// #include "lua.h"


///// Midi defs

// ///// constants
// // Internal/app resolution aka DeltaTicksPerQuarterNote or subbeats per beat.
// int InternalPPQ = 32;
// // Only 4/4 time supported.
// int BeatsPerBar = 4;
// // Convenience.
// int SubbeatsPerBeat = InternalPPQ;
// // Convenience.
// int SubeatsPerBar = InternalPPQ * BeatsPerBar;
///// bar vars - all zero-based.
// int TotalSubbeats = 0;
// int TotalBeats = TotalSubbeats / SubbeatsPerBeat;
// // The bar number.
// int Bar = TotalSubbeats / SubeatsPerBar;
// // The beat number in the bar.
// int Beat = TotalSubbeats / SubbeatsPerBeat % BeatsPerBar;
// // The subbeat in the beat.
// int Subbeat = TotalSubbeats % SubbeatsPerBeat;
///// app vars
// readonly double Tempo;
// double InternalPeriod()
// {
//     double secPerBeat = 60.0 / Tempo;
//     double msecPerT = 1000 * secPerBeat / SubbeatsPerBeat;
//     return msecPerT;
// }

// int RoundedInternalPeriod()
// {
//     double msecPerT = InternalPeriod();
//     int period = msecPerT > 1.0 ? (int)Math.Round(msecPerT) : 1;
//     return period;
// }

// double InternalToMsec(int t)
// {
//     double msec = InternalPeriod() * t;
//     return msec;
// }


// TODO1 these are not actually midi.
// Only 4/4 time supported.
#define BEATS_PER_BAR 4

// Internal/app resolution aka DeltaTicksPerQuarterNote or subbeats per beat.
#define INTERNAL_PPQ 32

// Convenience.
#define SUBBEATS_PER_BEAT INTERNAL_PPQ

// Convenience.
#define SUBEATS_PER_BAR SUBBEATS_PER_BEAT * BEATS_PER_BAR

// Total.
#define TOTAL_BEATS(total_subbeats) total_subbeats / SUBBEATS_PER_BEAT

// The bar number.
#define BAR(total_subbeats) total_subbeats / SUBEATS_PER_BAR

// The beat number in the bar.
#define BEAT(total_subbeats) total_subbeats / SUBBEATS_PER_BEAT % BEATS_PER_BAR

// // The subbeat in the beat.
#define SUBBEAT(total_subbeats) total_subbeats % SUBBEATS_PER_BEAT


double InternalPeriod()
{
    double secPerBeat = 60.0 / Tempo;
    double msecPerT = 1000 * secPerBeat / SubbeatsPerBeat;
    return msecPerT;
}

int RoundedInternalPeriod()
{
    double msecPerT = InternalPeriod();
    int period = msecPerT > 1.0 ? (int)Math.Round(msecPerT) : 1;
    return period;
}

double InternalToMsec(int t)
{
    double msec = InternalPeriod() * t;
    return msec;
}


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




// // Resolution for midi file events aka DeltaTicksPerQuarterNote - not needed unless doing file IO
// readonly int MidiFilePpq_F;
// ///// MidiTimeConverter
// long InternalToMidi_F(int t)
// {
//     long mtime = t * MidiFilePpq_F / SubbeatsPerBeat;
//     return mtime;
// }
// int MidiToInternal_F(long t)
// {
//     long itime = t * SubbeatsPerBeat / MidiFilePpq_F;
//     return (int)itime;
// }
// double MidiToSec_F(int t)
// {
//     double msec = MidiPeriod_F() * t / 1000.0;
//     return msec;
// }
// double MidiPeriod_F()
// {
//     double secPerBeat = 60.0 / Tempo;
//     double msecPerT = 1000 * secPerBeat / MidiFilePpq_F;
//     return msecPerT;
// }


#endif // MIDI_H
