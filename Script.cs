using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using NAudio.Midi;
using Ephemera.NBagOfTricks.Slog;
using Ephemera.NBagOfTricks;
using Ephemera.MidiLib;
using KeraLuaEx;
using static Ephemera.Nebulua.Common;


namespace Ephemera.Nebulua
{
    /// <summary>One channel definition.</summary>
    public record ChannelSpec(string ChannelName, string DeviceId, int ChannelNumber, int Patch, bool IsDrums);

    public class Script : IDisposable
    {
        #region Properties that can be read in the user script.
        /// <summary>Sound is playing. Lua: "playing".</summary>
        public bool Playing { set { _l.PushBoolean(value); _l.SetGlobal("playing"); } }

        /// <summary>Actual time since start pressed. Lua: "real_time".</summary>
        public double RealTime { set { _l.PushNumber(value); _l.SetGlobal("real_time"); } }

        /// <summary>Nebulator Speed in bpm. Lua: "tempo".</summary>
        public int Tempo
        {
            get
            {
                return _tempo;
            }
            set
            {
                _tempo = value;
                _l.PushInteger(value);
                _l.SetGlobal("tempo");
            }
        }
        int _tempo;

        /// <summary>Nebulator master Volume. Lua: "master_volume".</summary>
        public double MasterVolume
        {
            get
            {
                return _masterVolume;
            }
            set
            {
                _masterVolume = value;
                _l.PushNumber(value);
                _l.SetGlobal("master_volume");
            }
        }
        double _masterVolume;
        #endregion

        #region Fields
        /// <summary>Main logger.</summary>
        static readonly Logger _logger = LogManager.CreateLogger("Script");

        // Main execution lua state.
        readonly Lua _l = new();

        // Bound static functions.
        static readonly LuaFunction _fLog = Log;
        static readonly LuaFunction _fSendController = SendController;
        static readonly LuaFunction _fSendNote = SendNote;
        static readonly LuaFunction _fSendNoteOn = SendNoteOn;
        static readonly LuaFunction _fSendNoteOff = SendNoteOff;
        static readonly LuaFunction _fSendPatch = SendPatch;
        static readonly LuaFunction _fGetNotes = GetNotes;
        static readonly LuaFunction _fCreateNotes = CreateNotes;
        readonly static LuaFunction _fTimer = Timer;

        static Script _instance;

        /// <summary>Metrics.</summary>
        readonly Stopwatch _sw = new();
        long _startTicks = 0;

        // /// <summary>All the channels - key is user assigned name.</summary>
        // readonly Dictionary<string, Channel> _channels = new();

        ///// <summary>All devices to use for send. Key is my id (not the system driver name).</summary>
        //readonly Dictionary<string, IOutputDevice> _outputDevices = new();

        ///// <summary>All devices to use for receive. Key is name/id, not the system name.</summary>
        //readonly Dictionary<string, IInputDevice> _inputDevices = new();

        ///// <summary>Channel info collected from the script.</summary>
        //public List<ChannelSpec> ChannelSpecs { get; init; } = new();
        //public Dictionary<string, ChannelSpec> ChannelSpecs { get; init; } = new();

        /// <summary>All sections.</summary>
        internal List<Section> _sections = new();

        /// <summary>All the events defined in the script.</summary>
        internal List<MidiEventDesc> _scriptEvents = new();

        /// <summary>Script randomizer.</summary>
        readonly Random _rand = new();

        /// <summary>Resource clean up.</summary>
        internal bool _disposed = false;
        #endregion

        /// <summary>
        /// Internal callback to actually load the libs.
        /// </summary>
        /// <param name="p">Pointer to context.</param>
        /// <returns></returns>
        int OpenLib(IntPtr p)
        {
            // Open lib into global table.
            var l = Lua.FromIntPtr(p)!;
            l.NewLib(_libFuncs);

            return 1;
        }

