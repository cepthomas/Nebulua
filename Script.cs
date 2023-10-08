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
//using static Ephemera.Nebulua.Common;


namespace Ephemera.Nebulua
{
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

        /// <summary>Main execution lua state.</summary>
        readonly Lua _l = new();

        // Bound static functions.//TODO1
        static readonly LuaFunction _fLog = Log;
        static readonly LuaFunction _fSendController = SendController;
        static readonly LuaFunction _fSendNote = SendNote;
        static readonly LuaFunction _fSendNoteOn = SendNoteOn;
        static readonly LuaFunction _fSendNoteOff = SendNoteOff;
        static readonly LuaFunction _fSendPatch = SendPatch;
        // static readonly LuaFunction _fGetNotes = GetNotes;
        // static readonly LuaFunction _fCreateNotes = CreateNotes;

        /// <summary>Need static instance for binding functions.</summary>
        static Script _instance;

        /// <summary>All sections.</summary>
        internal List<Section> _sections = new();

        /// <summary>All the events defined in the script.</summary>
        internal List<MidiEventDesc> _scriptEvents = new();

        /// <summary>Resource clean up.</summary>
        internal bool _disposed = false;
        #endregion

        #region Lifecycle
        /// <summary>
        /// 
        /// </summary>
        public Script()
        {
            // Load C# impl functions. This table gets pushed on the stack and into globals.
            _l.RequireF("neb_api", OpenLib, true);

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
        readonly LuaRegister[] _libFuncs = new LuaRegister[]//TODO2 automate this also?
        {
            new LuaRegister("log", _fLog),
            new LuaRegister("send_controller", _fSendController),
            new LuaRegister("send_note", _fSendNote),
            new LuaRegister("send_note_on", _fSendNoteOn),
            new LuaRegister("send_note_off", _fSendNoteOff),
            new LuaRegister("send_patch", _fSendPatch),
            new LuaRegister(null, null)
        };
        #endregion

        #region Load the script
        /// <summary>
        /// Load file and init everything.
        /// This may throw an exception - client needs to handle them.
        /// </summary>
        /// <param name="fn">Lua file to open.</param>
        /// <param name="luaPaths">Optional additional lua paths.</param>
        public void LoadScript(string fn, List<string>? luaPaths = null)
        {
            // Load the script file.
            luaPaths ??= new();

            _l.SetLuaPath(luaPaths);

            // Load/parse the file.
            _l.LoadFile(fn);

            // Execute/init the script.
            _l.DoCall(0, Lua.LUA_MULTRET);

            // Get and init the channels.
            GetChannels();

            // Get the sequences and sections.
            // GetComposition();
        }

        /// <summary>
        /// Get and init the channels.
        /// </summary>
        /// <exception cref="InvalidOperationException"></exception>
        void GetChannels()
        {
            Common.OutputChannels.Clear();
            Common.InputChannels.Clear();

            _l.GetGlobal("channels");
            var channels = _l.ToTableEx(-1);
            _l.Pop(1);

            if (channels is not null)
            {
                foreach (var chname in channels.Names)
                {
                    var props = channels[chname] as TableEx;
                    var valid = props is not null;

                    if (valid)
                    {
                        // TODO1 refactor this mess.
                        string? device_id = props.Names.Contains("device_id") ? props["device_id"].ToString() : null;
                        int? channel_num = props.Names.Contains("channel") ? int.Parse(props["channel"].ToString()) : null;
                        int? patch = props.Names.Contains("patch") ? int.Parse(props["patch"].ToString()) : 0;
                        //bool show_note_names = props.Names.Contains("show_note_names") && bool.Parse(props["show_note_names"].ToString());
                        //bool draw_note_grid = props.Names.Contains("draw_note_grid") && bool.Parse(props["draw_note_grid"].ToString());

                        // required
                        valid = device_id is not null && channel_num is not null;

                        if (valid)
                        {
                            // Fill in the channel info with what this knows - main will fill in the blanks.
                            Channel channel = new()
                            {
                                ChannelName = chname,
                                ChannelNumber = (int)channel_num,
                                DeviceId = device_id,
                                Patch = (int)patch,
                                IsDrums = (int)channel_num! == MidiDefs.DEFAULT_DRUM_CHANNEL,
                            };

                            if (Common.OutputDevices.ContainsKey(device_id))
                            {
                                Common.OutputChannels.Add(chname, channel);
                            }
                            else if (Common.InputDevices.ContainsKey(device_id))
                            {
                                Common.InputChannels.Add(chname, channel);
                            }
                            else
                            {
                                //throw new InvalidOperationException($"Invalid device id {device_id} for {chname}");
                            }
                        }
                    }

                    if (!valid)
                    {
                        throw new InvalidOperationException($"Invalid channel spec for {chname}");
                    }
                }
            }
        }

        // /// <summary>
        // /// Get the sequences and sections.
        // /// </summary>
        // void GetComposition()
        // {
        //     _l.GetGlobal("_G");//TODO1 "sequences"
        //     var keys = GetKeys();
        //     _l.Pop(1); // GetGlobal

        //     foreach(var k in keys)
        //     {
        //         if (k.StartsWith("seq_"))
        //         {
        //             _l.GetGlobal(k);
        //             var s = _l.ToStringL(-1);
        //             var parts = s!.SplitByTokens(Environment.NewLine);


        //         }
        //     }
        // }
        #endregion


        // /// <summary>
        // /// Get all keys for table on stack top.
        // /// </summary>
        // /// <returns></returns>
        // /// <exception cref="InvalidOperationException"></exception>
        // List<string> GetKeys()
        // {
        //     // Check for valid value.
        //     if (_l.Type(-1)! != LuaType.Table)
        //     {
        //         throw new InvalidOperationException($"Expected table at top of stack but is {_l.Type(-1)}");
        //     }

        //     List<string> keys = new();

        //     // First key.
        //     _l.PushNil();

        //     // Key(-1) is replaced by the next key(-1) in table(-2).
        //     while (_l.Next(-2))
        //     {
        //         // Get key info (-2).
        //         //LuaType keyType = _l.Type(-2);
        //         //string? skey = keyType == LuaType.String ? _l.ToStringL(-2) : null;
        //         //int? ikey = keyType == LuaType.Number && _l.IsInteger(-2) ? _l.ToInteger(-2) : null;
        //         // Get val info (-1).
        //         //LuaType valType = _l.Type(-1);
        //         //string? sval = _l.ToStringL(-1);

        //         keys.Add(_l.ToStringL(-2)!);

        //         // Remove value(-1), now key on top at(-1).
        //         _l.Pop(1);
        //     }

        //     return keys;
        // }





        #region C# calls lua functions  TODO1 see gen.cs
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
            _l.DoCall(0, 0);

            // Get the results from the stack.
            // None.
        }

