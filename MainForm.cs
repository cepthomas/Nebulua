using System;
using System.Drawing;
using System.Windows.Forms;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using NAudio.Midi;
using Ephemera.NBagOfTricks;
using Ephemera.NBagOfTricks.Slog;
using Ephemera.NBagOfUis;


namespace Nebulua
{
    public partial class MainForm : Form
    {
        #region Fields
        /// <summary>App logger.</summary>
        readonly Logger _logger = LogManager.CreateLogger("APP");

        /// <summary>Common functionality.</summary>
        readonly Core _core = new();

        /// <summary>Current script. Null means none.</summary>
        string? _scriptFn = null;
        #endregion


        void DoOne(string msg)
        {
            //double msec = 1000.0 * (Stopwatch.GetTimestamp() - _tick) / Stopwatch.Frequency;
            //_logger.Info($"{msec} {msg}");
            //_tick = Stopwatch.GetTimestamp();
        }
        long _tick = Stopwatch.GetTimestamp();



        #region Lifecycle
        /// <summary>
        /// Constructor inits most of the UI.
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

            DoOne("11111");

            // Main window.
            Location = UserSettings.Current.FormGeometry.Location;
            Size = UserSettings.Current.FormGeometry.Size;
            WindowState = FormWindowState.Normal;
            BackColor = UserSettings.Current.BackColor;
            Text = $"Nebulua {MiscUtils.GetVersionString()} - No script loaded";


            DoOne("22222");

            /////////////////////////////////////////////////////////////////////////
            // TODO1 Running in VS is very slow to startup. Not from running the exe.
            // TODO1 Finalize the weird ui init stuff.


            #region Init the controls - simple

            //chkPlay.Click += Play_Click;
            //chkLoop.Click += (_, __) => State.Instance.DoLoop = chkLoop.Checked;
            //chkMonRcv.Checked = UserSettings.Current.MonitorRcv;
            //chkMonRcv.Click += (_, __) => UserSettings.Current.MonitorRcv = chkMonRcv.Checked;
            //chkMonSnd.Checked = UserSettings.Current.MonitorSnd;
            //chkMonSnd.Click += (_, __) => UserSettings.Current.MonitorSnd = chkMonSnd.Checked;
            //btnRewind.Click += Rewind_Click;
            //btnAbout.Click += About_Click;
            //btnKill.Click += (_, __) => { _core!.KillAll(); State.Instance.ExecState = ExecState.Idle; };
            //btnSettings.Click += Settings_Click;
            //sldVolume.ValueChanged += (_, __) => State.Instance.Volume = sldVolume.Value;
            //sldTempo.ValueChanged += (_, __) => State.Instance.Tempo = (int)sldTempo.Value;
            //traffic.MatchText.Add("ERR", Color.HotPink);
            //traffic.MatchText.Add("WRN", Color.Coral);
            //traffic.MatchText.Add("SND", Color.PaleGreen);
            //traffic.MatchText.Add("RCV", Color.LightBlue);
            //traffic.Font = new("Cascadia Mono", 9);
            //traffic.Prompt = "";
            //traffic.WordWrap = UserSettings.Current.WordWrap;
            ////ccMidiGen.Name = "ClickClack";
            ////ccMidiGen.MinX = 24; // C0
            ////ccMidiGen.MaxX = 96; // C6
            ////ccMidiGen.GridX = [12, 24, 36, 48, 60, 72, 84];
            ////ccMidiGen.MinY = 0; // min velocity == note off
            ////ccMidiGen.MaxY = 127; // max velocity
            ////ccMidiGen.GridY = [32, 64, 96];
            //ccMidiGen.MouseClickEvent += CcMidiGen_MouseClickEvent;
            //ccMidiGen.MouseMoveEvent += CcMidiGen_MouseMoveEvent;
            //ddbtnFile.Enabled = true;
            //ddbtnFile.Selected += File_Selected;

            #endregion


            #region Init the controls - moderate

            timeBar.BackColor = UserSettings.Current.BackColor;
            timeBar.ProgressColor = UserSettings.Current.ControlColor;
            timeBar.MarkerColor = Color.Black;

            //chkPlay.Image = GraphicsUtils.ColorizeBitmap((Bitmap)chkPlay.Image!, UserSettings.Current.ForeColor);
            chkPlay.BackColor = UserSettings.Current.BackColor;
            chkPlay.FlatAppearance.CheckedBackColor = UserSettings.Current.SelectedColor;
            chkPlay.Click += Play_Click;

