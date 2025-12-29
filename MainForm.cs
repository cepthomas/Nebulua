using System;
using System.Drawing;
using System.Windows.Forms;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Reflection;
using System.Runtime.CompilerServices;
using Ephemera.NBagOfTricks;
using Ephemera.NBagOfUis;
using Ephemera.MidiLibLite;
using Ephemera.MusicLib;


// TODO kinda slow startup running in debugger.

// Is patch an instrument or drum? TODO1

namespace Nebulua
{
    public partial class MainForm : Form
    {
        #region Fields
        /// <summary>App logger.</summary>
        readonly Logger _loggerApp = LogManager.CreateLogger("APP");

        /// <summary>Script logger.</summary>
        readonly Logger _loggerScr = LogManager.CreateLogger("SCR");

        /// <summary>Midi traffic logger.</summary>
        readonly Logger _loggerMidi = LogManager.CreateLogger("MID");

        /// <summary>The current settings.</summary>
        UserSettings _settings = new();

        /// <summary>The midi boss.</summary>
        readonly Manager _mgr = new();

        /// <summary>The interop.</summary>
        readonly Interop _interop = new();

        /// <summary>Interop serializing access.</summary>
        readonly object _interopLock = new();

        /// <summary>Fast timer.</summary>
        readonly MmTimerEx _mmTimer = new();

        /// <summary>Current script. Null means none.</summary>
        string? _scriptFn = null;

        /// <summary>Test for edited.</summary>
        DateTime _scriptTouch;

        /// <summary>All midi devices to use for send.</summary>
        readonly List<MidiOutputDevice> _outputDevices = [];

        /// <summary>All midi devices to use for receive. Includes any internal types.</summary>
        readonly List<MidiInputDevice> _inputDevices = [];

        /// <summary>All the channel play controls.</summary>
        readonly List<ChannelControl> _channelControls = [];

        /// <summary>Debugging.</summary>
        readonly TimeIt _tmit = new();

        /// <summary>Performance.</summary>
        //readonly TimingAnalyzer? _tan = null;
        #endregion

        #region State
        /// <summary>Internal state. </summary>
        enum ExecState
        {
            /// <summary>No script loaded.</summary>
            Idle,
            /// <summary>Script loaded, not running.</summary>
            Stop,
            /// <summary>Script loaded, running.</summary>
            Run,
            /// <summary>Fatal error, not running.</summary>
            Dead
        }
        ExecState _execState = ExecState.Idle;

        ExecState CurrentState
        {
            get { return _execState; }
            set { if (value != _execState) { UpdateState(value); } }
        }
        #endregion

