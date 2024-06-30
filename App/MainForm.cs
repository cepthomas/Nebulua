using System;
using System.Drawing;
using System.Windows.Forms;
using System.Collections.Generic;
using System.Reflection;
using System.Diagnostics;
using System.IO;
using System.Linq;
using NAudio.Midi;
using Ephemera.NBagOfTricks;
using Ephemera.NBagOfTricks.Slog;
using Ephemera.NBagOfUis;


// Curious - slow startup when running from VS/debugger but not from .exe.

// TODO1 update tests. Consolidate other dirs?


namespace Nebulua
{
    public partial class MainForm : Form
    {
        #region Fields
        /// <summary>App logger.</summary>
        readonly Logger _logger = LogManager.CreateLogger("App");

        /// <summary>Current script.</summary>
        string? _scriptFn = null;

        /// <summary>Common functionality.</summary>
        readonly Core _core = new();
        #endregion

        #region Lifecycle
        /// <summary>
        /// Inits control behavior.
        /// </summary>
        public MainForm()
        {
            SetStyle(ControlStyles.OptimizedDoubleBuffer | ControlStyles.SupportsTransparentBackColor, true);
            InitializeComponent();
            KeyPreview = true; // for routing kbd strokes properly

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
            BackColor = UserSettings.Current.BackColor;
            // Gets the icon associated with the currently executing assembly.
            Icon = Icon.ExtractAssociatedIcon(Assembly.GetExecutingAssembly().Location);

            // Misc settings.
            chkMonRcv.Checked = UserSettings.Current.MonitorRcv;
            chkMonSnd.Checked = UserSettings.Current.MonitorSnd;

            #region Cosmetics
            timeBar.BackColor = UserSettings.Current.BackColor;
            timeBar.ProgressColor = UserSettings.Current.ControlColor;
            timeBar.MarkerColor = Color.Black;

            chkPlay.Image = GraphicsUtils.ColorizeBitmap((Bitmap)chkPlay.Image!, UserSettings.Current.IconColor);
            chkPlay.BackColor = UserSettings.Current.BackColor;
            chkPlay.FlatAppearance.CheckedBackColor = UserSettings.Current.SelectedColor;

            chkLoop.Image = GraphicsUtils.ColorizeBitmap((Bitmap)chkLoop.Image!, UserSettings.Current.IconColor);
            chkLoop.BackColor = UserSettings.Current.BackColor;
            chkLoop.FlatAppearance.CheckedBackColor = UserSettings.Current.SelectedColor;

            chkMonRcv.BackColor = UserSettings.Current.BackColor;
            chkMonRcv.Image = GraphicsUtils.ColorizeBitmap((Bitmap)chkMonRcv.Image!, UserSettings.Current.IconColor);
            chkMonRcv.FlatAppearance.CheckedBackColor = UserSettings.Current.SelectedColor;

            chkMonSnd.BackColor = UserSettings.Current.BackColor;
            chkMonSnd.Image = GraphicsUtils.ColorizeBitmap((Bitmap)chkMonSnd.Image!, UserSettings.Current.IconColor);
            chkMonSnd.FlatAppearance.CheckedBackColor = UserSettings.Current.SelectedColor;

            btnRewind.BackColor = UserSettings.Current.BackColor;
            btnRewind.Image = GraphicsUtils.ColorizeBitmap((Bitmap)btnRewind.Image!, UserSettings.Current.IconColor);

            btnAbout.BackColor = UserSettings.Current.BackColor;
            btnAbout.Image = GraphicsUtils.ColorizeBitmap((Bitmap)btnAbout.Image!, UserSettings.Current.IconColor);

            btnKill.BackColor = UserSettings.Current.BackColor;
            btnKill.Image = GraphicsUtils.ColorizeBitmap((Bitmap)btnKill.Image!, UserSettings.Current.IconColor);

            btnReload.BackColor = UserSettings.Current.BackColor;
            btnReload.Image = GraphicsUtils.ColorizeBitmap((Bitmap)btnReload.Image!, UserSettings.Current.IconColor);

            btnSettings.BackColor = UserSettings.Current.BackColor;
            btnSettings.Image = GraphicsUtils.ColorizeBitmap((Bitmap)btnSettings.Image!, UserSettings.Current.IconColor);

            sldVolume.BackColor = UserSettings.Current.BackColor;
            sldVolume.DrawColor = UserSettings.Current.ControlColor;

            sldTempo.BackColor = UserSettings.Current.BackColor;
            sldTempo.DrawColor = UserSettings.Current.ControlColor;
            #endregion

            #region Complex controls
            // Text display.
            traffic.BackColor = UserSettings.Current.BackColor;
            traffic.MatchColors.Add("ERR", Color.LightPink);
            traffic.MatchColors.Add("WRN", Color.Plum);
            traffic.MatchColors.Add(" SND ", Color.Purple);
            traffic.MatchColors.Add(" RCV ", Color.Green);
            traffic.Font = new("Cascadia Mono", 9);
            traffic.Prompt = "";
            traffic.WordWrap = UserSettings.Current.WordWrap;

            // Midi generator.
            ccMidiGen.MinX = 24; // C0
            ccMidiGen.MaxX = 96; // C6
            ccMidiGen.GridX = [12, 24, 36, 48, 60, 72, 84];
            ccMidiGen.MinY = 0; // min velocity == note off
            ccMidiGen.MaxY = 127; // max velocity
            ccMidiGen.GridY = [32, 64, 96];
            ccMidiGen.UserEvent += CcMidiGen_UserEvent;
            #endregion

            #region Control events
            chkPlay.Click += Play_Click;
            btnRewind.Click += Rewind_Click;
            btnAbout.Click += About_Click;
            btnSettings.Click += Settings_Click;
            //btnKill.Click += (_, __) => { State.Instance.ExecState = ExecState.Kill; };
            //btnReload.Click += (_, __) => { State.Instance.ExecState = ExecState.Reload; };
            btnKill.Click += (_, __) => { _core.KillAll(); State.Instance.ExecState = ExecState.Idle; };
            btnReload.Click += (_, __) => { LoadScript(); };
            chkLoop.Click += (_, __) => State.Instance.DoLoop = chkLoop.Checked;
            sldVolume.ValueChanged += (_, __) => State.Instance.Volume = sldVolume.Value;
            sldTempo.ValueChanged += (_, __) => State.Instance.Tempo = (int)sldTempo.Value;
            chkMonRcv.Click += (_, __) => { UserSettings.Current.MonitorRcv = chkMonRcv.Checked; };
            chkMonSnd.Click += (_, __) => { UserSettings.Current.MonitorSnd = chkMonSnd.Checked; };
            #endregion

            // Now ready to go live.
            State.Instance.ValueChangeEvent += State_ValueChangeEvent;
        }

