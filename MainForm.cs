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


// TODO1 kinda slow startup running in debugger.


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
            Idle,
            /// <summary>Script loaded, not running.</summary>
            Stop,
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

        // enum ExecState { Idle, Stop, Run, Dead }
        ExecState _execState = ExecState.Idle;

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

        /// <summary>All midi devices to use for send.</summary>
        readonly List<MidiOutputDevice> _outputDevices = [];

        /// <summary>All midi devices to use for receive.</summary>
        readonly List<MidiInputDevice> _inputDevices = [];

        /// <summary>All channel play controls.</summary>
        readonly List<ChannelControl> _channelControls = [];
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

            // Settings.
            string appDir = MiscUtils.GetAppDataDir("Nebulua", "Ephemera");
            _settings = (UserSettings)SettingsCore.Load(appDir, typeof(UserSettings));
            LogManager.MinLevelFile = _settings.FileLogLevel;
            LogManager.MinLevelNotif = _settings.NotifLogLevel;
            LogManager.LogMessage += LogManager_LogMessage;
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
            btnKill.Click += (_, __) => { MidiManager.Instance.Kill(); UpdateState(ExecState.Idle); };

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
            ddbtnFile.Opening += (_, _) => PopulateFileMenu();
            ddbtnFile.Selected += File_Selected;
            #endregion

            // Hook script callbacks.
            Interop.Log += Interop_Log;
            Interop.OpenMidiInput += Interop_OpenMidiInput;
            Interop.OpenMidiOutput += Interop_OpenMidiOutput;
            Interop.SendMidiNote += Interop_SendMidiNote;
            Interop.SendMidiController += Interop_SendMidiController;
            Interop.SetTempo += Interop_SetTempo;

            MidiManager.Instance.MessageReceived += Mgr_MessageReceived;
            MidiManager.Instance.MessageSent += Mgr_MessageSent;

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

            base.OnLoad(e);
        }

        /// <summary>
        /// Goodbye. Clean up.
        /// </summary>
        /// <param name="e"></param>
        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            UpdateState(ExecState.Idle);

            // Just in case.
            MidiManager.Instance.Kill();

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
            MidiManager.Instance.DestroyChannels();
            MidiManager.Instance.DestroyDevices();

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
                MidiManager.Instance.Kill();
                _mmTimer.Stop();
                DestroyControls();
                UpdateState(ExecState.Idle);
                MidiManager.Instance.DestroyChannels();
                MidiManager.Instance.DestroyDevices();

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

                Text = $"Nebulua {MiscUtils.GetVersionString()} - {_scriptFn}";
                _settings.UpdateMru(_scriptFn!);

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

                timeBar.Invalidate(); // force update
            }
            catch (Exception ex)
            {
                ProcessException(ex);
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
        /// <summary>Handle state change.</summary>
        /// <param name="state">New state</param>
        void UpdateState(ExecState state)
        {
            if (state == _execState) return;

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
                UpdateState(ExecState.Stop);
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

            //this.InvokeIfRequired(_ =>
            //{
            //    if (e.CurrentTimeChange)
            //    {
            //        timeBar.Invalidate();
            //    }
            //});
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
                    UpdateState(_execState == ExecState.Stop ? ExecState.Run : ExecState.Stop);
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
                    else // Just warn w/context.
                    {
                        _loggerApp.Warn(ex.Context);
                        UpdateState(ExecState.Idle);
                    }
                    break;

                case AppException: // from app - generally not fatal
                case MidiLibException: // from lib - generally not fatal
                    // User can decide what to do with this. They may be recoverable so use warn.
                    _loggerApp.Warn(e.Message);
                    _loggerApp.Debug(e.ToString());
                    UpdateState(ExecState.Idle);
                    break;
                    
                default: // other/unknon - assume fatal
                    // Logging an exception will cause the app to stop.
                    _loggerApp.Exception(e);
                    UpdateState(ExecState.Dead);
                    break;
            }
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
                            MidiManager.Instance.Kill(); // just in case
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
                var chnd = HandleOps.Create(indev.Id, e.ChannelNumber, false);
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
            var indev = (MidiOutputDevice)sender!;
            var chnd = HandleOps.Create(indev.Id, e.ChannelNumber, false);
            _loggerMidi.Trace($">>> ! {e}");
        }

        /// <summary>
        /// Process UI change.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void ChannelControlEvent(object? sender, ChannelChangeEventArgs e)
        {
            var channel = (sender as ChannelControl)!.BoundChannel!;

            if (e.State)
            {
                // Update all channels.
                bool anySolo = _channelControls.Where(c => c.State == ChannelState.Solo).Any();

                foreach (var cc in _channelControls)
                {
                    bool enable = anySolo ?
                        cc.State == ChannelState.Solo :
                        cc.State != ChannelState.Mute;

                    channel.Enable = enable;
                    if (!enable)
                    {
                        MidiManager.Instance.Kill(channel);
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
            var chan_in = MidiManager.Instance.OpenInputChannel(e.dev_name, e.chan_num, e.chan_name);
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
            var chan_out = MidiManager.Instance.OpenOutputChannel(e.dev_name, e.chan_num, e.chan_name, e.patch);
            e.ret = chan_out.Handle;
        }

        /// <summary>
        /// Script wants to send a midi note.
        /// </summary>
        /// <param name="_"></param>
        /// <param name="e"></param>
        void Interop_SendMidiNote(object? _, SendMidiNoteArgs e)
        {
            e.ret = 0; // not used

            if (e.note_num is < 0 or > MidiDefs.MAX_MIDI) { throw new ArgumentException($"note_num:{e.note_num}"); }
            var channel = MidiManager.Instance.GetOutputChannel(e.chan_hnd) ?? throw new ArgumentException($"chan_hnd:{e.chan_hnd}");

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
        void Interop_SendMidiController(object? _, SendMidiControllerArgs e)
        {
            e.ret = 0; // not used

            if (e.controller is < 0 or > MidiDefs.MAX_MIDI) { throw new ArgumentException($"controller:{e.controller}"); }
            if (e.value is < 0 or > MidiDefs.MAX_MIDI) { throw new ArgumentException($"value:{e.value}"); }
            var channel = MidiManager.Instance.GetOutputChannel(e.chan_hnd) ?? throw new ArgumentException($"chan_hnd:{e.chan_hnd}");

            var se = new Controller(channel.ChannelNumber, e.controller, e.value);
            channel.Send(se);

            if (_settings.MonitorSnd)
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

        #region Channel Controls
        /// <summary>
        /// Destroy controls.
        /// </summary>
        void DestroyControls()
        {
            MidiManager.Instance.Kill();

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

            MidiManager.Instance.OutputChannels.ForEach(chan =>
            {
                var ctrl = new ChannelControl()
                {
                    BoundChannel = chan,
                    Options = DisplayOptions.SoloMute,
                    Location = new(x, y),
                    BorderStyle = BorderStyle.FixedSingle,
                    DrawColor = _settings.DrawColor,
                    SelectedColor = _settings.SelectedColor,
                    Volume = VolumeDefs.DEFAULT_VOLUME,
                };
                ctrl.ChannelChange += ChannelControl_ChannelChange;

                Controls.Add(ctrl);
                x += ctrl.Width + 4; // Width is not valid until after previous statement.
            });
        }
        #endregion

        #region Events
        /// <summary>
        /// UI clicked something -> configure channel.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void ChannelControl_ChannelChange(object? sender, ChannelChangeEventArgs e)
        {
            var cc = sender as ChannelControl;
            var channel = cc!.BoundChannel!;

            if (e.State)
            {
                // Update all channels.
                bool anySolo = _channelControls.Where(c => c.State == ChannelState.Solo).Any();

                foreach (var cciter in _channelControls)
                {
                    bool enable = anySolo ? cciter.State == ChannelState.Solo : cciter.State != ChannelState.Mute;

                    channel.Enable = enable;
                    if (!enable)
                    {
                        // Kill just in case.
                        MidiManager.Instance.Kill(channel);
                    }
                }
            }
        }
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
                    traffic.AppendLine("Fatal error - please fix then reload or restart");
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
        /// Set timer for this tempo.
        /// </summary>
        /// <param name="tempo"></param>
        void SetTimer(int tempo)
        {
            if (tempo > 0)
            {
                double sec_per_beat = 60.0 / tempo;
                double msec_per_sub = 1000 * sec_per_beat / MusicTime.TicksPerBeat;
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
