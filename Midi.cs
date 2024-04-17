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
        /// Normal constructor.
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


        #region All the names TODO1 ? generate from midi_defs.lua at runtime.
        /// <summary>The GM midi instrument definitions.</summary>
        static readonly List<string> _instruments = new()
        {
            "AcousticGrandPiano", "BrightAcousticPiano", "ElectricGrandPiano", "HonkyTonkPiano", "ElectricPiano1", "ElectricPiano2", "Harpsichord",
            "Clavinet", "Celesta", "Glockenspiel", "MusicBox", "Vibraphone", "Marimba", "Xylophone", "TubularBells", "Dulcimer", "DrawbarOrgan",
            "PercussiveOrgan", "RockOrgan", "ChurchOrgan", "ReedOrgan", "Accordion", "Harmonica", "TangoAccordion", "AcousticGuitarNylon",
            "AcousticGuitarSteel", "ElectricGuitarJazz", "ElectricGuitarClean", "ElectricGuitarMuted", "OverdrivenGuitar", "DistortionGuitar",
            "GuitarHarmonics", "AcousticBass", "ElectricBassFinger", "ElectricBassPick", "FretlessBass", "SlapBass1", "SlapBass2", "SynthBass1",
            "SynthBass2", "Violin", "Viola", "Cello", "Contrabass", "TremoloStrings", "PizzicatoStrings", "OrchestralHarp", "Timpani",
            "StringEnsemble1", "StringEnsemble2", "SynthStrings1", "SynthStrings2", "ChoirAahs", "VoiceOohs", "SynthVoice", "OrchestraHit",
            "Trumpet", "Trombone", "Tuba", "MutedTrumpet", "FrenchHorn", "BrassSection", "SynthBrass1", "SynthBrass2", "SopranoSax", "AltoSax",
            "TenorSax", "BaritoneSax", "Oboe", "EnglishHorn", "Bassoon", "Clarinet", "Piccolo", "Flute", "Recorder", "PanFlute", "BlownBottle",
            "Shakuhachi", "Whistle", "Ocarina", "Lead1Square", "Lead2Sawtooth", "Lead3Calliope", "Lead4Chiff", "Lead5Charang", "Lead6Voice",
            "Lead7Fifths", "Lead8BassAndLead", "Pad1NewAge", "Pad2Warm", "Pad3Polysynth", "Pad4Choir", "Pad5Bowed", "Pad6Metallic", "Pad7Halo",
            "Pad8Sweep", "Fx1Rain", "Fx2Soundtrack", "Fx3Crystal", "Fx4Atmosphere", "Fx5Brightness", "Fx6Goblins", "Fx7Echoes", "Fx8SciFi",
            "Sitar", "Banjo", "Shamisen", "Koto", "Kalimba", "BagPipe", "Fiddle", "Shanai", "TinkleBell", "Agogo", "SteelDrums", "Woodblock",
            "TaikoDrum", "MelodicTom", "SynthDrum", "ReverseCymbal", "GuitarFretNoise", "BreathNoise", "Seashore", "BirdTweet", "TelephoneRing",
            "Helicopter", "Applause", "Gunshot"
        };

        /// <summary>The GM midi drum kit definitions.</summary>
        static readonly Dictionary<int, string> _drumKits = new()
        {
            { 0, "Standard" }, { 8, "Room" }, { 16, "Power" }, { 24, "Electronic" }, { 25, "TR808" },
            { 32, "Jazz" }, { 40, "Brush" }, { 48, "Orchestra" }, { 56, "SFX" }
        };

        /// <summary>The GM midi drum definitions.</summary>
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

        /// <summary>The midi controller definitions.</summary>
        static readonly Dictionary<int, string> _controllers = new()
        {
           { 000, "BankSelect" }, { 001, "Modulation" }, { 002, "BreathController" }, { 004, "FootController" }, { 005, "PortamentoTime" },
           { 007, "Volume" }, { 008, "Balance" }, { 010, "Pan" }, { 011, "Expression" }, { 032, "BankSelectLSB" }, { 033, "ModulationLSB" },
           { 034, "BreathControllerLSB" }, { 036, "FootControllerLSB" }, { 037, "PortamentoTimeLSB" }, { 039, "VolumeLSB" },
           { 040, "BalanceLSB" }, { 042, "PanLSB" }, { 043, "ExpressionLSB" }, { 064, "Sustain" }, { 065, "Portamento" }, { 066, "Sostenuto" },
           { 067, "SoftPedal" }, { 068, "Legato" }, { 069, "Sustain2" }, { 084, "PortamentoControl" }, { 120, "AllSoundOff" },
           { 121, "ResetAllControllers" }, { 122, "LocalKeyboard" }, { 123, "AllNotesOff" }
        };
        #endregion

        // /// <summary>
        // /// Get patch name.
        // /// </summary>
        // /// <param name="which"></param>
        // /// <returns>The name.</returns>
        // public static string GetInstrumentName(int which)
        // {
        //     string ret = which switch
        //     {
        //         -1 => "NoPatch",
        //         >= 0 and < MIDI_VAL_MAX => _instruments[which],
        //         _ => throw new ArgumentOutOfRangeException(nameof(which)),
        //     };
        //     return ret;
        // }

        /// <summary>
        /// Get drum name.
        /// </summary>
        /// <param name="which"></param>
        /// <returns>The drum name or a fabricated one if unknown.</returns>
        public static string GetDrumName(int which)
        {
            return _drums.ContainsKey(which) ? _drums[which] : $"DRUM_{which}";
        }

        /// <summary>
        /// Get controller name.
        /// </summary>
        /// <param name="which"></param>
        /// <returns>The controller name or a fabricated one if unknown.</returns>
        public static string GetControllerName(int which)
        {
           return _controllers.ContainsKey(which) ? _controllers[which] : $"CTLR_{which}";
        }

        /// <summary>
        /// Get note or drum name.
        /// </summary>
        /// <param name="channel">To determine note or drum name.</param>
        /// <param name="noteNumber">Midi note.</param>
        /// <returns>Suitable string.</returns>
        public static string GetNoteName(int channel, int noteNumber)
        {
            return channel == 10 || channel == 16 ?
                GetDrumName(noteNumber) :
                MusicDefinitions.NoteNumberToName(noteNumber);
        }

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
                    s = $"{s} Note:{e.NoteNumber}({GetNoteName(chan_num, e.NoteNumber)}) Vel:{e.Velocity}";
                    break;

                case ControlChangeEvent e:
                    s = $"{s} Controller:{(int)e.Controller}({GetControllerName((int)e.Controller)}) Val:{e.ControllerValue}";
                    break;

                default: // Ignore others for now.
                    break;
            }

            return s;
        }
    }
}
