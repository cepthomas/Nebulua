using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NAudio.Midi;
using Ephemera.NBagOfTricks;


namespace Nebulua
{
    public class MidiDefs
    {
        /// <summary>Midi constant.</summary>
        public const int MIDI_VAL_MIN = 0;

        /// <summary>Midi constant.</summary>
        public const int MIDI_VAL_MAX = 127;

        /// <summary>Per device.</summary>
        public const int NUM_MIDI_CHANNELS = 16;

        /// <summary>Corresponds to midi velocity = 0.</summary>
        public const double VOLUME_MIN = 0.0;

        /// <summary>Corresponds to midi velocity = 127.</summary>
        public const double VOLUME_MAX = 1.0;

        /// <summary>Default value.</summary>
        public const double VOLUME_DEFAULT = 0.8;

        /// <summary>Allow UI controls some more headroom.</summary>
        public const double MAX_GAIN = 2.0;

        /// <summary>The normal drum channel.</summary>
        public const int DEFAULT_DRUM_CHANNEL = 10;

        /// <summary>Definitions from midi_defs.lua.</summary>
        public static Dictionary<int, string> Instruments { get; set; } = [];

        /// <summary>Definitions from midi_defs.lua.</summary>
        public static Dictionary<int, string> Drums { get; set; } = [];

        /// <summary>Definitions from midi_defs.lua.</summary>
        public static Dictionary<int, string> Controllers { get; set; } = [];

        /// <summary>Definitions from midi_defs.lua.</summary>
        public static Dictionary<int, string> DrumKits { get; set; } = [];
    }

    /// <summary>One channel in a midi device - in or out.</summary>
    public class MidiChannel
    {
        /// <summary>Channel name as defined by the script.</summary>
        public string ChannelName { get; set; } = "ZZZ";

        /// <summary>True if channel is active.</summary>
        public bool Enable { get; set; } = true;

        /// <summary>Current patch number. Only used for outputs.</summary>
        public int Patch { get; set; } = -1;
    }

    /// <summary>
    /// A midi input device.
    /// </summary>
    public class MidiInputDevice : IDisposable
    {
        #region Fields
        /// <summary>NAudio midi input device.</summary>
        readonly MidiIn? _midiIn = null;
        #endregion

        #region Properties
        /// <summary>Device name as defined by the system.</summary>
        public string DeviceName { get; }

        /// <summary>Info about device channels. Key is channel number, 1-based.</summary>
        public Dictionary<int, MidiChannel> Channels = [];
        #endregion

        #region Events
        /// <summary>Client needs to deal with this.</summary>
        public event EventHandler<MidiEvent>? ReceiveEvent;
        #endregion

        #region Lifecycle
        /// <summary>
        /// Normal constructor. OK to throw in here.
        /// </summary>
        /// <param name="deviceName">Client must supply name of device.</param>
        /// <exception cref="SyntaxException"></exception>
        public MidiInputDevice(string deviceName)
        {
            bool realInput = false;
            DeviceName = deviceName;

            // Figure out which midi input device.
            for (int i = 0; i < MidiIn.NumberOfDevices; i++)
            {
                if (deviceName == MidiIn.DeviceInfo(i).ProductName)
                {
                    _midiIn = new MidiIn(i);
                    _midiIn.MessageReceived += MidiIn_MessageReceived;
                    _midiIn.ErrorReceived += MidiIn_ErrorReceived;
                    realInput = true;
                    break;
                }
            }

            // Assume internal type.
            if (!realInput)
            {
                _midiIn = null;
            }
        }

        /// <summary>
        /// Resource clean up.
        /// </summary>
        public void Dispose()
        {
            _midiIn?.Stop();
            _midiIn?.Dispose();
        }
        #endregion

        #region Traffic
        /// <summary>
        /// Process real midi input event. Don't throw in this thread!
        /// </summary>
        void MidiIn_MessageReceived(object? sender, MidiInMessageEventArgs e)
        {
            // Decode the message. We only care about a few.
            MidiEvent evt = MidiEvent.FromRawMessage(e.RawMessage);

            // Is it in our registered inputs and enabled?
            var ch = Channels[evt.Channel - 1];
            if (ch is not null && ch.Enable)
            {
                // Invoke takes care of cross-thread issues.
                ReceiveEvent?.Invoke(this, evt);
            }
            // else ignore.
        }

        /// <summary>
        /// Process error midi event - parameter 1 is invalid. Do I care?
        /// </summary>
        void MidiIn_ErrorReceived(object? sender, MidiInMessageEventArgs e)
        {
            // string ErrorInfo = $"Message:0x{e.RawMessage:X8}";
        }
        #endregion
    }

    /// <summary>
    /// A midi output device.
    /// </summary>
    public class MidiOutputDevice : IDisposable
    {
        #region Fields
        /// <summary>NAudio midi output device.</summary>
        readonly MidiOut? _midiOut = null;
        #endregion

        #region Properties
        /// <summary>Device name as defined by the system.</summary>
        public string DeviceName { get; }

        /// <summary>Info about device channels. Key is channel number, 1-based.</summary>
        public Dictionary<int, MidiChannel> Channels = [];
        #endregion

        #region Lifecycle
        /// <summary>
        /// Normal constructor. OK to throw in here.
        /// </summary>
        /// <param name="deviceName">Client must supply name of device.</param>
        /// <exception cref="SyntaxException"></exception>
        public MidiOutputDevice(string deviceName)
        {
            DeviceName = deviceName;

            // Figure out which midi output device.
            for (int i = 0; i < MidiOut.NumberOfDevices; i++)
            {
                if (deviceName == MidiOut.DeviceInfo(i).ProductName)
                {
                    _midiOut = new MidiOut(i);
                    break;
                }
            }

            if (_midiOut is null)
            {
                List<string> devs = ["Valid midi outputs:"];
                for (int i = 0; i < MidiOut.NumberOfDevices; i++)
                {
                    devs.Add($"[{MidiOut.DeviceInfo(i).ProductName}]");
                }
                throw new SyntaxException($"Invalid output device name: {deviceName}. {string.Join(" ", devs)}");
            }
        }

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        public void Dispose()
        {
            // Resources.
            _midiOut?.Dispose();
        }
        #endregion

        #region Traffic
        /// <summary>
        /// Send midi event. OK to throw in here.
        /// </summary>
        public void Send(MidiEvent evt)
        {
            // Is it in our registered outputs and enabled?
            var ch = Channels[evt.Channel];
            if (ch is not null && ch.Enable)
            {
                _midiOut?.Send(evt.GetAsShortMessage());
            }
        }
        #endregion
    }
}
