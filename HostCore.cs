using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Threading;
using System.Diagnostics;
using NAudio.Midi;
using Ephemera.NBagOfTricks;


namespace Nebulua
{
    public class HostCore : IDisposable
    {
        #region Fields
        /// <summary>App logger.</summary>
        readonly Logger _logger = LogManager.CreateLogger("COR");

        /// <summary>The interop.</summary>
        readonly Interop _interop = new();

        /// <summary>Fast timer.</summary>
        readonly MmTimerEx _mmTimer = new();

        ///// <summary>Diagnostics for timing measurement.</summary>
        //readonly TimingAnalyzer? _tan = null;

        /// <summary>All midi devices to use for send.</summary>
        public readonly List<MidiOutput> _outputs = [];

        /// <summary>All midi devices to use for receive. Includes any internal types.</summary>
        readonly List<MidiInput> _inputs = [];

        /// <summary>Resource management.</summary>
        bool _disposed = false;

        /// <summary>Current script. Null means none.</summary>
        string? _scriptFn = null;

        /// <summary>key = channel handle, value = play/silent.</summary>
        Dictionary<int, bool> _outputGates = [];
        #endregion

        #region Lifecycle
        /// <summary>
        /// Constructor inits static stuff.
        /// </summary>
        public HostCore()
        {
            // Hook script callbacks.
            Interop.Log += Interop_Log;
            Interop.OpenMidiInput += Interop_OpenMidiInput;
            Interop.OpenMidiOutput += Interop_OpenMidiOutput;
            Interop.SendMidiNote += Interop_SendMidiNote;
            Interop.SendMidiController += Interop_SendMidiController;
            Interop.SetTempo += Interop_SetTempo;

            // State change handler.
            State.Instance.ValueChangeEvent += State_ValueChangeEvent;
        }

        /// <summary>
        /// Cleanup.
        /// </summary>
        public void Dispose()
        {
            if (!_disposed)
            {
                // Dispose managed state (managed objects).
                _mmTimer.Stop();
                _mmTimer.Dispose();

                LogManager.Stop();

                // Destroy devices
                ResetIo();

                // Release unmanaged resources. https://stackoverflow.com/a/4935448
                _interop.Dispose();

                _disposed = true;
            }
        }
        #endregion

        #region Public workers
        /// <summary>
        /// Load and execute script. Can throw.
        /// </summary>
        /// <param name="scriptFn">The script file or null to reload current.</param>
        /// <returns></returns>
        /// <exception cref="ArgumentException"></exception>
        public void LoadScript(string? scriptFn = null)
        {
            ResetIo();

            // Check file arg.
            if (scriptFn is not null)
            {
                if (scriptFn.EndsWith(".lua") && Path.Exists(scriptFn))
                {
                    _scriptFn = scriptFn;
                }
                else
                {
                    throw new ArgumentException($"Invalid script file: {scriptFn}");
                }
            }
            else if (_scriptFn is null)
            {
                throw new ArgumentException("Can't reload, no current file");
            }

            // Load and run the new script.
            _logger.Info($"Loading script file {_scriptFn}");

            // Set up runtime lua environment. The lua lib files, the dir containing the script file.
            var srcDir = MiscUtils.GetSourcePath(); // The source dir.
            var scriptDir = Path.GetDirectoryName(_scriptFn);
            var luaPath = $"{scriptDir}\\?.lua;{srcDir}\\LBOT\\?.lua;{srcDir}\\lua\\?.lua;;";

            _interop.Run(_scriptFn, luaPath);
            State.Instance.ExecState = ExecState.Idle;

            string smeta = _interop.Setup();
            // Get info about the script.
            // string smeta = _interop.NebCommand("section_info", "");
            Dictionary<int, string> sectInfo = [];
            var chunks = smeta.SplitByToken("|");
            foreach (var chunk in chunks)
            {
                var elems = chunk.SplitByToken(",");
                sectInfo[int.Parse(elems[1])] = elems[0];
            }
            State.Instance.InitSectionInfo(sectInfo);

            // Start pump timer.
            SetTimer(State.Instance.Tempo);
            _mmTimer.Start();
        }