        #region Lifecycle
        /// <summary>
        /// Constructor inits most of the UI.
        /// </summary>
        public MainForm()
        {
            _tmit.Snap("MainForm() enter");

            SetStyle(ControlStyles.OptimizedDoubleBuffer | ControlStyles.SupportsTransparentBackColor, true);
            InitializeComponent();

            Icon = Icon.ExtractAssociatedIcon(Assembly.GetExecutingAssembly().Location);

            KeyPreview = true; // for routing kbd strokes properly

            // Settings.
            string appDir = MiscUtils.GetAppDataDir("Nebulua", "Ephemera");
            _settings = (UserSettings)SettingsCore.Load(appDir, typeof(UserSettings));
            LogManager.LogMessage += LogManager_LogMessage;
            LogManager.MinLevelFile = _settings.FileLogLevel;
            LogManager.MinLevelNotif = _settings.NotifLogLevel;
            LogManager.Run(Path.Combine(appDir, "log.txt"), 50000);

            // Main window.
            Location = _settings.FormGeometry.Location;
            Size = _settings.FormGeometry.Size;
            WindowState = FormWindowState.Normal;
            Text = $"Nebulua {MiscUtils.GetVersionString()} - No script loaded";

            #region Init the controls
            GraphicsUtils.ColorizeControl(chkPlay, _settings.IconColor);
            chkPlay.BackColor = BackColor;
            chkPlay.FlatAppearance.CheckedBackColor = _settings.SelectedColor;
            chkPlay.Click += Play_Click;

            GraphicsUtils.ColorizeControl(chkLoop, _settings.IconColor);
            chkLoop.BackColor = BackColor;
            chkLoop.FlatAppearance.CheckedBackColor = _settings.SelectedColor;
            chkLoop.Click += (_, __) => timeBar.DoLoop = chkLoop.Checked;

            chkMonRcv.BackColor = BackColor;
            GraphicsUtils.ColorizeControl(chkMonRcv, _settings.IconColor);
            chkMonRcv.FlatAppearance.CheckedBackColor = _settings.SelectedColor;
            chkMonRcv.Checked = _settings.MonitorRcv;
            chkMonRcv.Click += (_, __) => _settings.MonitorRcv = chkMonRcv.Checked;

            chkMonSnd.BackColor = BackColor;
            GraphicsUtils.ColorizeControl(chkMonSnd, _settings.IconColor);
            chkMonSnd.FlatAppearance.CheckedBackColor = _settings.SelectedColor;
            chkMonSnd.Checked = _settings.MonitorSnd;
            chkMonSnd.Click += (_, __) => _settings.MonitorSnd = chkMonSnd.Checked;

            btnRewind.BackColor = BackColor;
            GraphicsUtils.ColorizeControl(btnRewind, _settings.IconColor);
            btnRewind.Click += Rewind_Click;

            btnAbout.BackColor = BackColor;
            GraphicsUtils.ColorizeControl(btnAbout, _settings.IconColor);
            btnAbout.Click += About_Click;

            btnKill.BackColor = BackColor;
            GraphicsUtils.ColorizeControl(btnKill, _settings.IconColor);
            btnKill.Click += (_, __) => { _mgr.Kill(); CurrentState = ExecState.Idle; };

            btnSettings.BackColor = BackColor;
            GraphicsUtils.ColorizeControl(btnSettings, _settings.IconColor);
            btnSettings.Click += Settings_Click;

            sldVolume.BackColor = BackColor;
            sldVolume.DrawColor = _settings.DrawColor;
            //sldVolume.ValueChanged += (_, __) => _volume = sldVolume.Value;

            sldTempo.BackColor = BackColor;
            sldTempo.DrawColor = _settings.DrawColor;
            sldTempo.ValueChanged += (_, __) => { SetTimer((int)sldTempo.Value); };

                traffic.BackColor = BackColor;
            traffic.MatchText.Add("ERR ", Color.LightPink);
            traffic.MatchText.Add("WRN ", Color.Yellow);
            traffic.MatchText.Add("SND ", Color.PaleGreen);
            traffic.MatchText.Add("RCV ", Color.LightBlue);
            traffic.Font = new("Cascadia Mono", 9);
            traffic.Prompt = "";
            traffic.WordWrap = _settings.WordWrap;

            timeBar.DrawColor = _settings.DrawColor;
            timeBar.SelectedColor = _settings.SelectedColor;
            timeBar.StateChange += TimeBar_StateChangeEvent;

            GraphicsUtils.ColorizeControl(ddbtnFile, _settings.IconColor);
            ddbtnFile.BackColor = BackColor;
            ddbtnFile.FlatAppearance.CheckedBackColor = _settings.SelectedColor;
            ddbtnFile.Enabled = true;
            ddbtnFile.Selected += File_Selected;
            #endregion

            // Hook script callbacks.
            Interop.Log += Interop_Log;
            Interop.OpenMidiInput += Interop_OpenMidiInput;
            Interop.OpenMidiOutput += Interop_OpenMidiOutput;
            Interop.SendMidiNote += Interop_SendMidiNote;
            Interop.SendMidiController += Interop_SendMidiController;
            Interop.SetTempo += Interop_SetTempo;

            _mgr.MessageReceive += Mgr_MessageReceive;
            _mgr.MessageSend += Mgr_MessageSend;

            Thread.CurrentThread.Name = "MAIN";
            // Trace($"+++ MainForm() [{Thread.CurrentThread.Name}] ({Environment.CurrentManagedThreadId})");

            _tmit.Snap("MainForm() exit");
        }

        /// <summary>
        /// Inits control appearance. Opens last script. Can throw.
        /// </summary>
        /// <param name="e"></param>
        protected override void OnLoad(EventArgs e)
        {
            _tmit.Snap("OnLoad() entry");

            PopulateFileMenu();

            if (_settings.OpenLastFile && _settings.RecentFiles.Count > 0)
            {
                OpenScriptFile(_settings.RecentFiles[0]);
            }

            base.OnLoad(e);

            _tmit.Snap("OnLoad() exit");
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="e"></param>
        protected override void OnShown(EventArgs e)
        {
            _tmit.Snap("OnShown() entry");

            base.OnShown(e);

            _tmit.Snap("OnShown() exit");
        }

        /// <summary>
        /// Goodbye.
        /// </summary>
        /// <param name="e"></param>
        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            CurrentState = ExecState.Idle;

            // Just in case.
            _mgr.Kill();

            // Destroy devices
            ResetDevices();

            // Save user settings.
            _settings.FormGeometry = new()
            {
                X = Location.X,
                Y = Location.Y,
                Width = Width,
                Height = Height
            };
            _settings.WordWrap = traffic.WordWrap;
            _settings.Save();

            LogManager.Stop();

            base.OnFormClosing(e);
        }

