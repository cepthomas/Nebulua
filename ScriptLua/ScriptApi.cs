using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Xml.Linq;
using System.Drawing;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using KeraLuaEx;
using Ephemera.NBagOfTricks.Slog;
using Ephemera.NBagOfTricks;
using Ephemera.MidiLib;
//using NAudio.Midi;
//using System.Windows.Forms;


// Nebulator script API stuff.


namespace Ephemera.Nebulua.Script
{
    public class ScriptApi : IDisposable
    {
        // #region Properties that can be read in the user script.
        // /// <summary>Sound is playing.</summary>
        // public bool Playing { set { _lMain.PushBoolean(value); _lMain.SetGlobal("playing"); } }

        // /// <summary>Current Nebulator step time.</summary>
        // public double StepTime { set { _lMain.PushNumber(value); _lMain.SetGlobal("step_time"); } }

        // /// <summary>Actual time since start pressed.</summary>
        // public double RealTime { set { _lMain.PushNumber(value); _lMain.SetGlobal("real_time"); } }

        // /// <summary>Nebulator Speed in bpm.</summary>
        // public int Tempo { set { _lMain.PushInteger(value); _lMain.SetGlobal("tempo"); } }

        // /// <summary>Nebulator master Volume.</summary>
        // public double MasterVolume { set { _lMain.PushNumber(value); _lMain.SetGlobal("master_volume"); } }
        // #endregion


        ///// >>>>>>>>>> From Nebulator.
        static readonly Dictionary<string, Channel> _channels = new();
        /// <summary>All devices to use for send. Key is my id (not the system driver name).</summary>
        static readonly Dictionary<string, IOutputDevice> _outputDevices = new();
        /// <summary>All devices to use for receive. Key is name/id, not the system name.</summary>
        static readonly Dictionary<string, IInputDevice> _inputDevices = new();



        #region Fields - private or internal
        /// <summary>Main logger.</summary>
        static readonly Logger _logger = LogManager.CreateLogger("Script");

        // Main execution lua state.
        static readonly Lua _lMain = new();

        // Bound static functions.
        static readonly LuaFunction _fLog = Log;
        static readonly LuaFunction _fSendController = SendController;
        static readonly LuaFunction _fSendNote = SendNote;
        static readonly LuaFunction _fSendNoteOn = SendNoteOn;
        static readonly LuaFunction _fSendNoteOff = SendNoteOff;
        static readonly LuaFunction _fSendPatch = SendPatch;
        static readonly LuaFunction _fGetNotes = GetNotes;
        static readonly LuaFunction _fCreateNotes = CreateNotes;

        #endregion

        #region Lifecycle
        /// <summary>
        /// Load file and init everything.
        /// This may throw an exception - client needs to handle them.
        /// </summary>
        /// <param name="fn">Lua file to open.</param>
// <param name="luaPaths">Optional lua paths.</param>
        public static void Load(string fn)//, List<string> luaPaths)// = null)
        {
            // Load the script file.
            bool ok = true;

            string path = fn; //Path.Combine("Test", "scripts", $"{fn}.lua");

            try
            {
                LuaStatus lstat = _lMain.LoadFile(path);

                // Bind lua functions to internal.
                _lMain.Register("log", _fLog);
                _lMain.Register("send_controller", _fSendController); //send_controller(chan, controller, val)
                _lMain.Register("send_note", _fSendNote); //send_note(chan, note, vol, dur)
                _lMain.Register("send_note_on", _fSendNoteOn); //send_note_on(chan, note, vol)
                _lMain.Register("send_note_off", _fSendNoteOff); //send_note_off(chan, note)
                _lMain.Register("send_patch", _fSendPatch); // send_patch(chan, patch)
                _lMain.Register("get_notes", _fGetNotes); //get_notes("B4.MY_SCALE")
                _lMain.Register("create_notes", _fCreateNotes); //create_notes("MY_SCALE", "1 3 4 b7")

                // TODOApp get/init the inputs and outputs. And anything else....
                _channels.Clear();
            }
            catch (Exception ex)
            {
                throw;
            }
        }

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

        #region C# calls lua functions
        /// <summary>Called to initialize Nebulator stuff.</summary>
        //throws
        public static void Setup()
        {
            // Get the function to be called.
            LuaType gtype = _lMain.GetGlobal("setup"); // TODOApp check these.

            // Push the arguments to the call.
            // None.

            // Do the actual call.
            LuaStatus lstat = _lMain.PCall(0, 0, 0);
            _lMain.CheckLuaStatus(lstat);

            // Get the results from the stack.
            // None.
        }

        /// <summary>Called every mmtimer increment.</summary>
                //throws

        public static void Step(int bar, int beat, int subdiv)
        {
            // Get the function to be called. Check return.
            LuaType gtype = _lMain.GetGlobal("step"); // TODOApp check these.

            // Push the arguments to the call.
            _lMain.PushInteger(bar);
            _lMain.PushInteger(beat);
            _lMain.PushInteger(subdiv);

            // Do the actual call.
            LuaStatus lstat = _lMain.PCall(3, 0, 0);
            _lMain.CheckLuaStatus(lstat);

            // Get the results from the stack.
            // None.
        }

