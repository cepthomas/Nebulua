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
        public const int MIN_MIDI = 0;

        /// <summary>Midi constant.</summary>
        public const int MAX_MIDI = 127;

        /// <summary>Per device.</summary>
        public const int NUM_CHANNELS = 16;

        /// <summary>Default value.</summary>
        public const double DEFAULT_VOLUME = 0.8;

        /// <summary>Allow UI controls some more headroom.</summary>
        public const double MAX_VOLUME = 2.0;

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
        /// Normal constructor.
        /// </summary>
        /// <param name="deviceName">Client must supply name of device.</param>
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
                    _midiIn.Start();
                    realInput = true;
                    break;
                }
            }

            if (_midiIn is null)
            {
                if (deviceName == "ccMidiGen") // Assume internal type.
                {
                    _midiIn = null;
                    realInput = false;
                }
                else
                {
                    throw new AppException($"Invalid input midi device name [{deviceName}]");
                }
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
            if (Channels.TryGetValue(evt.Channel, out MidiChannel? value) && value.Enable)
            {
                // Invoke takes care of cross-thread issues.
                ReceiveEvent?.Invoke(this, evt);
            }
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
        /// <exception cref="LuaException"></exception>
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
                 throw new AppException($"Invalid output midi device name [{deviceName}]");
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
            if (Channels.TryGetValue(evt.Channel, out MidiChannel? value) && value.Enable)
            {
                _midiOut?.Send(evt.GetAsShortMessage());
            }
        }
        #endregion
    }

    
}
