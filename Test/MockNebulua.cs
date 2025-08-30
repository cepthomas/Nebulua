using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using NAudio.Midi;
using Ephemera.NBagOfTricks;


namespace Nebulua
{
    public class MidiInput //: IDisposable
    {
        public string DeviceName { get; } = "???";
        public bool[] Channels { get; } = new bool[Common.NUM_MIDI_CHANNELS];
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
    }

    public class MidiOutput //: IDisposable
    {
        public string DeviceName { get; } = "???";
        public bool[] Channels { get; } = new bool[Common.NUM_MIDI_CHANNELS];
        public bool Enable { get; set; } = true;

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
        public Color BackColor { get; set; } = Color.LightYellow;
        [JsonConverter(typeof(JsonColorConverter))]
        public Color IconColor { get; set; } = Color.Purple;
        [JsonConverter(typeof(JsonColorConverter))]
        public Color PenColor { get; set; } = Color.Black;
        [JsonConverter(typeof(JsonColorConverter))]
        public Color ActiveColor { get; set; } = Color.DodgerBlue;
        [JsonConverter(typeof(JsonColorConverter))]
        public Color SelectedColor { get; set; } = Color.Moccasin;

        public bool WordWrap { get; set; } = false;
        public bool MonitorRcv { get; set; } = false;
        public bool MonitorSnd { get; set; } = false;
    }
}
