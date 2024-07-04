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


// TODO1 slow startup when running from VS/debugger but not from .exe.
// TODO1 lua require() file edits don't reload?


namespace Nebulua
{
    public partial class MainForm : Form
    {
        #region Fields
        /// <summary>App logger.</summary>
        readonly Logger _logger = LogManager.CreateLogger("App");

        /// <summary>Common functionality.</summary>
        readonly Core _core = new();


        FileSystemWatcher _watcher = new();

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
            // Get the icon associated with the currently executing assembly.
            Icon = Icon.ExtractAssociatedIcon(Assembly.GetExecutingAssembly().Location);

            PopulateFileMenu();

            _watcher.NotifyFilter = NotifyFilters.LastWrite;
            _watcher.Changed += Watcher_Changed;

            #region Init the controls
            timeBar.BackColor = UserSettings.Current.BackColor;
            timeBar.ProgressColor = UserSettings.Current.ControlColor;
            timeBar.MarkerColor = Color.Black;

            chkPlay.Image = GraphicsUtils.ColorizeBitmap((Bitmap)chkPlay.Image!, UserSettings.Current.IconColor);
            chkPlay.BackColor = UserSettings.Current.BackColor;
            chkPlay.FlatAppearance.CheckedBackColor = UserSettings.Current.SelectedColor;
            chkPlay.Click += Play_Click;

            chkLoop.Image = GraphicsUtils.ColorizeBitmap((Bitmap)chkLoop.Image!, UserSettings.Current.IconColor);
            chkLoop.BackColor = UserSettings.Current.BackColor;
            chkLoop.FlatAppearance.CheckedBackColor = UserSettings.Current.SelectedColor;
            chkLoop.Click += (_, __) => State.Instance.DoLoop = chkLoop.Checked;

            chkMonRcv.BackColor = UserSettings.Current.BackColor;
            chkMonRcv.Image = GraphicsUtils.ColorizeBitmap((Bitmap)chkMonRcv.Image!, UserSettings.Current.IconColor);
            chkMonRcv.FlatAppearance.CheckedBackColor = UserSettings.Current.SelectedColor;
            chkMonRcv.Checked = UserSettings.Current.MonitorRcv;
            chkMonRcv.Click += (_, __) => { UserSettings.Current.MonitorRcv = chkMonRcv.Checked; };

            chkMonSnd.BackColor = UserSettings.Current.BackColor;
            chkMonSnd.Image = GraphicsUtils.ColorizeBitmap((Bitmap)chkMonSnd.Image!, UserSettings.Current.IconColor);
            chkMonSnd.FlatAppearance.CheckedBackColor = UserSettings.Current.SelectedColor;
            chkMonSnd.Checked = UserSettings.Current.MonitorSnd;
            chkMonSnd.Click += (_, __) => { UserSettings.Current.MonitorSnd = chkMonSnd.Checked; };

            btnRewind.BackColor = UserSettings.Current.BackColor;
            btnRewind.Image = GraphicsUtils.ColorizeBitmap((Bitmap)btnRewind.Image!, UserSettings.Current.IconColor);
            btnRewind.Click += Rewind_Click;

            btnAbout.BackColor = UserSettings.Current.BackColor;
            btnAbout.Image = GraphicsUtils.ColorizeBitmap((Bitmap)btnAbout.Image!, UserSettings.Current.IconColor);
            btnAbout.Click += About_Click;

            btnKill.BackColor = UserSettings.Current.BackColor;
            btnKill.Image = GraphicsUtils.ColorizeBitmap((Bitmap)btnKill.Image!, UserSettings.Current.IconColor);
            btnKill.Click += (_, __) => { _core.KillAll(); State.Instance.ExecState = ExecState.Idle; };

            btnReload.BackColor = UserSettings.Current.BackColor;
            btnReload.Image = GraphicsUtils.ColorizeBitmap((Bitmap)btnReload.Image!, UserSettings.Current.IconColor);
            btnReload.Click += (_, __) => { _core.LoadScript(); };

