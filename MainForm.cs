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
using Ephemera.MidiLib;
using Ephemera.MusicLib;


namespace Nebulua
{
    public partial class MainForm : Form
    {
        #region Types
        /// <summary>Application level error. Above lua level.</summary>
        public class AppException(string message) : Exception(message) { }

        /// <summary>Internal state. </summary>
        enum ExecState
        {
            /// <summary>No script loaded.</summary>
            Empty,
            /// <summary>Script loaded, not running.</summary>
            Idle,
            /// <summary>Script loaded, running.</summary>
            Run,
            /// <summary>Fatal error, not running.</summary>
            Dead
        }
        #endregion

        #region Fields
        /// <summary>App logger.</summary>
        readonly Logger _loggerApp = LogManager.CreateLogger("APP");

        /// <summary>Script logger.</summary>
        readonly Logger _loggerScr = LogManager.CreateLogger("SCR");

        /// <summary>Midi traffic logger.</summary>
        readonly Logger _loggerMidi = LogManager.CreateLogger("MID");

        /// <summary>The current state.</summary>
        ExecState _execState = ExecState.Empty;

        /// <summary>The current settings.</summary>
        UserSettings _settings = new();

        /// <summary>The interop.</summary>
        readonly Interop _interop = new();

        /// <summary>Fast timer.</summary>
        readonly MmTimerEx _mmTimer = new();

        /// <summary>Current script. Null means none.</summary>
        string? _scriptFn = null;

        /// <summary>Test for edited.</summary>
        DateTime _scriptTouch;

        /// <summary>Midi boss.</summary>
        readonly ChannelManager CM;

        /// <summary>Midi boss.</summary>
        readonly InputManager IM;
        #endregion

