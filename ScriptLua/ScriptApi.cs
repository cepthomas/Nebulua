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


namespace Ephemera.Nebulua.Script
{
    /// <summary>One channel definition.</summary>
    public record ChannelSpec(string ChannelName, string DeviceId, int ChannelNumber, int Patch, bool IsDrums);

    public class ScriptApi : IDisposable
    {
        #region Properties that can be read in the user script.
        /// <summary>Sound is playing. Lua: "playing".</summary>
        public bool Playing { set { _lMain.PushBoolean(value); _lMain.SetGlobal("playing"); } }

        /// <summary>Current Nebulator step time.</summary>
        // public double StepTime { set { _lMain.PushNumber(value); _lMain.SetGlobal("step_time"); } }

        /// <summary>Actual time since start pressed. Lua: "real_time".</summary>
        public double RealTime { set { _lMain.PushNumber(value); _lMain.SetGlobal("real_time"); } }

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
                _lMain.PushInteger(value);
                _lMain.SetGlobal("tempo");
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
                _lMain.PushNumber(value);
                _lMain.SetGlobal("master_volume");
            }
        }
        double _masterVolume;
        #endregion




        /// <summary>Metrics.</summary>
        /*static*/ readonly Stopwatch _sw = new();
        /*static*/ long _startTicks = 0;




        #region Fields
        /// <summary>Main logger.</summary>
        static readonly Logger _logger = LogManager.CreateLogger("ScriptApi");

        // Main execution lua state.
        /*static*/ readonly Lua _lMain = new();

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


        static ScriptApi _instance;


        /// <summary>All the channels - key is user assigned name.</summary>
        /*static*/ readonly Dictionary<string, Channel> _channels = new();

        /// <summary>All devices to use for send. Key is my id (not the system driver name).</summary>
        /*static*/ readonly Dictionary<string, IOutputDevice> _outputDevices = new();

        /// <summary>All devices to use for receive. Key is name/id, not the system name.</summary>
        /*static*/ readonly Dictionary<string, IInputDevice> _inputDevices = new();

        /// <summary>Channel info collected from the script.</summary>
        public List<ChannelSpec> ChannelSpecs { get; init; } = new();

        /// <summary>All sections.</summary>
        internal List<Section> _sections = new();

        /// <summary>All the events defined in the script.</summary>
        internal List<MidiEventDesc> _scriptEvents = new();

        /// <summary>Script randomizer.</summary>
        /*static*/ readonly Random _rand = new();

        /// <summary>Resource clean up.</summary>
        internal bool _disposed = false;
        #endregion


        // /// <summary>
        // /// Load the lua libs implemented in C#.
        // /// </summary>
        // /// <param name="l">Lua context</param>
        // public /*static*/ void Load(Lua l)
        // {
        //     // Load app stuff. This table gets pushed on the stack and into globals.
        //     l.RequireF("neb_api", OpenLib, true);

        //     // Other inits.
        //     _startTicks = 0;
        //     _sw.Start();
        // }

        /// <summary>
        /// Internal callback to actually load the libs.
        /// </summary>
        /// <param name="p">Pointer to context.</param>
        /// <returns></returns>
        /*static*/ int OpenLib(IntPtr p)
        {
            // Open lib into global table.
            var l = Lua.FromIntPtr(p)!;
            l.NewLib(_libFuncs);

            return 1;
        }

        /// <summary>
        /// Bind the C# functions to lua.
        /// </summary>
        /*static*/ readonly LuaRegister[] _libFuncs = new LuaRegister[]
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

        public ScriptApi()
        {
            // Load C# impl functions. This table gets pushed on the stack and into globals.
            _lMain.RequireF("neb_api", OpenLib, true);

            // Other inits.
            _startTicks = 0;
            _sw.Start();

            _instance = this;
        }



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

            try
            {
                _lMain.SetLuaPath(luaPaths);
                _lMain.LoadFile(fn);

                // Load(_lMain);

                // /// <summary>
                // /// Load the lua libs implemented in C#.
                // /// </summary>
                // /// <param name="l">Lua context</param>
                // public /*static*/ void Load(Lua l)
                // {
                    // // Load C# impl functions. This table gets pushed on the stack and into globals.
                    // _lMain.RequireF("neb_api", OpenLib, true);

                    // // Other inits.
                    // _startTicks = 0;
                    // _sw.Start();
                // }



                // PCall executes (loads) the file.
                var res = _lMain.PCall(0, Lua.LUA_MULTRET, 0);


                // TODOapp Get and init the devices.
                GetDevices();

                // Get the sequences and sections.
                GetComposition();

            }
            catch (Exception ex)
            {
                throw;
            }
        }

        // /// <summary>
        // /// Set up runtime stuff.
        // /// </summary>
        // /// <param name="channels">All output channels.</param>
        // public void Init(Dictionary<string, Channel> channels)//TODOapp??
        // {
        //    _channels = channels;
        // }

        /// <summary> </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary> </summary>
        /// <param name="disposing"></param>
        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                _lMain.Close();
            }
        }
        #endregion



        // Get and init the devices.
        void GetDevices()
        {
            _channels.Clear();

            // Get the globals.
            _lMain.PushGlobalTable();


            var g = _lMain.ToDataTable(2, true).AsDict();

            var devs = (g["devices"] as DataTable).AsDict();

            foreach(var dev in devs)
            {



            }

            _lMain.Pop(1); // from PushGlobalTable()

        }


    //for k, v in pairs(mut) do
    //    if type(v) == "function" and k:match("suite_") then
    //        -- Found something to do. Run it in between optional test boilerplate.
    //        pn.start_suite(k.. " in " .. mfn)

    //        local ok, result = pcall(mut["setup"], pn)-- optional
    //        if not ok then
    //            internal_error(result)
    //            script_fail = true
    //            goto done
    //        end

    //        ok, result = pcall(v, pn)
    //        if not ok then
    //            internal_error(result)
    //            script_fail = true
    //            goto done
    //        end

    //        ok, result = pcall(mut["teardown"], pn) -- optional
    //        if not ok then
    //            internal_error(result)
    //            script_fail = true
    //            goto done
    //        end
    //    end
    //end



        // Get the sequences and sections.
        void GetComposition()
        {

        }








        #region C# calls lua functions  // TODOapp check all- see luaex
        /// <summary>
        /// Called to initialize Nebulator stuff.
        /// </summary>
        public void Setup()
        {
            // Get the function to be called.
            _lMain.GetGlobal("setup");

            // Push the arguments to the call.
            // None.

            // Do the actual call.
            _lMain.PCall(0, 0, 0);

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
            _lMain.GetGlobal("step");

            // Push the arguments to the call.
            _lMain.PushInteger(bar);
            _lMain.PushInteger(beat);
            _lMain.PushInteger(subdiv);

            // Do the actual call.
            _lMain.PCall(3, 0, 0);

            // Get the results from the stack.
            // None.
        }

        /// <summary>
        /// Called when input arrives. Optional.
        /// </summary>
        /// <param name="dev"></param>
        /// <param name="channel"></param>
        /// <param name="note"></param>
        /// <param name="vel"></param>
        public void InputNote(string dev, int channel, int note, int vel)
        {
            // Get the function to be called. Check return.
            if (_lMain.GetGlobal("input_note") != LuaType.Function) // optional function
            {
                _lMain.Pop(1);
                return;
            }

            // Push the arguments to the call.
            _lMain.PushString(dev);
            _lMain.PushInteger(channel);
            _lMain.PushInteger(note);
            _lMain.PushInteger(vel);

            // Do the actual call.
            _lMain.PCall(4, 0, 0);

            // Get the results from the stack.
            // None.
        }

        /// <summary>
        /// Called when input arrives. Optional.
        /// </summary>
        /// <param name="dev"></param>
        /// <param name="channel"></param>
        /// <param name="controller"></param>
        /// <param name="value"></param>
        public void InputController(string dev, int channel, int controller, int value)
        {
            // Get the function to be called. Check return.
            if (_lMain.GetGlobal("input_controller") != LuaType.Function) // optional function
            {
                _lMain.Pop(1);
                return;
            }

            // Push the arguments to the call.
            _lMain.PushString(dev);
            _lMain.PushInteger(channel);
            _lMain.PushInteger(controller);
            _lMain.PushInteger(value);

            // Do the actual call.
            _lMain.PCall(4, 0, 0);

            // Get the results from the stack.
            // None.
        }
        #endregion

        #region Lua calls C# functions

        /// <summary> </summary>
        static int Log(IntPtr p)
        {
            Lua l = Lua.FromIntPtr(p)!;

            // Get args.
            int numArgs = l.GetTop();
            var level = l.ToInteger(1);
            var msg = l.ToStringL(2);

            // Do the work.
            _logger.Log((LogLevel)level, msg);

            return 0;
        }

        /// <summary> </summary>
        static int SendNote(IntPtr p) // TODOapp also string?
        {
            int numRes = 0;
            int notenum = 0;

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

            return numRes;
        }

        /// <summary>Send an explicit note on immediately. Caller is responsible for sending note off later.</summary>
        static int SendNoteOn(IntPtr p) // TODOapp also string?
        {
            int numRes = 0;
            //SendNote(chanName, notenum, vol);
            int notenum = 0;

            return numRes;
        }

        /// <summary>Send an explicit note off immediately.</summary>
        static int SendNoteOff(IntPtr p) // TODOapp also string?
        {
            int numRes = 0;
            //SendNote(chanName, notenum, 0);
            int notenum = 0;

            return numRes;
        }

        /// <summary>Send a controller immediately.</summary>
        static int SendController(IntPtr p)
        {
            var l = Lua.FromIntPtr(p);
            int numArgs = l.GetTop();
            int numRes = 0;

            ///// Get function arguments.
            string chanName = l.ToStringL(1);
            string controller = l.ToStringL(2);
            long? val = l.ToInteger(3);

            ///// Do the work.
            var ch = _instance._channels[chanName];
            int ctlrid = MidiDefs.GetControllerNumber(controller);
            //TODOapp ch.SendController((MidiController)ctlrid, (int)val);

            return numRes;
        }

        /// <summary>Send a midi patch immediately.</summary>
        static int SendPatch(IntPtr p)
        {
            var l = Lua.FromIntPtr(p);
            int numArgs = l.GetTop();
            int numRes = 0;

            ///// Get function arguments.
            string chanName = l.ToStringL(1)!;
            string patch = l.ToStringL(2)!;

            ///// Do the work.
            var ch = _instance._channels[chanName];
            int patchid = MidiDefs.GetInstrumentNumber(patch); // TODOapp handle fail
            ch.Patch = patchid;
            ch.SendPatch();

            //l.PushBoolean(true);
            //numRes++;
            return numRes;
        }

        #endregion


        #region TODOapp these could be in the script

        // CreateSequence(int beats, SequenceElements elements) -- -> Sequence

        // CreateSection(int beats, string name, SectionElements elements) -- -> Section

        /// <summary>Add a named chord or scale definition.</summary>
        static int CreateNotes(IntPtr p)
        {
            var l = Lua.FromIntPtr(p)!;

            // Get args.
            int numArgs = l.GetTop();

            var name = l.ToStringL(1);
            var parts = l.ToStringL(2);

            // Do the work.
            int numRes = 0;
            MusicDefinitions.AddChordScale(name, parts);

            return numRes;
        }

        /// <summary> </summary>
        static int GetNotes(IntPtr p)
        {
            var l = Lua.FromIntPtr(p)!;

            // Get args.
            int numArgs = l.GetTop();

            var noteString = l.ToStringL(1);

            // Do the work.
            int numRes = 0;
            List<int> notes = MusicDefinitions.GetNotesFromString(noteString);
            var dt = new DataTable(notes);
            l.PushDataTable(dt);

            numRes++;

            return numRes;
        }

        #endregion




        /// <summary>
        /// Lua script requires a high res timestamp - msec as double.
        /// </summary>
        /// <param name="p">Pointer to context.</param>
        /// <returns></returns>
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





        ////////////////////////////////////////////////////////////////////////////
        ///////////// TODOapp sequences etc from ScriptBase //////////////////////////
        ////////////////////////////////////////////////////////////////////////////


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
                            var ch = _channels[sectel.ChannelName];
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