            btnSettings.BackColor = UserSettings.Current.BackColor;
            btnSettings.Image = GraphicsUtils.ColorizeBitmap((Bitmap)btnSettings.Image!, UserSettings.Current.IconColor);
            btnSettings.Click += Settings_Click;

            sldVolume.BackColor = UserSettings.Current.BackColor;
            sldVolume.DrawColor = UserSettings.Current.ControlColor;
            sldVolume.ValueChanged += (_, __) => State.Instance.Volume = sldVolume.Value;

            sldTempo.BackColor = UserSettings.Current.BackColor;
            sldTempo.DrawColor = UserSettings.Current.ControlColor;
            sldTempo.ValueChanged += (_, __) => State.Instance.Tempo = (int)sldTempo.Value;

            traffic.BackColor = UserSettings.Current.BackColor;
            traffic.MatchColors.Add("ERR", Color.LightPink);
            traffic.MatchColors.Add("WRN", Color.Plum);
            traffic.MatchColors.Add("SND", Color.Purple);
            traffic.MatchColors.Add("RCV", Color.Green);
            traffic.Font = new("Cascadia Mono", 9);
            traffic.Prompt = "";
            traffic.WordWrap = UserSettings.Current.WordWrap;
            //traffic.Clear();

            ccMidiGen.MinX = 24; // C0
            ccMidiGen.MaxX = 96; // C6
            ccMidiGen.GridX = [12, 24, 36, 48, 60, 72, 84];
            ccMidiGen.MinY = 0; // min velocity == note off
            ccMidiGen.MaxY = 127; // max velocity
            ccMidiGen.GridY = [32, 64, 96];
            ccMidiGen.MouseClickEvent += CcMidiGen_MouseClickEvent;
            ccMidiGen.MouseMoveEvent += CcMidiGen_MouseMoveEvent;

            ddbtnFile.Image = GraphicsUtils.ColorizeBitmap((Bitmap)ddbtnFile.Image!, UserSettings.Current.IconColor);
            ddbtnFile.BackColor = UserSettings.Current.BackColor;
            ddbtnFile.FlatAppearance.CheckedBackColor = UserSettings.Current.SelectedColor;
            ddbtnFile.Enabled = true;
            ddbtnFile.Selected += File_Selected;
            ddbtnFile.Click += File_Reload;
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
            if (UserSettings.Current.OpenLastFile && UserSettings.Current.RecentFiles.Count > 0)
            {
                OpenScriptFile(UserSettings.Current.RecentFiles[0]);
            }


            // try
            // {
            //     // Process cmd line args. Validity checked in LoadScript().
            //     var args = Environment.GetCommandLineArgs();
            //     var scriptFn = args.Length > 1 ? args[1] : null;
            //     _logger.Info(scriptFn is null ? "Reloading script" : $"Loading script file {scriptFn}");

            //     _core.LoadScript(scriptFn);
            //     Text = $"Nebulua {MiscUtils.GetVersionString()} - {scriptFn}";

            //     timeBar.Invalidate();
            // }
            // catch (Exception ex)
            // {
            //     var (fatal, msg) = Utils.ProcessException(ex);
            //     if (fatal)
            //     {
            //         // Logging an error will cause the app to exit.
            //         _logger.Error(msg);
            //     }
            //     else
            //     {
            //         // User can decide what to do with this. They may be recoverable so use warn.
            //         State.Instance.ExecState = ExecState.Idle;
            //         _logger.Warn(msg);
            //     }
            // }

            base.OnLoad(e);
        }

        /// <summary>
        /// Goodbye.
        /// </summary>
        /// <param name="e"></param>
        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            State.Instance.ExecState = ExecState.Idle;

