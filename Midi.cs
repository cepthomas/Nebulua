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
    /// A midi output layer - associated with a single device.
    /// </summary>
    public class MidiOutput
    {
        #region Fields
        /// <summary>Low level midi output device.</summary>
        readonly MidiOut? _midiOut = null;

        /// <summary>My logger.</summary>
        readonly Logger _logger = LogManager.CreateLogger("MidiOut");
        #endregion

        #region Properties
        /// <summary>Device name as defined by the system.</summary>
        public string DeviceName { get; }

        /// <summary>Are we ok?</summary>
        public bool Valid { get { return _midiOut is not null; } }

        /// <summary>True if registered by script, 0-based.</summary>
        public bool[] Channels { get; } = new bool[Defs.NUM_MIDI_CHANNELS];

        /// <summary>Log traffic at Trace level.</summary>
        public bool LogEnable { get { return _logger.Enable; } set { _logger.Enable = value; } }
        #endregion

        #region Lifecycle
        /// <summary>
        /// Normal constructor.
        /// </summary>
        /// <param name="deviceName">Client must supply name of device.</param>
        public MidiOutput(string deviceName)
        {
            DeviceName = deviceName;
            LogEnable = false;
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

        /// <summary>Send midi event.</summary>
        public void SendEvent(MidiEvent evt)
        {
            if(_midiOut is not null)
            {
                _midiOut.Send(evt.GetAsShortMessage());
                if (LogEnable)
                {
                    _logger.Trace(evt.ToString());
                }
            }
        }
    }


    /// <summary>
    /// A midi input layer - associated with a single device.
    /// </summary>
    public class MidiInput
    {
        #region Fields
        /// <summary>Low level midi input device.</summary>
        readonly MidiIn? _midiIn = null;

        /// <summary>Control.</summary>
        bool _capturing = false;

        /// <summary>My logger.</summary>
        readonly Logger _logger = LogManager.CreateLogger("MidiIn");
        #endregion

        #region Properties
        /// <summary>Device name as defined by the system.</summary>
        public string DeviceName { get; }

        /// <summary>Are we ok?</summary>
        public bool Valid { get { return _midiIn is not null; } }

        /// <summary>True if registered by script, 0-based.</summary>
        public bool[] Channels { get; } = new bool[Defs.NUM_MIDI_CHANNELS];

        /// <summary>Log traffic at Trace level.</summary>
        public bool LogEnable { get { return _logger.Enable; } set { _logger.Enable = value; } }

        /// <summary>Capture on/off.</summary>
        public bool CaptureEnable
        {
            get { return _capturing; }
            set { if (value) _midiIn?.Start(); else _midiIn?.Stop(); _capturing = value; }
        }
        #endregion

        #region Events
        /// <summary>Client needs to deal with this.</summary>
        public event EventHandler<MidiEvent>? InputReceiveEvent;
        #endregion

        #region Lifecycle
        /// <summary>
        /// Normal constructor.
        /// </summary>
        /// <param name="deviceName">Client must supply name of device.</param>
        public MidiInput(string deviceName)
        {
            DeviceName = deviceName;
            LogEnable = false;
            
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

        /// <summary>
        /// Process input midi event.
        /// </summary>
        void MidiIn_MessageReceived(object? sender, MidiInMessageEventArgs e)
        {
            // Decode the message. We only care about a few.
            MidiEvent me = MidiEvent.FromRawMessage(e.RawMessage);

            // Is it in our registered inputs?
            int chan_num = me.Channel;
            if (Channels[chan_num - 1])
            {
                InputReceiveEvent?.Invoke(this, me);
            }
            // else ignore.
        }

        /// <summary>
        /// Process error midi event - Parameter 1 is invalid.
        /// </summary>
        void MidiIn_ErrorReceived(object? sender, MidiInMessageEventArgs e)
        {
            // string ErrorInfo = $"Message:0x{e.RawMessage:X8}";
            // _logger.Trace(ErrorInfo);
        }
    }
}