            //chkLoop.Image = GraphicsUtils.ColorizeBitmap((Bitmap)chkLoop.Image!, UserSettings.Current.ForeColor);
            chkLoop.BackColor = UserSettings.Current.BackColor;
            chkLoop.FlatAppearance.CheckedBackColor = UserSettings.Current.SelectedColor;
            chkLoop.Click += (_, __) => State.Instance.DoLoop = chkLoop.Checked;

            chkMonRcv.BackColor = UserSettings.Current.BackColor;
            //chkMonRcv.Image = GraphicsUtils.ColorizeBitmap((Bitmap)chkMonRcv.Image!, UserSettings.Current.ForeColor);
            chkMonRcv.FlatAppearance.CheckedBackColor = UserSettings.Current.SelectedColor;
            chkMonRcv.Checked = UserSettings.Current.MonitorRcv;
            chkMonRcv.Click += (_, __) => UserSettings.Current.MonitorRcv = chkMonRcv.Checked;

            chkMonSnd.BackColor = UserSettings.Current.BackColor;
            //chkMonSnd.Image = GraphicsUtils.ColorizeBitmap((Bitmap)chkMonSnd.Image!, UserSettings.Current.ForeColor);
            chkMonSnd.FlatAppearance.CheckedBackColor = UserSettings.Current.SelectedColor;
            chkMonSnd.Checked = UserSettings.Current.MonitorSnd;
            chkMonSnd.Click += (_, __) => UserSettings.Current.MonitorSnd = chkMonSnd.Checked;

            btnRewind.BackColor = UserSettings.Current.BackColor;
            //btnRewind.Image = GraphicsUtils.ColorizeBitmap((Bitmap)btnRewind.Image!, UserSettings.Current.ForeColor);
            btnRewind.Click += Rewind_Click;

            btnAbout.BackColor = UserSettings.Current.BackColor;
            //btnAbout.Image = GraphicsUtils.ColorizeBitmap((Bitmap)btnAbout.Image!, UserSettings.Current.ForeColor);
            btnAbout.Click += About_Click;

            btnKill.BackColor = UserSettings.Current.BackColor;
            //btnKill.Image = GraphicsUtils.ColorizeBitmap((Bitmap)btnKill.Image!, UserSettings.Current.ForeColor);
            btnKill.Click += (_, __) => { _core!.KillAll(); State.Instance.ExecState = ExecState.Idle; };

            btnSettings.BackColor = UserSettings.Current.BackColor;
            //btnSettings.Image = GraphicsUtils.ColorizeBitmap((Bitmap)btnSettings.Image!, UserSettings.Current.ForeColor);
            btnSettings.Click += Settings_Click;

            sldVolume.BackColor = UserSettings.Current.BackColor;
            sldVolume.DrawColor = UserSettings.Current.ControlColor;
            sldVolume.ValueChanged += (_, __) => State.Instance.Volume = sldVolume.Value;

            sldTempo.BackColor = UserSettings.Current.BackColor;
            sldTempo.DrawColor = UserSettings.Current.ControlColor;
            sldTempo.ValueChanged += (_, __) => State.Instance.Tempo = (int)sldTempo.Value;

            traffic.BackColor = UserSettings.Current.BackColor;
            traffic.MatchText.Add("ERR", Color.HotPink);
            traffic.MatchText.Add("WRN", Color.Coral);
            traffic.MatchText.Add("SND", Color.PaleGreen);
            traffic.MatchText.Add("RCV", Color.LightBlue);
            traffic.Font = new("Cascadia Mono", 9);
            traffic.Prompt = "";
            traffic.WordWrap = UserSettings.Current.WordWrap;

            ccMidiGen.Name = "ccMidiGen";
            ccMidiGen.MinX = 24; // C0
            ccMidiGen.MaxX = 96; // C6
            ccMidiGen.GridX = [12, 24, 36, 48, 60, 72, 84];
            ccMidiGen.MinY = 0; // min velocity == note off
            ccMidiGen.MaxY = 127; // max velocity
            ccMidiGen.GridY = [32, 64, 96];
            ccMidiGen.MouseClickEvent += CcMidiGen_MouseClickEvent;
            ccMidiGen.MouseMoveEvent += CcMidiGen_MouseMoveEvent;

            //ddbtnFile.Image = GraphicsUtils.ColorizeBitmap((Bitmap)ddbtnFile.Image!, UserSettings.Current.ForeColor);
            ddbtnFile.BackColor = UserSettings.Current.BackColor;
            ddbtnFile.FlatAppearance.CheckedBackColor = UserSettings.Current.SelectedColor;
            ddbtnFile.Enabled = true;
            ddbtnFile.Selected += File_Selected;

            #endregion

            #region Init the controls - fancy

            //timeBar.BackColor = UserSettings.Current.BackColor;
            //timeBar.ProgressColor = UserSettings.Current.ControlColor;
            //timeBar.MarkerColor = Color.Black;