            // Just in case.
            _core.KillAll();

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
            if (disposing)
            {
                components?.Dispose();
                _core.Dispose();
                _watcher.Dispose();
            }
            base.Dispose(disposing);
        }
        #endregion


        #region File handling

        // >>>>>> TODO1 combine reload and file button - disable reload until dirty


        /// <summary>
        /// Create the menu with the recently used files.
        /// </summary>
        void PopulateFileMenu()
        {
            List<string> options = [];
            options.Add("Open...");
            options.Add("");
            UserSettings.Current.RecentFiles.ForEach(f => options.Add(f));
            ddbtnFile.Options = options;
        }

        // /// <summary>
        // /// Update the mru with the user selection.
        // /// </summary>
        // /// <param name="fn">The selected file.</param>
        // void AddToRecentDefs(string fn)
        // {
        //     if (File.Exists(fn))
        //     {
        //         _settings.UpdateMru(fn);
        //         PopulateFileMenu();
        //     }
        // }


        void File_Selected(object? sender, string sel)
        {
            if (sel == "Open...") // navigate to
            {
                string dir = "";
                if (UserSettings.Current.ScriptPath != "")
                {
                    if (Directory.Exists(UserSettings.Current.ScriptPath))
                    {
                        dir = UserSettings.Current.ScriptPath;
                    }
                    else
                    {
                        _logger.Warn("Your script path is invalid, edit your settings");
                    }
                }

                using OpenFileDialog openDlg = new()
                {
                    Filter = "Nebulua files | *.lua",
                    Title = "Select a Nebulua file",
                    InitialDirectory = dir,
                };

                if (openDlg.ShowDialog() == DialogResult.OK)
                {
                    OpenScriptFile(openDlg.FileName);
                }
            }
            else // specific file
            {
                OpenScriptFile(sel);
            }
        }


        void File_Reload(object? sender, EventArgs e)
        {
            OpenScriptFile();
        }


        void Watcher_Changed(object sender, FileSystemEventArgs e)
        {
            // Do something - indicate??
        }

        /// <summary>
        /// Common script file opener.
        /// </summary>
        /// <param name="scriptFn">The script file to open.</param>
        void OpenScriptFile(string? scriptFn = null)
        {
            try
            {
                _logger.Info(scriptFn is null ? "Reloading script" : $"Loading script file {scriptFn}");

                _core.LoadScript(scriptFn);

                // Everything ok.
                if (scriptFn is not null) // new file
                {
                    Text = $"Nebulua {MiscUtils.GetVersionString()} - {scriptFn}";
                    _watcher.Filter = Path.GetFileName(scriptFn);
                    _watcher.Path = scriptFn;
                    _watcher.EnableRaisingEvents = true;
                    // AddToRecentDefs(scriptFn);
                    UserSettings.Current.UpdateMru(scriptFn);
                    PopulateFileMenu();
                }
                else // reload
                {

                }

                timeBar.Invalidate();
            }
            catch (Exception ex)
            {
                var (fatal, msg) = Utils.ProcessException(ex); // >>>>>>>> thread???
                if (fatal)
                {
                    // Logging an error will cause the app to exit.
                    _logger.Error(msg);
                }
                else
                {
                    // User can decide what to do with this. They may be recoverable so use warn.
                    State.Instance.ExecState = ExecState.Idle;
                    _logger.Warn(msg);
                }
            }
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
        /// User clicked something.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void CcMidiGen_MouseClickEvent(object? sender, ClickClack.UserEventArgs e)
        {
            if (e.X is not null && e.Y is not null)
            {
                string name = ((ClickClack)sender!).Name;
                int x = (int)e.X;
                int y = (int)e.Y;
                _core.InjectReceiveEvent(name, 1, x, y < 0 ? 0 : y);
            }
        }

        /// <summary>
        /// Provide tool tip text.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void CcMidiGen_MouseMoveEvent(object? sender, ClickClack.UserEventArgs e)
        {
            e.Text = $"{MusicDefinitions.NoteNumberToName((int)e.X!)} V:{e.Y}";
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
                    traffic.AppendLine("Fatal error - restart");
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