        /// <summary>
        /// Bind the C# functions lua can call.
        /// </summary>
        readonly LuaRegister[] _libFuncs = new LuaRegister[]
        {
            new LuaRegister("log", _fLog),
            new LuaRegister("send_controller", _fSendController), //send_controller(chan, controller, val)
            new LuaRegister("send_note", _fSendNote), //send_note(chan, note, vol, dur)
            new LuaRegister("send_note_on", _fSendNoteOn), //send_note_on(chan, note, vol)
            new LuaRegister("send_note_off", _fSendNoteOff), //send_note_off(chan, note)
            new LuaRegister("send_patch", _fSendPatch), // send_patch(chan, patch)
            new LuaRegister("get_notes", _fGetNotes), //get_notes("B4.MY_SCALE")
            new LuaRegister("create_notes", _fCreateNotes), //create_notes("MY_SCALE", "1 3 4 b7")
            new LuaRegister("timer", _fTimer),
            new LuaRegister(null, null)
        };

        #region Lifecycle
        /// <summary>
        /// 
        /// </summary>
        public Script()
        {
            // Load C# impl functions. This table gets pushed on the stack and into globals.
            _l.RequireF("neb_api", OpenLib, true);

            // Other inits.
            _startTicks = 0;
            _sw.Start();

            _instance = this;
        }

