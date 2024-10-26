using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NAudio.Midi;
using Ephemera.NBagOfTricks;


namespace Nebulua
{
    public class MidiDefs
    {
        // Midi defs.
        public const int MIDI_VAL_MIN = 0;
        public const int MIDI_VAL_MAX = 127;
        public const int NUM_MIDI_CHANNELS = 16;
    }

    /// <summary>Mock for entities in Midi.cs. See real class for doc.</summary>
    public class MidiInput
    {
        public string DeviceName { get; }
        public bool[] Channels { get; } = new bool[MidiDefs.NUM_MIDI_CHANNELS];
        public bool CaptureEnable { get; set; } = true;

        public event EventHandler<MidiEvent>? ReceiveEvent;

        public MidiInput(string deviceName)
        {
            DeviceName = deviceName;
            Channels.ForEach(b => b = false);
        }

        public void Dispose()
        {
        }

        void MidiIn_MessageReceived(object? sender, MidiInMessageEventArgs e)
        {
        }

        void MidiIn_ErrorReceived(object? sender, MidiInMessageEventArgs e)
        {
        }
    }

    /// <summary>Mock for entities in Midi.cs. See real class for doc.</summary>
    public class MidiOutput
    {
        public string DeviceName { get; }
        public bool[] Channels { get; } = new bool[MidiDefs.NUM_MIDI_CHANNELS];
        public MidiOutput(string deviceName)
        {
            DeviceName = deviceName;
            Channels.ForEach(b => b = false);
        }

        public void Dispose()
        {
        }

        public void Send(MidiEvent evt)
        {
        }
    }
}