        /// <summary>
        ///  Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            DestroyControls();
            _mgr.DestroyDevices();
            
            if (disposing)
            {
                // Wait a bit in case there are some lingering events.
                //System.Threading.Thread.Sleep(100);

                components?.Dispose();

                // Release unmanaged resources.
                _mmTimer.Stop();
                _mmTimer.Dispose();
                _interop.Dispose();

            }
            base.Dispose(disposing);
        }
        #endregion

        #region Script File Handling
        /// <summary>
        /// Script file opener/reloader.
        /// </summary>
        /// <param name="openScriptFn">The script file to open or null if reload.</param>
        void OpenScriptFile(string? openScriptFn)
        {
            lock(_interopLock)
            {
                try
                {
                    // Clean up first.
                    // Just in case.
                    _mgr.Kill();
                    _mmTimer.Stop();
                    DestroyControls();
                    CurrentState = ExecState.Idle;
                    // Destroy devices
                    ResetDevices();

                    // Determine file to load.
                    if (openScriptFn is not null)
                    {
                        _scriptFn = openScriptFn;
                    }

                    // Check valid file.
                    if (_scriptFn is null || !_scriptFn.EndsWith(".lua") || !Path.Exists(_scriptFn))
                    {
                        _scriptFn = null;                    
                        _scriptTouch = DateTime.MinValue;                    
                        throw new AppException($"Invalid script file [{_scriptFn}]");
                    }

                    // OK to load.
                    _scriptTouch = File.GetLastWriteTime(_scriptFn);
                    _loggerApp.Info($"Loading script {_scriptFn}");

                    // Set up runtime lua environment. The lua lib files, the dir containing the script file.
                    var srcDir = MiscUtils.GetSourcePath(); // The source dir.
                    var scriptDir = Path.GetDirectoryName(_scriptFn);
                    var luaPath = $"{scriptDir}\\?.lua;{srcDir}\\LBOT\\?.lua;{srcDir}\\lua\\?.lua;;";

                    _interop.RunScript(_scriptFn, luaPath);
                    string smeta = _interop.Setup();

                    // Get info about the script.
                    Dictionary<int, string> sectInfo = [];
                    var chunks = smeta.SplitByToken("|");

                    chunks.ForEach(ch =>
                    {
                        var elems = ch.SplitByToken(",");
                        sectInfo[int.Parse(elems[1])] = elems[0];
                    });

                    timeBar.InitSectionInfo(sectInfo);

                    CreateControls();

                    // Start timer.
                    sldTempo.Value = 100;
                    _mmTimer.Start();

                    Text = $"Nebulua {MiscUtils.GetVersionString()} - {_scriptFn}";
                    _settings.UpdateMru(_scriptFn!);

                    PopulateFileMenu();

                    timeBar.Invalidate(); // force update
                }
                catch (Exception ex)
                {
                    ProcessException(ex);
                }
            }
        }

        /// <summary>
        /// Selection from user.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="fsel"></param>
        void File_Selected(object? sender, string fsel)
        {
            switch (fsel)
            {
                case "Open...":
                    {
                        using OpenFileDialog openDlg = new()
                        {
                            Filter = "Nebulua files | *.lua",
                            Title = "Select a Nebulua file",
                            InitialDirectory = _settings.ScriptPath,
                        };

                        if (openDlg.ShowDialog() == DialogResult.OK)
                        {
                            OpenScriptFile(openDlg.FileName);
                        }
                    }
                    break;

                case "Reload":
                    OpenScriptFile(null);
                    break;

                default: // specific file
                    OpenScriptFile(fsel);
                    break;
            }
        }

        /// <summary>
        /// Create the menu with the recently used files.
        /// </summary>
        void PopulateFileMenu()
        {
            List<string> options = [];
            options.Add("Open...");

            if (_scriptFn is not null)
            {
                options.Add("Reload");
            }

            if (_settings.RecentFiles.Count > 0)
            {
                options.Add("");
                _settings.RecentFiles.ForEach(options.Add);
            }

            ddbtnFile.SetOptions(options);
        }
        #endregion

