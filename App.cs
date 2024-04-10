using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Threading;
using NAudio.Midi;
using Ephemera.NBagOfTricks;
using Ephemera.NBagOfTricks.Slog;
using Interop;


namespace Nebulua
{
    public partial class App : IDisposable
    {
        #region Fields
        /// <summary>App logger.</summary>
        readonly Logger _logger = LogManager.CreateLogger("App");

        /// <summary>The API(s). Key is opaque lua context pointer.</summary>
        readonly Dictionary<long, Api> _api = [];

        /// <summary>Client supplied context for LUA_PATH.</summary>
        readonly List<string> _lpath = [];

        /// <summary>Talk to the user.</summary>
        readonly Cli _cli;

        /// <summary>The config contents.</summary>
        readonly Config? _config;

        /// <summary>Fast timer.</summary>
        readonly MmTimerEx _mmTimer = new();

        /// <summary>Diagnostics for timing measurement.</summary>
        readonly TimingAnalyzer? _tan = null;

        /// <summary>All devices to use for send.</summary>
        readonly List<MidiOutput> _outputs = [];

        /// <summary>All devices to use for receive.</summary>
        readonly List<MidiInput> _inputs = [];

        /// <summary>Current script.</summary>
        readonly string _scriptFn = "";

        /// <summary>Resource management.</summary>
        bool _disposed = false;
        #endregion

        #region Lifecycle
        /// <summary>
        /// Constructor inits stuff.
        /// </summary>
        public App()
        {
            _cli = new(Console.In, Console.Out);
            _cli.Write("Greetings from Nebulua!");

            try
            {
                // Process cmd line args.
                string? configFn = null;
                var args = StringUtils.SplitByToken(Environment.CommandLine, " ");
                args.RemoveAt(0); // remove the binary

                foreach (var arg in args)
                {
                    if (arg.EndsWith(".ini"))
                    {
                        configFn = arg;
                    }
                    else if (arg.EndsWith(".lua"))
                    {
                        _scriptFn = arg;
                    }
                    else
                    {
                        throw new ApplicationArgumentException($"Invalid command line: {arg}");
                    }
                }
                if (_scriptFn is null)
                {
                    throw new ApplicationArgumentException($"Missing nebulua script file");
                }

                _config = new(configFn);

                // Init logging.
                LogManager.MinLevelFile = LogLevel.Debug;
                LogManager.MinLevelNotif = LogLevel.Warn;
                LogManager.LogMessage += LogManager_LogMessage;
                var f = File.OpenWrite(_config.LogFilename); // ensure file exists
                f?.Close();
                LogManager.Run(_config.LogFilename, 100000);

                // Set up runtime lua environment.
                var exePath = Environment.CurrentDirectory; // where exe lives
                _lpath.Add($@"{exePath}\lua_code"); // app lua files
                _lpath.Add($@"{exePath}\lbot"); // lbot files

                // Hook script callbacks.
                Api.CreateChannel += Interop_CreateChannel;
                Api.Send += Interop_Send;
                Api.Log += Interop_Log;
                Api.PropertyChange += Interop_PropertyChange;

                State.Instance.PropertyChangeEvent += State_PropertyChangeEvent;
            }
            // Anything that throws is fatal.
            catch (Exception ex)
            {
                FatalError(ex, "App constructor failed.");
            }
        }

        /// <summary>
        /// Clean up.
        /// </summary>
        /// <param name="disposing"></param>
        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    // Template said: dispose managed state (managed objects).
                    _mmTimer.Stop();
                    _mmTimer.Dispose();

                    LogManager.Stop();

                    // Destroy devices
                    _inputs.ForEach(d => d.Dispose());
                    _inputs.Clear();
                    _outputs.ForEach(d => d.Dispose());
                    _outputs.Clear();
                }

