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

        /// <summary>The interop API.</summary>
        Api? _api;

        /// <summary>Client supplied context for LUA_PATH.</summary>
        readonly List<string> _luaPath = [];

        /// <summary>Fast timer.</summary>
        readonly MmTimerEx _mmTimer = new();

        ///// <summary>Diagnostics for timing measurement.</summary>
        //readonly TimingAnalyzer? _tan = null;

        /// <summary>All midi devices to use for send.</summary>
        readonly List<MidiOutput> _outputs = [];

        /// <summary>All midi devices to use for receive. Includes any internal types.</summary>
        readonly List<MidiInput> _inputs = [];

        /// <summary>Current script.</summary>
        string? _currentScriptFn = null;

        /// <summary>Resource management.</summary>
        bool _disposed = false;
        #endregion

        #region Lifecycle
        /// <summary>
        /// Constructor inits stuff. Can throw on main thread.
        /// </summary>
        public Core()
        {
            // Set up runtime lua environment.
            var exePath = Environment.CurrentDirectory; // where exe lives
            _luaPath.Add($@"{exePath}\lua_code"); // app lua files

            // Hook script callbacks.
            Api.CreateChannel += Interop_CreateChannel;
            Api.Send += Interop_Send;
            Api.Log += Interop_Log;
            Api.PropertyChange += Interop_PropertyChange;

            // State change handler.
            State.Instance.ValueChangeEvent += State_ValueChangeEvent;
        }

        /// <summary>
        /// Clean up.
        /// </summary>
        /// <param name="disposing">True if managed resources should be disposed; otherwise, false.</param>
        protected virtual void Dispose(bool disposing)
        {
            //_logger.Debug($"Core.Dispose(bool disposing) this={GetHashCode()} _api={_api?.GetHashCode()} _disposed={_disposed} disposing={disposing}");

            if (!_disposed)
            {
                if (disposing)
                {
                    // Called via myClass.Dispose(). 
                    // OK to use any private object references.
                    // Dispose managed state (managed objects).
                    _mmTimer.Stop();
                    _mmTimer.Dispose();

                    LogManager.Stop();

                    // Destroy devices
                    _inputs.ForEach(d => d.Dispose());
                    _inputs.Clear();
                    _outputs.ForEach(d => d.Dispose());
                    _outputs.Clear();
                }

                // Release unmanaged resources. https://stackoverflow.com/a/4935448
                // Set large fields to null.
                _api?.Dispose();

                _disposed = true;
            }
        }

        /// <summary>
        /// TODO1 Override finalizer only if 'Dispose(bool disposing)' has code to free unmanaged resources
        /// </summary>
        ~Core()
        {
            //_logger.Debug($"Core.~Core() this={GetHashCode()} _api={_api?.GetHashCode()} _disposed={_disposed}");

            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: false);
        }

        /// <summary>
        /// Cleanup.
        /// </summary>
        public void Dispose()
        {
            //_logger.Debug($"Core.Dispose() this={GetHashCode()} _api={_api?.GetHashCode()} _disposed={_disposed}");

            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
        #endregion

        #region Primary workers
        /// <summary>
        /// Load and execute script. Can throw on main thread.
        /// </summary>
        /// <param name="scriptFn">The script file or null to reload current.</param>
        /// <returns></returns>
        public NebStatus LoadScript(string? scriptFn = null)
        {
            NebStatus stat = NebStatus.Ok;

            if (scriptFn is not null)
            {
                // New file.
                _currentScriptFn = scriptFn;
            }
            else if (_currentScriptFn is null)
            {
                throw new InvalidOperationException("Can't reload, no current file");
            }

            // Create script api. Clean up old first.
            _api?.Dispose();
            _api = new(_luaPath);

            _logger.Info($"Loading script file {_currentScriptFn}");

            stat = _api.OpenScript(_currentScriptFn);
            if (stat != NebStatus.Ok)
            {
                throw new ApiException("Api open script failed", _api.Error);
            }

            // Get info about the script.
            State.Instance.InitSectionInfo(_api.SectionInfo);

            State.Instance.ExecState = ExecState.Idle;

            // Start timer.
            SetTimer(State.Instance.Tempo);
            _mmTimer.Start();

            return stat;
        }

        /// <summary>
        /// Input from internal non-midi device.
        /// </summary>
        public void InjectReceiveEvent(string devName, int channel, int noteNum, int velocity)
        {
            var input = _inputs.FirstOrDefault(o => o.DeviceName == devName) ??
                throw new ScriptSyntaxException($"Invalid internal device:{devName}");
            NoteEvent nevt = velocity > 0 ?
                new NoteOnEvent(0, channel, noteNum, velocity, 0) :
                new NoteEvent(0, channel, MidiCommandCode.NoteOff, noteNum, 0);

            Midi_ReceiveEvent(input, nevt);
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
                // Do script. TODO Handle solo and/or mute like nebulator.

                //_tan?.Arm();

                NebStatus stat = _api!.Step(State.Instance.CurrentTick);
                if (stat != NebStatus.Ok)
                {
                   CallbackError(new ApiException("Step() failed", _api.Error));
                }

                // Read stopwatch and diff/stats.
                //string? s = _tan?.Dump();

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
            int chan_hnd = ChannelHandle.MakeInHandle(index, e.Channel);
            bool logit = true;

            switch (e)
            {
                case NoteOnEvent evt:
                    stat = _api!.RcvNote(chan_hnd, evt.NoteNumber, (double)evt.Velocity / Defs.MIDI_VAL_MAX);
                    break;

                case NoteEvent evt:
                    stat = _api!.RcvNote(chan_hnd, evt.NoteNumber, 0);
                    break;

                case ControlChangeEvent evt:
                    stat = _api!.RcvController(chan_hnd, (int)evt.Controller, evt.ControllerValue);
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
               CallbackError(new ApiException("Midi Receive() failed", _api!.Error));
            }
        }
        #endregion

        #region Script Event Handlers
        /// <summary>
        /// Script wants to define a channel. Can throw.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void Interop_CreateChannel(object? sender, CreateChannelArgs e)
        {
            e.Ret = 0; // chan_hnd default means invalid

            // Check args.
            if (e.DevName is null || e.DevName.Length == 0 || e.ChanNum < 1 || e.ChanNum > Defs.NUM_MIDI_CHANNELS)
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
                e.Ret = ChannelHandle.MakeOutHandle(_outputs.Count - 1, e.ChanNum);

                // Send the patch now.
                PatchChangeEvent pevt = new(0, e.ChanNum, e.Patch);
                output.Send(pevt);
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
                e.Ret = ChannelHandle.MakeInHandle(_inputs.Count - 1, e.ChanNum);
            }
        }

        /// <summary>
        /// Sending some midi fro script. Can throw.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void Interop_Send(object? _, SendArgs e)
        {
            e.Ret = 0; // not used

            // Check args.
            var (index, chan_num) = ChannelHandle.DeconstructHandle(e.ChanHnd);
            if (index >= _outputs.Count || chan_num < 1 || chan_num > Defs.NUM_MIDI_CHANNELS ||
                !_outputs[index].Channels[chan_num - 1])
            {
                throw new ScriptSyntaxException($"Script has invalid channel: {e.ChanHnd}");
            }
            if (e.What < 0 || e.What >= Defs.MIDI_VAL_MAX || e.Value < 0 || e.Value >= Defs.MIDI_VAL_MAX)
            {
                throw new ScriptSyntaxException($"Script has invalid payload: {e.What} {e.Value}");
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
        /// Set timer for this tempo.
        /// </summary>
        /// <param name="tempo"></param>
        void SetTimer(int tempo)
        {
            if (tempo > 0)
            {
                double sec_per_beat = 60.0 / tempo;
                double msec_per_sub = 1000 * sec_per_beat / Defs.SUBS_PER_BEAT;
                double period = msec_per_sub > 1.0 ? msec_per_sub : 1;
                _mmTimer.SetTimer((int)Math.Round(period, 2), MmTimer_Callback);
            }
            else // stop
            {
                _mmTimer.SetTimer(0, MmTimer_Callback);
            }
        }

        /// <summary>
        /// General purpose handler for errors in callback functions because they can't throw exceptions.
        /// </summary>
        /// <param name="ex">The exception</param>
        void CallbackError(Exception e)
        {
            string serr = e switch
            {
                ApiException ex => $"Api Error: {ex.Message}:{Environment.NewLine}{ex.ApiError}",
                ScriptSyntaxException ex => $"Script Syntax Error: {ex.Message}",
                ApplicationArgumentException ex => $"Application Argument Error: {ex.Message}",
                _ => $"Other error: {e}{Environment.NewLine}{e.StackTrace}",
            };

            // Client can decide what to do with this. They may be recoverable so use warn.
            _logger.Warn(serr);
            // _logger.Error(serr);
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
            (int index, int chan_num) = ChannelHandle.DeconstructHandle(chan_hnd);
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
        #endregion
    }
}
