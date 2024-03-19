using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using NAudio.Midi;
using Ephemera.NBagOfTricks;
using Ephemera.NBagOfTricks.Slog;
using System.Threading;


namespace Nebulua
{
    public partial class App : IDisposable
    {

        // TODO2 Script lua_State access syncronization. 
        // HANDLE ghMutex; 
        // #define ENTER_CRITICAL_SECTION WaitForSingleObject(ghMutex, INFINITE)
        // #define EXIT_CRITICAL_SECTION ReleaseMutex(ghMutex)
        void ENTER_CRITICAL_SECTION() { }
        void EXIT_CRITICAL_SECTION() { }


        // TODO1 better run control: 
        // cli implementation: loop on/off, set to section|all|start/end-bartime.
        // Keep going.
        bool _doLoop = false;
        // Loop start tick. -1 means start of composition.
        int _loopStart = -1;
        // Loop end tick. -1 means end of composition.
        int _loopEnd = -1;
        // #region Enums
        // /// <summary>Internal status.</summary>
        // enum PlayCommand { Start, Stop, Rewind, StopRewind, UpdateUiTime }
        // #endregion


        #region Fields
        /// <summary>App logger.</summary>
        readonly Logger _logger = LogManager.CreateLogger("App");

        /// <summary>The singleton API.</summary>
        readonly Interop.Api _api = Interop.Api.Instance;

        /// <summary>CLI.</summary>
        TextWriter _cliOut;

        /// <summary>CLI.</summary>
        TextReader _cliIn;

        /// <summary>Fast timer.</summary>
        readonly MmTimerEx _mmTimer = new();

        /// <summary>Diagnostics for timing measurement.</summary>
        readonly TimingAnalyzer _tan = new() { SampleSize = 100 };

        /// <summary>All devices to use for send.</summary>
        readonly List< MidiOutput> _outputs = [];

        /// <summary>All devices to use for receive.</summary>
        readonly List<MidiInput> _inputs = [];

        /// <summary>The script execution state.</summary>
        bool _scriptRunning = false;

        /// <summary>The app execution state.</summary>
        bool _appRunning = true;

        /// <summary>Length of composition in ticks.</summary>
        int _length = 0;

        /// <summary>Current tempo in bpm.</summary>
        int _tempo = 100;

        /// <summary>Where are we in composition.</summary>
        int _currentTick = 0;

        /// <summary>Monitor midi input.</summary>
        bool _monInput = false;

        /// <summary>Monitor midi output.</summary>
        bool _monOutput = false;
        private bool disposedValue;
        #endregion

        #region Lifecycle
        /// <summary>
        /// Constructor inits some stuff.
        /// </summary>
        public App()
        {
            _cliOut = Console.Out;
            _cliIn = Console.In;

            // Init logging.
            LogManager.MinLevelFile = LogLevel.Debug;
            LogManager.MinLevelNotif = LogLevel.Warn;
            LogManager.LogMessage += LogManager_LogMessage;
            LogManager.Run("_log.txt", 100000);
            _logger.Info("Log says hihyhhhhhhhhhhhhhhhhhh");

            //// TODO1 Create a mutex with no initial owner.
            //ghMutex = CreateMutex(NULL, FALSE, NULL);
            //if (ghMutex == NULL) { EXEC_FAIL(11, "CreateMutex() failed."); }

            InitCli();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="disposing"></param>
        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    // TO-DO: dispose managed state (managed objects)
                    _mmTimer.Stop();
                    _mmTimer.Dispose();

                    LogManager.Stop();

                    // Destroy devices
                    _inputs.ForEach(d => d.Dispose());
                    _inputs.Clear();
                    _outputs.ForEach(d => d.Dispose());
                    _outputs.Clear();
                }

