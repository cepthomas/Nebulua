using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NAudio.Midi;
using Ephemera.NBagOfTricks;
// using Ephemera.NBagOfTricks.Slog;


namespace Ephemera.Nebulua
{
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
            // Decode the message. We only care about a few.
            MidiEvent evt = MidiEvent.FromRawMessage(e.RawMessage);
        }

        void MidiIn_ErrorReceived(object? sender, MidiInMessageEventArgs e)
        {
            // string ErrorInfo = $"Message:0x{e.RawMessage:X8}";
        }
    }

    /// <summary>
    /// A midi output device.
    /// </summary>
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


    public class MidiDefs
    {
        // Midi caps.
        public const int MIDI_VAL_MIN = 0;

        // Midi caps.
        public const int MIDI_VAL_MAX = 127;

        // Midi per device.
        public const int NUM_MIDI_CHANNELS = 16;

        public static string FormatMidiEvent(MidiEvent evt, int tick, int chan_hnd)
        {
            // Common part.
            (int index, int chan_num) = ChannelHandle.DeconstructHandle(chan_hnd);
            string s = $"{tick:00000} {MusicTime.Format(tick)} {evt.CommandCode} Dev:{index} Ch:{chan_num} ";

            //switch (evt)
            //{
            //    case NoteEvent e:
            //        var snote = chan_num == 10 || chan_num == 16 ?
            //            _drums.ContainsKey(e.NoteNumber) ? _drums[e.NoteNumber] : $"DRUM_{e.NoteNumber}" :
            //            MusicDefinitions.NoteNumberToName(e.NoteNumber);
            //        s = $"{s} {e.NoteNumber}:{snote} Vel:{e.Velocity}";
            //        break;

            //    case ControlChangeEvent e:
            //        var sctl = Enum.IsDefined(e.Controller) ? e.Controller.ToString() : $"CTLR_{e.Controller}";
            //        s = $"{s} {(int)e.Controller}:{sctl} Val:{e.ControllerValue}";
            //        break;

            //    default: // Ignore others for now.
            //        break;
            //}

            return s;
        }
    }

}