        /// <summary>
        /// Inits control appearance. Opens script. Can throw.
        /// </summary>
        /// <param name="e"></param>
        protected override void OnLoad(EventArgs e)
        {
            try
            {
                _logger.Debug($"MainForm.OnLoad()");

                // Process cmd line args.
                _scriptFn = null;

                var args = Environment.GetCommandLineArgs();
                if (args.Length == 2 && args[1].EndsWith(".lua") && Path.Exists(args[1]))
                {
                    _scriptFn = args[1];
                }
                else
                {
                    throw new ApplicationArgumentException($"Invalid nebulua script file: {args[1]}");
                }

                // OK so far. Assemble the engine.
                Text = $"Nebulua {MiscUtils.GetVersionString()} - {_scriptFn}";
                // _logger.Debug($"MainForm.OnLoad() 1");
                LoadScript();
                //_core.RunScript(_scriptFn);
                // _logger.Debug($"MainForm.OnLoad() 2");

                timeBar.Invalidate();
            }
            catch (Exception ex) // Anything that throws is fatal.
            {
                State.Instance.ExecState = ExecState.Idle;//? Dead;
                string serr = ex switch
                {
                    ApiException exx => $"Api Error: {exx.Message}:{Environment.NewLine}{exx.ApiError}",
                    ConfigException exx => $"Config File Error: {exx.Message}",
                    ScriptSyntaxException exx => $"Script Syntax Error: {exx.Message}",
                    ApplicationArgumentException exx => $"Application Argument Error: {exx.Message}",
                    _ => $"Other error: {ex}{Environment.NewLine}{ex.StackTrace}",
                };

                // TODO1 reload or restart?
                //var serr = $"Fatal error in {_scriptFn} - please restart application.{Environment.NewLine}{ex.Message}";
                //traffic.AppendLine(serr);
                _logger.Error(serr);
            }

            base.OnLoad(e);
            // _logger.Debug($"MainForm.OnLoad() 4");
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="e"></param>
        protected override void OnShown(EventArgs e)
        {
            // _logger.Debug($"MainForm.OnShown()");
            base.OnShown(e);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="e"></param>
        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            State.Instance.ExecState = ExecState.Idle;

            // Just in case.
            _core.KillAll();
 //           State.Instance.ExecState = ExecState.Kill;

            LogManager.Stop();

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

            base.OnFormClosing(e);
        }

        /// <summary>
        ///  Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            //_logger.Debug($"MainForm.Dispose() this={this} _core={_core} disposing={disposing}");

            if (disposing && (components != null))
            {
                components.Dispose();
                _core?.Dispose();
            }
            base.Dispose(disposing);
        }
        #endregion


