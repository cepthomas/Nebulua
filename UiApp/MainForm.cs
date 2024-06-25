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
using Nebulua.Common;


// Curious - slow startup when running from VS/debugger but not from .exe.

// TODO migrate CliApp to UI, maybe nbot.

namespace Nebulua.UiApp
{
    public partial class MainForm : Form
    {
        #region Fields
        /// <summary>App logger.</summary>
        readonly Logger _logger = LogManager.CreateLogger("UiApp");

        /// <summary>App settings.</summary>
        UserSettings _settings;

        /// <summary>Current script.</summary>
        string? _scriptFn = null;

        /// <summary>Common functionality.</summary>
        Core? _core;
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
            _settings = (UserSettings)SettingsCore.Load(appDir, typeof(UserSettings));
            LogManager.LogMessage += LogManager_LogMessage;
            LogManager.MinLevelFile = _settings.FileLogLevel;
            LogManager.MinLevelNotif = _settings.NotifLogLevel;
            LogManager.Run(Path.Combine(appDir, "uilog.txt"), 100000);
            _logger.Debug($"MainForm.MainForm() this={this}");

            Location = _settings.FormGeometry.Location;
            Size = _settings.FormGeometry.Size;
            WindowState = FormWindowState.Normal;
            BackColor = _settings.BackColor;

            // Gets the icon associated with the currently executing assembly.
            Icon = Icon.ExtractAssociatedIcon(Assembly.GetExecutingAssembly().Location);

            // Behavior.
            chkPlay.Click += Play_Click;
            btnRewind.Click += Rewind_Click;
            btnAbout.Click += About_Click;
            btnSettings.Click += Settings_Click;
            btnKill.Click += (_, __) => { State.Instance.ExecState = ExecState.Kill; };
            btnReload.Click += (_, __) => { State.Instance.ExecState = ExecState.Reload; };
            chkLoop.Click += (_, __) => State.Instance.DoLoop = chkLoop.Checked;
            chkMonRcv.Click += (_, __) => State.Instance.MonRcv = chkMonRcv.Checked;
            chkMonSnd.Click += (_, __) => State.Instance.MonSnd = chkMonSnd.Checked;
            sldVolume.ValueChanged += (_, __) => State.Instance.Volume = sldVolume.Value;
            sldTempo.ValueChanged += (_, __) => State.Instance.Tempo = (int)sldTempo.Value;

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
                _logger.Debug($"MainForm.OnLoad() this={this} _core={_core}");

                // Process cmd line args.
                _scriptFn = null;

                var args = Environment.GetCommandLineArgs();
                if (args.Count() == 2 && args[1].EndsWith(".lua") && Path.Exists(args[1]))
                {
                    _scriptFn = args[1];
                }
                else
                {
                    throw new ApplicationArgumentException($"Invalid nebulua script file: {args[1]}");
                }

                #region Cosmetics
                timeBar.BackColor = _settings.BackColor;
                timeBar.ProgressColor = _settings.ControlColor;
                timeBar.MarkerColor = Color.Black;

                chkPlay.Image = GraphicsUtils.ColorizeBitmap((Bitmap)chkPlay.Image!, _settings.ControlColor);
                chkPlay.BackColor = _settings.BackColor;
                chkPlay.FlatAppearance.CheckedBackColor = _settings.SelectedColor;

                chkLoop.Image = GraphicsUtils.ColorizeBitmap((Bitmap)chkLoop.Image!, _settings.ControlColor);
                chkLoop.BackColor = _settings.BackColor;
                chkLoop.FlatAppearance.CheckedBackColor = _settings.SelectedColor;

                chkMonRcv.BackColor = _settings.BackColor;
                chkMonRcv.Image = GraphicsUtils.ColorizeBitmap((Bitmap)chkMonRcv.Image!, _settings.ControlColor);
                chkMonRcv.FlatAppearance.CheckedBackColor = _settings.SelectedColor;