        public void EnableChannel(int devNum, int chanNum, bool enable) // TODO
        {
            var chanHnd = MakeOutHandle(devNum, chanNum);
            if (_outputGates.ContainsKey(chanHnd))
            {
                _outputGates[chanHnd] = enable;
            }
            else
            {
                throw new ArgumentException($"Invalid channel handle: {devNum}:{chanNum}");
            }
        }



//         public void SetOutputState(ChannelState state, int devNum, int chanNum)
//         {

// // _outputStates


//                 // TODO check all outs for any solo(s)

//                 bool anySolo = channels.Values.Where(c => c.State == ChannelState.Solo).Any();

//                 // Process any sequence steps.
//                 foreach (var ch in _channels.Values)
//                 {
//                     // Is it ok to play now?
//                     bool play = ch.State == ChannelState.Solo || (ch.State == ChannelState.Normal && !anySolo);

//                     if (play)
//                     {
//                         // Need exception handling here to protect from user script errors.
//                         try
//                         {
//                             ch.DoStep(_stepTime.TotalSubs);
//                         }
//                         catch (Exception ex)
//                         {
//                             ProcessScriptRuntimeError(ex);
//                         }
//                     }
//                 }

//                 // var chanHnd = MakeOutHandle(_outputs.IndexOf(output), e.chan_num);
//                 // _outputStates.Add(chanHnd, true) // throws if exists already!
//                 // e.ret = chanHnd; // valid return



//                 // // Check args for valid device and channel.
//                 // var (devIndex, chanNum) = DeconstructHandle(e.chan_hnd);

//                 // if (devIndex >= _outputs.Count ||
//                 //     chanNum < 1 ||
//                 //     chanNum > Defs.NUM_MIDI_CHANNELS ||
//                 //     _outputs[devIndex].Channels[chanNum - 1] == false)
//                 // {
//                 //     throw new SyntaxException($"Script has invalid channel: {e.chan_hnd}");
//                 // }

//                 // // Sound or quiet?
//                 // if (_outputStates[e.chan_hnd])
//                 // {
//                 //     int note_num = MathUtils.Constrain(e.note_num, 0, Defs.MIDI_VAL_MAX);

//                 //     // Check for note off.
//                 //     var vol = e.volume * State.Instance.Volume;
//                 //     int vel = vol == 0.0 ? 0 : MathUtils.Constrain((int)(vol * Defs.MIDI_VAL_MAX), 0, Defs.MIDI_VAL_MAX);
//                 //     MidiEvent evt = vel == 0?
//                 //         new NoteEvent(0, chanNum, MidiCommandCode.NoteOff, note_num, 0) :
//                 //         new NoteEvent(0, chanNum, MidiCommandCode.NoteOn, note_num, vel);

//                 //     var output = _outputs[devIndex];
//                 //     output.Send(evt);


//         }



        /// <summary>
        /// Input from internal non-midi device. Doesn't throw.
        /// </summary>
        public void InjectReceiveEvent(string devName, int channel, int noteNum, int velocity)
        {
            var input = _inputs.FirstOrDefault(o => o.DeviceName == devName);

            if (input is not null)
            {
                velocity = MathUtils.Constrain(velocity, Defs.MIDI_VAL_MIN, Defs.MIDI_VAL_MAX);
                NoteEvent nevt = velocity > 0 ?
                    new NoteOnEvent(0, channel, noteNum, velocity, 0) :
                    new NoteEvent(0, channel, MidiCommandCode.NoteOff, noteNum, 0);
                Midi_ReceiveEvent(input, nevt);
            }
            else
            {
                CallbackError(new SyntaxException($"Invalid internal device:{devName}"));
            }
        }

        /// <summary>
        /// Stop all midi.
        /// </summary>
        public void KillAll()
        {
            foreach (var o in _outputs)
            {
                for (int i = 0; i < Defs.NUM_MIDI_CHANNELS; i++)
                {
                    ControlChangeEvent cevt = new(0, i + 1, MidiController.AllNotesOff, 0);
                    o.Send(cevt);
                }
            }

            // Hard reset.
            State.Instance.ExecState = ExecState.Idle;
        }
        #endregion

        #region Event handlers
        /// <summary>
        /// Handler for state changes of interest. Doesn't throw.
        /// Responsible for core stuff like tempo, kill.
        /// </summary>
        /// <param name="_"></param>
        /// <param name="name">Specific State value.</param>
        void State_ValueChangeEvent(object? _, string name)
        {
            switch (name)
            {
                case "CurrentTick":
                    break;

                case "Tempo":
                    SetTimer(State.Instance.Tempo);
                    break;
            }
        }

