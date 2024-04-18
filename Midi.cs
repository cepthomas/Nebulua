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


        #region All the names TODO ? generate from midi_defs.lua at runtime.
        // /// <summary>The GM midi instrument definitions.</summary>
        // static readonly Dictionary<int, string> _instruments = new()
        // {
        //     { 000, "AcousticGrandPiano" }, { 001, "BrightAcousticPiano" }, { 002, "ElectricGrandPiano" }, { 003, "HonkyTonkPiano" },
        //     { 004, "ElectricPiano1" }, { 005, "ElectricPiano2" }, { 006, "Harpsichord" }, { 007, "Clavinet" }, { 008, "Celesta" },
        //     { 009, "Glockenspiel" }, { 010, "MusicBox" }, { 011, "Vibraphone" },  { 012, "Marimba" }, { 013, "Xylophone" },
        //     { 014, "TubularBells" }, { 015, "Dulcimer" }, { 016, "DrawbarOrgan" }, { 017, "PercussiveOrgan" },  { 018, "RockOrgan" },
        //     { 019, "ChurchOrgan" }, { 020, "ReedOrgan" }, { 021, "Accordion" }, { 022, "Harmonica" }, { 023, "TangoAccordion" },
        //     { 024, "AcousticGuitarNylon" }, { 025, "AcousticGuitarSteel" }, { 026, "ElectricGuitarJazz" }, { 027, "ElectricGuitarClean" },
        //     { 028, "ElectricGuitarMuted" }, { 029, "OverdrivenGuitar" },  { 030, "DistortionGuitar" }, { 031, "GuitarHarmonics" },
        //     { 032, "AcousticBass" }, { 033, "ElectricBassFinger" }, { 034, "ElectricBassPick" }, { 035, "FretlessBass" },
        //     { 036, "SlapBass1" }, { 037, "SlapBass2" }, { 038, "SynthBass1" }, { 039, "SynthBass2" }, { 040, "Violin" }, { 041, "Viola" },
        //     { 042, "Cello" }, { 043, "Contrabass" }, { 044, "TremoloStrings" }, { 045, "PizzicatoStrings" }, { 046, "OrchestralHarp" },
        //     { 047, "Timpani" }, { 048, "StringEnsemble1" }, { 049, "StringEnsemble2" }, { 050, "SynthStrings1" }, { 051, "SynthStrings2" },
        //     { 052, "ChoirAahs" }, { 053, "VoiceOohs" }, { 054, "SynthVoice" }, { 055, "OrchestraHit" }, { 056, "Trumpet" },
        //     { 057, "Trombone" }, { 058, "Tuba" }, { 059, "MutedTrumpet" }, { 060, "FrenchHorn" }, { 061, "BrassSection" },
        //     { 062, "SynthBrass1" }, { 063, "SynthBrass2" }, { 064, "SopranoSax" }, { 065, "AltoSax" }, { 066, "TenorSax" },
        //     { 067, "BaritoneSax" }, { 068, "Oboe" }, { 069, "EnglishHorn" }, { 070, "Bassoon" }, { 071, "Clarinet" }, { 072, "Piccolo" },
        //     { 073, "Flute" }, { 074, "Recorder" }, { 075, "PanFlute" }, { 076, "BlownBottle" }, { 077, "Shakuhachi" }, { 078, "Whistle" },
        //     { 079, "Ocarina" }, { 080, "Lead1Square" }, { 081, "Lead2Sawtooth" }, { 082, "Lead3Calliope" }, { 083, "Lead4Chiff" },
        //     { 084, "Lead5Charang" }, { 085, "Lead6Voice" }, { 086, "Lead7Fifths" }, { 087, "Lead8BassAndLead" }, { 088, "Pad1NewAge" },
        //     { 089, "Pad2Warm" }, { 090, "Pad3Polysynth" }, { 091, "Pad4Choir" }, { 092, "Pad5Bowed" }, { 093, "Pad6Metallic" },
        //     { 094, "Pad7Halo" }, { 095, "Pad8Sweep" }, { 096, "Fx1Rain" }, { 097, "Fx2Soundtrack" }, { 098, "Fx3Crystal" },
        //     { 099, "Fx4Atmosphere" }, { 100, "Fx5Brightness" }, { 101, "Fx6Goblins" }, { 102, "Fx7Echoes" }, { 103, "Fx8SciFi" },
        //     { 104, "Sitar" }, { 105, "Banjo" },  { 106, "Shamisen" }, { 107, "Koto" }, { 108, "Kalimba" }, { 109, "BagPipe" },
        //     { 110, "Fiddle" }, { 111, "Shanai" }, { 112, "TinkleBell" }, { 113, "Agogo" }, { 114, "SteelDrums" }, { 115, "Woodblock" },
        //     { 116, "TaikoDrum" }, { 117, "MelodicTom" }, { 118, "SynthDrum" }, { 119, "ReverseCymbal" }, { 120, "GuitarFretNoise" },
        //     { 121, "BreathNoise" }, { 122, "Seashore" }, { 123, "BirdTweet" }, { 124, "TelephoneRing" }, { 125, "Helicopter" },
        //     { 126, "Applause" }, { 127, "Gunshot" }
        // };

        // /// <summary>The GM midi drum kit definitions.</summary>
        // static readonly Dictionary<int, string> _drumKits = new()
        // {
        //     { 0, "Standard" }, { 8, "Room" }, { 16, "Power" }, { 24, "Electronic" }, { 25, "TR808" },
        //     { 32, "Jazz" }, { 40, "Brush" }, { 48, "Orchestra" }, { 56, "SFX" }
        // };

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

        // /// <summary>
        // /// Get drum name.
        // /// </summary>
        // /// <param name="which"></param>
        // /// <returns>The drum name or a fabricated one if unknown.</returns>
        // public static string GetDrumName(int which)
        // {
        //     return _drums.ContainsKey(which) ? _drums[which] : $"DRUM_{which}";
        // }

        // /// <summary>
        // /// Get controller name.
        // /// </summary>
        // /// <param name="which"></param>
        // /// <returns>The controller name or a fabricated one if unknown.</returns>
        // public static string GetControllerName(int which)
        // {
        //    return _controllers.ContainsKey(which) ? _controllers[which] : $"CTLR_{which}";
        // }

        // /// <summary>
        // /// Get note or drum name.
        // /// </summary>
        // /// <param name="channel">To determine note or drum name.</param>
        // /// <param name="noteNumber">Midi note.</param>
        // /// <returns>Suitable string.</returns>
        // public static string GetNoteName(int channel, int noteNumber)
        // {
        //     return channel == 10 || channel == 16 ?
        //         GetDrumName(noteNumber) :
        //         MusicDefinitions.NoteNumberToName(noteNumber);
        // }

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
                        _drums.ContainsKey(e.NoteNumber) ? _drums[e.NoteNumber] : $"DRUM_{e.NoteNumber}"
                        GetDrumName(e.NoteNumber) :
                        MusicDefinitions.NoteNumberToName(e.NoteNumber);

                    s = $"{s} Note:{e.NoteNumber}({snote}) Vel:{e.Velocity}";
                    break;

                case ControlChangeEvent e:
                    var sctl = _controllers.ContainsKey(e.Controller) ? _controllers[e.Controller] : $"CTLR_{e.Controller}";
                    s = $"{s} Controller:{(int)e.Controller}({sctl}) Val:{e.ControllerValue}";
                    break;

                default: // Ignore others for now.
                    break;
            }

            return s;
        }
    }
}