                chkMonSnd.BackColor = _settings.BackColor;
                chkMonSnd.Image = GraphicsUtils.ColorizeBitmap((Bitmap)chkMonSnd.Image!, _settings.ControlColor);
                chkMonSnd.FlatAppearance.CheckedBackColor = _settings.SelectedColor;

                btnRewind.BackColor = _settings.BackColor;
                btnRewind.Image = GraphicsUtils.ColorizeBitmap((Bitmap)btnRewind.Image!, _settings.ControlColor);

                btnAbout.BackColor = _settings.BackColor;
                btnAbout.Image = GraphicsUtils.ColorizeBitmap((Bitmap)btnAbout.Image!, _settings.ControlColor);

                btnKill.BackColor = _settings.BackColor;
                btnKill.Image = GraphicsUtils.ColorizeBitmap((Bitmap)btnKill.Image!, _settings.ControlColor);

                btnReload.BackColor = _settings.BackColor;
                btnReload.Image = GraphicsUtils.ColorizeBitmap((Bitmap)btnReload.Image!, _settings.ControlColor);

                btnSettings.BackColor = _settings.BackColor;
                btnSettings.Image = GraphicsUtils.ColorizeBitmap((Bitmap)btnSettings.Image!, _settings.ControlColor);

                sldVolume.BackColor = _settings.BackColor;
                sldVolume.DrawColor = _settings.ControlColor;

                sldTempo.BackColor = _settings.BackColor;
                sldTempo.DrawColor = _settings.ControlColor;

                // Text display.
                traffic.BackColor = _settings.BackColor;
                traffic.MatchColors.Add("ERR", Color.LightPink);
                traffic.MatchColors.Add("WRN", Color.Plum);
                traffic.MatchColors.Add(" SND ", Color.Purple);
                traffic.MatchColors.Add(" RCV ", Color.Green);
                traffic.Font = new("Cascadia Mono", 9);
                traffic.Prompt = "";
                traffic.WordWrap = _settings.WordWrap;
                #endregion

                // OK so far. Assemble the engine.
                _logger.Debug($"MainForm.OnLoad() 1");
                _core = new Core();
                _logger.Debug($"MainForm.OnLoad() 2");
                _core.RunScript(_scriptFn);
                _logger.Debug($"MainForm.OnLoad() 3");

                timeBar.Invalidate();

                Text = $"Nebulua {MiscUtils.GetVersionString()} - {_scriptFn}";
            }
            catch (Exception ex) // Anything that throws is fatal.
            {
                State.Instance.ExecState = ExecState.Dead;
                var serr = $"Fatal error in {_scriptFn} - please restart application.{Environment.NewLine}{ex.Message}";
                traffic.AppendLine(serr);
            }

            base.OnLoad(e);
            _logger.Debug($"MainForm.OnLoad() 4");
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="e"></param>
        protected override void OnShown(EventArgs e)
        {
            _logger.Debug($"MainForm.OnShown()");
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
            State.Instance.ExecState = ExecState.Kill;

            LogManager.Stop();

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

            base.OnFormClosing(e);
        }

        /// <summary>
        ///  Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            _logger.Debug($"MainForm.Dispose() this={this} _core={_core} disposing={disposing}");

            if (disposing && (components != null))
            {
                components.Dispose();
                _core?.Dispose();
            }
            base.Dispose(disposing);
        }
        #endregion

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
                traffic.AppendLine(e.Message);

                if (e.Level == LogLevel.Error)
                {
                    traffic.AppendLine("Fatal error, you must restart");
                    State.Instance.ExecState = ExecState.Dead;
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
                    case "ControlColor":
                    case "SelectedColor":
                    case "BackColor":
                    case "IconColor":
                        restart = true;
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
               ls.Add("- " + MidiOut.DeviceInfo(i).ProductName);
            }
            ls.Add($"");
            ls.Add($"## Inputs");
            ls.Add($"");
            for (int i = 0; i < MidiIn.NumberOfDevices; i++)
            {
                ls.Add("- " + MidiIn.DeviceInfo(i).ProductName);
            }

            Tools.MarkdownToHtml([.. ls], Tools.MarkdownMode.DarkApi, true);
        }
        #endregion
    }
}