        #region Lifecycle
        /// <summary>
        /// Constructor inits most of the UI.
        /// </summary>
        public MainForm()
        {
            SetStyle(ControlStyles.OptimizedDoubleBuffer | ControlStyles.SupportsTransparentBackColor, true);
            InitializeComponent();

            Icon = Icon.ExtractAssociatedIcon(Assembly.GetExecutingAssembly().Location);

            KeyPreview = true; // for routing kbd strokes properly

            CM = new(Controls);
            IM = new();

            // Settings.
            string appDir = MiscUtils.GetAppDataDir("Nebulua", "Ephemera");
            _settings = (UserSettings)SettingsCore.Load(appDir, typeof(UserSettings));
            LogManager.MinLevelFile = _settings.FileLogLevel;
            LogManager.MinLevelNotif = _settings.NotifLogLevel;
            LogManager.LogMessage += LogManager_LogMessage;

            // Main window.
            WindowState = FormWindowState.Normal;
            StartPosition = FormStartPosition.Manual;
            Location = _settings.FormGeometry.Location;
            Size = _settings.FormGeometry.Size;
            SetTitle();

            #region Init the controls
            GraphicsUtils.ColorizeControl(chkPlay, _settings.IconColor);
            chkPlay.BackColor = BackColor;
            chkPlay.FlatAppearance.CheckedBackColor = _settings.SelectedColor;
            chkPlay.Click += Play_Click;

            GraphicsUtils.ColorizeControl(chkLoop, _settings.IconColor);
            chkLoop.BackColor = BackColor;
            chkLoop.FlatAppearance.CheckedBackColor = _settings.SelectedColor;
            chkLoop.Click += (_, __) => timeBar.DoLoop = chkLoop.Checked;

            chkMonRecv.BackColor = BackColor;
            GraphicsUtils.ColorizeControl(chkMonRecv, _settings.IconColor);
            chkMonRecv.FlatAppearance.CheckedBackColor = _settings.SelectedColor;
            chkMonRecv.Checked = _settings.MonitorRecv;
            chkMonRecv.Click += (_, __) => _settings.MonitorRecv = chkMonRecv.Checked;

            chkMonSend.BackColor = BackColor;
            GraphicsUtils.ColorizeControl(chkMonSend, _settings.IconColor);
            chkMonSend.FlatAppearance.CheckedBackColor = _settings.SelectedColor;
            chkMonSend.Checked = _settings.MonitorSend;
            chkMonSend.Click += (_, __) => _settings.MonitorSend = chkMonSend.Checked;

            btnRewind.BackColor = BackColor;
            GraphicsUtils.ColorizeControl(btnRewind, _settings.IconColor);
            btnRewind.Click += Rewind_Click;

            btnAbout.BackColor = BackColor;
            GraphicsUtils.ColorizeControl(btnAbout, _settings.IconColor);
            btnAbout.Click += About_Click;

            btnKill.BackColor = BackColor;
            GraphicsUtils.ColorizeControl(btnKill, _settings.IconColor);
            btnKill.Click += (_, __) => { CM.KillAll(); };

            btnSettings.BackColor = BackColor;
            GraphicsUtils.ColorizeControl(btnSettings, _settings.IconColor);
            btnSettings.Click += Settings_Click;

            sldTempo.DrawColor = _settings.DrawColor;
            sldTempo.Minimum = 50;
            sldTempo.Maximum = 200;
            sldTempo.Resolution = 5;
            sldTempo.Value = 100;
            sldTempo.ValueChanged += (_, __) => { SetTimer((int)sldTempo.Value); };
            //sldTempo.Invalidate();

            sldVolume.DrawColor = _settings.DrawColor;
            sldVolume.Minimum = 0.0;
            sldVolume.Maximum = 2.0;
            sldVolume.Resolution = 0.05;
            sldVolume.Value = 0.8;
            //sldVolume.ValueChanged += (_, __) => _volume = sldVolume.Value;
            //sldVolume.Invalidate();

            tvInfo.BackColor = BackColor;
            tvInfo.Font = new("Cascadia Mono", 9);
            tvInfo.Prompt = "";
            tvInfo.WordWrap = _settings.WordWrap;
            tvInfo.Matchers = 
            [
                new("ERR", Color.Red),
                new("WRN", Color.Green),
                new("Send ", Color.PaleGreen),
                new("ReCV ", Color.LightBlue),
            ];

            timeBar.DrawColor = _settings.DrawColor;
            timeBar.SelectedColor = _settings.SelectedColor;
            timeBar.StateChange += TimeBar_StateChangeEvent;

            GraphicsUtils.ColorizeControl(ddbtnFile, _settings.IconColor);
            ddbtnFile.BackColor = BackColor;
            ddbtnFile.FlatAppearance.CheckedBackColor = _settings.SelectedColor;
            ddbtnFile.Enabled = true;
            ddbtnFile.Opening += (_, _) => PopulateFileMenu();
            ddbtnFile.Selected += File_Selected;
            // Init file picker.
            PopulateFileMenu();
            #endregion

            // Hook script callbacks.
            Interop.Log += Interop_Log;
            Interop.OpenInput += Interop_OpenInput;
            Interop.OpenChannel += Interop_OpenChannel;
            Interop.SendNote += Interop_SendNote;
            Interop.SendController += Interop_SendController;
            Interop.SetTempo += Interop_SetTempo;

            IM.MessageReceived += Mgr_MessageReceived;
            //CM.MessageSent += Mgr_MessageSent;

            Thread.CurrentThread.Name = "MAIN";
        }

        /// <summary>
        /// Opens last script. Can throw.
        /// </summary>
        /// <param name="e">Args</param>
        protected override void OnLoad(EventArgs e)
        {
            if (_settings.OpenLastFile && _settings.RecentFiles.Count > 0)
            {
                OpenScriptFile(_settings.RecentFiles[0]);
            }

            MidiDefs.GenUserDeviceInfo().ForEach(l => tvInfo.Append(l));

            base.OnLoad(e);
        }

        /// <summary>
        /// Goodbye. Clean up.
        /// </summary>
        /// <param name="e"></param>
        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            UpdateState(ExecState.Empty);

