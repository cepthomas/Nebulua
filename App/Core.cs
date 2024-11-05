using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Threading;
using System.Diagnostics;
using NAudio.Midi;
using Ephemera.NBagOfTricks;
using Ephemera.NBagOfTricks.Slog;
using Nebulua.Interop;


namespace Nebulua
{
    public class Core : IDisposable
    {
        #region Fields
        /// <summary>App logger.</summary>
        readonly Logger _logger = LogManager.CreateLogger("Core");

        /// <summary>The interop.</summary>
        AppInterop _interop;

        /// <summary>Fast timer.</summary>
        readonly MmTimerEx _mmTimer = new();

        ///// <summary>Diagnostics for timing measurement.</summary>
        //readonly TimingAnalyzer? _tan = null;

        /// <summary>All midi devices to use for send.</summary>
        readonly List<MidiOutput> _outputs = [];

        /// <summary>All midi devices to use for receive. Includes any internal types.</summary>
        readonly List<MidiInput> _inputs = [];

        /// <summary>Resource management.</summary>
        bool _disposed = false;
        #endregion

        #region Proerties
        /// <summary>Current script.</summary>
        public string? CurrentScriptFn { get; private set; }

        /// <summary>Error message.</summary>
        public string Error { get { return _interop.Error; } }
        #endregion

        #region Lifecycle
        /// <summary>
        /// Constructor inits static stuff.
        /// </summary>
        public Core()
        {
            // Hook script callbacks.
            AppInterop.CreateChannel += Interop_CreateChannel;
            AppInterop.Send += Interop_Send;
            AppInterop.Log += Interop_Log;
            AppInterop.PropertyChange += Interop_PropertyChange;

            // State change handler.
            State.Instance.ValueChangeEvent += State_ValueChangeEvent;

            // Set up runtime lua environment.
            _interop = new([Path.Join(Utils.GetAppRoot(), "lua")]);
        }

        /// <summary>
        /// Cleanup. Note that I am taking charge of explicit disposal of resources.
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

        #region Primary workers
        /// <summary>
        /// Load and execute script. Can throw.
        /// </summary>
        /// <param name="scriptFn">The script file or null to reload current.</param>
        /// <returns></returns>
        /// <exception cref="ApplicationArgumentException"></exception>
        /// <exception cref="InvalidOperationException"></exception>
        /// <exception cref="AppInteropException"></exception>
        public NebStatus LoadScript(string? scriptFn = null)
        {
            ResetIo();

            // Check file arg.
            if (scriptFn is not null)
            {
                if (scriptFn.EndsWith(".lua") && Path.Exists(scriptFn))
                {
                    CurrentScriptFn = scriptFn;
                }
                else
                {
                    throw new ApplicationArgumentException($"Invalid script file: {scriptFn}");
                }
            }
            else if (CurrentScriptFn is null)
            {
                throw new InvalidOperationException("Can't reload, no current file");
            }

            // Unload current modules so reload will be minty fresh. This may fail safely if no script loaded yet.
            string ret = _interop.NebCommand("unload_all", "no arg");

            _logger.Info($"Loading script file {CurrentScriptFn}");

            NebStatus stat = _interop.OpenScript(CurrentScriptFn);
            if (stat != NebStatus.Ok)
            {
                throw new AppInteropException("AppInterop open script failed", _interop.Error);
            }

            // Do some config now.
            _interop.NebCommand("root_dir", Utils.GetAppRoot());

            // Get info about the script.
            Dictionary<int, string> sectInfo = [];
            string sinfo = _interop.NebCommand("section_info", "no arg");

            var chunks = sinfo.SplitByToken("|");
            foreach (var chunk in chunks)
            {
                var elems = chunk.SplitByToken(",");
                sectInfo[int.Parse(elems[1])] = elems[0];
            }
            State.Instance.InitSectionInfo(sectInfo);

            State.Instance.ExecState = ExecState.Idle;

            // Start timer.
            SetTimer(State.Instance.Tempo);
            _mmTimer.Start();

            return stat;
        }

        /// <summary>
        /// Input from internal non-midi device. Doesn't throw.
        /// </summary>
        public void InjectReceiveEvent(string devName, int channel, int noteNum, int velocity)
        {
            var input = _inputs.FirstOrDefault(o => o.DeviceName == devName);
            
            if (input is not null)
            {
                NoteEvent nevt = velocity > 0 ?
                    new NoteOnEvent(0, channel, noteNum, velocity, 0) :
                    new NoteEvent(0, channel, MidiCommandCode.NoteOff, noteNum, 0);

                Midi_ReceiveEvent(input, nevt);
            }
            else
            {
                CallbackError(new ScriptSyntaxException($"Invalid internal device:{devName}"));
            }
        }