        /// <summary>
        /// Process events. These are on the client UI thread now. Doesn't throw.
        /// </summary>
        /// <param name="totalElapsed"></param>
        /// <param name="periodElapsed"></param>
        void MmTimer_Callback(double totalElapsed, double periodElapsed)
        {
            if (State.Instance.ExecState == ExecState.Run)
            {
                try
                {
                    // Do script.
                    //_tan?.Arm();

                    int ret = _interop.Step(State.Instance.CurrentTick);

                    // Read stopwatch and diff/stats.
                    //string? s = _tan?.Dump();

                    if (State.Instance.IsComposition)
                    {
                        // Bump time and check state.
                        int start = State.Instance.LoopStart == -1 ? 0 : State.Instance.LoopStart;
                        int end = State.Instance.LoopEnd == -1 ? State.Instance.Length : State.Instance.LoopEnd;

                        if (++State.Instance.CurrentTick >= end) // done
                        {
                            // Keep going? else stop/rewind.
                            if (State.Instance.DoLoop)
                            {
                                // Keep going.
                                State.Instance.CurrentTick = start;
                            }
                            else
                            {
                                // Stop and rewind.
                                State.Instance.ExecState = ExecState.Idle;
                                State.Instance.CurrentTick = start;

                                // just in case
                                KillAll();
                            }
                        }
                    }
                    else // dynamic script
                    {
                        ++State.Instance.CurrentTick;
                    }
                }
                catch (Exception ex)
                {
                    _logger.Exception(ex);
                }
            }
        }

        /// <summary>
        /// Midi input arrived. These are on the client UI thread now. Can throw.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void Midi_ReceiveEvent(object? sender, MidiEvent e)
        {
            try
            {
                int index = _inputs.IndexOf((MidiInput)sender!);
                int chanHnd = MakeInHandle(index, e.Channel);
                bool logit = true;

                switch (e)
                {
                    case NoteOnEvent evt:
                        _interop.ReceiveMidiNote(chanHnd, evt.NoteNumber, (double)evt.Velocity / Defs.MIDI_VAL_MAX);
                        break;

                    case NoteEvent evt:
                        _interop.ReceiveMidiNote(chanHnd, evt.NoteNumber, 0);
                        break;

                    case ControlChangeEvent evt:
                        _interop.ReceiveMidiController(chanHnd, (int)evt.Controller, evt.ControllerValue);
                        break;

                    default: // Ignore others for now.
                        logit = false;
                        break;
                }

                if (logit && UserSettings.Current.MonitorRcv)
                {
                    _logger.Trace($"RCV {FormatMidiEvent(e, State.Instance.ExecState == ExecState.Run ? State.Instance.CurrentTick : 0, chanHnd)}");
                }
            }
            catch (Exception ex)
            {
                _logger.Exception(ex);
            }
        }
        #endregion

        #region Script => Host Callbacks
        /// <summary>
        /// Script creates an input channel.
        /// </summary>
        /// <param name="_"></param>
        /// <param name="e"></param>
        void Interop_OpenMidiInput(object? _, OpenMidiInputArgs e)
        {
            try
            {
                e.ret = 0;

                // Check args.
                if (e.dev_name is null || e.dev_name.Length == 0 || e.chan_num < 1 || e.chan_num > Defs.NUM_MIDI_CHANNELS)
                {
                    throw new SyntaxException($"Script has invalid input midi device: {e.dev_name}");
                }

                // Locate or create the device.
                var input = _inputs.FirstOrDefault(o => o.DeviceName == e.dev_name);
                if (input is null)
                {
                    input = new(e.dev_name); // throws if invalid
                    input.ReceiveEvent += Midi_ReceiveEvent;
                    _inputs.Add(input);
                }

                if (e.chan_num == 0) // listen to all
                {
                    Enumerable.Range(0, Defs.NUM_MIDI_CHANNELS).ForEach(ch => input.Channels[ch] = true);
                }
                else
                {
                    input.Channels[e.chan_num - 1] = true;
                }

                e.ret = MakeInHandle(_inputs.Count - 1, e.chan_num);
            }
            catch (Exception ex)
            {
                CallbackError(ex);
            }
        }