        /// <summary>
        /// 
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="disposing"></param>
        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                _l.Close();
            }
        }
        #endregion


        /// <summary>
        /// Load file and init everything.
        /// This may throw an exception - client needs to handle them.
        /// </summary>
        /// <param name="fn">Lua file to open.</param>
        /// <param name="luaPaths">Optional additional lua paths.</param>
        public void LoadScript(string fn, List<string>? luaPaths = null)
        {
            // Load the script file.
            string? path = Path.GetDirectoryName(fn);
            luaPaths ??= new();
            luaPaths.Add(path!);

            _l.SetLuaPath(luaPaths);

            // Load/parse the file.
            _l.LoadFile(fn);

            // Execute/init the script.
            _l.PCall(0, Lua.LUA_MULTRET, 0);
            //_l.SetTop(0);

            // Get and init the channels.
            GetChannels();

            // Get the sequences and sections.
            GetComposition();
        }


        // Get and init the channels.
        // Fill in the channel info with what this knows - main will fill in the blanks.
        void GetChannels()
        {
            Channels.Clear();
//            ChannelSpecs.Clear();

            _l.GetGlobal("channels");
            var channels = _l.ToTableEx(-1);
            _l.Pop(1);
            //var s = channels.Dump("channels");


// channels =
// {
//     keys  = { device_id="midi_out",  channel=1,  patch=inst.AcousticGrandPiano },
//     bass  = { device_id="midi_out",  channel=2,  patch=inst.AcousticBass },
//     synth = { device_id="midi_out",  channel=3,  patch=inst.Lead1Square },
//     drums = { device_id="midi_out",  channel=10, patch=kit.Jazz },
//     tune  = { device_id="midi_in",   channel=1   },
//     trig  = { device_id="virt_key",  channel=2,  }, -- optional: show_note_names
//     whiz  = { device_id="bing_bong", channel=10, }, -- optional: draw_note_grid, min_note, max_note, min_control, max_control
// }

            if (channels is not null)
            {
                foreach (var chname in channels.Names)
                {
                    var props = channels[chname] as TableEx;

                    // TODO refactor this mess.
                    string? device_id = props.Names.Contains("device_id") ? props["device_id"].ToString() : null;
                    int? channel_num = props.Names.Contains("channel") ? int.Parse(props["channel"].ToString()) : null;
                    int? patch = props.Names.Contains("patch") ? int.Parse(props["patch"].ToString()) : 0;
                    bool show_note_names = props.Names.Contains("show_note_names") && bool.Parse(props["show_note_names"].ToString());
                    bool draw_note_grid = props.Names.Contains("draw_note_grid") && bool.Parse(props["draw_note_grid"].ToString());

                    // required
                    var valid = device_id is not null && channel_num is not null;


                    if (valid)
                    {
                        Channel channel = new()
                        {
                            ChannelName = chname,
                            ChannelNumber = (int)channel_num,
                            DeviceId = device_id,
                    //        Volume = _nppVals.GetDouble(chspec.ChannelName, "volume", MidiLibDefs.VOLUME_DEFAULT),
                    //        State = (ChannelState)_nppVals.GetInteger(chspec.ChannelName, "state", (int)ChannelState.Normal),
                            Patch = (int)patch,
                            IsDrums = (int)channel_num! == MidiDefs.DEFAULT_DRUM_CHANNEL,
                    //        Selected = false,
                    //        Device = _outputDevices[chspec.DeviceId],
                    //        AddNoteOff = true
                        };
                        Channels.Add(chname, channel);






                        //ChannelSpec chan = new(chname, device_id!, (int)channel!, patch ?? 0, (int)channel! == MidiDefs.DEFAULT_DRUM_CHANNEL);
                        //ChannelSpecs.Add(chname, chan);
                    }
                    else
                    {
                        throw new InvalidOperationException($"Invalid channel spec for {chname}");
                    }
                }
            }
        }




        // Get the sequences and sections.
        void GetComposition()
        {

        }



        #region C# calls lua functions
        /// <summary>
        /// Called to initialize Nebulator stuff.
        /// </summary>
        public void Setup()
        {
            // Get the function to be called.
            _l.GetGlobal("setup");

            // Push the arguments to the call.
            // None.

            // Do the actual call.
            _l.PCall(0, 0, 0);

            // Get the results from the stack.
            // None.
        }

        /// <summary>
        /// Called every mmtimer increment.
        /// </summary>
        /// <param name="bar"></param>
        /// <param name="beat"></param>
        /// <param name="subdiv"></param>
        public void Step(int bar, int beat, int subdiv)
        {
            // Get the function to be called. Check return.
            _l.GetGlobal("step");

            // Push the arguments to the call.
            _l.PushInteger(bar);
            _l.PushInteger(beat);
            _l.PushInteger(subdiv);

            // Do the actual call.
            _l.PCall(3, 0, 0);

            // Get the results from the stack.
            // None.
        }

        /// <summary>
        /// Called when input arrives. Optional.
        /// </summary>
        /// <param name="channel"></param>
        /// <param name="note"></param>
        /// <param name="vel"></param>
        public void InputNote(string channel, int note, int vel) // TODO string or??? also ctrlr.
        {
            // Get the function to be called. Check return.
            if (_l.GetGlobal("input_note") != LuaType.Function) // optional function
            {
                _l.Pop(1);
                return;
            }

            // Push the arguments to the call.
            _l.PushString(channel);
            _l.PushInteger(note);
            _l.PushInteger(vel);

            // Do the actual call.
            _l.PCall(3, 0, 0);

            // Get the results from the stack.
            // None.
        }

        /// <summary>
        /// Called when input arrives. Optional.
        /// </summary>
        /// <param name="channel"></param>
        /// <param name="controller"></param>
        /// <param name="value"></param>
        public void InputController(string channel, int controller, int value)
        {
            // Get the function to be called. Check return.
            if (_l.GetGlobal("input_controller") != LuaType.Function) // optional function
            {
                _l.Pop(1);
                return;
            }

            // Push the arguments to the call.
            _l.PushString(channel);
            _l.PushInteger(controller);
            _l.PushInteger(value);

            // Do the actual call.
            _l.PCall(4, 0, 0);

            // Get the results from the stack.
            // None.
        }
        #endregion

        #region Lua calls C# functions TODO all these need implementation and arg int/string handling
        /// <summary> </summary>
        static int Log(IntPtr p)
        {
            Lua l = Lua.FromIntPtr(p)!;

            var s = l.DumpStack();

            // Get args.
            var level = l.ToInteger(1); //TODO prob make cat into single char
            var msg = l.ToStringL(2);

            // Do the work.
            _logger.Log((LogLevel)level, msg);

            return 0;
        }

        /// <summary> </summary>
        static int SendNote(IntPtr p)
        {
            Lua l = Lua.FromIntPtr(p)!;

            // Get args.         api.send_note("synth", note_num, VEL, 1.0) TODO
            int numArgs = l.GetTop();
            // var level = l.ToInteger(1);
            // var msg = l.ToStringL(2);

            // Do the work.
            //string chanName, int notenum, double vol, double dur) //send_note(chan, note, vol, dur)

            //if (!_channels.ContainsKey(chanName))
            //{
            //    throw new ArgumentException($"Invalid channel [{chanName}]");
            //}

            //var ch = _channels[chanName];
            //int absnote = MathUtils.Constrain(Math.Abs(notenum), MidiDefs.MIN_MIDI, MidiDefs.MAX_MIDI);

            //// If vol is positive it's note on else note off.
            //if (vol > 0)
            //{
            //    double vel = ch.NextVol(vol) * MasterVolume;
            //    int velPlay = (int)(vel * MidiDefs.MAX_MIDI);
            //    velPlay = MathUtils.Constrain(velPlay, MidiDefs.MIN_MIDI, MidiDefs.MAX_MIDI);

            //    NoteOnEvent evt = new(StepTime.TotalSubdivs, ch.ChannelNumber, absnote, velPlay, dur.TotalSubdivs);
            //    ch.SendEvent(evt);
            //}
            //else
            //{
            //    NoteEvent evt = new(StepTime.TotalSubdivs, ch.ChannelNumber, MidiCommandCode.NoteOff, absnote, 0);
            //    ch.SendEvent(evt);
            //}

            // Return results.
            return 0;
        }

        /// <summary>Send an explicit note on immediately. Caller is responsible for sending note off later.</summary>
        static int SendNoteOn(IntPtr p)
        {
            //SendNote(chanName, notenum, vol); TODO

            // Return results.
            return 0;
        }

        /// <summary>Send an explicit note off immediately.</summary>
        static int SendNoteOff(IntPtr p)
        {
            //SendNote(chanName, notenum, 0); TODO

            // Return results.
            return 0;
        }

        /// <summary>Send a controller immediately.</summary>
        static int SendController(IntPtr p)
        {
            var l = Lua.FromIntPtr(p);

            ///// Get function arguments.
            string? chanName = l.ToStringL(1);
            int? controller = l.ToInteger(2);
            int? val = l.ToInteger(3);

            ///// Do the work.
            var ch = Channels[chanName];
            //int ctlrid = MidiDefs.GetControllerNumber(controller);
            //ch.SendController((MidiController)ctlrid, (int)val);
            ch.SendController((MidiController)controller, (int)val);

            // Return results.
            return 0;
        }

        /// <summary>Send a midi patch immediately.</summary>
        static int SendPatch(IntPtr p)
        {
            var l = Lua.FromIntPtr(p);

            ///// Get function arguments.
            string chanName = l.ToStringL(1)!;
            string patch = l.ToStringL(2)!;

            ///// Do the work.
            var ch = Channels[chanName];
            int patchid = MidiDefs.GetInstrumentNumber(patch); // handle fail?
            ch.Patch = patchid;
            ch.SendPatch();

            // Return results.
            return 0;
        }



        // TODO these could be in the script
        // CreateSequence(int beats, SequenceElements elements) -- -> Sequence
        // CreateSection(int beats, string name, SectionElements elements) -- -> Section

        /// <summary>Add a named chord or scale definition.</summary>
        static int CreateNotes(IntPtr p)
        {
            var l = Lua.FromIntPtr(p)!;

            // Get args.
            var name = l.ToStringL(1);
            var parts = l.ToStringL(2);

            // Do the work.
            MusicDefinitions.AddChordScale(name, parts);

            // Return results.
            return 0;
        }

        /// <summary> </summary>
        static int GetNotes(IntPtr p)
        {
            var l = Lua.FromIntPtr(p)!;

            // Get args.
            var noteString = l.ToStringL(1);

            // Do the work.
            List<int> notes = MusicDefinitions.GetNotesFromString(noteString);

            // Return results.
            l.PushList(notes);
            return 1;
        }

        /// <summary>
        /// Lua script requires a high res timestamp - msec as double.
        /// </summary>
        /// <param name="p">Pointer to context.</param>
        /// <returns>Number of results</returns>
        static int Timer(IntPtr p)
        {
            var l = Lua.FromIntPtr(p)!;

            // Get arguments.
            bool on = l.ToBoolean(-1);

            // Do the work.
            double totalMsec = 0;
            if (on)
            {
                _instance._startTicks = _instance._sw.ElapsedTicks; // snap
            }
            else if (_instance._startTicks > 0)
            {
                long t = _instance._sw.ElapsedTicks; // snap
                totalMsec = (t - _instance._startTicks) * 1000D / Stopwatch.Frequency;
            }

            // Return results.
            l.PushNumber(totalMsec);
            return 1;
        }
        #endregion





        /// <summary>
        /// Convert script sequences etc to internal events.
        /// </summary>
        public void BuildSteps()
        {
            // Build all the events.
            int sectionBeat = 0;

            foreach (Section section in _sections)
            {
                foreach (SectionElement sectel in section.Elements)
                {
                    if (sectel.Sequences.Length > 0)
                    {
                        // Current index in the sequences list.
                        int seqIndex = 0;

                        // Current beat in the section.
                        int beatInSect = 0;

                        while (beatInSect < section.Beats)
                        {
                            var seq = sectel.Sequences[seqIndex];
                            //was AddSequence(sectel.Channel, seq, sectionBeat + beatInSect);
                            var ch = Channels[sectel.ChannelName];
                            int beat = sectionBeat + beatInSect;
                            var ecoll = ConvertToEvents(ch, seq, beat);
                            _scriptEvents.AddRange(ecoll);

                            beatInSect += seq.Beats;
                            if (seqIndex < sectel.Sequences.Length - 1)
                            {
                                seqIndex++;
                            }
                        }
                    }
                }

                // Update accumulated time.
                sectionBeat += section.Beats;
            }
        }

        /// <summary>
        /// Get all section names and when they start. The end marker is also added.
        /// </summary>
        /// <returns></returns>
        public Dictionary<int, string> GetSectionMarkers()
        {
            Dictionary<int, string> info = new();
            int when = 0;

            foreach (Section sect in _sections)
            {
                info.Add(when, sect.Name);
                when += sect.Beats;
            }

            // Add the dummy end marker.
            info.Add(when, "");

            return info;
        }

        /// <summary>
        /// Get all events.
        /// </summary>
        /// <returns>Enumerator for all events.</returns>
        public IEnumerable<MidiEventDesc> GetEvents()
        {
            return _scriptEvents;
        }

        /// <summary>
        /// Generate events from sequence notes.
        /// </summary>
        /// <param name="channel">Which channel to send it on.</param>
        /// <param name="seq">Which notes to send.</param>
        /// <param name="startBeat">Which beat to start sequence at.</param>
        List<MidiEventDesc> ConvertToEvents(Channel channel, Sequence seq, int startBeat)
        {
            List<MidiEventDesc> events = new();

            foreach (SequenceElement seqel in seq.Elements)
            {
                // Create the note start and stop times.
                BarTime startNoteTime = new BarTime(startBeat * MidiSettings.LibSettings.SubdivsPerBeat) + seqel.When;
                BarTime stopNoteTime = startNoteTime + (seqel.Duration.TotalSubdivs == 0 ? new(1) : seqel.Duration); // 1 is a short hit

                // Is it a function?
                if (seqel.ScriptFunction is not null)
                {
                    FunctionMidiEvent evt = new(startNoteTime.TotalSubdivs, channel.ChannelNumber, seqel.ScriptFunction);
                    events.Add(new(evt, channel.ChannelName));
                }
                else // plain ordinary
                {
                    // Process all note numbers.
                    foreach (int noteNum in seqel.Notes)
                    {
                        ///// Note on.
                        double vel = channel.NextVol(seqel.Volume) * MasterVolume;
                        int velPlay = (int)(vel * MidiDefs.MAX_MIDI);
                        velPlay = MathUtils.Constrain(velPlay, MidiDefs.MIN_MIDI, MidiDefs.MAX_MIDI);

                        NoteOnEvent evt = new(startNoteTime.TotalSubdivs, channel.ChannelNumber, noteNum, velPlay, seqel.Duration.TotalSubdivs);
                        events.Add(new(evt, channel.ChannelName));
                    }
                }
            }

            return events;
        }
    }
}
