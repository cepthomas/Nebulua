using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using Ephemera.NBagOfTricks;
using Ephemera.NBagOfTricks.Slog;


namespace Nebulua
{
    ///////////// Mock settings. /////////////
    public class UserSettings : SettingsCore
    {
        public static UserSettings Current { get; set; } = new();
        public string ScriptPath { get; set; } = "";
        public bool OpenLastFile { get; set; } = true;
        [JsonConverter(typeof(JsonStringEnumConverter))]
        public LogLevel FileLogLevel { get; set; } = LogLevel.Trace;
        [JsonConverter(typeof(JsonStringEnumConverter))]
        public LogLevel NotifLogLevel { get; set; } = LogLevel.Debug;
        [JsonConverter(typeof(JsonColorConverter))]
        public Color ForeColor { get; set; } = Color.Purple;
        [JsonConverter(typeof(JsonColorConverter))]
        public Color ControlColor { get; set; } = Color.DodgerBlue;
        [JsonConverter(typeof(JsonColorConverter))]
        public Color SelectedColor { get; set; } = Color.Moccasin;
        [JsonConverter(typeof(JsonColorConverter))]
        public Color BackColor { get; set; } = Color.LightYellow;
        public bool WordWrap { get; set; } = false;
        public bool MonitorRcv { get; set; } = false;
        public bool MonitorSnd { get; set; } = false;
    }    
}

///////////// Mock for entities in NAudio. See real class for doc. /////////////
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

        public int GetAsShortMessage()
        {
            return (Channel - 1) + (int)CommandCode;
        }

        public static MidiEvent FromRawMessage(int rawMessage)
        {
            long absoluteTime = 0;
            int b = rawMessage & 0xFF;
            int data1 = (rawMessage >> 8) & 0xFF;
            int data2 = (rawMessage >> 16) & 0xFF;
            MidiCommandCode commandCode;
            int channel = 1;

            if ((b & 0xF0) == 0xF0)
            {
                // both bytes are used for command code in this case
                commandCode = (MidiCommandCode)b;
            }
            else
            {
                commandCode = (MidiCommandCode)(b & 0xF0);
                channel = (b & 0x0F) + 1;
            }

            MidiEvent me;
            switch (commandCode)
            {
                case MidiCommandCode.NoteOn:
                case MidiCommandCode.NoteOff:
                case MidiCommandCode.KeyAfterTouch:
                    if (data2 > 0 && commandCode == MidiCommandCode.NoteOn)
                    {
                        me = new NoteOnEvent(absoluteTime, channel, data1, data2, 0);
                    }
                    else
                    {
                        me = new NoteEvent(absoluteTime, channel, commandCode, data1, data2);
                    }
                    break;
                case MidiCommandCode.ControlChange:
                    me = new ControlChangeEvent(absoluteTime, channel, (MidiController)data1, data2);
                    break;
                case MidiCommandCode.PatchChange:
                    me = new PatchChangeEvent(absoluteTime, channel, data1);
                    break;
                //case MidiCommandCode.ChannelAfterTouch:
                //    me = new ChannelAfterTouchEvent(absoluteTime, channel, data1);
                //    break;
                //case MidiCommandCode.PitchWheelChange:
                //    me = new PitchWheelChangeEvent(absoluteTime, channel, data1 + (data2 << 7));
                //    break;
                case MidiCommandCode.TimingClock:
                case MidiCommandCode.StartSequence:
                case MidiCommandCode.ContinueSequence:
                case MidiCommandCode.StopSequence:
                case MidiCommandCode.AutoSensing:
                    me = new MidiEvent(absoluteTime, channel, commandCode);
                    break;
                //case MidiCommandCode.MetaEvent:
                //case MidiCommandCode.Sysex:
                default:
                    throw new FormatException(String.Format("Unsupported MIDI Command Code for Raw Message {0}", commandCode));
            }
            return me;
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

        public event EventHandler<MidiInMessageEventArgs>? MessageReceived;
        public event EventHandler<MidiInMessageEventArgs>? ErrorReceived;

        public MidiIn(int i)
        {
        }

        public void Dispose()
        {
        }

        public void Start()
        {
        }

        public void Stop()
        {
        }

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

        public MidiOut(int  i)
        {
        }

        public void Dispose()
        {
        }

        public void Start()
        {
        }

        //_midiOut?.Send(evt.GetAsShortMessage());
        public void Send(int shortMsg)
        {
        }
    }
 }