            // Save user settings.
            _settings.FormGeometry = new()
            {
                X = Location.X,
                Y = Location.Y,
                Width = Width,
                Height = Height
            };
            _settings.WordWrap = tvInfo.WordWrap;
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
            CM.Dispose();
            IM.Dispose();

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
            try
            {
                // Clean up first.
                CM.KillAll();
                _mmTimer.Stop();
                CM.Reset();
                IM.Reset();

                // Determine file to load. Null means reload current.
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

                _settings.UpdateMru(_scriptFn!);

                _interop.RunScript(_scriptFn, luaPath);
                string smeta = _interop.Setup();

                // Get info about the script.
                Dictionary<MusicTime, string> sectInfo = [];
                var chunks = smeta.SplitByToken("|");

                chunks.ForEach(ch =>
                {
                    var elems = ch.SplitByToken(",");
                    sectInfo[new(int.Parse(elems[1]))] = elems[0];
                });

                timeBar.InitSectionInfo(sectInfo);

                // was CreateControls();

                // DestroyControls();

                /// create a control for each channel and bind object
                int x = timeBar.Left;
                int y = timeBar.Bottom + 5;

                // Get all script-defined channels and make corresponding UI element.
                var channels = CM.GetChannels().Select(ch => ch.chan).ToList();

                channels.ForEach(chan =>
                {
                    var ctrl = new ChannelControl()
                    {
                        Info1 = $"M{chan.ChannelNumber} {chan.ChannelName}",
                        Info2 = $"{chan.PatchName} ({chan.Patch}){Environment.NewLine}{chan}",
                        Location = new(x, y),
                        BorderStyle = BorderStyle.FixedSingle,
                        DrawColor = _settings.DrawColor,
                        SelectedColor = _settings.SelectedColor,
                        Volume = VolumeDefs.DEFAULT_VOLUME,
                    };
                    //ctrl.InfoClick += ChannelControl_InfoClick;
                    CM.Bind(chan, ctrl);

                    x += ctrl.Width + 4; // Width is not valid until after previous statement.
                });

                // Start timer.
                sldTempo.Value = 100;
                _mmTimer.Start();

                UpdateState(ExecState.Idle);

                timeBar.Invalidate(); // force update
            }
            catch (Exception ex)
            {
                ProcessException(ex);
            }
            SetTitle();
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
        /// <summary>Handle state change.</summary>
        /// <param name="state">New state</param>
        void UpdateState(ExecState state)
        {
            if (state == _execState) return;

            switch (state)
            {
                case ExecState.Empty:
                    _scriptFn = null;
                    chkPlay.Checked = false;
                    chkPlay.Enabled = false;
                    CM.KillAll();
                    _execState = ExecState.Empty;
                    break;

                case ExecState.Idle:
                    if (_scriptFn is not null)
                    {
                        chkPlay.Checked = false;
                        chkPlay.Enabled = true;
                        CM.KillAll();
                        _execState = ExecState.Idle;
                    }
                    else
                    {
                        chkPlay.Checked = false;
                        chkPlay.Enabled = false;
                        CM.KillAll();
                        _execState = ExecState.Empty;
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
                        CM.KillAll();
                        _execState = ExecState.Empty;
                    }
                    break;

                case ExecState.Dead:
                    chkPlay.Checked = false;
                    chkPlay.Enabled = false;
                    CM.KillAll();
                    _execState = ExecState.Dead;
                    break;
            }

            SetTitle();
        }
        
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