        /// <summary>
        /// Stop all midi.
        /// </summary>
        public void KillAll()
        {
            foreach (var o in _outputs)
            {
                for (int i = 0; i < MidiDefs.NUM_MIDI_CHANNELS; i++)
                {
                    ControlChangeEvent cevt = new(0, i + 1, MidiController.AllNotesOff, 0);
                    o.Send(cevt);
                }
            }

            // Hard reset.
            State.Instance.ExecState = ExecState.Idle;
        }

        /// <summary>
        /// Handler for state changes of interest. Doesn't throw.
        /// Responsible for core stuff like tempo, kill.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="name">Specific State value.</param>
        void State_ValueChangeEvent(object? sender, string name)
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
                // Do script. TODOF Handle solo and/or mute like nebulator.

                //_tan?.Arm();

                NebStatus stat = _interop!.Step(State.Instance.CurrentTick);
                if (stat != NebStatus.Ok)
                {
                   CallbackError(new AppInteropException("Step() failed", _interop.Error));
                }

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
        }

        /// <summary>
        /// Midi input arrived. These are on the client UI thread now. Can throw.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void Midi_ReceiveEvent(object? sender, MidiEvent e)
        {
            NebStatus stat = NebStatus.Ok;

            int index = _inputs.IndexOf((MidiInput)sender!);
            int chan_hnd = MakeInHandle(index, e.Channel);
            bool logit = true;

            switch (e)
            {
                case NoteOnEvent evt:
                    stat = _interop!.RcvNote(chan_hnd, evt.NoteNumber, (double)evt.Velocity / MidiDefs.MIDI_VAL_MAX);
                    break;

                case NoteEvent evt:
                    stat = _interop!.RcvNote(chan_hnd, evt.NoteNumber, 0);
                    break;

                case ControlChangeEvent evt:
                    stat = _interop!.RcvController(chan_hnd, (int)evt.Controller, evt.ControllerValue);
                    break;

                default: // Ignore others for now.
                    logit = false;
                    break;
            }

            if (logit && UserSettings.Current.MonitorRcv)
            {
                _logger.Trace($"RCV {FormatMidiEvent(e, State.Instance.ExecState == ExecState.Run ? State.Instance.CurrentTick : 0, chan_hnd)}");
            }

            if (stat != NebStatus.Ok)
            {
               CallbackError(new AppInteropException("Midi Receive() failed", _interop!.Error));
            }
        }
        #endregion

        #region Script Event Handlers
        /// <summary>
        /// Script wants to define a channel. Can throw.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        /// <exception cref="ScriptSyntaxException"></exception>
        void Interop_CreateChannel(object? sender, CreateChannelArgs e)
        {
            //Debug.WriteLine($"Core.Interop_CreateChannel() this={GetHashCode()} _interop={_interop?.GetHashCode()} sender={sender?.GetHashCode()} _disposed={_disposed}");

            e.Ret = 0; // chan_hnd default means invalid

            // Check args.
            if (e.DevName is null || e.DevName.Length == 0 || e.ChanNum < 1 || e.ChanNum > MidiDefs.NUM_MIDI_CHANNELS)
            {
                throw new ScriptSyntaxException($"Script has invalid input midi device: {e.DevName}");
            }

            if (e.IsOutput)
            {
                // Locate or create the device.
                var output = _outputs.FirstOrDefault(o => o.DeviceName == e.DevName);
                if (output is null) 
                {
                    output = new(e.DevName); // throws if invalid
                    _outputs.Add(output);
                }

                output.Channels[e.ChanNum - 1] = true;
                e.Ret = MakeOutHandle(_outputs.Count - 1, e.ChanNum);

                if (e.Patch >= 0)
                {
                    // Send the patch now.
                    PatchChangeEvent pevt = new(0, e.ChanNum, e.Patch);
                    output.Send(pevt);
                }
            }
            else
            {
                // Locate or create the device.
                var input = _inputs.FirstOrDefault(o => o.DeviceName == e.DevName);
                if (input is null)
                {
                    input = new(e.DevName); // throws if invalid
                    input.ReceiveEvent += Midi_ReceiveEvent;
                    _inputs.Add(input);
                }

                input.Channels[e.ChanNum - 1] = true;
                e.Ret = MakeInHandle(_inputs.Count - 1, e.ChanNum);
            }
        }

