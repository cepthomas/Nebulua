using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NAudio.Midi;
using Ephemera.NBagOfTricks;
using Ephemera.NBagOfTricks.Slog;


namespace Nebulua
{
    /// <summary>
    /// A midi input device.
    /// </summary>
    public class MidiInput
    {
        #region Fields
        /// <summary>Low level midi input device.</summary>
        readonly MidiIn? _midiIn = null;
        #endregion

        #region Properties
        /// <summary>Device name as defined by the system.</summary>
        public string DeviceName { get; }

        /// <summary>True if registered by script, 0-based.</summary>
        public bool[] Channels { get; } = new bool[Defs.NUM_MIDI_CHANNELS];

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
        /// Normal constructor.
        /// </summary>
        /// <param name="deviceName">Client must supply name of device.</param>
        public MidiInput(string deviceName)
        {
            DeviceName = deviceName;
            Channels.ForEach(b => b = false);

            // Figure out which midi input device.
            for (int i = 0; i < MidiIn.NumberOfDevices; i++)
            {
                if (deviceName == MidiIn.DeviceInfo(i).ProductName)
                {
                    _midiIn = new MidiIn(i);
                    _midiIn.MessageReceived += MidiIn_MessageReceived;
                    _midiIn.ErrorReceived += MidiIn_ErrorReceived;
                    break;
                }
            }

            if (_midiIn is null)
            {
                List<string> devs = ["Valid midi inputs:"];
                for (int i = 0; i < MidiIn.NumberOfDevices; i++)
                {
                    devs.Add($"\"{MidiIn.DeviceInfo(i).ProductName}\"");
                }
                throw new ArgumentOutOfRangeException($"Invalid input device name: {deviceName}. {string.Join(" ", devs)}");
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
        /// Process input midi event.
        /// </summary>
        void MidiIn_MessageReceived(object? sender, MidiInMessageEventArgs e)
        {
            // Decode the message. We only care about a few.
            MidiEvent evt = MidiEvent.FromRawMessage(e.RawMessage);

            // Is it in our registered inputs?
            int chan_num = evt.Channel;
            if (Channels[chan_num - 1])
            {
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
    public class MidiOutput
    {
        #region Fields
        /// <summary>Low level midi output device.</summary>
        readonly MidiOut? _midiOut = null;
        #endregion

        #region Properties
        /// <summary>Device name as defined by the system.</summary>
        public string DeviceName { get; }

        /// <summary>True if registered by script, 0-based.</summary>
        public bool[] Channels { get; } = new bool[Defs.NUM_MIDI_CHANNELS];
        #endregion

        #region Lifecycle
        /// <summary>
        /// Normal constructor.
        /// </summary>
        /// <param name="deviceName">Client must supply name of device.</param>
        public MidiOutput(string deviceName)
        {
            DeviceName = deviceName;
            // LogEnable = false;
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
                throw new ArgumentOutOfRangeException($"Invalid output device name: {deviceName}. {string.Join(" ", devs)}");
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
        /// <summary>Send midi event.</summary>
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
