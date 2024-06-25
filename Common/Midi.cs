using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NAudio.Midi;
using Ephemera.NBagOfTricks;
using Ephemera.NBagOfTricks.Slog;


namespace Nebulua.Common
{
    /// <summary>
    /// A midi input device.
    /// </summary>
    public class MidiInput : IDisposable
    {
        #region Fields
        /// <summary>Low level midi input device.</summary>
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
                throw new ScriptSyntaxException($"Invalid input device name: {deviceName}. {string.Join(" ", devs)}");
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
        /// Process input midi event. Dont throw!
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
                throw new ScriptSyntaxException($"Invalid output device name: {deviceName}. {string.Join(" ", devs)}");
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
        /// <summary>Send midi event. OK to throw in here.</summary>
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

    public class MidiDefs
    {
        #region Definitions
        // Midi caps.
        public const int MIDI_VAL_MIN = 0;

        // Midi caps.
        public const int MIDI_VAL_MAX = 127;

        // Midi per device.
        public const int NUM_MIDI_CHANNELS = 16;
        #endregion

        #region Drum names
        /// <summary>The GM midi drum definitions. TODO get from lua script.</summary>
        static readonly Dictionary<int, string> _drums = new()
        {
            { 035, "AcousticBassDrum" }, { 036, "BassDrum1" }, { 037, "SideStick" }, { 038, "AcousticSnare" }, { 039, "HandClap" },
            { 040, "ElectricSnare" }, { 041, "LowFloorTom" }, { 042, "ClosedHiHat" }, { 043, "HighFloorTom" }, { 044, "PedalHiHat" },
            { 045, "LowTom" }, { 046, "OpenHiHat" }, { 047, "LowMidTom" }, { 048, "HiMidTom" }, { 049, "CrashCymbal1" },
            { 050, "HighTom" }, { 051, "RideCymbal1" }, { 052, "ChineseCymbal" }, { 053, "RideBell" }, { 054, "Tambourine" },
            { 055, "SplashCymbal" }, { 056, "Cowbell" }, { 057, "CrashCymbal2" }, { 058, "Vibraslap" }, { 059, "RideCymbal2" },
            { 060, "HiBongo" }, { 061, "LowBongo" }, { 062, "MuteHiConga" }, { 063, "OpenHiConga" }, { 064, "LowConga" },
            { 065, "HighTimbale" }, { 066, "LowTimbale" }, { 067, "HighAgogo" }, { 068, "LowAgogo" }, { 069, "Cabasa" },
            { 070, "Maracas" }, { 071, "ShortWhistle" }, { 072, "LongWhistle" }, { 073, "ShortGuiro" }, { 074, "LongGuiro" },
            { 075, "Claves" }, { 076, "HiWoodBlock" }, { 077, "LowWoodBlock" }, { 078, "MuteCuica" }, { 079, "OpenCuica" },
            { 080, "MuteTriangle" }, { 081, "OpenTriangle" }
        };
        #endregion

        /// <summary>
        /// Create string suitable for logging.
        /// </summary>
        /// <param name="evt">Midi event to format.</param>
        /// <param name="tick">Current tick.</param>
        /// <param name="chan_hnd">Channel info.</param>
        /// <returns>Suitable string.</returns>
        public static string FormatMidiEvent(MidiEvent evt, int tick, int chan_hnd)
        {
            // Common part.
            (int index, int chan_num) = ChannelHandle.DeconstructHandle(chan_hnd);
            string s = $"{tick:00000} {MusicTime.Format(tick)} {evt.CommandCode} Dev:{index} Ch:{chan_num} ";

            switch (evt)
            {
                case NoteEvent e:
                    var snote = chan_num == 10 || chan_num == 16 ?
                        _drums.ContainsKey(e.NoteNumber) ? _drums[e.NoteNumber] : $"DRUM_{e.NoteNumber}" :
                        MusicDefinitions.NoteNumberToName(e.NoteNumber);
                    s = $"{s} {e.NoteNumber}:{snote} Vel:{e.Velocity}";
                    break;

                case ControlChangeEvent e:
                    var sctl = Enum.IsDefined(e.Controller) ? e.Controller.ToString() : $"CTLR_{e.Controller}";
                    s = $"{s} {(int)e.Controller}:{sctl} Val:{e.ControllerValue}";
                    break;

                default: // Ignore others for now.
                    break;
            }

            return s;
        }
    }
}