                // TO-DO: free unmanaged resources (unmanaged objects) and override finalizer
                // TO-DO: set large fields to null
                disposedValue = true;
            }
        }

        /// <summary>
        /// TO-DO: override finalizer only if 'Dispose(bool disposing)' has code to free unmanaged resources
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
        /// 
        /// </summary>
        /// <param name="fn"></param>
        public int Run(string fn)
        {
            int stat = Defs.NEB_OK;
            CliWrite("Greetings from Nebulua!");

            // Lock access to lua context during init.
            ENTER_CRITICAL_SECTION();

            // Create and init script api.
            stat = _api.Init();
            if (stat != Defs.NEB_OK)
            {
                _logger.Error(_api.Error);
            }

            // Hook script events.
            _api.CreateChannelEvent += CreateChannelEventHandler;
            _api.SendEvent += SendEventHandler;
            _api.MiscInternalEvent += MiscInternalEventHandler;

            // Load the script.
            stat = _api.OpenScript(fn);
            if (stat != Defs.NEB_OK)
            {
                _logger.Error(_api.Error);
            }

            _length = _api.SectionInfo.Last().Key;

            // Start timer.
            SetTimer(_tempo);
            _mmTimer.Start();

            ///// Good to go now. /////
            EXIT_CRITICAL_SECTION();

            ///// Loop forever doing cli requests. /////
            while (_appRunning)
            {
                stat = DoCli();
                if (stat != Defs.NEB_OK)
                {
                    _logger.Error(_api.Error);
                    _appRunning = false;
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
        /// Process events -- this is in an interrupt handler!
        /// </summary>
        /// <param name="totalElapsed"></param>
        /// <param name="periodElapsed"></param>
        void MmTimerCallback(double totalElapsed, double periodElapsed)
        {
            if (_scriptRunning)
            {
                // Do script. TODO3 Handle solo/mute like nebulator.
                //_tan.Arm(); TODO2

                // Lock access to lua context.
                ENTER_CRITICAL_SECTION();
                // Read stopwatch.
                int stat = _api.Step(_currentTick);
                EXIT_CRITICAL_SECTION();

                // // Read stopwatch and diff/stats.
                // if (_tan.Grab())
                // {
                //     DumpTan();
                // }

                // Update state.

                if (stat != Defs.NEB_OK)
                {
                    // Stop everything.
                    SetTimer(0);
                    KillAll();
                    _logger.Error(_api.Error);
                    _scriptRunning = false;
                    _currentTick = 0;
                }
                else
                {
                    // Bump time and check state.
                    int start = _loopStart == -1 ? 0 : _loopStart;
                    int end = _loopEnd == -1 ? _length : _loopEnd;
                    if (++_currentTick >= end) // done
                    {
                        // Keep going? else stop/rewind.
                        _scriptRunning = _doLoop;
 
                        if (_doLoop)
                        {
                            // Keep going.
                            _currentTick = start;
                        }
                        else
                        {
                            // Stop and rewind.
                            _currentTick = start;
 
                            // just in case
                            KillAll();
                        }
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
                    CliWrite(e.Message);
                    // Fatal, shut down.
                    _appRunning = false;
                    break;

                case LogLevel.Warn:
                    CliWrite(e.Message);
                    break;

                default:
                    // ignore
                    break;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void InputReceiveEvent(object? sender, MidiEvent e)
        {
            int stat = Defs.NEB_OK;
            int index = _inputs.IndexOf((MidiInput)sender!);
            int chan_hnd = Utils.MAKE_IN_HANDLE(index, e.Channel);

            switch (e)
            {
                case NoteOnEvent evt:
                    ENTER_CRITICAL_SECTION();
                    stat = _api.InputNote(chan_hnd, evt.NoteNumber, (double)evt.Velocity / Defs.MIDI_VAL_MAX);
                    EXIT_CRITICAL_SECTION();
                    break;

                case NoteEvent evt:
                    ENTER_CRITICAL_SECTION();
                    stat = _api.InputNote(chan_hnd, evt.NoteNumber, (double)evt.Velocity / Defs.MIDI_VAL_MAX);
                    EXIT_CRITICAL_SECTION();
                    break;

                case ControlChangeEvent evt:
                    ENTER_CRITICAL_SECTION();
                    stat = _api.InputController(chan_hnd, (int)evt.Controller, evt.ControllerValue);
                    EXIT_CRITICAL_SECTION();
                    break;

                default:
                    // Ignore.
                    break;
            }

            if (stat != Defs.NEB_OK)
            {
                _logger.Error(_api.Error);
            }

            if (_monInput)
            {
                _logger.Trace($"MIDI_IN{e}");
            }
        }
        #endregion

        #region Script Event Handlers
        /// <summary>
        /// Script wants to define a channel.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void CreateChannelEventHandler(object? sender, Interop.CreateChannelEventArgs e)
        {
            e.Ret = 0; // default means invalid chan_hnd

            if (e.DevName is not null && e.DevName.Length != 0 && e.ChanNum >= 1 && e.ChanNum <= Defs.NUM_MIDI_CHANNELS)
            {
                if (e.IsOutput) // TODO2 switch?
                {
                    var output = _outputs.FirstOrDefault(o => o.DeviceName == e.DevName);
                    if (output == null)
                    {
                        output = new(e.DevName);
                        if (output.Valid)
                        {
                            _outputs.Add(output);
                        }
                        else
                        {
                            _logger.Error($"Script has invalid output midi device: {e.DevName}");
                            return; //*** early return
                        }
                    }

                    output.LogEnable = _monOutput;
                    output.Channels[e.ChanNum - 1] = true;
                    e.Ret = Utils.MAKE_OUT_HANDLE(_outputs.Count - 1, e.ChanNum);

                    // Send the patch now.
                    PatchChangeEvent pevt = new(0, e.ChanNum, e.Patch);
                    output.SendEvent(pevt);
                }
                else
                {
                    var input = _inputs.FirstOrDefault(o => o.DeviceName == e.DevName);
                    if (input == null)
                    {
                        input = new(e.DevName);
                        input.InputReceiveEvent += InputReceiveEvent;
                        if (input.Valid)
                        {
                            _inputs.Add(input);
                        }
                        else
                        {
                            _logger.Error($"Script has invalid input midi device: {e.DevName}");
                            return; //*** early return
                        }
                    }

                    input.LogEnable = _monInput;
                    input.Channels[e.ChanNum - 1] = true;
                    e.Ret = Utils.MAKE_IN_HANDLE(_inputs.Count - 1, e.ChanNum);
                }
            }
            else
            {
                _logger.Error($"Script has invalid input midi device: {e.DevName}");
                return; //*** early return
            }
        }

        /// <summary>
        /// Sending some midi.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void SendEventHandler(object? sender, Interop.SendEventArgs e)
        {
            // Dig out the device.
            var (index, chan_num) = Utils.SPLIT_HANDLE(e.ChanHnd);

            if (index < _outputs.Count)
            {
                var output = _outputs[index];

                if (output.Channels[chan_num - 1])
                {
                    if (e.IsNote) // TODO2 switch?
                    {
                        if (e.Value == 0)
                        {
                            var noff = new NoteEvent(0, chan_num, MidiCommandCode.NoteOff, e.What, 0);
                            output.SendEvent(noff);
                        }
                        else
                        {
                            var non = new NoteEvent(0, chan_num, MidiCommandCode.NoteOn, e.What, e.Value);
                            output.SendEvent(non);
                        }
                    }
                    else
                    {
                        var ctlr = new ControlChangeEvent(0, chan_num, (MidiController)e.What, e.Value);
                        output.SendEvent(ctlr);
                    }
                }
                else
                {
                    _logger.Error($"Script has invalid output midi device for channel: {e.ChanHnd}");
                    return; //*** early return
                }
            }
            else
            {
                _logger.Error($"Script has invalid channel: {e.ChanHnd}");
                return; //*** early return
            }

            if (_monOutput)
            {
                _logger.Trace($"MIDI_OUT{e}");
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void MiscInternalEventHandler(object? sender, Interop.MiscInternalEventArgs e)
        {
            if (e.Bpm > 0)
            {
                if (e.Bpm >= 30 && e.Bpm <= 240)
                {
                    _tempo = e.Bpm;
                    SetTimer(_tempo);
                }
                else
                {
                    _logger.Error($"Invalid tempo: {e.Bpm}");
                    return; //*** early return
                }
            }
            else
            {
                _logger.Log((LogLevel)e.LogLevel, $"SCRIPT {e.Msg}");
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
                    o.SendEvent(cevt);
                }
            }

            // Hard reset.
            _scriptRunning = false;
        }

        /// <summary>
        /// Diagnostics.
        /// </summary>
        void DumpTan()
        {
            // Time ordered.
            List<string> ls = [];
            _tan.Times.ForEach(t => ls.Add($"{t}"));
            File.WriteAllLines(@"..\..\out\intervals_ordered.csv", ls);

            // Sorted by (rounded) times.
            Dictionary<double, int> _bins = [];
            for (int i = 0; i < _tan.Times.Count; i++)
            {
                var t = Math.Round(_tan.Times[i], 2);
                _bins[t] = _bins.TryGetValue(t, out int value) ? value + 1 : 1;
            }
            ls.Clear();
            ls.Add($"Msec,Count");
            var vv = _bins.Keys.ToList();
            vv.Sort();
            vv.ForEach(v => ls.Add($"{v},{_bins[v]}"));
            File.WriteAllLines(@"..\..\out\intervals_sorted.csv", ls);
        }
        #endregion
    }
}