        /// <summary>
        /// Called every mmtimer increment.
        /// </summary>
        /// <param name="bar"></param>
        /// <param name="beat"></param>
        /// <param name="subbeat"></param>
        public void Step(int bar, int beat, int subbeat)
        {
            // Get the function to be called. Check return.
            _l.GetGlobal("step");

            // Push the arguments to the call.
            _l.PushInteger(bar);
            _l.PushInteger(beat);
            _l.PushInteger(subbeat);

            // Do the actual call.
            _l.DoCall(3, 0);

            // Get the results from the stack.
            // None.
        }

        /// <summary>
        /// Called when input arrives. Optional.
        /// </summary>
        /// <param name="channelName"></param>
        /// <param name="note"></param>
        /// <param name="vel"></param>
        public void InputNote(string channelName, int note, int vel)
        {
            // Get the function to be called. Check return.
            if (_l.GetGlobal("input_note") != LuaType.Function) // optional function
            {
                _l.Pop(1);
                return;
            }

            // Push the arguments to the call.
            _l.PushString(channelName);
            _l.PushInteger(note);
            _l.PushInteger(vel);

            // Do the actual call.
            _l.DoCall(3, 0);

            // Get the results from the stack.
            // None.
        }

        /// <summary>
        /// Called when input arrives. Optional.
        /// </summary>
        /// <param name="channelName"></param>
        /// <param name="controller"></param>
        /// <param name="value"></param>
        public void InputController(string channelName, int controller, int value)
        {
            // Get the function to be called. Check return.
            if (_l.GetGlobal("input_controller") != LuaType.Function) // optional function
            {
                _l.Pop(1);
                return;
            }

            // Push the arguments to the call.
            _l.PushString(channelName);
            _l.PushInteger(controller);
            _l.PushInteger(value);

            // Do the actual call.
            _l.DoCall(4, 0);

            // Get the results from the stack.
            // None.
        }
        #endregion

        #region Lua calls C# functions TODO1 see gen.cs
        /// <summary> </summary>
        static int Log(IntPtr p)
        {
            Lua l = Lua.FromIntPtr(p)!;

            // var s = l.DumpStack();

            // Get args.
            var level = l.ToInteger(1);
            var msg = l.ToStringL(2);

            // Do the work.
            _logger.Log((LogLevel)level!, msg ?? "???");

            return 0;
        }

        /// <summary> </summary>
        /// api.send_note(S "synth", I note_num, N volume, X dur)
        /// if volume is 0 note_off else note_on
        /// if dur is 0 dur = note_on with dur = 0.1 (for drum/hit)
        static int SendNote(IntPtr p)
        {
            Lua l = Lua.FromIntPtr(p)!;

            // Get args. 
            int numArgs = l.GetTop();
            var level = l.ToInteger(1);
            var msg = l.ToStringL(2);

            // Do the work.
            //string channelName, int notenum, double vol, double dur) //send_note(chan, note, vol, dur)

            //if (!_channels.ContainsKey(channelName))
            //{
            //    throw new ArgumentException($"Invalid channel [{channelName}]");
            //}

            //var ch = _channels[channelName];
            //int absnote = MathUtils.Constrain(Math.Abs(notenum), MidiDefs.MIN_MIDI, MidiDefs.MAX_MIDI);

            //// If vol is positive it's note on else note off.
            //if (vol > 0)
            //{
            //    double vel = ch.NextVol(vol) * MasterVolume;
            //    int velPlay = (int)(vel * MidiDefs.MAX_MIDI);
            //    velPlay = MathUtils.Constrain(velPlay, MidiDefs.MIN_MIDI, MidiDefs.MAX_MIDI);

            //    NoteOnEvent evt = new(StepTime.TotalSubbeats, ch.ChannelNumber, absnote, velPlay, dur.TotalSubbeats);
            //    ch.SendEvent(evt);
            //}
            //else
            //{
            //    NoteEvent evt = new(StepTime.TotalSubbeats, ch.ChannelNumber, MidiCommandCode.NoteOff, absnote, 0);
            //    ch.SendEvent(evt);
            //}

            // Return results.
            return 0;
        }

