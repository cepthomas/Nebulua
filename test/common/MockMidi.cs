using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NAudio.Midi;
using Ephemera.NBagOfTricks;
using Nebulua.Common;


/////////////////////////// from Midi.cs /////////////////////////////////
namespace Nebulua.Common ///Test
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

/////////////////////////// from NAudio.Midi /////////////////////////////////
namespace NAudio.Midi
{
    public enum MidiCommandCode : byte
    {
        NoteOff = 128,
        NoteOn = 144,
        KeyAfterTouch = 160,
        ControlChange = 176,
        PatchChange = 192,
        ChannelAfterTouch = 208,
        PitchWheelChange = 224,
        Sysex = 240,
        Eox = 247,
        TimingClock = 248,
        StartSequence = 250,
        ContinueSequence = 251,
        StopSequence = 252,
        AutoSensing = 254,
        MetaEvent = byte.MaxValue
    }

    public enum MidiController : byte
    {
        BankSelect = 0,
        Modulation = 1,
        BreathController = 2,
        FootController = 4,
        MainVolume = 7,
        Pan = 10,
        Expression = 11,
        BankSelectLsb = 32,
        Sustain = 64,
        Portamento = 65,
        Sostenuto = 66,
        SoftPedal = 67,
        LegatoFootswitch = 68,
        ResetAllControllers = 121,
        AllNotesOff = 123
    }


    public class NoteEvent : MidiEvent
    {
        public virtual int NoteNumber { get; set; } = 0;

        public int Velocity { get; set; } = 0;

        public NoteEvent(long absoluteTime, int channel, MidiCommandCode commandCode, int noteNumber, int velocity)
            //: base(absoluteTime, channel, commandCode)
        {
            NoteNumber = noteNumber;
            Velocity = velocity;
        }
    }

    public class NoteOnEvent : NoteEvent
    {
        public int NoteLength { get; set; } = 0;

        public NoteOnEvent(long absoluteTime, int channel, int noteNumber, int velocity, int duration)
        : base(absoluteTime, channel, MidiCommandCode.NoteOn, noteNumber, velocity)
        {
            //OffEvent = new NoteEvent(absoluteTime, channel, MidiCommandCode.NoteOff, noteNumber, 0);
            NoteLength = duration;
        }
    }


    public class ControlChangeEvent : MidiEvent
    {
        public MidiController Controller { get; set; } = 0;

        public int ControllerValue { get; set; } = 0;

        public ControlChangeEvent(long absoluteTime, int channel, MidiController controller, int controllerValue)
            //: base(absoluteTime, channel, MidiCommandCode.ControlChange)
        {
            Controller = controller;
            ControllerValue = controllerValue;
        }
    }

    public class PatchChangeEvent : MidiEvent
    {
        public int Patch { get; set; } = 0;

        public PatchChangeEvent(long absoluteTime, int channel, int patchNumber)
            //: base(absoluteTime, channel, MidiCommandCode.PatchChange)
        {
            Patch = patchNumber;
        }
    }


    public class MidiEvent
    {
        public virtual int Channel { get; set; } = 0;

        public int DeltaTime { get; set; } = 0;

        public long AbsoluteTime { get; set; } = 0;

        public MidiCommandCode CommandCode { get; set; } = 0;

        public static MidiEvent FromRawMessage(int rawMessage)
        {
            return new MidiEvent();
        }

        protected MidiEvent()
        {
        }
    }

    public class MidiInMessageEventArgs : EventArgs
    {
        public int RawMessage { get; private set; } = 0;
        public MidiEvent? MidiEvent { get; private set; }
        public int Timestamp { get; private set; } = 0;
        public MidiInMessageEventArgs(int message, int timestamp) { }
    }

    public class DeviceInfo()
    {
        public string ProductName { get; set; } = "";
    }

    public class MidiIn
    {
        public static int NumberOfDevices { get; set; } = 3;
        public static DeviceInfo DeviceInfo(int i)
        {
            return new DeviceInfo() { ProductName = $"DeviceIn{i}" };
        }

    }
    public class MidiOut
    {
        public static int NumberOfDevices { get; set; } = 3;
        public static DeviceInfo DeviceInfo(int i)
        {
            return new DeviceInfo() { ProductName = $"DeviceOut{i}" };
        }

    }
    //    for (int i = 0; i < MidiOut.NumberOfDevices; i++)
    //{
    //    _out.WriteLine("  " + MidiOut.DeviceInfo(i).ProductName);
    //}

    //_out.WriteLine($"Midi input devices:");
    //for (int i = 0; i < MidiIn.NumberOfDevices; i++)
    //{
    //    _out.WriteLine("  " + MidiIn.DeviceInfo(i).ProductName);
    //}

}
