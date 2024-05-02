using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Threading;
using NAudio.Midi;
using Ephemera.NBagOfTricks;
using Ephemera.NBagOfTricks.Slog;
using Nebulua.Interop;


namespace Nebulua.Common
{
    public class Core : IDisposable
    {
        #region Fields
        /// <summary>App logger.</summary>
        readonly Logger _logger = LogManager.CreateLogger("Core");

        /// <summary>The interop API. TODO support multiple?</summary>
        Api? _api;

        /// <summary>Client supplied context for LUA_PATH.</summary>
        readonly List<string> _luaPath = [];

        /// <summary>The config contents.</summary>
        readonly Config _config;

        /// <summary>Fast timer.</summary>
        readonly MmTimerEx _mmTimer = new();

        ///// <summary>Diagnostics for timing measurement.</summary>
        //readonly TimingAnalyzer? _tan = null;

        /// <summary>All devices to use for send.</summary>
        readonly List<MidiOutput> _outputs = [];

        /// <summary>All devices to use for receive.</summary>
        readonly List<MidiInput> _inputs = [];

        /// <summary>Current script.</summary>
        string? _scriptFn = null;

        /// <summary>Resource management.</summary>
        bool _disposed = false;
        #endregion

        #region Lifecycle
        /// <summary>
        /// Constructor inits stuff. Can throw on main thread.
        /// </summary>
        /// <param name="configFn">Config to use.</param>
        public Core(string? configFn)
        {
            _config = new(configFn);

            // Init logging.
            LogManager.MinLevelFile = _config.FileLevel;
            LogManager.MinLevelNotif = _config.NotifLevel;
            var f = File.OpenWrite(_config.LogFilename); // ensure file exists
            f?.Close();
            LogManager.Run(_config.LogFilename, 100000);

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
        /// Override finalizer only if 'Dispose(bool disposing)' has code to free unmanaged resources
        /// </summary>
        ~Core()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: false);
        }

        /// <summary>
        /// Cleanup.
        /// </summary>
        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
        #endregion

        #region Primary workers
        /// <summary>
        /// Load and execute script. Can throw on main thread.
        /// </summary>
        public NebStatus RunScript(string scriptFn)
        {
            NebStatus stat = NebStatus.Ok;
            _scriptFn = scriptFn;

            OpenScript(_scriptFn);

            // Start timer.
            SetTimer(State.Instance.Tempo);
            _mmTimer.Start();

            return stat;
        }

        /// <summary>
        /// Reload the externally modified script.
        /// </summary>
        public void Reload()
        {
            _api?.Dispose();
            _api = null;
            OpenScript(_scriptFn);
        }

        /// <summary>
        /// Handler for state changes of interest. Doesn't throw.
        /// Responsible for core stuff like tempo, kill.
        /// </summary>
        /// <param name="_"></param>
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

                case "ExecState":
                    switch (State.Instance.ExecState)
                    {
                        case ExecState.Kill:
                            KillAll();
                            State.Instance.ExecState = ExecState.Idle;
                            break;

                        case ExecState.Reload:
                            Reload();
                            State.Instance.ExecState = ExecState.Idle;
                            break;
                    }
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
                // Do script. TODO Handle solo/mute like nebulator.
                //_tan?.Arm();

                NebStatus stat = _api!.Step(State.Instance.CurrentTick);
                if (stat != NebStatus.Ok)
                {
                   CallbackError(new ApiException("Step() failed", _api.Error));
                }

                // Read stopwatch and diff/stats.
                //string? s = _tan?.Dump();

                // Update state.
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
                        // _cmdProc.Write("done"); // custom handle with state change
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
                    stat = _api!.RcvNote(chan_hnd, evt.NoteNumber, (double)evt.Velocity / MidiDefs.MIDI_VAL_MAX);
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

            if (logit && State.Instance.MonRcv)
            {
                _logger.Trace($"RCV {MidiDefs.FormatMidiEvent(e, State.Instance.ExecState == ExecState.Run ? State.Instance.CurrentTick : 0, chan_hnd)}");
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
            if (e.DevName is null || e.DevName.Length == 0 || e.ChanNum < 1 || e.ChanNum > MidiDefs.NUM_MIDI_CHANNELS)
            {
                throw new ScriptSyntaxException($"Script has invalid input midi device: {e.DevName}");
            }

            if (e.IsOutput)
            {
                var output = _outputs.FirstOrDefault(o => o.DeviceName == e.DevName);
                if (output == null)
                {
                    output = new(e.DevName);
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
                var input = _inputs.FirstOrDefault(o => o.DeviceName == e.DevName);
                if (input == null)
                {
                    input = new(e.DevName); // throws
                    input.ReceiveEvent += Midi_ReceiveEvent;
                    _inputs.Add(input);
                }

                input.Channels[e.ChanNum - 1] = true;
                e.Ret = ChannelHandle.MakeInHandle(_inputs.Count - 1, e.ChanNum);
            }
        }

        /// <summary>
        /// Sending some midi. Can throw.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void Interop_Send(object? sender, SendArgs e)
        {
            e.Ret = 0; // not used

            // Check args.
            var (index, chan_num) = ChannelHandle.DeconstructHandle(e.ChanHnd);
            if (index >= _outputs.Count || chan_num < 1 || chan_num > MidiDefs.NUM_MIDI_CHANNELS ||
                !_outputs[index].Channels[chan_num - 1])
            {
                throw new ScriptSyntaxException($"Script has invalid channel: {e.ChanHnd}");
            }
            if (e.What < 0 || e.What >= MidiDefs.MIDI_VAL_MAX || e.Value < 0 || e.Value >= MidiDefs.MIDI_VAL_MAX)
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

            if (State.Instance.MonSnd)
            {
                _logger.Trace($"SND {MidiDefs.FormatMidiEvent(evt, State.Instance.ExecState == ExecState.Run ? State.Instance.CurrentTick : 0, e.ChanHnd)}");
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
                _logger.Log((LogLevel)e.LogLevel, $"SCR {e.Msg}");
                e.Ret = 0;
            }
            else
            {
                CallbackError(new ScriptSyntaxException("Invalid log level: {e.LogLevel}"));
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
        /// Open the specified script.
        /// </summary>
        /// <param name="scriptFn"></param>
        /// <exception cref="ApiException"></exception>
        void OpenScript(string scriptFn)
        {
            // Create script api.
            _api = new(_luaPath);

            // Load the script.
            var s = $"Loading script file {scriptFn}";
            _logger.Info(s);

            NebStatus stat = _api.OpenScript(scriptFn);
            if (stat != NebStatus.Ok)
            {
                throw new ApiException("OpenScript() failed", _api.Error);
            }

            // Fix the section info.
            State.Instance.InitSectionInfo(_api.SectionInfo);
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
        /// Stop all midi.
        /// </summary>
        void KillAll()
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
        /// General purpose handler for errors in callback functions ecause they can't throw exceptions.
        /// </summary>
        /// <param name="ex">The exception</param>
        void CallbackError(Exception e)
        {
            string serr = e switch
            {
                ApiException ex => $"Api Error: {ex.Message}:{Environment.NewLine}{ex.ApiError}",
                ConfigException ex => $"Config File Error: {ex.Message}",
                ScriptSyntaxException ex => $"Script Syntax Error: {ex.Message}",
                ApplicationArgumentException ex => $"Application Argument Error: {ex.Message}",
                _ => $"Other error: {e}{Environment.NewLine}{e.StackTrace}",
            };

            // Client can decide what to do with this.
            _logger.Error(serr);
        }
        #endregion
    }
}