        /// <summary>Send an explicit note on immediately. Caller is responsible for sending note off later.</summary>
        /// api.send_note_on(S "synth", I note_num, N volume)
        static int SendNoteOn(IntPtr p)
        {
            // Return results.
            return 0;
        }

        /// <summary>Send an explicit note off immediately.</summary>
        /// api.send_note_off(S "synth", I note_num)
        static int SendNoteOff(IntPtr p)
        {
            // Return results.
            return 0;
        }

        /// <summary>Send a controller immediately.</summary>
        /// api.send_controller("synth", ctrl.Pan, 90) -- SII

        static int SendController(IntPtr p)
        {
            var l = Lua.FromIntPtr(p)!;

            // ///// Get function arguments.
            // string? channelName = l.ToStringL(1);
            // int? controller = l.ToInteger(2);
            // int? val = l.ToInteger(3);

            // ///// Do the work.
            // var ch = Common.OutputChannels[channelName];
            // //int ctlrid = MidiDefs.GetControllerNumber(controller);
            // //ch.SendController((MidiController)ctlrid, (int)val);
            // ch.SendController((MidiController)controller, (int)val);

            // Return results.
            return 0;
        }

        /// <summary>Send a midi patch immediately.</summary>
        /// api.send_patch("synth", inst.Lead1Square)
        static int SendPatch(IntPtr p)
        {
            var l = Lua.FromIntPtr(p)!;

            // ///// Get function arguments.
            // string channelName = l.ToStringL(1)!;
            // string patch = l.ToStringL(2)!;

            // ///// Do the work.
            // var ch = Common.OutputChannels[channelName];
            // int patchid = MidiDefs.GetInstrumentNumber(patch); // handle fail?
            // ch.Patch = patchid;
            // ch.SendPatch();

            // Return results.
            return 0;
        }

        // /// <summary>Add a named chord or scale definition.</summary>
        // static int CreateNotes(IntPtr p)
        // {
        //     var l = Lua.FromIntPtr(p)!;

        //     // Get args.
        //     var name = l.ToStringL(1);
        //     var parts = l.ToStringL(2);

        //     // Do the work.
        //     MusicDefinitions.AddChordScale(name, parts);

        //     // Return results.
        //     return 0;
        // }

        // /// <summary> </summary>
        // static int GetNotes(IntPtr p)
        // {
        //     var l = Lua.FromIntPtr(p)!;

        //     // Get args.
        //     var noteString = l.ToStringL(1);

        //     // Do the work.
        //     List<int> notes = MusicDefinitions.GetNotesFromString(noteString);

        //     // Return results.
        //     l.PushList(notes);
        //     return 1;
        // }
        #endregion

        #region these could be in the script
        // CreateSequence(int beats, SequenceElements elements) -- -> Sequence
        // CreateSection(int beats, string name, SectionElements elements) -- -> Section

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
                            var ch = Common.OutputChannels[sectel.ChannelName];
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
                BarTime startNoteTime = new BarTime(startBeat * MidiSettings.LibSettings.SubbeatsPerBeat) + seqel.When;
                BarTime stopNoteTime = startNoteTime + (seqel.Duration.TotalSubbeats == 0 ? new(1) : seqel.Duration); // 1 is a short hit

                // Is it a function?
                if (seqel.ScriptFunction is not null)
                {
                    FunctionMidiEvent evt = new(startNoteTime.TotalSubbeats, channel.ChannelNumber, seqel.ScriptFunction);
                    events.Add(new(evt, channel.ChannelName));
                }
                else // plain ordinary
                {
                    // Process all note numbers.
                    foreach (int noteNum in seqel.Notes)
                    {
                        ///// Note on.
                        double vel = channel.NextVol(seqel.Volume) * _masterVolume;
                        int velPlay = (int)(vel * MidiDefs.MAX_MIDI);
                        velPlay = MathUtils.Constrain(velPlay, MidiDefs.MIN_MIDI, MidiDefs.MAX_MIDI);

                        NoteOnEvent evt = new(startNoteTime.TotalSubbeats, channel.ChannelNumber, noteNum, velPlay, seqel.Duration.TotalSubbeats);
                        events.Add(new(evt, channel.ChannelName));
                    }
                }
            }

            return events;
        }
        #endregion
    }
}
