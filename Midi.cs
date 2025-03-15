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
        // Midi defs.
        public const int MIDI_VAL_MIN = 0;
        public const int MIDI_VAL_MAX = 127;
        public const int NUM_MIDI_CHANNELS = 16;
    }

    /// <summary>
    /// A midi input device.
    /// </summary>
    public class MidiInput : IDisposable
    {
        #region Fields
        /// <summary>Low level real midi input device.</summary>
        readonly MidiIn? _midiIn = null;
        #endregion

        #region Properties
        /// <summary>Device name as defined by the system.</summary>
        public string DeviceName { get; }

        /// <summary>True if registered by script, 0-based.</summary>
        public bool[] Channels { get; } = new bool[MidiDefs.NUM_MIDI_CHANNELS];

        /// <summary>Device capture on/off.</summary>
        public bool CaptureEnable
        {
            get { return _capturing; }
            set { if (value) _midiIn?.Start(); else _midiIn?.Stop(); _capturing = value; }
        }
        bool _capturing = false;
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
        public MidiInput(string deviceName)
        {
            bool valid = false;
            DeviceName = deviceName;
            Channels.ForEach(b => b = false);

            // Figure out which midi input device. Check internals first.
            if (deviceName == "ClickClack")
            {
                // ok, do nothing.
                valid = true;
            }
            else // Real device.
            {
                for (int i = 0; i < MidiIn.NumberOfDevices; i++)
                {
                    if (deviceName == MidiIn.DeviceInfo(i).ProductName)
                    {
                        _midiIn = new MidiIn(i);
                        _midiIn.MessageReceived += MidiIn_MessageReceived;
                        _midiIn.ErrorReceived += MidiIn_ErrorReceived;
                        valid = true;
                        break;
                    }
                }
            }

            if (!valid)
            {
                List<string> devs = ["Valid midi inputs:"];
                for (int i = 0; i < MidiIn.NumberOfDevices; i++)
                {
                    devs.Add($"\"{MidiIn.DeviceInfo(i).ProductName}\"");
                }
                throw new SyntaxException($"Invalid input device name: {deviceName}. {string.Join(" ", devs)}");
            }
            else
            {
                CaptureEnable = true;
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

            // Is it in our registered inputs?
            int chan_num = evt.Channel;
            if (Channels[chan_num - 1])
            {
                // Invoke takes care of cross-thread issues.
                ReceiveEvent?.Invoke(this, evt);
            }
            // else ignore.
        }

        /// <summary>
        /// Process error midi event - Parameter 1 is invalid. Do I care?
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
    public class MidiOutput : IDisposable
    {
        #region Fields
        /// <summary>Low level midi output device.</summary>
        readonly MidiOut? _midiOut = null;
        #endregion

        #region Properties
        /// <summary>Device name as defined by the system.</summary>
        public string DeviceName { get; }

        /// <summary>True if registered by script, 0-based.</summary>
        public bool[] Channels { get; } = new bool[MidiDefs.NUM_MIDI_CHANNELS];
        #endregion

        #region Lifecycle
        /// <summary>
        /// Normal constructor. OK to throw in here.
        /// </summary>
        /// <param name="deviceName">Client must supply name of device.</param>
        /// <exception cref="SyntaxException"></exception>
        public MidiOutput(string deviceName)
        {
            DeviceName = deviceName;
            Channels.ForEach(b => b = false);

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
                    devs.Add($"\"{MidiOut.DeviceInfo(i).ProductName}\"");
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
            // Is it in our registered inputs?
            int chan_num = evt.Channel;
            if (Channels[chan_num - 1])
            {
                _midiOut?.Send(evt.GetAsShortMessage());
            }
        }
        #endregion
    }
}