        /// <summary>Called when input arrives. Optional.</summary>
                //throws

        public static void InputNote(string dev, int channel, int note, int vel)
        {
            // Get the function to be called. Check return.
            LuaType gtype = _lMain.GetGlobal("input_note");
            if (gtype != LuaType.Function) // optional function
                return;

            // Push the arguments to the call.
            _lMain.PushString(dev);
            _lMain.PushInteger(channel);
            _lMain.PushInteger(note);
            _lMain.PushInteger(vel);

            // Do the actual call.
            LuaStatus lstat = _lMain.PCall(4, 0, 0);
            _lMain.CheckLuaStatus(lstat);

            // Get the results from the stack.
            // None.
        }

        /// <summary>Called when input arrives. Optional.</summary>
                //throws

        public static void InputController(string dev, int channel, int controller, int value)
        {
            // Get the function to be called. Check return.
            LuaType gtype = _lMain.GetGlobal("input_controller");
            if (gtype != LuaType.Function) // optional function
                return;

            // Push the arguments to the call.
            _lMain.PushString(dev);
            _lMain.PushInteger(channel);
            _lMain.PushInteger(controller);
            _lMain.PushInteger(value);

            // Do the actual call.
            LuaStatus lstat = _lMain.PCall(4, 0, 0);
            _lMain.CheckLuaStatus(lstat);

            // Get the results from the stack.
            // None.
        }
        #endregion

        #region Lua calls C# functions

        // TODOApp impl these??
        // CreateSequence(int beats, SequenceElements elements) -- -> Sequence
        // CreateSection(int beats, string name, SectionElements elements) -- -> Section

        /// <summary>Add a named chord or scale definition.</summary>
        static int CreateNotes(IntPtr p)
        {
            var l = Lua.FromIntPtr(p)!;

            // Get args.
            int numArgs = l.GetTop();

            var name = l.ToString(1);
            var parts = l.ToString(2);

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

            var noteString = l.ToString(1);

            // Do the work.
            int numRes = 0;
            List<int> notes = MusicDefinitions.GetNotesFromString(noteString);
            l.PushList(notes);
            numRes++;

            return numRes;
        }

        /// <summary> </summary>
        static int Log(IntPtr p)
        {
            Lua l = Lua.FromIntPtr(p)!;

            // Get args.
            int numArgs = l.GetTop();
            var level = l.ToInteger(1);
            var msg = l.ToString(2);

            // Do the work.
            _logger.Log((LogLevel)level, msg);

            return 0;
        }

        /// <summary> </summary>
        static int SendNote(IntPtr p)
        {
            int numRes = 0;
            int notenum = 0; // TODOApp also string!

            //string chanName, int notenum, double vol, double dur) //send_note(chan, note, vol, dur)

            //if (!_channels.ContainsKey(chanName))
            //{
            //    throw new ArgumentException($"Invalid channel: {chanName}");
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
        static int SendNoteOn(IntPtr p)
        {
            int numRes = 0;
            //SendNote(chanName, notenum, vol);
            int notenum = 0; // TODOApp also string!

            return numRes;
        }

        /// <summary>Send an explicit note off immediately.</summary>
        static int SendNoteOff(IntPtr p)
        {
            int numRes = 0;
            //SendNote(chanName, notenum, 0);
            int notenum = 0; // TODOApp also string!

            return numRes;
        }

        /// <summary>Send a controller immediately.</summary>
        static int SendController(IntPtr p)
        {
            var l = Lua.FromIntPtr(p);
            int numArgs = l.GetTop();
            int numRes = 0;

            ///// Get function arguments.
            string chanName = l.ToString(1);
            string controller = l.ToString(2);
            long? val = l.ToInteger(3); // TODOApp handle fail

            ///// Do the work.
            var ch = _channels[chanName]; // TODOApp handle fail
            int ctlrid = MidiDefs.GetControllerNumber(controller);
//TODOApp            ch.SendController((MidiController)ctlrid, (int)val);

            return numRes;
        }

        /// <summary>Send a midi patch immediately.</summary>
        static int SendPatch(IntPtr p)
        {
            var l = Lua.FromIntPtr(p);
            int numArgs = l.GetTop();
            int numRes = 0;

            ///// Get function arguments.
            string chanName = l.ToString(1)!;
            string patch = l.ToString(2)!;

            ///// Do the work.
            var ch = _channels[chanName]; // TODOApp handle fail
            int patchid = MidiDefs.GetInstrumentNumber(patch); // TODOApp handle fail
            ch.Patch = patchid;
            ch.SendPatch();

            //l.PushBoolean(true);
            //numRes++;
            return numRes;
        }
        #endregion
    }
}