        /// <summary>
        /// Script creates an output channel.
        /// </summary>
        /// <param name="_"></param>
        /// <param name="e"></param>
        void Interop_OpenMidiOutput(object? _, OpenMidiOutputArgs e)
        {
            try
            {
                e.ret = 0; // chanHnd default means invalid

                // Check args.
                if (e.dev_name is null || e.dev_name.Length == 0 || e.chan_num < 1 || e.chan_num > Defs.NUM_MIDI_CHANNELS)
                {
                    throw new SyntaxException($"Script has invalid input midi device: {e.dev_name}");
                }

                // Locate or create the device.
                var output = _outputs.FirstOrDefault(o => o.DeviceName == e.dev_name);
                if (output is null)
                {
                    output = new(e.dev_name); // throws if invalid
                    _outputs.Add(output);
                }

                // Add specific channel.
                output.Channels[e.chan_num - 1] = true;
                var chanHnd = MakeOutHandle(_outputs.IndexOf(output), e.chan_num);
                _outputGates.Add(chanHnd, true); // throws if exists already!
                e.ret = chanHnd; // valid return

                if (e.patch >= 0)
                {
                    // Send the patch now.
                    PatchChangeEvent pevt = new(0, e.chan_num, e.patch);
                    output.Send(pevt);
                }
            }
            catch (Exception ex)
            {
                CallbackError(ex);
            }
        }

        /// <summary>
        /// Script wants to send a midi note.
        /// </summary>
        /// <param name="_"></param>
        /// <param name="e"></param>
        void Interop_SendMidiNote(object? _, SendMidiNoteArgs e)
        {
            try
            {
                e.ret = 0; // not used

                // Check args for valid device and channel.
                var (devIndex, chanNum) = DeconstructHandle(e.chan_hnd);

                if (devIndex >= _outputs.Count ||
                    chanNum < 1 ||
                    chanNum > Defs.NUM_MIDI_CHANNELS ||
                    _outputs[devIndex].Channels[chanNum - 1] == false)
                {
                    throw new SyntaxException($"Script has invalid channel: {e.chan_hnd}");
                }

                // Sound or quiet?
                if (_outputGates[e.chan_hnd])
                {
                    int note_num = MathUtils.Constrain(e.note_num, 0, Defs.MIDI_VAL_MAX);

                    // Check for note off.
                    var vol = e.volume * State.Instance.Volume;
                    int vel = vol == 0.0 ? 0 : MathUtils.Constrain((int)(vol * Defs.MIDI_VAL_MAX), 0, Defs.MIDI_VAL_MAX);
                    MidiEvent evt = vel == 0?
                        new NoteEvent(0, chanNum, MidiCommandCode.NoteOff, note_num, 0) :
                        new NoteEvent(0, chanNum, MidiCommandCode.NoteOn, note_num, vel);

                    var output = _outputs[devIndex];
                    output.Send(evt);

                    if (UserSettings.Current.MonitorSnd)
                    {
                        _logger.Trace($"SND {FormatMidiEvent(evt, State.Instance.ExecState == ExecState.Run ? State.Instance.CurrentTick : 0, e.chan_hnd)}");
                    }
                }
            }
            catch (Exception ex)
            {
                CallbackError(ex);
            }
        }

        /// <summary>
        /// Script wants to send a midi controller.
        /// </summary>
        /// <param name="_"></param>
        /// <param name="e"></param>
        void Interop_SendMidiController(object? _, SendMidiControllerArgs e)
        {
            try
            {
                e.ret = 0; // not used

                // Check args.
                var (devIndex, chanNum) = DeconstructHandle(e.chan_hnd);

                if (devIndex >= _outputs.Count || chanNum < 1 || chanNum > Defs.NUM_MIDI_CHANNELS || !_outputs[devIndex].Channels[chanNum - 1])
                {
                    throw new SyntaxException($"Script has invalid channel: {e.chan_hnd}");
                }

                int controller = MathUtils.Constrain(e.controller, 0, Defs.MIDI_VAL_MAX);
                int value = MathUtils.Constrain(e.value, 0, Defs.MIDI_VAL_MAX);

                var output = _outputs[devIndex];
                MidiEvent evt;

                evt = new ControlChangeEvent(0, chanNum, (MidiController)controller, value);

                output.Send(evt);

                if (UserSettings.Current.MonitorSnd)
                {
                    _logger.Trace($"SND {FormatMidiEvent(evt, State.Instance.ExecState == ExecState.Run ? State.Instance.CurrentTick : 0, e.chan_hnd)}");
                }
            }
            catch (Exception ex)
            {
                CallbackError(ex);
            }
        }