        #region Run Control
        /// <summary>
        /// Update state.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void Play_Click(object? sender, EventArgs e)
        {
            if (chkPlay.Checked)
            {
                // Maybe reload.
                if (_settings.AutoReload && _scriptFn is not null)
                {
                    var touch = File.GetLastWriteTime(_scriptFn);
                    if (touch > _scriptTouch)
                    {
                        OpenScriptFile(null);
                    }
                }

                CurrentState = ExecState.Run;
            }
            else
            {
                CurrentState = ExecState.Stop;
            }
        }

        /// <summary>
        /// Handler for state changes.
        /// </summary>
        /// <param name="_"></param>
        /// <param name="name">Specific State value.</param>
        void TimeBar_StateChangeEvent(object? _, StateChangeEventArgs e)
        {
           this.InvokeIfRequired(_ =>
           {
                if (e.CurrentTimeChange)
                {
                    timeBar.Invalidate();
                }
           });
        }

        /// <summary>
        /// Rewind.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void Rewind_Click(object? sender, EventArgs e)
        {
            timeBar.Rewind();
            // Current tick may have been corrected for loop.
            //timeBar.Current = State.Instance.CurrentTick;
        }

        /// <summary>
        /// Do some global key handling. Space bar is used for stop/start playing.
        /// </summary>
        /// <param name="e"></param>
        protected override void OnKeyDown(KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Space) // && _scriptFn is not null)
            {
                CurrentState = CurrentState == ExecState.Stop ? ExecState.Run : ExecState.Stop;
                e.Handled = true;
            }
            base.OnKeyDown(e);
        }

        /// <summary>
        /// General exception processor. Doesn't throw.
        /// </summary>
        /// <param name="e"></param>
        /// <returns>(bool fatal, string msg)</returns>
        (bool fatal, string msg) ProcessException(Exception e)
        {
            bool fatal = false; // default
            string msg = e.Message; // default

            switch (e)
            {
                case LuaException ex:
                    if (ex.Error.Contains("FATAL")) // bad lua internal error
                    {
                        fatal = true;
                        CurrentState = ExecState.Dead;
                    }
                    break;

                case AppException: // from app - not fatal
                    break;

                default: // other/unknon - assume fatal
                    fatal = true;
                    if (e.StackTrace is not null)
                    {
                        msg += $"{Environment.NewLine}{e.StackTrace}";
                    }
                    break;
            }

            if (fatal)
            {
                // Logging an error will cause the app to exit.
                _loggerApp.Error(msg);
            }
            else
            {
                // User can decide what to do with this. They may be recoverable so use warn.
                _loggerApp.Warn(msg);
                CurrentState = ExecState.Idle;
            }

            return (fatal, msg);
        }
        #endregion

        #region Callback Handlers
        /// <summary>
        /// Process events. This is on a system thread.
        /// </summary>
        /// <param name="totalElapsed"></param>
        /// <param name="periodElapsed"></param>
        void MmTimer_Callback(double totalElapsed, double periodElapsed)
        {
            //_inMmTimer = true;

            if (CurrentState == ExecState.Run)
            {
                lock(_interopLock)
                {
                   // if (_threadId is not null)// && _threadId != Thread.CurrentThread.ManagedThreadId)
                   // {
                   //     Trace($"!!! MmTimer_Callback() [{Thread.CurrentThread.Name}:{Environment.CurrentManagedThreadId}]");
                   //     throw new InvalidOperationException();
                   // }
                    //_threadId = Environment.CurrentManagedThreadId;

                    try
                    {
                        // Do script.
                        //_tan?.Arm();

                        if (string.IsNullOrEmpty(Thread.CurrentThread.Name))
                        {
                            Thread.CurrentThread.Name = "MM_TIMER";
                        }

                       // if (State.Instance.CurrentTick  % 1000 == 0)
                       // {
                       //     Trace($"+++ MmTimer_Callback() {State.Instance.CurrentTick} [{Thread.CurrentThread.Name}:{Environment.CurrentManagedThreadId}]");
                       // }

                        int ret = _interop.Step(timeBar.Current);

                        // Read stopwatch and diff/stats.
                        //string? s = _tan?.Dump();

                        if (timeBar.Valid)
                        {
                            // Bump time and check state.
                            if (!timeBar.Increment())
                            {
                                // Stop and rewind.
                                CurrentState = ExecState.Idle;
                                //State.Instance.CurrentTick = start;

                                // just in case
                                _mgr.Kill();
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        ProcessException(ex);
                    }

                    //Trace($"--- MmTimer_Callback() EXIT");
                    //_threadId = null;
                }
            }
            //_inMmTimer = false;
        }

        /// <summary>
        /// Midi message arrived. Pass along to the script. This is on a system thread.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        //void Midi_ReceiveEvent(object? sender, MidiEvent e)
        void Mgr_MessageReceive(object? sender, BaseMidiEvent e)
        {
            // Trace($"+++ Mgr_MessageReceive() ENTER [_threadId={_threadId ?? -1}] [{Thread.CurrentThread.Name}:{Environment.CurrentManagedThreadId}]");
            lock (_interopLock)
            {
                //Trace($"+++ Mgr_MessageReceive() LOCKED [_inMmTimer={_inMmTimer}]");
                //if (_threadId is not null) // && _threadId != Thread.CurrentThread.ManagedThreadId)
                //{
                //    Trace($"!!! Midi_ReceiveEvent() [{Thread.CurrentThread.Name}:{Environment.CurrentManagedThreadId}]");
                //    throw new InvalidOperationException();
                //}
                //_threadId = Environment.CurrentManagedThreadId;

                try
                {
                    if (string.IsNullOrEmpty(Thread.CurrentThread.Name))
                    {
                        Thread.CurrentThread.Name = "MIDI_RCV";
                    }

                    //Trace($"+++ Mgr_MessageReceive() [{Thread.CurrentThread.Name}:{Environment.CurrentManagedThreadId}]");

                    var indev = (MidiInputDevice)sender!;
                    var chnd = ChannelHandle.Create(indev.Id, e.ChannelNumber, false);
                    bool logit = true;

                    switch (e)
                    {
                        case NoteOn evt:
                            _interop.ReceiveMidiNote(chnd, evt.Note, (double)evt.Velocity / MidiDefs.MAX_MIDI);
                            break;

                        case NoteOff evt:
                            _interop.ReceiveMidiNote(chnd, evt.Note, 0);
                            break;

                        case Controller evt:
                            _interop.ReceiveMidiController(chnd, (int)evt.ControllerId, evt.Value);
                            break;

                        default: // Ignore others for now.
                            logit = false;
                            break;
                    }

                    if (logit && _settings.MonitorRcv)
                    {
                        _loggerMidi.Trace($"<<< {FormatMidiEvent(e, chnd)}");
                    }
                }
                catch (Exception ex)
                {
                    ProcessException(ex);
                }

                //Trace($"--- Mgr_MessageReceive() EXIT");
                //_threadId = null;
            }
        }

        /// <summary>
        /// Midi message sent. Just for logging.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        //void Midi_ReceiveEvent(object? sender, MidiEvent e)
        void Mgr_MessageSend(object? sender, BaseMidiEvent e)
        {
            // Actual sent.
            var indev = (MidiOutputDevice)sender!;
            var chnd = ChannelHandle.Create(indev.Id, e.ChannelNumber, false);
            _loggerMidi.Trace($">>> ! {FormatMidiEvent(e, chnd)}");
        }

        /// <summary>
        /// Process UI change.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void ChannelControlEvent(object? sender, ChannelControl.ChannelChangeEventArgs e)
        {
            var cc = sender as ChannelControl;
            var channel = cc!.BoundChannel!;

            if (e.StateChange)
            {
                // Update all channels.
                bool anySolo = _channelControls.Where(c => c.State == ChannelControl.ChannelState.Solo).Any();

                foreach (var cciter in _channelControls)
                {
                    bool enable = anySolo ?
                        cciter.State == ChannelControl.ChannelState.Solo :
                        cciter.State != ChannelControl.ChannelState.Mute;

                    channel.Enable = enable;
                    if (!enable)
                    {
                        // Kill just in case.
                        _mgr.Kill(channel);
                    }
                }
            }
        }
        #endregion

        #region Script ==> Host Functions
        /// <summary>
        /// Script creates an input channel.
        /// </summary>
        /// <param name="_"></param>
        /// <param name="e"></param>
        /// <exception cref="AppException">From called functions</exception>
        void Interop_OpenMidiInput(object? _, OpenMidiInputArgs e)
        {
            var chan_in = _mgr.OpenMidiInput(e.dev_name, e.chan_num, e.chan_name);
            e.ret = chan_in.Handle;
        }

        /// <summary>
        /// Script creates an output channel.
        /// </summary>
        /// <param name="_"></param>
        /// <param name="e"></param>
        /// <exception cref="AppException">From called functions</exception>
        void Interop_OpenMidiOutput(object? _, OpenMidiOutputArgs e)
        {
            // Create channels and initialize controls.
            var chan_out = _mgr.OpenMidiOutput(e.dev_name, e.chan_num, e.chan_name, e.patch);
            e.ret = chan_out.Handle;
        }

        /// <summary>
        /// Script wants to send a midi note. Doesn't throw.
        /// </summary>
        /// <param name="_"></param>
        /// <param name="e"></param>
        void Interop_SendMidiNote(object? _, SendMidiNoteArgs e)
        {
            e.ret = 0; // not used

            if (e.note_num is < 0 or > MidiDefs.MAX_MIDI) { throw new ArgumentOutOfRangeException(nameof(e.note_num)); }
            var channel = _mgr.GetOutputChannel(e.chan_hnd) ?? throw new ArgumentException(nameof(e.chan_hnd));

            if (e.volume == 0.0)
            {
                channel.Device.Send(new NoteOff(channel.ChannelNumber, e.note_num));
            }
            else
            {
                var vel = (int)MathUtils.Constrain(e.volume * sldVolume.Value * MidiDefs.MAX_MIDI, 0, MidiDefs.MAX_MIDI);
                channel.Device.Send(new NoteOn(channel.ChannelNumber, e.note_num, vel));
            }
        }

        /// <summary>
        /// Script wants to send a midi controller. Doesn't throw.
        /// </summary>
        /// <param name="_"></param>
        /// <param name="e"></param>
        void Interop_SendMidiController(object? _, SendMidiControllerArgs e)
        {
            e.ret = 0; // not used

            if (e.controller is < 0 or > MidiDefs.MAX_MIDI) { throw new ArgumentOutOfRangeException(nameof(e.controller)); }
            if (e.value is < 0 or > MidiDefs.MAX_MIDI) { throw new ArgumentOutOfRangeException(nameof(e.value)); }
            var channel = _mgr.GetOutputChannel(e.chan_hnd) ?? throw new ArgumentException(nameof(e.chan_hnd));

            var se = new Controller(channel.ChannelNumber, e.controller, e.value);
            channel.Device.Send(se);

            if (_settings.MonitorSnd)
            {
                // Intended sent.
                _loggerMidi.Trace($">>> {FormatMidiEvent(se, e.chan_hnd)}");
            }
        }

        /// <summary>
        /// Script wants to change tempo. Doesn't throw.
        /// </summary>
        /// <param name="_"></param>
        /// <param name="e"></param>
        void Interop_SetTempo(object? _, SetTempoArgs e)
        {
            e.ret = 0; // not used

            if (e.bpm >= 30 && e.bpm <= 240)
            {
                sldTempo.Value = e.bpm; //TODO1 confirm this triggers => SetTimer((int)sldTempo.Value);
            }
            else if (e.bpm == 0)
            {
                SetTimer(0);
            }
            else
            {
                e.ret = -1;
                // Leave as is or reset?
                // SetTimer(0);
                _loggerScr.Warn($"Invalid tempo {e.bpm}");
            }
        }

        /// <summary>
        /// Script wants to log something. Doesn't throw.
        /// </summary>
        /// <param name="_"></param>
        /// <param name="e"></param>
        void Interop_Log(object? _, LogArgs e)
        {
            e.ret = 0; // not used

            if (e.level < (int)LogLevel.Trace || e.level > (int)LogLevel.Error)
            {
                e.ret = -1;
                _loggerScr.Warn($"Invalid log level {e.level}");
                e.level = (int)LogLevel.Warn;
            }

            string s = $"{e.msg ?? "null"}";
            switch ((LogLevel)e.level)
            {
                case LogLevel.Trace: _loggerScr.Trace(s); break;
                case LogLevel.Debug: _loggerScr.Debug(s); break;
                case LogLevel.Info: _loggerScr.Info(s); break;
                case LogLevel.Warn: _loggerScr.Warn(s); break;
                case LogLevel.Error: _loggerScr.Error(s); break;
            }
        }
        #endregion

        #region Channel Controls
        /// <summary>
        /// Destroy controls.
        /// </summary>
        void DestroyControls()
        {
            _mgr.Kill();

            // Clean out our current elements.
            _channelControls.ForEach(c =>
            {
                Controls.Remove(c);
                c.Dispose();
            });
            _channelControls.Clear();
        }

        /// <summary>
        /// Create controls.
        /// </summary>
        void CreateControls()
        {
            DestroyControls();

            /// create a control for each channel and bind object
            int x = timeBar.Left;
            int y = timeBar.Bottom + 5;

            _mgr.OutputChannels.ForEach(chan =>
            {
                var ctrl = new ChannelControl()
                {
                    BoundChannel = chan,
                    Options = ChannelControl.DisplayOptions.SoloMute,
                    UserRenderer = null,
                    Location = new(x, y),
                    BorderStyle = BorderStyle.FixedSingle,
                    DrawColor = _settings.DrawColor,
                    SelectedColor = _settings.SelectedColor,
                    Volume = Defs.DEFAULT_VOLUME,
                };
                ctrl.ChannelChange += ChannelControl_ChannelChange;
                ctrl.SendMidi += ChannelControl_SendMidi;

                Controls.Add(ctrl);
                x += ctrl.Width + 4; // Width is not valid until after previous statement.
            });
        }
        #endregion

        #region Events
        /// <summary>
        /// UI clicked something -> send some midi. Works for different sources.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void ChannelControl_SendMidi(object? sender, BaseMidiEvent e)
        {
            var channel = (sender as ChannelControl)!.BoundChannel;
            channel.Device.Send(e);
        }

        /// <summary>
        /// UI clicked something -> configure channel.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void ChannelControl_ChannelChange(object? sender, ChannelControl.ChannelChangeEventArgs e)
        {
            var cc = sender as ChannelControl;
            var channel = cc!.BoundChannel!;

            if (e.StateChange)
            {
                // Update all channels.
                bool anySolo = _channelControls.Where(c => c.State == ChannelControl.ChannelState.Solo).Any();

                foreach (var cciter in _channelControls)
                {
                    bool enable = anySolo ?
                        cciter.State == ChannelControl.ChannelState.Solo :
                        cciter.State != ChannelControl.ChannelState.Mute;

                    channel.Enable = enable;
                    if (!enable)
                    {
                        // Kill just in case.
                        _mgr.Kill(channel);
                    }
                }
            }

            //if (e.PatchChange)
            //{
            //    Tell(INFO, $"PatchChange [{channel.Patch}]");
            //    channel.Device.Send(new Patch(channel.ChannelNumber, channel.Patch));
            //}

            //if (e.AliasFileChange)
            //{
            //    Tell(INFO, $"AliasFileChange [{channel.AliasFile}]");
            //}
        }
        #endregion

        #region Midi Utilities
        /// <summary>
        /// Create string suitable for logging. Doesn't throw.
        /// </summary>
        /// <param name="evt">Midi event to format.</param>
        /// <param name="chanHnd">Channel info.</param>
        /// <returns>Suitable string.</returns>
        string FormatMidiEvent(BaseMidiEvent evt, int chnd)
        {
            // Common part.
            int tick = CurrentState == ExecState.Run ? timeBar.Current : 0;
            int devId = ChannelHandle.DeviceId(chnd);
            int chanNum = ChannelHandle.ChannelNumber(chnd);
            MusicTime mt = new(tick);

            string s = $"{tick:00000} {mt} Dev:{devId} Ch:{chanNum} ";

            switch (evt)
            {
                case NoteOn e:
                    var snoteon = chanNum == 10 || chanNum == 16 ?
                        $"DRUM_{e.Note}" :
                        MusicDefinitions.NoteNumberToName(e.Note);
                    s = $"{s} {e.Note}:{snoteon} Vel:{e.Velocity}";
                    break;

                case NoteOff e:
                    var snoteoff = chanNum == 10 || chanNum == 16 ?
                        $"DRUM_{e.Note}" :
                        MusicDefinitions.NoteNumberToName(e.Note);
                    s = $"{s} {e.Note}:{snoteoff}";
                    break;

                case Controller e:
                    var sctl = MidiDefs.Instance.GetControllerName(e.ControllerId);
                    s = $"{s} {sctl}:{e.Value}";
                    break;

                default: // Ignore others for now.
                    break;
            }

            return s;
        }
        #endregion

        #region Misc Stuff
        /// <summary>Handle state change.</summary>
        /// <param name="state">New state</param>
        void UpdateState(ExecState state)
        {
            switch (state)
            {
                case ExecState.Idle:
                    _scriptFn = null;
                    chkPlay.Checked = false;
                    chkPlay.Enabled = false;
                    _execState = ExecState.Idle;
                    break;

                case ExecState.Stop:
                    if (_scriptFn is not null)
                    {
                        chkPlay.Checked = false;
                        chkPlay.Enabled = true;
                        _execState = ExecState.Stop;
                    }
                    else
                    {
                        chkPlay.Checked = false;
                        chkPlay.Enabled = false;
                        _execState = ExecState.Idle;
                    }
                    break;

                case ExecState.Run:
                    if (_scriptFn is not null)
                    {
                        chkPlay.Checked = true;
                        chkPlay.Enabled = true;
                        _execState = ExecState.Run;
                    }
                    else
                    {
                        chkPlay.Checked = false;
                        chkPlay.Enabled = false;
                        _execState = ExecState.Idle;
                    }
                    break;

                case ExecState.Dead:
                    chkPlay.Checked = false;
                    chkPlay.Enabled = false;
                    _execState = ExecState.Dead;
                    break;
            }
        }

        /// <summary>
        /// Capture bad things and display them to the user.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void LogManager_LogMessage(object? sender, LogMessageEventArgs e)
        {
            this.InvokeIfRequired(_ =>
            {
                traffic.AppendLine(e.ShortMessage);

                if (e.Level == LogLevel.Error)
                {
                    traffic.AppendLine("Fatal error - please fix then restart");
                    CurrentState = ExecState.Dead;
                }
            });
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void Settings_Click(object? sender, EventArgs e)
        {
            var changes = SettingsEditor.Edit(_settings, "User Settings", 500);

            // Detect changes of interest.
            bool restart = false;

            foreach (var (name, cat) in changes)
            {
                switch (name)
                {
                    case "DrawColor":
                    case "BackColor":
                    case "IconColor":
                    case "SelectedColor":
                        restart = true;
                        break;

                    case "FileLogLevel":
                        LogManager.MinLevelFile = _settings.FileLogLevel;
                        break;

                    case "NotifLogLevel":
                        LogManager.MinLevelNotif = _settings.NotifLogLevel;
                        break;
                }
            }

            if (restart)
            {
                MessageBox.Show("Restart required for device changes to take effect");
            }
        }

        /// <summary>
        /// The meaning of life.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void About_Click(object? sender, EventArgs e)
        {
            // Main help.
            Tools.ShowReadme("Nebulua");

            // Show the user devices.
            List<string> ls = [];

            // Show them what they have.
            var outs = MidiOutputDevice.GetAvailableDevices();
            var ins = MidiInputDevice.GetAvailableDevices();

            ls.Add($"# Your Midi Devices");

            ls.Add($"");
            ls.Add($"## Inputs");
            ls.Add($"");

            if (ins.Count == 0)
            {
                ls.Add($"- None");
            }
            else
            {
                ins.ForEach(d => ls.Add($"- [{d}]"));
            }

            ls.Add($"## Outputs");
            if (outs.Count == 0)
            {
                ls.Add($"- None");
            }
            else
            {
                outs.ForEach(d => ls.Add($"- [{d}]"));
            }

            traffic.AppendLine(string.Join(Environment.NewLine, ls));
        }

        /// <summary>
        /// Clean up devices. Doesn't throw.
        /// </summary>
        void ResetDevices()
        {
            _inputDevices.ForEach(d => d.Dispose());
            _inputDevices.Clear();
            _outputDevices.ForEach(d => d.Dispose());
            _outputDevices.Clear();
        }

        /// <summary>
        /// Set timer for this tempo. Doesn't throw.
        /// </summary>
        /// <param name="tempo"></param>
        void SetTimer(int tempo)
        {
            if (tempo > 0)
            {
                double sec_per_beat = 60.0 / tempo;
                double msec_per_sub = 1000 * sec_per_beat / MusicTime.TicksPerBeat;
                double period = msec_per_sub > 1.0 ? msec_per_sub : 1;
                _mmTimer.SetTimer((int)Math.Round(period, 2), MmTimer_Callback);
            }
            else // stop
            {
                _mmTimer.SetTimer(0, MmTimer_Callback);
            }
        }
        #endregion
    }
}