            //chkPlay.Image = GraphicsUtils.ColorizeBitmap((Bitmap)chkPlay.Image!, UserSettings.Current.ForeColor);
            //chkPlay.BackColor = UserSettings.Current.BackColor;
            //chkPlay.FlatAppearance.CheckedBackColor = UserSettings.Current.SelectedColor;
            //chkPlay.Click += Play_Click;

            //chkLoop.Image = GraphicsUtils.ColorizeBitmap((Bitmap)chkLoop.Image!, UserSettings.Current.ForeColor);
            //chkLoop.BackColor = UserSettings.Current.BackColor;
            //chkLoop.FlatAppearance.CheckedBackColor = UserSettings.Current.SelectedColor;
            //chkLoop.Click += (_, __) => State.Instance.DoLoop = chkLoop.Checked;

            //chkMonRcv.BackColor = UserSettings.Current.BackColor;
            //chkMonRcv.Image = GraphicsUtils.ColorizeBitmap((Bitmap)chkMonRcv.Image!, UserSettings.Current.ForeColor);
            //chkMonRcv.FlatAppearance.CheckedBackColor = UserSettings.Current.SelectedColor;
            //chkMonRcv.Checked = UserSettings.Current.MonitorRcv;
            //chkMonRcv.Click += (_, __) => UserSettings.Current.MonitorRcv = chkMonRcv.Checked;

            //chkMonSnd.BackColor = UserSettings.Current.BackColor;
            //chkMonSnd.Image = GraphicsUtils.ColorizeBitmap((Bitmap)chkMonSnd.Image!, UserSettings.Current.ForeColor);
            //chkMonSnd.FlatAppearance.CheckedBackColor = UserSettings.Current.SelectedColor;
            //chkMonSnd.Checked = UserSettings.Current.MonitorSnd;
            //chkMonSnd.Click += (_, __) => UserSettings.Current.MonitorSnd = chkMonSnd.Checked;

            //btnRewind.BackColor = UserSettings.Current.BackColor;
            //btnRewind.Image = GraphicsUtils.ColorizeBitmap((Bitmap)btnRewind.Image!, UserSettings.Current.ForeColor);
            //btnRewind.Click += Rewind_Click;

            //btnAbout.BackColor = UserSettings.Current.BackColor;
            //btnAbout.Image = GraphicsUtils.ColorizeBitmap((Bitmap)btnAbout.Image!, UserSettings.Current.ForeColor);
            //btnAbout.Click += About_Click;

            //btnKill.BackColor = UserSettings.Current.BackColor;
            //btnKill.Image = GraphicsUtils.ColorizeBitmap((Bitmap)btnKill.Image!, UserSettings.Current.ForeColor);
            //btnKill.Click += (_, __) => { _core!.KillAll(); State.Instance.ExecState = ExecState.Idle; };

            //btnSettings.BackColor = UserSettings.Current.BackColor;
            //btnSettings.Image = GraphicsUtils.ColorizeBitmap((Bitmap)btnSettings.Image!, UserSettings.Current.ForeColor);
            //btnSettings.Click += Settings_Click;

            //sldVolume.BackColor = UserSettings.Current.BackColor;
            //sldVolume.DrawColor = UserSettings.Current.ControlColor;
            //sldVolume.ValueChanged += (_, __) => State.Instance.Volume = sldVolume.Value;

            //sldTempo.BackColor = UserSettings.Current.BackColor;
            //sldTempo.DrawColor = UserSettings.Current.ControlColor;
            //sldTempo.ValueChanged += (_, __) => State.Instance.Tempo = (int)sldTempo.Value;

            //traffic.BackColor = UserSettings.Current.BackColor;
            //traffic.MatchText.Add("ERR", Color.HotPink);
            //traffic.MatchText.Add("WRN", Color.Coral);
            //traffic.MatchText.Add("SND", Color.PaleGreen);
            //traffic.MatchText.Add("RCV", Color.LightBlue);
            //traffic.Font = new("Cascadia Mono", 9);
            //traffic.Prompt = "";
            //traffic.WordWrap = UserSettings.Current.WordWrap;

            //ccMidiGen.Name = "ccMidiGen";
            //ccMidiGen.MinX = 24; // C0
            //ccMidiGen.MaxX = 96; // C6
            //ccMidiGen.GridX = [12, 24, 36, 48, 60, 72, 84];
            //ccMidiGen.MinY = 0; // min velocity == note off
            //ccMidiGen.MaxY = 127; // max velocity
            //ccMidiGen.GridY = [32, 64, 96];
            //ccMidiGen.MouseClickEvent += CcMidiGen_MouseClickEvent;
            //ccMidiGen.MouseMoveEvent += CcMidiGen_MouseMoveEvent;