        /// <summary>
        /// Script wants to change tempo.
        /// </summary>
        /// <param name="_"></param>
        /// <param name="e"></param>
        /// <exception cref="SyntaxException"></exception>
        void Interop_SetTempo(object? _, SetTempoArgs e)
        {
            if (e.bpm >= 30 && e.bpm <= 240)
            {
                State.Instance.Tempo = e.bpm;
                SetTimer(State.Instance.Tempo);
                //e.Ret = 0;
            }
            else if (e.bpm == 0)
            {
                SetTimer(0);
            }
            else
            {
                //e.Ret = 1;
                SetTimer(0);
                CallbackError(new SyntaxException($"SCRIPT Invalid tempo: {e.bpm}"));
            }
        }

        /// <summary>
        /// Script wants to log something.
        /// </summary>
        /// <param name="_"></param>
        /// <param name="e"></param>
        void Interop_Log(object? _, LogArgs e)
        {
            if (e.level >= (int)LogLevel.Trace && e.level <= (int)LogLevel.Error)
            {
                string s = $"SCRIPT {e.msg ?? "null"}";
                switch ((LogLevel)e.level)
                {
                    case LogLevel.Trace: _logger.Trace(s); break;
                    case LogLevel.Debug: _logger.Debug(s); break;
                    case LogLevel.Info:  _logger.Info(s); break;
                    case LogLevel.Warn:  _logger.Warn(s); break;
                    case LogLevel.Error: _logger.Error(s); break;
                }

                e.ret = 0;
            }
            else
            {
                CallbackError(new SyntaxException($"SCRIPT Invalid log level: {e.level}"));
            }
        }
        #endregion

        #region Private functions
        /// <summary>
        /// Clean up devices.
        /// </summary>
        void ResetIo()
        {
            _inputs.ForEach(d => d.Dispose());
            _inputs.Clear();
            _outputs.ForEach(d => d.Dispose());
            _outputs.Clear();
            _outputGates.Clear();
        }

        /// <summary>
        /// Set timer for this tempo.
        /// </summary>
        /// <param name="tempo"></param>
        void SetTimer(int tempo)
        {
            if (tempo > 0)
            {
                double sec_per_beat = 60.0 / tempo;
                double msec_per_sub = 1000 * sec_per_beat / MusicTime.SUBS_PER_BEAT;
                double period = msec_per_sub > 1.0 ? msec_per_sub : 1;
                _mmTimer.SetTimer((int)Math.Round(period, 2), MmTimer_Callback);
            }
            else // stop
            {
                _mmTimer.SetTimer(0, MmTimer_Callback);
            }
        }

        /// <summary>
        /// General purpose handler for errors in callback functions/threads that can't throw exceptions.
        /// </summary>
        /// <param name="ex">The exception</param>
        void CallbackError(Exception ex)
        {
            var (fatal, msg) = Utils.ProcessException(ex);
            if (fatal)
            {
                State.Instance.ExecState = ExecState.Dead;
                // Logging an error will cause the app to exit.
                _logger.Error(msg);
            }
            else
            {
                // User can decide what to do with this. They may be recoverable so use warn.
                State.Instance.ExecState = ExecState.Dead;
                _logger.Warn(msg);
            }
        }

        /// <summary>
        /// Create string suitable for logging.
        /// </summary>
        /// <param name="evt">Midi event to format.</param>
        /// <param name="tick">Current tick.</param>
        /// <param name="chanHnd">Channel info.</param>
        /// <returns>Suitable string.</returns>
        string FormatMidiEvent(MidiEvent evt, int tick, int chanHnd)
        {
            // Common part.
            (int devIndex, int chanNum) = DeconstructHandle(chanHnd);
            string s = $"{tick:00000} {MusicTime.Format(tick)} {evt.CommandCode} Dev:{devIndex} Ch:{chanNum} ";

            switch (evt)
            {
                case NoteEvent e:
                    var snote = chanNum == 10 || chanNum == 16 ?
                        $"DRUM_{e.NoteNumber}" :
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

        /// <summary>Make a standard output handle.</summary>
        int MakeOutHandle(int devIndex, int chanNum)
        {
            return (devIndex << 8) | chanNum | 0x8000;
        }

        /// <summary>Make a standard input handle.</summary>
        int MakeInHandle(int devIndex, int chanNum)
        {
            return (devIndex << 8) | chanNum;
        }

        /// <summary>Take apart a standard in/out handle.</summary>
        (int devIndex, int chanNum) DeconstructHandle(int chanHnd)
        {
            return (((chanHnd & ~0x8000) >> 8) & 0xFF, (chanHnd & ~0x8000) & 0xFF);
        }
        #endregion
    }
}