        /// <summary>
        /// Sending some midi from script. Can throw.
        /// </summary>
        /// <param name="_"></param>
        /// <param name="e"></param>
        /// <exception cref="ScriptSyntaxException"></exception>
        void Interop_Send(object? _, SendArgs e)
        {
            e.Ret = 0; // not used

            // Check args.
            var (index, chan_num) = DeconstructHandle(e.ChanHnd);

            if (index >= _outputs.Count || chan_num < 1 || chan_num > MidiDefs.NUM_MIDI_CHANNELS || !_outputs[index].Channels[chan_num - 1])
            {
                throw new ScriptSyntaxException($"Script has invalid channel: {e.ChanHnd}");
            }

            if (e.What < 0 || e.What > MidiDefs.MIDI_VAL_MAX || e.Value < 0 || e.Value > MidiDefs.MIDI_VAL_MAX)
            {
                // Warn and constrain, not stop.
                _logger.Warn($"Script has invalid payload: {e.What} {e.Value}");
                e.What = MathUtils.Constrain(e.What, 0, MidiDefs.MIDI_VAL_MAX);
                e.Value = MathUtils.Constrain(e.Value, 0, MidiDefs.MIDI_VAL_MAX);
                // throw new ScriptSyntaxException($"Script has invalid payload: {e.What} {e.Value}");
            }

            var output = _outputs[index];
            MidiEvent evt;

            if (e.IsNote)
            {
                // Check velocity for note off.
                evt = e.Value == 0 ?
                    new NoteEvent(0, chan_num, MidiCommandCode.NoteOff, e.What, 0) :
                    new NoteEvent(0, chan_num, MidiCommandCode.NoteOn, e.What, e.Value);
            }
            else
            {
                evt = new ControlChangeEvent(0, chan_num, (MidiController)e.What, e.Value);
            }

            output.Send(evt);

            if (UserSettings.Current.MonitorSnd)
            {
                _logger.Trace($"SND {FormatMidiEvent(evt, State.Instance.ExecState == ExecState.Run ? State.Instance.CurrentTick : 0, e.ChanHnd)}");
            }
        }

        /// <summary>
        /// Log something from script.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void Interop_Log(object? sender, LogArgs e)
        {
            if (e.LogLevel >= (int)LogLevel.Trace && e.LogLevel <= (int)LogLevel.Error) 
            {
                _logger.Log((LogLevel)e.LogLevel, $"SCRIPT {e.Msg}");
                e.Ret = 0;
            }
            else
            {
                CallbackError(new ScriptSyntaxException($"SCRIPT Invalid log level: {e.LogLevel}"));
            }
        }

        /// <summary>
        /// Script wants to change a property. Can throw.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        /// <exception cref="ScriptSyntaxException"></exception>
        void Interop_PropertyChange(object? sender, PropertyArgs e)
        {
            if (e.Bpm >= 30 && e.Bpm <= 240)
            {
                State.Instance.Tempo = e.Bpm;
                SetTimer(State.Instance.Tempo);
                e.Ret = 0;
            }
            else if (e.Bpm == 0)
            {
                SetTimer(0);
            }
            else
            {
                e.Ret = 1;
                SetTimer(0);
                throw new ScriptSyntaxException($"Invalid tempo: {e.Bpm}");
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
        /// <param name="chan_hnd">Channel info.</param>
        /// <returns>Suitable string.</returns>
        public string FormatMidiEvent(MidiEvent evt, int tick, int chan_hnd)
        {
            // Common part.
            (int index, int chan_num) = DeconstructHandle(chan_hnd);
            string s = $"{tick:00000} {MusicTime.Format(tick)} {evt.CommandCode} Dev:{index} Ch:{chan_num} ";

            switch (evt)
            {
                case NoteEvent e:
                    var snote = chan_num == 10 || chan_num == 16 ?
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
        public int MakeOutHandle(int index, int chan_num)
        {
            return (index << 8) | chan_num | 0x8000;
        }

        /// <summary>Make a standard input handle.</summary>
        public int MakeInHandle(int index, int chan_num)
        {
            return (index << 8) | chan_num;
        }

        /// <summary>Take apart a standard in/out handle.</summary>
        public (int index, int chan_num) DeconstructHandle(int chan_hnd)
        {
            return (((chan_hnd & ~0x8000) >> 8) & 0xFF, (chan_hnd & ~0x8000) & 0xFF);
        }

        #endregion
    }
}