            //ddbtnFile.Image = GraphicsUtils.ColorizeBitmap((Bitmap)ddbtnFile.Image!, UserSettings.Current.ForeColor);
            //ddbtnFile.BackColor = UserSettings.Current.BackColor;
            //ddbtnFile.FlatAppearance.CheckedBackColor = UserSettings.Current.SelectedColor;
            //ddbtnFile.Enabled = true;
            //ddbtnFile.Selected += File_Selected;

            #endregion


            ///////////////////////////////////
            btnGo.Click += (_, __) => Console.WriteLine("<<<<<< GOGOGOGO >>>>>");
            DoOne("33333");

            //// Now ready to go live.
            State.Instance.ValueChangeEvent += State_ValueChangeEvent;
        }

        /// <summary>
        /// Inits control appearance. Opens script. Can throw.
        /// </summary>
        /// <param name="e"></param>
        protected override void OnLoad(EventArgs e)
        {
            DoOne("666666");

            PopulateFileMenu();

            if (UserSettings.Current.OpenLastFile && UserSettings.Current.RecentFiles.Count > 0)
            {
                OpenScriptFile(UserSettings.Current.RecentFiles[0]);
            }

            DoOne("77777");

            base.OnLoad(e);
            DoOne("88888");
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
            }
            base.Dispose(disposing);
        }
        #endregion

        #region File handling
        /// <summary>
        /// Create the menu with the recently used files.
        /// </summary>
        void PopulateFileMenu()
        {
            List<string> options = [];
            options.Add("Open...");
            if (_scriptFn is not null)
            {
                options.Add("Reload"); // TODO1 better way?
            }
            if (UserSettings.Current.RecentFiles.Count > 0)
            {
                options.Add("");
                UserSettings.Current.RecentFiles.ForEach(options.Add);
            }
            ddbtnFile.SetOptions(options);
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
                    OpenScriptFile();
                    break;

                default: // specific file
                    OpenScriptFile(fsel);
                    break;
            }
        }

        /// <summary>
        /// Common script file opener or reloader.
        /// </summary>
        /// <param name="scriptFn">The script file to open.</param>
        void OpenScriptFile(string? scriptFn = null)
        {
            try
            {
                if (scriptFn is not null)
                {
                    _scriptFn = scriptFn;
                    _logger.Info($"Loading new script {_scriptFn}");
                }
                else if (_scriptFn is not null)
                {
                    _logger.Info($"Reloading script {_scriptFn}");
                }
                else
                {
                    _logger.Info($"No script loaded");
                }

                if (_scriptFn is not null)
                {
                    _core.LoadScript(_scriptFn); // may throw

                    // Everything ok.
                    //var fn = _core.__scriptFn!;
                    Text = $"Nebulua {MiscUtils.GetVersionString()} - {_scriptFn}";
                    UserSettings.Current.UpdateMru(_scriptFn!);
                }

                PopulateFileMenu();

                timeBar.Invalidate(); // force update
            }
            catch (Exception ex)
            {
                var (fatal, msg) = Utils.ProcessException(ex);
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
                if (chkPlay.Checked)
                {
                    State.Instance.ExecState = ExecState.Run;
                }
                else
                {
                    State.Instance.ExecState = ExecState.Idle;
                    _core.KillAll();

                }
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
                traffic.AppendLine(e.ShortMessage);
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
                    case "ForeColor":
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
        /// The meaning of life. TODOF fights with github flavor a bit.
        /// </summary>
        void About_Click(object? sender, EventArgs e)
        {
            // Consolidate docs.
            var appDir = Environment.CurrentDirectory;
            var files = new List<string>()
            {
                Path.Join(appDir, "README.md"),
                Path.Join(appDir, "docs", "definitions.md"),
                Path.Join(appDir, "docs", "writing_scripts.md"),
                Path.Join(appDir, "docs", "midi_defs.md"),
                Path.Join(appDir, "docs", "music_defs.md"),
                Path.Join(appDir, "docs", "tech_notes.md"),
            };

            List<string> ls = [];
            foreach (var f in files)
            {
                ls.AddRange(File.ReadAllLines(f));
                ls.Add($"");
            }

            // Show them what they have.
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

            var html = Tools.MarkdownToHtml([.. ls], Tools.MarkdownMode.DarkApi, false);
            var docfn = Path.Join(appDir, "doc.html");
            File.WriteAllText(docfn, html);
            new Process { StartInfo = new ProcessStartInfo(docfn) { UseShellExecute = true } }.Start();
        }
        #endregion
    }
}