        void LoadScript()
        {
            try
            {
                _core.RunScript(_scriptFn);
            }
            catch (Exception ex) // Anything that throws is fatal.
            {
                State.Instance.ExecState = ExecState.Idle;//? Dead;
                string serr = ex switch
                {
                    ApiException exx => $"Api Error: {exx.Message}:{Environment.NewLine}{exx.ApiError}",
                    ConfigException exx => $"Config File Error: {exx.Message}",
                    ScriptSyntaxException exx => $"Script Syntax Error: {exx.Message}",
                    ApplicationArgumentException exx => $"Application Argument Error: {exx.Message}",
                    _ => $"Other error: {ex}{Environment.NewLine}{ex.StackTrace}",
                };

                // TODO1 reload or restart?
                //var serr = $"Fatal error in {_scriptFn} - please restart application.{Environment.NewLine}{ex.Message}";
                //traffic.AppendLine(serr);
                _logger.Error(serr);
            }
        }




        #region Event handlers
        /// <summary>
        /// Handler for state changes for ui display.
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
                        break;

                    case "ExecState":
                        lblState.Text = State.Instance.ExecState.ToString();
                        if (State.Instance.ExecState != ExecState.Run)
                        {
                            chkPlay.Checked = false;
                        }
                        break;
                }
            });
        }

        /// <summary>
        /// Update state.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void Play_Click(object? sender, EventArgs e)
        {
            if (State.Instance.ExecState == ExecState.Idle || State.Instance.ExecState == ExecState.Run)
            {
                State.Instance.ExecState = chkPlay.Checked ? ExecState.Run : ExecState.Idle;
            }
            else // something wrong
            {
                //State.Instance.ExecState = ExecState.Dead;
            }
        }

        /// <summary>
        /// Rewind
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
        /// User clicked something.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void CcMidiGen_UserEvent(object? sender, ClickClack.UserEventArgs e)
        {
            //traffic.AppendLine(e.ToString());

            if (e.X is not null && e.Y is not null)
            {
                string name = ((ClickClack)sender!).Name;
                int x = (int)e.X;
                int y = (int)e.Y;
                _core.InjectReceiveEvent(name, 1, x, y < 0 ? 0 : y);
            }
        }

        /// <summary>
        /// Do some global key handling. Space bar is used for stop/start playing.
        /// </summary>
        /// <param name="e"></param>
        protected override void OnKeyDown(KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Space)
            {
                chkPlay.Checked = !chkPlay.Checked;
                Play_Click(null, new());
                e.Handled = true;
            }
            base.OnKeyDown(e);
        }

        /// <summary>
        /// Capture bad events and display them to the user.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void LogManager_LogMessage(object? sender, LogMessageEventArgs e)
        {
            this.InvokeIfRequired(_ =>
            {
                if (e.Level == LogLevel.Error)
                {
                    traffic.AppendLine(e.Message);
                    traffic.AppendLine("Fatal error - you must restart TODO1??");
                    State.Instance.ExecState = ExecState.Dead;
                }
                else
                {
                    traffic.AppendLine($"{e.Message}");
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
                    case "SelectedColor":
                    case "BackColor":
                    case "IconColor":
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
        void About_Click(object? sender, EventArgs e)
        {
            // Consolidate docs.
            List<string> ls = [];
            ls.AddRange(File.ReadAllLines("docs\\README.md"));
            ls.Add($"");
            ls.AddRange(File.ReadAllLines("docs\\midi_defs.md"));
            ls.Add($"");
            ls.AddRange(File.ReadAllLines("docs\\music_defs.md"));
            ls.Add($"");

            ls.Add($"# Your Midi Devices");
            ls.Add($"");
            ls.Add($"## Outputs");
            ls.Add($"");
            for (int i = 0; i < MidiOut.NumberOfDevices; i++)
            {
               ls.Add($"- \"{MidiOut.DeviceInfo(i).ProductName}\"");
            }
            ls.Add($"");
            ls.Add($"## Inputs");
            ls.Add($"");
            for (int i = 0; i < MidiIn.NumberOfDevices; i++)
            {
                ls.Add($"- \"{MidiIn.DeviceInfo(i).ProductName}\"");
            }

            Tools.MarkdownToHtml([.. ls], Tools.MarkdownMode.DarkApi, true);
        }
        #endregion
    }
}
