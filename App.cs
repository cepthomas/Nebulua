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
        readonly List<string> _lpath;

        /// <summary>Talk to the user.</summary>
        readonly Cli _cli;

        /// <summary>Fast timer.</summary>
        readonly MmTimerEx _mmTimer = new();

        /// <summary>Diagnostics for timing measurement.</summary>
        readonly TimingAnalyzer? _tan = null;

        /// <summary>All devices to use for send.</summary>
        readonly List<MidiOutput> _outputs = [];

        /// <summary>All devices to use for receive.</summary>
        readonly List<MidiInput> _inputs = [];

        /// <summary>Resource management.</summary>
        bool _disposed;
        #endregion

        #region Lifecycle
        /// <summary>
        /// Constructor inits some stuff.
        /// </summary>
        /// <param name="lpath">LUA_PATH components.</param>
        public App(List<string> lpath)
        {
            _lpath = lpath;

            // Init logging. TODO1 need to set from config or cmd line.
            LogManager.MinLevelFile = LogLevel.Debug;
            LogManager.MinLevelNotif = LogLevel.Warn;
            LogManager.LogMessage += LogManager_LogMessage;
            LogManager.Run("_log.txt", 100000);

            _cli = new(Console.In, Console.Out, "->");
            _cli.Write("Greetings from Nebulua!");

            // Hook script events.
            Api.CreateChannel += Interop_CreateChannel;
            Api.Send += Interop_Send;
            Api.Log += Interop_Log;
            Api.PropertyChange += Interop_PropertyChange;

            State.Instance.PropertyChangeEvent += State_PropertyChangeEvent;
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
                    // Template said: dispose managed state (managed objects)
                    _mmTimer.Stop();
                    _mmTimer.Dispose();

                    LogManager.Stop();

                    // Destroy devices
                    _inputs.ForEach(d => d.Dispose());
                    _inputs.Clear();
                    _outputs.ForEach(d => d.Dispose());
                    _outputs.Clear();
                }

                // Template said: free unmanaged resources (unmanaged objects) and override finalizer
                // Template said: set large fields to null
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
        /// <param name="fn"></param>
        public NebStatus Run(string fn)
        {
            NebStatus stat;

            // Create script api.
            Api api = new(_lpath);
            _api.Add(api.Id, api);

            // Load the script.
            stat = api.OpenScript(fn);
            if (stat != NebStatus.Ok)
            {
                FatalError(stat, "Interop OpenScript() failed", api.Error);
            }

            State.Instance.Length = api.SectionInfo.Last().Key;

            // Start timer.
            SetTimer(State.Instance.Tempo);
            _mmTimer.Start();

            ///// Good to go now. Loop forever doing cli requests. /////
            while (State.Instance.ExecState != ExecState.Exit)
            {
                stat = _cli.Read();
                if (stat != NebStatus.Ok)
                {
                    FatalError(stat, "Cli Read() failed", api.Error);
                }
            }

            ///// Done. /////

            // Wait a bit in case there are some lingering events.
            Thread.Sleep(100);

            // Just in case.
            KillAll();

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
        /// Process events -- this is in an interrupt handler!
        /// </summary>
        /// <param name="totalElapsed"></param>
        /// <param name="periodElapsed"></param>
        void MmTimerCallback(double totalElapsed, double periodElapsed)
        {
            if (State.Instance.ExecState == ExecState.Run)
            {
                // Do script. TODOF Handle solo/mute like nebulator.
                _tan?.Arm();

                foreach (var api in _api.Values)
                {
                    NebStatus stat = api.Step(State.Instance.CurrentTick);
                    if (stat != NebStatus.Ok)
                    {
                        FatalError(stat, "Api Step() failed", api.Error);
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
                        State.Instance.ExecState = ExecState.Idle;
                        State.Instance.CurrentTick = start;

                        // just in case
                        KillAll();
                    }
                }
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
        /// Midi input arrived.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void Midi_ReceiveEvent(object? sender, MidiEvent e)
        {
            NebStatus stat = NebStatus.Ok;
            int index = _inputs.IndexOf((MidiInput)sender!);
            int chan_hnd = Utils.MakeInHandle(index, e.Channel);

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
                    FatalError(stat, "Api Midi Receive() failed", api.Error);
                }

                if (State.Instance.MonRcv)
                {
                    _logger.Trace($"MIDI_RCV {e}");
                }
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
            e.Ret = 0; // default means invalid chan_hnd

            // Get Api.
            //Api api = _api[e.Id];

            try
            {
                // Check args.
                if (e.DevName is null || e.DevName.Length == 0 || e.ChanNum < 1 || e.ChanNum > Defs.NUM_MIDI_CHANNELS)
                {
                    throw new ArgumentException($"Script has invalid input midi device: {e.DevName}");
                }

                if (e.IsOutput)
                {
                    var output = _outputs.FirstOrDefault(o => o.DeviceName == e.DevName);
                    if (output == null)
                    {
                        output = new(e.DevName); //throws
                        _outputs.Add(output);
                    }

                    output.LogEnable = State.Instance.MonSend;
                    output.Channels[e.ChanNum - 1] = true;
                    e.Ret = Utils.MakeOutHandle(_outputs.Count - 1, e.ChanNum);

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

                    input.LogEnable = State.Instance.MonRcv;
                    input.Channels[e.ChanNum - 1] = true;
                    e.Ret = Utils.MakeInHandle(_inputs.Count - 1, e.ChanNum);
                }
            }
            catch (Exception ex)
            {
                e.Ret = 0;
                FatalError(NebStatus.SyntaxError, "Script CreateChannel() failed", ex.Message);
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
                var (index, chan_num) = Utils.DeconstructHandle(e.ChanHnd);
                if (index >= _outputs.Count || chan_num < 1 || chan_num > Defs.NUM_MIDI_CHANNELS ||
                    !_outputs[index].Channels[chan_num - 1])
                {
                    throw new ArgumentException($"Script has invalid channel: {e.ChanHnd}");
                }
                if (e.What < 0 || e.What >= Defs.MIDI_VAL_MAX || e.Value < 0 || e.Value >= Defs.MIDI_VAL_MAX)
                {
                    throw new ArgumentException($"Script has invalid payload: {e.What} {e.Value}");
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
                
                if (State.Instance.MonSend)
                {
                    _logger.Trace($"MIDI_SND {e}");
                }
            }
            catch (Exception ex)
            {
                e.Ret = 0;
                FatalError(NebStatus.SyntaxError, "Script Send() failed", ex.Message);
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
                }
                else
                {
                    FatalError(NebStatus.SyntaxError, $"Script Invalid tempo: {e.Bpm}");
                }
            }
            else
            {
                SetTimer(0);
            }
            e.Ret = 0;
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
                _mmTimer.SetTimer((int)Math.Round(period, 2), MmTimerCallback);
            }
            else // stop
            {
                _mmTimer.SetTimer(0, MmTimerCallback);
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
        /// <param name="stat"></param>
        /// <param name="error1"></param>
        /// <param name="error2"></param>
        void FatalError(NebStatus stat, string error1, string? error2 = null)
        {
            string s2 = string.IsNullOrEmpty(error2) ? "" : $"{Environment.NewLine}    {error2}";
            string s = $"Fatal error {stat}: {error1}{s2}";
            _logger.Error(s);

            // Stop everything.
            SetTimer(0);
            State.Instance.CurrentTick = 0;
            KillAll();

            // Flush log.
            Thread.Sleep(200);

            Environment.Exit((int)stat);
        }
        #endregion
    }
}
