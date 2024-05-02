using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Ephemera.NBagOfTricks;


/// <summary>Mock for entities in NAudio. See real class for doc.</summary>
namespace NAudio.Midi
{
    public enum MidiCommandCode : byte
    {
        NoteOff = 128, NoteOn = 144, KeyAfterTouch = 160, ControlChange = 176, PatchChange = 192, ChannelAfterTouch = 208,
        PitchWheelChange = 224, Sysex = 240, Eox = 247, TimingClock = 248, StartSequence = 250, ContinueSequence = 251,
        StopSequence = 252, AutoSensing = 254, MetaEvent = byte.MaxValue
    }

    public enum MidiController : byte
    {
        BankSelect = 0, Modulation = 1, BreathController = 2, FootController = 4, MainVolume = 7, Pan = 10, Expression = 11,
        BankSelectLsb = 32, Sustain = 64, Portamento = 65, Sostenuto = 66, SoftPedal = 67, LegatoFootswitch = 68,
        ResetAllControllers = 121, AllNotesOff = 123
    }

    public class MidiEvent
    {
        public virtual int Channel { get; set; } = 0;
        public int DeltaTime { get; set; } = 0;
        public long AbsoluteTime { get; set; } = 0;
        public MidiCommandCode CommandCode { get; set; } = 0;

        protected MidiEvent(long absoluteTime, int channel, MidiCommandCode commandCode)
        {
            AbsoluteTime = absoluteTime;
            Channel = channel;
            CommandCode = commandCode;
        }
    }

    public class NoteEvent : MidiEvent
    {
        public virtual int NoteNumber { get; set; } = 0;
        public int Velocity { get; set; } = 0;

        public NoteEvent(long absoluteTime, int channel, MidiCommandCode commandCode, int noteNumber, int velocity)
            : base(absoluteTime, channel, commandCode)
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
            NoteLength = duration;
        }
    }

    public class ControlChangeEvent : MidiEvent
    {
        public MidiController Controller { get; set; } = 0;
        public int ControllerValue { get; set; } = 0;

        public ControlChangeEvent(long absoluteTime, int channel, MidiController controller, int controllerValue)
            : base(absoluteTime, channel, MidiCommandCode.ControlChange)
        {
            Controller = controller;
            ControllerValue = controllerValue;
        }
    }

    public class PatchChangeEvent : MidiEvent
    {
        public int Patch { get; set; } = 0;

        public PatchChangeEvent(long absoluteTime, int channel, int patchNumber)
            : base(absoluteTime, channel, MidiCommandCode.PatchChange)
        {
            Patch = patchNumber;
        }
    }

    public class MidiInMessageEventArgs : EventArgs
    {
        public int RawMessage { get; private set; } = 0;
        public MidiEvent? MidiEvent { get; private set; }
        public int Timestamp { get; private set; } = 0;
        public MidiInMessageEventArgs(int message, int timestamp) { }
    }

    public class DeviceInfo
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
}