                // Template said: free unmanaged resources (unmanaged objects) and override finalizer.
                // Template said: set large fields to null.
                _disposed = true;
            }
        }

        /// <summary>
        /// Template said: override finalizer only if 'Dispose(bool disposing)' has code to free unmanaged resources
        /// </summary>
        ~App()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: false);
        }

        /// <summary>
        /// 
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
        /// Load and execute script.
        /// </summary>
        public NebStatus Run()
        {
            NebStatus stat = NebStatus.Ok;

            try
            {
                // Create script api.
                Api api = new(_lpath);
                _api.Add(api.Id, api);

                // Load the script.
                stat = api.OpenScript(_scriptFn);
                if (stat != NebStatus.Ok)
                {
                    throw new ApiException("Api OpenScript() failed", api.Error);
                }

                State.Instance.Length = api.SectionInfo.Last().Key;

                // Start timer.
                SetTimer(State.Instance.Tempo);
                _mmTimer.Start();

                ///// Good to go now. Loop forever doing cli requests. /////

                while (State.Instance.ExecState != ExecState.Exit)
                {
                    bool cliok = _cli.Read();
                    // Should not throw. Cli will take care of its own errors.
                    if (!cliok)
                    {
                        ////Fatal Error(stat, "Cli Read() failed", api.Error);
                        //throw new("Cli Read() failed", api.Error);
                    }
                }

                ///// Normal done. /////

                _cli.Write("shutting down");

                // Wait a bit in case there are some lingering events.
                Thread.Sleep(100);

                // Just in case.
                KillAll();
            }
            catch (Exception ex)
            {
                FatalError(ex, "App Run() failed");
            }

            return stat;
        }

        /// <summary>
        /// Handler for state changes. Some may originate in this component, others from elsewhere.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void State_PropertyChangeEvent(object? sender, string name)
        {
            switch (name)
            {
                case "CurrentTick":
                    if (sender != this) { }
                    break;

                case "Tempo":
                    SetTimer(State.Instance.Tempo);
                    break;

                case "ScriptState":
                    switch (State.Instance.ExecState)
                    {
                        case ExecState.Idle:
                        case ExecState.Run:
                        case ExecState.Exit:
                            break;

                        case ExecState.Kill:
                            KillAll();
                            State.Instance.ExecState = ExecState.Idle;
                            break;
                    }
                    break;
            }
        }

        /// <summary>
        /// Process events. This is in an interrupt handler so can't throw exceptions.
        /// </summary>
        /// <param name="totalElapsed"></param>
        /// <param name="periodElapsed"></param>
        void MmTimer_Callback(double totalElapsed, double periodElapsed)
        {
            try
            {
                if (State.Instance.ExecState == ExecState.Run)
                {
                    // Do script. TODO Handle solo/mute like nebulator.
                    _tan?.Arm();

                    foreach (var api in _api.Values)
                    {
                        NebStatus stat = api.Step(State.Instance.CurrentTick);
                        if (stat != NebStatus.Ok)
                        {
                            //Fatal Error(stat, "Api Step() failed", api.Error);
                            throw new ApiException("Api Step() failed", api.Error);
                        }
                    }

                    // Read stopwatch and diff/stats.
                    string? s = _tan?.Dump();

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
                            _cli.Write("done");
                            State.Instance.ExecState = ExecState.Idle;
                            State.Instance.CurrentTick = start;

                            // just in case
                            KillAll();
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                FatalError(ex, "Api Step() failed");
            }
        }

        /// <summary>
        /// Capture bad events and display them to the user. If error shut down.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void LogManager_LogMessage(object? sender, LogMessageEventArgs e)
        {
            switch (e.Level)
            {
                case LogLevel.Error:
                    _cli.Write(e.Message);
                    // Fatal, shut down.
                    State.Instance.ExecState = ExecState.Exit;
                    break;

                case LogLevel.Warn:
                    _cli.Write(e.Message);
                    break;

                default:
                    // ignore
                    break;
            }
        }

        /// <summary>
        /// Midi input arrived. This is in an interrupt handler so can't throw exceptions.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void Midi_ReceiveEvent(object? sender, MidiEvent e)
        {
            NebStatus stat = NebStatus.Ok;

            try
            {
                int index = _inputs.IndexOf((MidiInput)sender!);
                int chan_hnd = ChannelHandle.MakeInHandle(index, e.Channel);

                foreach (var api in _api.Values)
                {
                    switch (e)
                    {
                        case NoteOnEvent evt:
                            stat = api.RcvNote(chan_hnd, evt.NoteNumber, (double)evt.Velocity / Defs.MIDI_VAL_MAX);
                            break;

                        case NoteEvent evt:
                            stat = api.RcvNote(chan_hnd, evt.NoteNumber, 0);
                            break;

                        case ControlChangeEvent evt:
                            stat = api.RcvController(chan_hnd, (int)evt.Controller, evt.ControllerValue);
                            break;

                        default: // Ignore.
                            break;
                    }

                    if (stat != NebStatus.Ok)
                    {
                        throw new ApiException("Midi Receive() failed", api.Error);
                    }

                    if (State.Instance.MonRx)
                    {
                        _logger.Trace($"MIDIRX {e}");
                    }
                }
            }
            catch (Exception ex)
            {
                FatalError(ex, "Midi Receive() failed");
            }
        }
        #endregion

        #region Script Event Handlers
        /// <summary>
        /// Script wants to define a channel.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void Interop_CreateChannel(object? sender, CreateChannelArgs e)
        {
            e.Ret = 0; // chan_hnd default means invalid

            // Get Api.
            //Api api = _api[e.Id];

            try
            {
                // Check args.
                if (e.DevName is null || e.DevName.Length == 0 || e.ChanNum < 1 || e.ChanNum > Defs.NUM_MIDI_CHANNELS)
                {
                    throw new SyntaxException($"Script has invalid input midi device: {e.DevName}");
                }

                if (e.IsOutput)
                {
                    var output = _outputs.FirstOrDefault(o => o.DeviceName == e.DevName);
                    if (output == null)
                    {
                        output = new(e.DevName); //throws
                        _outputs.Add(output);
                    }

                    //output.LogEnable = State.Instance.MonSend;
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

                    //input.LogEnable = State.Instance.MonRcv;
                    input.Channels[e.ChanNum - 1] = true;
                    e.Ret = ChannelHandle.MakeInHandle(_inputs.Count - 1, e.ChanNum);
                }
            }
            catch (Exception ex)  // Any exception is considered fatal.
            {
                e.Ret = 0;
                FatalError(ex, "CreateChannel() failed");
            }
        }

        /// <summary>
        /// Sending some midi.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void Interop_Send(object? sender, SendArgs e)
        {
            e.Ret = 0; // not used

            // Get Api.
            //Api api = _api[e.Id];

            try
            {
                // Check args.
                var (index, chan_num) = ChannelHandle.DeconstructHandle(e.ChanHnd);
                if (index >= _outputs.Count || chan_num < 1 || chan_num > Defs.NUM_MIDI_CHANNELS ||
                    !_outputs[index].Channels[chan_num - 1])
                {
                    throw new SyntaxException($"Script has invalid channel: {e.ChanHnd}");
                }
                if (e.What < 0 || e.What >= Defs.MIDI_VAL_MAX || e.Value < 0 || e.Value >= Defs.MIDI_VAL_MAX)
                {
                    throw new SyntaxException($"Script has invalid payload: {e.What} {e.Value}");
                }

                var output = _outputs[index];

                if (e.IsNote)
                {
                    output.Send(e.Value == 0 ?
                        new NoteEvent(0, chan_num, MidiCommandCode.NoteOff, e.What, 0) :
                        new NoteEvent(0, chan_num, MidiCommandCode.NoteOn, e.What, e.Value));
                }
                else
                {
                    output.Send(new ControlChangeEvent(0, chan_num, (MidiController)e.What, e.Value));
                }

                if (State.Instance.MonTx)
                {
                    _logger.Trace($"MIDITX {e}");
                }
            }
            catch (Exception ex)
            {
                e.Ret = 0;
                FatalError(ex, "Send() failed");
            }
        }

        /// <summary>
        /// Log something from script.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void Interop_Log(object? sender, LogArgs e)
        {
            _logger.Log((LogLevel)e.LogLevel, $"SCRIPT {e.Msg}");
            e.Ret = 0;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void Interop_PropertyChange(object? sender, PropertyArgs e)
        {
            // Get Api.
            //Api api = _api[e.Id];

            if (e.Bpm > 0)
            {
                if (e.Bpm >= 30 && e.Bpm <= 240)
                {
                    State.Instance.Tempo = e.Bpm;
                    SetTimer(State.Instance.Tempo);
                    e.Ret = 0;
                }
                else
                {
                    _cli.Write($"Invalid tempo: {e.Bpm}");
                    e.Ret = 1;
                }
            }
            else
            {
                SetTimer(0);
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
        /// Stop all midi.
        /// </summary>
        void KillAll()
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
        /// General purpose handler for fatal errors. Causes app exit.
        /// </summary>
        /// <param name="ex">The exception</param>
        /// <param name="info">Extra info</param>
        void FatalError(Exception e, string info)
        {
            string serr;

            switch (e)
            {
                case ApiException ex:
                    serr = $"ApiException: {ex.Message}{Environment.NewLine}ApiError: {ex.ApiError}";
                    break;

                case ApplicationArgumentException ex:
                    serr = $"ApplicationArgumentException: {ex.Message}";
                    break;

                case ConfigException ex:
                    serr = $"ConfigException: {ex.Message}";
                    break;

                case SyntaxException ex:
                    serr = $"SyntaxException: {ex.Message}";
                    break;

                default:
                    serr = $"Other exception: {e}{Environment.NewLine}{e.StackTrace}";
                    break;
            }

            _logger.Error(serr);

            // Stop everything.
            SetTimer(0);
            State.Instance.CurrentTick = 0;
            KillAll();

            // Flush log.
            Thread.Sleep(200);

            Environment.Exit(1);
        }
        #endregion
    }
}