                UpdateState(ExecState.Run);
            }
            else
            {
                UpdateState(ExecState.Idle);
            }
        }

        /// <summary>
        /// Handler for user state changes.
        /// </summary>
        /// <param name="_"></param>
        /// <param name="e">What changed.</param>
        void TimeBar_StateChangeEvent(object? _, TimeBar.StateChangeEventArgs e)
        {
            if (e.CurrentTimeChange)
            {
                timeBar.Invalidate();
            }
        }

        /// <summary>
        /// Rewind.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void Rewind_Click(object? sender, EventArgs e)
        {
            timeBar.Rewind();
        }

        /// <summary>
        /// Do some global key handling. Space bar is used for stop/start playing.
        /// </summary>
        /// <param name="e"></param>
        protected override void OnKeyDown(KeyEventArgs e)
        {
            switch (e.KeyCode)
            {
                case Keys.Space:  // Handle start/stop toggle.
                    switch (_execState)
                    {
                        case ExecState.Idle: UpdateState(ExecState.Run); break;
                        case ExecState.Run: UpdateState(ExecState.Idle); break;
                    }
                    e.Handled = true;
                    break;

                case Keys.Escape:  // Reset timeBar.
                    timeBar.ResetSelection();
                    e.Handled = true;
                    break;

                default:
                    break;
            }

            base.OnKeyDown(e);
        }

        /// <summary>
        /// General exception processor.
        /// </summary>
        /// <param name="e"></param>
        void ProcessException(Exception e)
        {
            switch (e)
            {
                case LuaException ex:
                    if (ex.Error.Contains("FATAL")) // bad lua internal error
                    {
                        // Logging an exception will cause the app to stop.
                        _loggerApp.Exception(e);
                        UpdateState(ExecState.Dead);
                    }
                    else if(ex.Message.StartsWith("COMP")) // Special for composition errors
                    {
                        _loggerApp.Warn(ex.Message.Replace("COMP", "").Trim());
                        UpdateState(ExecState.Empty);
                    }
                    else // Just warn w/context.
                    {
                        if (ex.Error != "") _loggerApp.Warn(ex.Error);
                        if (ex.Context != "") _loggerApp.Warn(ex.Context);
                        UpdateState(ExecState.Empty);
                    }
                    break;

                case AppException: // from app - generally not fatal
                case MidiLibException: // from lib - generally not fatal
                    // User can decide what to do with this. They may be recoverable so use warn.
                    _loggerApp.Warn(e.Message);
                    _loggerApp.Debug(e.ToString());
                    UpdateState(ExecState.Empty);
                    break;
                    
                default: // other/unknon - assume fatal
                    // Logging an exception will cause the app to stop.
                    _loggerApp.Exception(e);
                    UpdateState(ExecState.Dead);
                    break;
            }
            SetTitle();
        }
        #endregion

        #region Callback Handlers
        /// <summary>
        /// Process events.
        /// </summary>
        /// <param name="totalElapsed"></param>
        /// <param name="periodElapsed"></param>
        void MmTimerCallback(double totalElapsed, double periodElapsed)
        {
            if (_execState != ExecState.Run) return;

            this.InvokeIfRequired(_ =>
            {
                // Execute the script step. Note: Need exception handling here to protect from user script errors.
                try
                {
                    // Do script.
                    int ret = _interop.Step(timeBar.Current.Tick);

                    // Bump time and check state.
                    bool done = !timeBar.Increment();

                    // Check for end of play. If free running keep going.
                    if (!timeBar.FreeRunning && done)
                    {
                        if (chkLoop.Checked)
                        {
                            timeBar.Rewind();
                        }
                        else
                        {
                            // Stop and rewind.
                            UpdateState(ExecState.Idle);
                            timeBar.Rewind();
                        }
                    }
                }
                catch (Exception ex)
                {
                    ProcessException(ex);
                }
            });
        }

        /// <summary>
        /// Midi message arrived. Pass along to the script.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void Mgr_MessageReceived(object? sender, BaseEvent e)
        {
            try
            {
                var indev = (MidiInputDevice)sender!;
                //var chnd = HandleOps.Create(indev.Id, e.ChannelNumber, false);
                var chnd = new ChannelHandle(indev.Id, e.ChannelNumber, false);
                bool logit = true;

                switch (e)
                {
                    case NoteOn evt:
                        _interop.ReceiveNote(chnd.Raw, evt.Note, (double)evt.Velocity / MidiDefs.MAX_MIDI);
                        break;

                    case NoteOff evt:
                        _interop.ReceiveNote(chnd.Raw, evt.Note, 0);
                        break;

                    case Controller evt:
                        _interop.ReceiveController(chnd.Raw, evt.Id, evt.Value);
                        break;

                    default: // Ignore others for now.
                        logit = false;
                        break;
                }

                if (logit && _settings.MonitorRecv)
                {
                    _loggerMidi.Trace($"<<< {e}");
                }
            }
            catch (Exception ex)
            {
                ProcessException(ex);
            }
        }

        /// <summary>
        /// Midi message sent. Just for logging.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void Mgr_MessageSent(object? sender, BaseEvent e)
        {
            // Actual sent.
            var outdev = (MidiOutputDevice)sender!;
            var chnd = new ChannelHandle(outdev.Id, e.ChannelNumber, false);
            _loggerMidi.Trace($">>> ! {chnd}");
        }
        #endregion

        #region Script ==> Host Functions
        /// <summary>
        /// Script creates an input channel.
        /// </summary>
        /// <param name="_"></param>
        /// <param name="e"></param>
        /// <exception cref="AppException">From called functions</exception>
        void Interop_OpenInput(object? _, OpenInputArgs e)
        {
            var min = IM.OpenMidiInput(e.dev_name, e.chan_num, e.chan_name);
            //var chnd = min.Handle;
            //_loggerMidi.Trace($">>> ! {chnd}");
        }

        /// <summary>
        /// Script creates an output channel.
        /// </summary>
        /// <param name="_"></param>
        /// <param name="e"></param>
        /// <exception cref="AppException">From called functions</exception>
        void Interop_OpenChannel(object? _, OpenChannelArgs e)
        {
            // Create channels and initialize controls.
            var chan_out = CM.OpenChannel(e.dev_name, e.chan_num, e.chan_name, e.patch, false); //TODO1 last arg is isdrums
            e.ret = chan_out.Handle.Raw;
        }

        /// <summary>
        /// Script wants to send a midi note.
        /// </summary>
        /// <param name="_"></param>
        /// <param name="e"></param>
        void Interop_SendNote(object? _, SendNoteArgs e)
        {
            e.ret = 0; // not used

            if (e.note_num is < 0 or > MidiDefs.MAX_MIDI) { throw new ArgumentException($"note_num:{e.note_num}"); }
            var chnd = new ChannelHandle(e.chan_hnd);
            var channel = CM.GetChannel(chnd.ChannelNumber) ?? throw new ArgumentException($"chan_hnd:{e.chan_hnd}");

            if (e.volume == 0.0)
            {
                channel.Send(new NoteOff(channel.ChannelNumber, e.note_num));
            }
            else
            {
                var vel = (int)MathUtils.Constrain(e.volume * sldVolume.Value * MidiDefs.MAX_MIDI, 0, MidiDefs.MAX_MIDI);
                channel.Send(new NoteOn(channel.ChannelNumber, e.note_num, vel));
            }
        }

        /// <summary>
        /// Script wants to send a midi controller.
        /// </summary>
        /// <param name="_"></param>
        /// <param name="e"></param>
        void Interop_SendController(object? _, SendControllerArgs e)
        {
            e.ret = 0; // not used

            if (e.controller is < 0 or > MidiDefs.MAX_MIDI) { throw new ArgumentException($"controller:{e.controller}"); }
            if (e.value is < 0 or > MidiDefs.MAX_MIDI) { throw new ArgumentException($"value:{e.value}"); }
            var chnd = new ChannelHandle(e.chan_hnd);
            var channel = CM.GetChannel(chnd.ChannelNumber) ?? throw new ArgumentException($"chan_hnd:{e.chan_hnd}");

            var se = new Controller(channel.ChannelNumber, e.controller, e.value);
            channel.Send(se);

            if (_settings.MonitorSend)
            {
                // Intended sent.
                _loggerMidi.Trace($">>> {e}");
            }
        }

        /// <summary>
        /// Script wants to change tempo.
        /// </summary>
        /// <param name="_"></param>
        /// <param name="e"></param>
        void Interop_SetTempo(object? _, SetTempoArgs e)
        {
            e.ret = 0; // not used

            if (e.bpm == 0)
            {
                SetTimer(0);
            }
            else if (e.bpm is < 30 or > 240)
            {
                _loggerScr.Warn($"Invalid tempo {e.bpm}");
            }
            else
            {
                sldTempo.Value = e.bpm;
                SetTimer(e.bpm);
            }
        }

        /// <summary>
        /// Script wants to log something.
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
                case LogLevel.Info:  _loggerScr.Info(s); break;
                case LogLevel.Warn:  _loggerScr.Warn(s); break;
                case LogLevel.Error: _loggerScr.Error(s); break;
            }
        }
        #endregion

        #region Events

        #endregion

        #region Misc Stuff
        /// <summary>
        /// General info.
        /// </summary>
        void SetTitle()
        {
            Text = $"Nebulua {MiscUtils.GetVersionString()} - {_scriptFn ?? "No file loaded"} - <<<{_execState}>>>";
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
                var devs = MidiInputDevice.GetAvailableDevices();
                for (int i = 0; i < NAudio.Midi.MidiIn.NumberOfDevices; i++)
                {
                    var s = NAudio.Midi.MidiIn.DeviceInfo(i).ProductName;
                }

                tvInfo.Append(e.Message);

                if (e.Level == LogLevel.Error)
                {
                    tvInfo.Append("Fatal error - please fix then reload or restart");
                    UpdateState(ExecState.Dead);
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

            // Show them what they have.
            MidiDefs.GenUserDeviceInfo().ForEach(l => tvInfo.Append(l));
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
                double msec_per_sub = 1000 * sec_per_beat / MusicTime.SubbeatsPerBeat;
                double period = msec_per_sub > 1.0 ? msec_per_sub : 1;
                _mmTimer.SetTimer((int)Math.Round(period, 2), MmTimerCallback);
            }
            else // stop
            {
                _mmTimer.SetTimer(0, MmTimerCallback);
            }
        }
        #endregion
    }
}
