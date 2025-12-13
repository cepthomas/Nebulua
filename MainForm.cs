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
//using NAudio.Midi;
using Ephemera.NBagOfTricks;
using Ephemera.NBagOfUis;
using Ephemera.MidiLibLite;


// TODO kinda slow startup running in debugger.

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

///////////////////////////// NEW NEW //////////////////////////////////////
        /// <summary>All the channel controls.</summary>
//        readonly List<ChannelControl> _channelControls = [];

        /// <summary>Cosmetics.</summ ary>
//        readonly Color _controlColor = Color.Aquamarine;

        /// <summary>Cosmetics.</summary>
//        readonly Color _selectedColor = Color.Yellow;

        /// <summary>The boss.</summary>
        readonly Manager _mgr = new();





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
            get
            {
                return _execState;
            }
            set
            {
                if (value != _execState)
                {
                    switch (value)
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
            }
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
            UserSettings.Current = (UserSettings)SettingsCore.Load(appDir, typeof(UserSettings));
            LogManager.LogMessage += LogManager_LogMessage;
            LogManager.MinLevelFile = UserSettings.Current.FileLogLevel;
            LogManager.MinLevelNotif = UserSettings.Current.NotifLogLevel;
            LogManager.Run(Path.Combine(appDir, "log.txt"), 50000);

            // Main window.
            Location = UserSettings.Current.FormGeometry.Location;
            Size = UserSettings.Current.FormGeometry.Size;
            WindowState = FormWindowState.Normal;
            Text = $"Nebulua {MiscUtils.GetVersionString()} - No script loaded";

            #region Init the controls
            GraphicsUtils.ColorizeControl(chkPlay, UserSettings.Current.IconColor);
            chkPlay.BackColor = BackColor;
            chkPlay.FlatAppearance.CheckedBackColor = UserSettings.Current.SelectedColor;
            chkPlay.Click += Play_Click;

            GraphicsUtils.ColorizeControl(chkLoop, UserSettings.Current.IconColor);
            chkLoop.BackColor = BackColor;
            chkLoop.FlatAppearance.CheckedBackColor = UserSettings.Current.SelectedColor;
            chkLoop.Click += (_, __) => State.Instance.DoLoop = chkLoop.Checked;

            chkMonRcv.BackColor = BackColor;
            GraphicsUtils.ColorizeControl(chkMonRcv, UserSettings.Current.IconColor);
            chkMonRcv.FlatAppearance.CheckedBackColor = UserSettings.Current.SelectedColor;
            chkMonRcv.Checked = UserSettings.Current.MonitorRcv;
            chkMonRcv.Click += (_, __) => UserSettings.Current.MonitorRcv = chkMonRcv.Checked;

            chkMonSnd.BackColor = BackColor;
            GraphicsUtils.ColorizeControl(chkMonSnd, UserSettings.Current.IconColor);
            chkMonSnd.FlatAppearance.CheckedBackColor = UserSettings.Current.SelectedColor;
            chkMonSnd.Checked = UserSettings.Current.MonitorSnd;
            chkMonSnd.Click += (_, __) => UserSettings.Current.MonitorSnd = chkMonSnd.Checked;

            btnRewind.BackColor = BackColor;
            GraphicsUtils.ColorizeControl(btnRewind, UserSettings.Current.IconColor);
            btnRewind.Click += Rewind_Click;

            btnAbout.BackColor = BackColor;
            GraphicsUtils.ColorizeControl(btnAbout, UserSettings.Current.IconColor);
            btnAbout.Click += About_Click;

            btnKill.BackColor = BackColor;
            GraphicsUtils.ColorizeControl(btnKill, UserSettings.Current.IconColor);
            btnKill.Click += (_, __) => { _mgr.Kill(); CurrentState = ExecState.Idle; };
//btnKillMidi.Click += (_, __) => { _mgr.Kill(); };

            btnSettings.BackColor = BackColor;
            GraphicsUtils.ColorizeControl(btnSettings, UserSettings.Current.IconColor);
            btnSettings.Click += Settings_Click;

            sldVolume.BackColor = BackColor;
            sldVolume.DrawColor = UserSettings.Current.ControlColor;
            sldVolume.ValueChanged += (_, __) => State.Instance.Volume = sldVolume.Value;

            sldTempo.BackColor = BackColor;
            sldTempo.DrawColor = UserSettings.Current.ControlColor;
            sldTempo.ValueChanged += (_, __) => State.Instance.Tempo = (int)sldTempo.Value;

            traffic.BackColor = BackColor;
            traffic.MatchText.Add("ERR ", Color.LightPink);
            traffic.MatchText.Add("WRN ", Color.Yellow);
            traffic.MatchText.Add("SND ", Color.PaleGreen);
            traffic.MatchText.Add("RCV ", Color.LightBlue);
            traffic.Font = new("Cascadia Mono", 9);
            traffic.Prompt = "";
            traffic.WordWrap = UserSettings.Current.WordWrap;

            GraphicsUtils.ColorizeControl(ddbtnFile, UserSettings.Current.IconColor);
            ddbtnFile.BackColor = BackColor;
            ddbtnFile.FlatAppearance.CheckedBackColor = UserSettings.Current.SelectedColor;
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

            State.Instance.ValueChangeEvent += State_ValueChangeEvent;

_mgr.InputReceive += Mgr_InputReceive;

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

            ReadMidiDefs();

            PopulateFileMenu();

            if (UserSettings.Current.OpenLastFile && UserSettings.Current.RecentFiles.Count > 0)
            {
                OpenScriptFile(UserSettings.Current.RecentFiles[0]);
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
            UserSettings.Current.FormGeometry = new()
            {
                X = Location.X,
                Y = Location.Y,
                Width = Width,
                Height = Height
            };
            UserSettings.Current.WordWrap = traffic.WordWrap;
            UserSettings.Current.Save();

            LogManager.Stop();

            base.OnFormClosing(e);
        }

        /// <summary>
        ///  Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
///////////////////////////// NEW NEW //////////////////////////////////////
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

                    State.Instance.InitSectionInfo(sectInfo);

                    CreateControls();

                    // Start timer.
                    SetTimer(State.Instance.Tempo);
                    _mmTimer.Start();

                    Text = $"Nebulua {MiscUtils.GetVersionString()} - {_scriptFn}";
                    UserSettings.Current.UpdateMru(_scriptFn!);

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
                            InitialDirectory = UserSettings.Current.ScriptPath,
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

            if (UserSettings.Current.RecentFiles.Count > 0)
            {
                options.Add("");
                UserSettings.Current.RecentFiles.ForEach(options.Add);
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
                if (UserSettings.Current.AutoReload && _scriptFn is not null)
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
        void State_ValueChangeEvent(object? _, string name)
        {
            this.InvokeIfRequired(_ =>
            {
                switch (name)
                {
                    case "CurrentTick":
                        timeBar.Invalidate();
                        break;

                    case "Tempo":
                        sldTempo.Value = State.Instance.Tempo;
                        SetTimer(State.Instance.Tempo);
                        break;
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
            State.Instance.CurrentTick = 0;
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
//                    if (_threadId is not null)// && _threadId != Thread.CurrentThread.ManagedThreadId)
//                    {
//                        Trace($"!!! MmTimer_Callback() [{Thread.CurrentThread.Name}:{Environment.CurrentManagedThreadId}]");
//                        throw new InvalidOperationException();
//                    }
                    //_threadId = Environment.CurrentManagedThreadId;

                    try
                    {
                        // Do script.
                        //_tan?.Arm();

                        if (string.IsNullOrEmpty(Thread.CurrentThread.Name))
                        {
                            Thread.CurrentThread.Name = "MM_TIMER";
                        }

//                        if (State.Instance.CurrentTick  % 1000 == 0)
//                        {
//                            Trace($"+++ MmTimer_Callback() {State.Instance.CurrentTick} [{Thread.CurrentThread.Name}:{Environment.CurrentManagedThreadId}]");
//                        }

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
                                    CurrentState = ExecState.Idle;
                                    State.Instance.CurrentTick = start;

                                    // just in case
                                    _mgr.Kill();
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
                        ProcessException(ex);
                    }
                    //Trace($"--- MmTimer_Callback() EXIT");
                    //_threadId = null;
                }
            }
            //_inMmTimer = false;
        }

        /// <summary>
        /// Midi input arrived. This is on a system thread.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        //void Midi_ReceiveEvent(object? sender, MidiEvent e)
        void Mgr_InputReceive(object? sender, BaseMidiEvent e)
        {
            // Trace($"+++ Midi_ReceiveEvent() ENTER [_threadId={_threadId ?? -1}] [{Thread.CurrentThread.Name}:{Environment.CurrentManagedThreadId}]");
            lock (_interopLock)
            {
                //Trace($"+++ Midi_ReceiveEvent() LOCKED [_inMmTimer={_inMmTimer}]");
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

                    //Trace($"+++ Midi_ReceiveEvent() [{Thread.CurrentThread.Name}:{Environment.CurrentManagedThreadId}]");

                    var indev = (MidiInputDevice)sender!;

                    ChannelHandle chnd = new(indev.Id, e.ChannelNumber, false);
                    //int chanHnd = ch;
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

                    if (logit && UserSettings.Current.MonitorRcv)
                    {
                        _loggerMidi.Trace($"<<< {FormatMidiEvent(e, CurrentState == ExecState.Run ? State.Instance.CurrentTick : 0, chnd)}");
                    }
                }
                catch (Exception ex)
                {
                    ProcessException(ex);
                }

                //Trace($"--- Midi_ReceiveEvent() EXIT");
                //_threadId = null;
            }
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
                //Tell(INFO, $"StateChange");

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

            ////////////////////////// OLD //////////////////////////
            //// Update all channel enables.
            //bool anySolo = _channelControls.Where(c => c.State == PlayState.Solo).Any();

            //foreach (var c in _channelControls)
            //{
            //    bool enable = anySolo ? c.State == PlayState.Solo : c.State != PlayState.Mute;

            //    var ch = c.ChHandle;

            //    if (ch.DeviceId >= _outputs.Count)
            //    {
            //        throw new AppException($"Invalid device id [{ch.DeviceId}]");
            //    }

            //    var output = _outputs[ch.DeviceId];
            //    if (!output.Channels.TryGetValue(ch.ChannelNumber, out MidiChannel? value))
            //    {
            //        throw new AppException($"Invalid channel [{ch.ChannelNumber}]");
            //    }

            //    value.Enable = enable;
            //    if (!enable)
            //    {
            //        // Kill just in case.
            //        _outputs[ch.DeviceId].Send(new ControlChangeEvent(0, ch.ChannelNumber, MidiController.AllNotesOff, 0));
            //    }
            //};
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

            /////////////////////////////////// OLD //////////////////////////////////////
            // e.ret = -1; // default is invalid

            // // Check args.
            // if (string.IsNullOrEmpty(e.dev_name))
            // {
            //     _loggerScr.Warn($"Invalid input midi device {e.dev_name}");
            //     return;
            // }

            // if (e.chan_num < 1 || e.chan_num > MidiDefs.NUM_CHANNELS)
            // {
            //     _loggerScr.Warn($"Invalid input midi channel {e.chan_num}");
            //     return;
            // }

            // try
            // {
            //     // Locate or create the device.
            //     var input = _inputs.FirstOrDefault(o => o.DeviceName == e.dev_name);
            //     if (input is null)
            //     {
            //         input = new(e.dev_name); // throws if invalid
            //         input.ReceiveEvent += Midi_ReceiveEvent;
            //         _inputs.Add(input);
            //     }

            //     MidiChannel ch = new() { ChannelName = e.chan_name, Enable = true };
            //     input.Channels.Add(e.chan_num, ch);

            //     ChannelHandle chHnd = new(_inputs.Count - 1, e.chan_num, Direction.Input);
            //     e.ret = chHnd;
            // }
            // catch (AppException ex)
            // {
            //     ProcessException(ex);
            // }
        }

        /// <summary>
        /// Script creates an output channel.
        /// </summary>
        /// <param name="_"></param>
        /// <param name="e"></param>
        /// <exception cref="AppException">From called functions</exception>
        void Interop_OpenMidiOutput(object? _, OpenMidiOutputArgs e)
        {

// OpenMidiOutputArgs            
//     property String^ dev_name;
//     /// <summary>Midi channel number 1 => 16</summary>
//     property int chan_num;
//     /// <summary>User channel name</summary>
//     property String^ chan_name;
//     /// <summary>Midi patch number 0 => 127</summary>
//     property int patch;
//     /// <summary>Channel handle or -1 if error</summary>
//     property int ret;


            // Create channels and initialize controls.
            var chan_out = _mgr.OpenMidiOutput(e.dev_name, e.chan_num, e.chan_name, e.patch);
            e.ret = chan_out.Handle;


            /////////////////////////////////// OLD //////////////////////////////////////
            // e.ret = -1; // default is invalid

            // // Check args.
            // if (string.IsNullOrEmpty(e.dev_name))
            // {
            //     _loggerScr.Warn($"Invalid output midi device {e.dev_name ?? "null"}");
            //     return;
            // }

            // if (e.chan_num < 1 || e.chan_num > MidiDefs.NUM_CHANNELS)
            // {
            //     _loggerScr.Warn($"Invalid output midi channel {e.chan_num}");
            //     return;
            // }

            // try
            // {
            //     // Locate or create the device.
            //     var output = _outputs.FirstOrDefault(o => o.DeviceName == e.dev_name);
            //     if (output is null)
            //     {
            //         output = new(e.dev_name); // throws if invalid
            //         _outputs.Add(output);
            //     }

            //     // Add specific channel.
            //     MidiChannel ch = new() { ChannelName = e.chan_name, Enable = true, Patch = e.patch };
            //     output.Channels.Add(e.chan_num, ch);

            //     ChannelHandle chHnd = new(_outputs.Count - 1, e.chan_num, Direction.Output);
            //     e.ret = chHnd;

            //     if (e.patch >= 0)
            //     {
            //         // Send the patch now.
            //         PatchChangeEvent pevt = new(0, e.chan_num, e.patch);
            //         output.Send(pevt);
            //         output.Channels[e.chan_num].Patch = e.patch;
            //     }
            // }
            // catch (AppException ex)
            // {
            //     ProcessException(ex);
            // }
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
            
            ChannelHandle chnd = new(e.chan_hnd);

            var ch = _mgr.GetOutputChannel(chnd);
            if (e.volume == 0.0)
            {
                ch.Device.Send(new NoteOff(chnd.ChannelNumber, e.note_num));
            }
            else
            {
                ch.Device.Send(new NoteOn(chnd.ChannelNumber, e.note_num, (int)MathUtils.Constrain(e.volume * MidiDefs.MAX_MIDI, 0, MidiDefs.MAX_MIDI)));
            }


//            // Check args for valid device and channel.
//            ChannelHandle ch = new(e.chan_hnd);

//            if (ch.DeviceId >= _outputs.Count ||
//                ch.ChannelNumber < 1 ||
//                ch.ChannelNumber > MidiDefs.NUM_CHANNELS)
//            {
//                _loggerScr.Warn($"Invalid channel {e.chan_hnd}");
//                return;
//            }

////            Trace($"+++ Interop_SendMidiNote() [{Thread.CurrentThread.Name}] ({Environment.CurrentManagedThreadId})");

//            // Sound or quiet?
//            var output = _outputs[ch.DeviceId];
//            if (output.Channels[ch.ChannelNumber].Enable)
//            {
//                int note_num = MathUtils.Constrain(e.note_num, MidiDefs.MIN_MIDI, MidiDefs.MAX_MIDI);

//                // Check for note off.
//                var vol = e.volume * State.Instance.Volume;
//                int vel = vol == 0.0 ? 0 : MathUtils.Constrain((int)(vol * MidiDefs.MAX_MIDI), MidiDefs.MIN_MIDI, MidiDefs.MAX_MIDI);
//                MidiEvent evt = vel == 0?
//                    new NoteEvent(0, ch.ChannelNumber, MidiCommandCode.NoteOff, note_num, 0) :
//                    new NoteEvent(0, ch.ChannelNumber, MidiCommandCode.NoteOn, note_num, vel);

//                output.Send(evt);

//                if (UserSettings.Current.MonitorSnd)
//                {
//                    _loggerMidi.Trace($">>> {FormatMidiEvent(evt, CurrentState == ExecState.Run ? State.Instance.CurrentTick : 0, e.chan_hnd)}");
//                }
//            }
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

            ChannelHandle chnd = new(e.chan_hnd);

            var ch = _mgr.GetOutputChannel(chnd);
            var se = new Controller(chnd.ChannelNumber, e.controller, e.value);
            ch.Device.Send(se);



            //// Check args.
            //ChannelHandle ch = new(e.chan_hnd);

            //if (ch.DeviceId >= _outputs.Count ||
            //    ch.ChannelNumber < 1 ||
            //    ch.ChannelNumber > MidiDefs.NUM_CHANNELS)
            //{
            //    _loggerScr.Warn($"Invalid channel {e.chan_hnd}");
            //    return;
            //}

            //int controller = MathUtils.Constrain(e.controller, MidiDefs.MIN_MIDI, MidiDefs.MAX_MIDI);
            //int value = MathUtils.Constrain(e.value, MidiDefs.MIN_MIDI, MidiDefs.MAX_MIDI);

            //var output = _outputs[ch.DeviceId];
            //MidiEvent evt;

            //evt = new ControlChangeEvent(0, ch.ChannelNumber, (MidiController)controller, value);

            //output.Send(evt);

            if (UserSettings.Current.MonitorSnd)
            {
                _loggerMidi.Trace($">>> {FormatMidiEvent(se, CurrentState == ExecState.Run ? State.Instance.CurrentTick : 0, e.chan_hnd)}");
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
                State.Instance.Tempo = e.bpm;
                SetTimer(State.Instance.Tempo);
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
            // int x = sldMasterVolume.Left;
            // int y = sldMasterVolume.Bottom + 10;
            int x = timeBar.Left;
            int y = timeBar.Bottom + 5;

            //List<OutputChannel> channels = [chan_out1, chan_out2];

            //     _mgr.      public OutputChannel GetOutputChannel(ChannelHandle chnd)



            channels.ForEach(chan =>
            {
                var ctrl = new ChannelControl()
                {
                    BoundChannel = chan,
                    UserRenderer = null,
                    Location = new(x, y),
                    BorderStyle = BorderStyle.FixedSingle,
                    ControlColor = UserSettings.Current.ControlColor,
                    SelectedColor = UserSettings.Current.SelectedColor,
                    Volume = Defs.DEFAULT_VOLUME,
                };
                ctrl.ChannelChange += ChannelControl_ChannelChange;
                ctrl.SendMidi += ChannelControl_SendMidi;

                Controls.Add(ctrl);
                x += ctrl.Width + 4; // Width is not valid until after previous statement.
            });





///////////////////// OLD ///////////////////////////////////
///////////////////// OLD ///////////////////////////////////
///////////////////// OLD ///////////////////////////////////

            // // Create channels and controls.
            // int x = timeBar.Left;
            // int y = timeBar.Bottom + 5;

            // List<ChannelHandle> valchs = [];
            // for (int devNum = 0; devNum < _outputs.Count; devNum++)
            // {
            //     var output = _outputs[devNum];
            //     output.Channels.ForEach(ch => { valchs.Add(new(devNum, ch.Key, Direction.Output)); });
            // }

            // valchs.ForEach(ch =>
            // {
            //     ChannelControl control = new(ch)
            //     {
            //         Location = new(x, y),
            //         Info = GetInfo(ch)
            //     };

            //     control.ChannelControlEvent += ChannelControlEvent;
            //     Controls.Add(control);
            //     _channelControls.Add(control);

            //     // Adjust positioning for next iteration.
            //     x += control.Width + 5;
            // });


            // // local func
            // List<string> GetInfo(ChannelHandle ch)
            // {
            //     string devName = "unknown";
            //     string chanName = "unknown";
            //     int patchNum = -1;

            //     if (ch.Direction == Direction.Output)
            //     {
            //         if (ch.DeviceId < _outputs.Count)
            //         {
            //             var dev = _outputs[ch.DeviceId];
            //             devName = dev.DeviceName;
            //             chanName = dev.Channels[ch.ChannelNumber].ChannelName;
            //             patchNum = dev.Channels[ch.ChannelNumber].Patch;
            //         }
            //     }
            //     else
            //     {
            //         if (ch.DeviceId < _inputs.Count)
            //         {
            //             var dev = _inputs[ch.DeviceId];
            //             devName = dev.DeviceName;
            //             chanName = dev.Channels[ch.ChannelNumber].ChannelName;
            //         }
            //     }

            //     List<string> ret = [];
            //     ret.Add($"{(ch.Direction == Direction.Output ? "output: " : "input: ")}:{chanName}");
            //     ret.Add($"device: {devName}");

            //     if (patchNum != -1)
            //     {
            //         // Determine patch name.
            //         string sname;
            //         if (ch.ChannelNumber == MidiDefs.DEFAULT_DRUM_CHANNEL)
            //         {
            //             sname = $"kit: {patchNum}";
            //             if (MidiDefs.DrumKits.TryGetValue(patchNum, out string? kitName))
            //             {
            //                 sname += ($" {kitName}");
            //             }
            //         }
            //         else
            //         {
            //             sname = $"patch: {patchNum}";
            //             if (MidiDefs.Instruments.TryGetValue(patchNum, out string? patchName))
            //             {
            //                 sname += ($" {patchName}");
            //             }
            //         }

            //         ret.Add(sname);
            //     }

            //     return ret;
            // }
        }
        #endregion

        #region Midi Utilities
        /// <summary>
        /// Input from internal non-midi device. Doesn't throw.
        /// </summary>
        void InjectMidiInEvent(string devName, int channel, int noteNum, int velocity)
        {
            var input = _inputDevices.FirstOrDefault(o => o.DeviceName == devName);

            if (input is not null)
            {
                velocity = MathUtils.Constrain(velocity, MidiDefs.MIN_MIDI, MidiDefs.MAX_MIDI);
                NoteEvent nevt = velocity > 0 ?
                    new NoteOnEvent(0, channel, noteNum, velocity, 0) :
                    new NoteEvent(0, channel, MidiCommandCode.NoteOff, noteNum, 0);

//                Trace($"+++ InjectMidiInEvent() [{Thread.CurrentThread.Name}] ({Environment.CurrentManagedThreadId})");
                Midi_ReceiveEvent(input, nevt);
//                Trace($"+++ InjectMidiInEvent() EXIT");
            }
            //else do I care?
        }

        ///// <summary>
        ///// Stop all midi. Doesn't throw.
        ///// </summary>
        //void KillAll()
        //{
        //    _outputs.ForEach(op => op.Channels.ForEach(ch => op.Send(new ControlChangeEvent(0, ch.Key, MidiController.AllNotesOff, 0))));

        //    // Hard reset.
        //    CurrentState = ExecState.Idle;
        //}

        /// <summary>
        /// Create string suitable for logging. Doesn't throw.
        /// </summary>
        /// <param name="evt">Midi event to format.</param>
        /// <param name="tick">Current tick.</param>
        /// <param name="chanHnd">Channel info.</param>
        /// <returns>Suitable string.</returns>
        string FormatMidiEvent(BaseMidiEvent evt, int tick, int chanHnd)
        {
            // Common part.
            ChannelHandle ch = new(chanHnd);

            string s = $"{tick:00000} {MusicTime.Format(tick)} Dev:{ch.DeviceId} Ch:{ch.ChannelNumber} ";

            switch (evt)
            {
                case NoteOn e:
                    var snoteon = ch.ChannelNumber == 10 || ch.ChannelNumber == 16 ?
                        $"DRUM_{e.Note}" :
                        MusicDefinitions.NoteNumberToName(e.Note);
                    s = $"{s} {e.Note}:{snoteon} Vel:{e.Velocity}";
                    break;

                case NoteOff e:
                    var snoteoff = ch.ChannelNumber == 10 || ch.ChannelNumber == 16 ?
                        $"DRUM_{e.Note}" :
                        MusicDefinitions.NoteNumberToName(e.Note);
                    s = $"{s} {e.Note}:{snoteoff}";
                    break;

                case Controller e:
                    var sctl = MidiDefs.TheDefs.GetControllerName(e.ControllerId);
                    s = $"{s} {sctl}:{e.Value}";
                    break;

                default: // Ignore others for now.
                    break;
            }

            return s;
        }

        /// <summary>
        /// Read the lua midi definitions for internal consumption.
        /// </summary>
        //void ReadMidiDefs()
        //{
        //    //var srcDir = MiscUtils.GetSourcePath().Replace("\\", "/");

        //    List<string> s = [
        //        "local mid = require('midi_defs')",
        //        "for _,v in ipairs(mid.gen_list()) do print(v) end"
        //        ];

        //    var (ecode, sres) = ExecuteLuaChunk(s);

        //    if (ecode == 0)
        //    {
        //        foreach (var line in sres.SplitByToken(Environment.NewLine))
        //        {
        //            var parts = line.SplitByToken(",");

        //            switch (parts[0])
        //            {
        //                case "instrument": MidiDefs.Instruments.Add(int.Parse(parts[2]), parts[1]); break;
        //                case "drum": MidiDefs.Drums.Add(int.Parse(parts[2]), parts[1]); break;
        //                case "controller": MidiDefs.Controllers.Add(int.Parse(parts[2]), parts[1]); break;
        //                case "kit": MidiDefs.DrumKits.Add(int.Parse(parts[2]), parts[1]); break;
        //            }
        //        }
        //    }
        //}
        #endregion

        #region Misc Stuff
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
                    traffic.AppendLine("Fatal error - please restart");
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
            var changes = SettingsEditor.Edit(UserSettings.Current, "User Settings", 500);

            // Detect changes of interest.
            bool restart = false;

            foreach (var (name, cat) in changes)
            {
                switch (name)
                {
                    case "ControlColor":
                    case "BackColor":
                    case "IconColor":
                    case "SelectedColor":
                        restart = true;
                        break;

                    case "FileLogLevel":
                        LogManager.MinLevelFile = UserSettings.Current.FileLogLevel;
                        break;

                    case "NotifLogLevel":
                        LogManager.MinLevelNotif = UserSettings.Current.NotifLogLevel;
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
            var devices = DeviceUtils.GetDevicesDoc();

            traffic.AppendLine(devices);
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
                double msec_per_sub = 1000 * sec_per_beat / MusicTime.SUBS_PER_BEAT;
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
