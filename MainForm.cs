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
using NAudio.Midi;
using Ephemera.NBagOfTricks;
using Ephemera.NBagOfUis;


// TODO slow startup:
// dur:295.510 tot:295.510 MainForm() enter
// dur:1390.500 tot:1686.010 MainForm() exit
// dur:035.980 tot:1721.990 OnLoad() entry
// dur:284.873 tot:2006.863 OnLoad() exit


namespace Nebulua
{
    public partial class MainForm : Form
    {
        #region Fields
        /// <summary>App logger.</summary>
        readonly Logger _logger = LogManager.CreateLogger("APP");

        /// <summary>Common functionality.</summary>
        readonly HostCore _hostCore = new();

        /// <summary>Current script. Null means none.</summary>
        string? _loadedScriptFn = null;

        /// <summary>Test for edited.</summary>
        DateTime _scriptTouch;

        /// <summary>Diagnostic.</summary>
        readonly TimeIt _tmit = new();

        /// <summary>All the channel play controls.</summary>
        readonly List<ChannelControl> _channelControls = [];
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
            BackColor = UserSettings.Current.BackColor;
            Text = $"Nebulua {MiscUtils.GetVersionString()} - No script loaded";

            #region Init the controls
            chkPlay.Image = ((Bitmap)chkPlay.Image!).Colorize(UserSettings.Current.IconColor);
            chkPlay.BackColor = UserSettings.Current.BackColor;
            chkPlay.FlatAppearance.CheckedBackColor = UserSettings.Current.SelectedColor;
            chkPlay.Click += Play_Click;

            chkLoop.Image = ((Bitmap)(chkLoop.Image!)).Colorize(UserSettings.Current.IconColor);
            chkLoop.BackColor = UserSettings.Current.BackColor;
            chkLoop.FlatAppearance.CheckedBackColor = UserSettings.Current.SelectedColor;
            chkLoop.Click += (_, __) => State.Instance.DoLoop = chkLoop.Checked;

            chkMonRcv.BackColor = UserSettings.Current.BackColor;
            chkMonRcv.Image = ((Bitmap)(chkMonRcv.Image!)).Colorize(UserSettings.Current.IconColor);
            chkMonRcv.FlatAppearance.CheckedBackColor = UserSettings.Current.SelectedColor;
            chkMonRcv.Checked = UserSettings.Current.MonitorRcv;
            chkMonRcv.Click += (_, __) => UserSettings.Current.MonitorRcv = chkMonRcv.Checked;

            chkMonSnd.BackColor = UserSettings.Current.BackColor;
            chkMonSnd.Image = ((Bitmap)(chkMonSnd.Image!)).Colorize(UserSettings.Current.IconColor);
            chkMonSnd.FlatAppearance.CheckedBackColor = UserSettings.Current.SelectedColor;
            chkMonSnd.Checked = UserSettings.Current.MonitorSnd;
            chkMonSnd.Click += (_, __) => UserSettings.Current.MonitorSnd = chkMonSnd.Checked;

            btnRewind.BackColor = UserSettings.Current.BackColor;
            btnRewind.Image = ((Bitmap)(btnRewind.Image!)).Colorize(UserSettings.Current.IconColor);
            btnRewind.Click += Rewind_Click;

            btnAbout.BackColor = UserSettings.Current.BackColor;
            btnAbout.Image = ((Bitmap)(btnAbout.Image!)).Colorize(UserSettings.Current.IconColor);
            btnAbout.Click += About_Click;

            btnKill.BackColor = UserSettings.Current.BackColor;
            btnKill.Image = ((Bitmap)(btnKill.Image!)).Colorize(UserSettings.Current.IconColor);
            btnKill.Click += (_, __) => { _hostCore!.KillAll(); State.Instance.ExecState = ExecState.Idle; };

            btnSettings.BackColor = UserSettings.Current.BackColor;
            btnSettings.Image = ((Bitmap)(btnSettings.Image!)).Colorize(UserSettings.Current.IconColor);
            btnSettings.Click += Settings_Click;

            sldVolume.BackColor = UserSettings.Current.BackColor;
            sldVolume.DrawColor = UserSettings.Current.ActiveColor;
            sldVolume.ValueChanged += (_, __) => State.Instance.Volume = sldVolume.Value;

            sldTempo.BackColor = UserSettings.Current.BackColor;
            sldTempo.DrawColor = UserSettings.Current.ActiveColor;
            sldTempo.ValueChanged += (_, __) => State.Instance.Tempo = (int)sldTempo.Value;

            traffic.BackColor = UserSettings.Current.BackColor;
            traffic.MatchText.Add("ERR ", Color.LightPink);
            traffic.MatchText.Add("WRN ", Color.Yellow);
            traffic.MatchText.Add("SND ", Color.PaleGreen);
            traffic.MatchText.Add("RCV ", Color.LightBlue);
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
            
            ddbtnFile.Image = ((Bitmap)ddbtnFile.Image!).Colorize(UserSettings.Current.IconColor);
            ddbtnFile.BackColor = UserSettings.Current.BackColor;
            ddbtnFile.FlatAppearance.CheckedBackColor = UserSettings.Current.SelectedColor;
            ddbtnFile.Enabled = true;
            ddbtnFile.Selected += File_Selected;
            #endregion

            State.Instance.ValueChangeEvent += State_ValueChangeEvent;

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
            State.Instance.ExecState = ExecState.Idle;

            // Just in case.
            _hostCore.KillAll();

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
                // Wait a bit in case there are some lingering events.
                //System.Threading.Thread.Sleep(100);

                //DestroyDevices();

                components?.Dispose();
                _hostCore.Dispose();
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

            if (_loadedScriptFn is not null)
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
                    _loadedScriptFn = scriptFn;
                    _logger.Info($"Loading new script {_loadedScriptFn}");
                    _scriptTouch = File.GetLastWriteTime(_loadedScriptFn);
                }
                else if (_loadedScriptFn is not null)
                {
                    _logger.Info($"Reloading script {_loadedScriptFn}");
                }
                else
                {
                    _logger.Info($"No script loaded");
                }

                if (_loadedScriptFn is not null)
                {
                    DestroyControls();

                    _hostCore.LoadScript(_loadedScriptFn); // may throw

                    CreateControls();

                    // Everything ok.
                    Text = $"Nebulua {MiscUtils.GetVersionString()} - {_loadedScriptFn}";
                    UserSettings.Current.UpdateMru(_loadedScriptFn!);
                }

                PopulateFileMenu();

                timeBar.Invalidate(); // force update
            }
            catch (Exception ex)
            {
                var (fatal, msg) = Utils.ProcessException(ex);

                if (fatal)
                {
                    // Logging an error will cause the app to stop.
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

        #region UI Event handlers
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
                        //lblState.Text = State.Instance.ExecState.ToString();
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
            if (_loadedScriptFn is null)
            {
                _logger.Warn("No script file loaded");
                return;
            }


            //if (chkPlay.Checked && State.Instance.ExecState == ExecState.Dead)
            //{
            //chkPlay.Checked = false;
            //_logger.Warn("Script is dead");
            //return;
            //}

            switch (State.Instance.ExecState, chkPlay.Checked) // TODO1 fix/test
            {
                case (ExecState.Idle, true):
                    MaybeReload();
                    State.Instance.ExecState = ExecState.Run;
                    break;

                case (ExecState.Idle, false):
                    //
                    break;

                case (ExecState.Run, true):
                    //
                    break;

                case (ExecState.Run, false):
                    State.Instance.ExecState = ExecState.Idle;
                    _hostCore.KillAll();
                    break;

                case (ExecState.Dead, true):

                    break;

                case (ExecState.Dead, false):

                    break;
            };

            void MaybeReload()
            {
                if (UserSettings.Current.AutoReload)
                {
                    var lastTouch = File.GetLastWriteTime(_loadedScriptFn);
                    if (lastTouch > _scriptTouch)
                    {
                        OpenScriptFile();
                    }
                }
            }
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
        /// User clicked something.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void CcMidiGen_MouseClickEvent(object? sender, ClickClack.UserEventArgs e)
        {
            if (e.X is not null && e.Y is not null)
            {
                string name = ((ClickClack)sender!).Name;
                int x = (int)e.X; // note
                int y = (int)e.Y; // velocity
                _hostCore.InjectReceiveEvent(name, 1, x, y);
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
            if (e.KeyCode == Keys.Space && _loadedScriptFn is not null)
            {
                chkPlay.Checked = !chkPlay.Checked;
                Play_Click(null, new());
                e.Handled = true;
            }
            base.OnKeyDown(e);
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
                    case "ActiveColor":
                    case "BackColor":
                    case "IconColor":
                    case "PenColor":
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
            Tools.ShowReadme("Nebulua");

            MidiInfo();
        }

        /// <summary>
        /// Show the builtin definitions and user devices.
        /// </summary>
        void MidiInfo()
        {
            // Consolidate docs.
            List<string> ls = [];

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

            // Generate definitions content.
            var srcDir = MiscUtils.GetSourcePath().Replace("\\", "/");
            var luaPath = $"{srcDir}/LBOT/?.lua;{srcDir}/lua/?.lua;;";

            List<string> s = [
                "local mid = require('midi_defs')",
                "local mus = require('music_defs')",
                "for _,v in ipairs(mid.gen_md()) do print(v) end",
                "for _,v in ipairs(mus.gen_md()) do print(v) end",
                ];

            var (_, sres) = ExecuteLuaChunk(s);

            ls.Add(sres);
            ls.Add($"");

            // Show readme.
            var html = Tools.MarkdownToHtml([.. ls], Tools.MarkdownMode.DarkApi, false);

            // Show midi stuff.
            string docfn = Path.GetTempFileName() + ".html";
            try
            {
                File.WriteAllText(docfn, html);
                var proc = new Process { StartInfo = new ProcessStartInfo(docfn) { UseShellExecute = true } };

                proc.Exited += (_, __) => File.Delete(docfn);
                proc.Start();
            }
            catch
            {
            }
        }
        #endregion

        #region Private stuff
        /// <summary>
        /// Process UI change.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void ChannelControlEvent(object? sender, ChannelControlEventArgs e)
        {
            ChannelControl control = (ChannelControl)sender!;

            // Update all channel enables.
            bool anySolo = _channelControls.Where(c => c.State == PlayState.Solo).Any();

            foreach (var c in _channelControls)
            {
                if (anySolo) // only solo
                {
                    _hostCore.EnableOutputChannel(c.ChHandle, c.State == PlayState.Solo);
                }
                else // except mute
                {
                    _hostCore.EnableOutputChannel(c.ChHandle, c.State != PlayState.Mute);
                }
            }
        }

        /// <summary>
        /// Destroy controls.
        /// </summary>
        void DestroyControls()
        {
            _hostCore.KillAll();

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

            // Create channels and controls.
            int x = timeBar.Left;
            int y = timeBar.Bottom + 5; // 0 + CONTROL_SPACING;

            _hostCore.ValidOutputChannels().ForEach(ch =>
            {
                ChannelControl control = new(ch)
                {
                    Location = new(x, y),
                    Info = _hostCore.GetInfo(ch)
                };

                control.ChannelControlEvent += ChannelControlEvent;
                Controls.Add(control);
                _channelControls.Add(control);

                // Adjust positioning for next iteration.
                x += control.Width + 5;
            });
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
                    traffic.AppendLine("Fatal error - please restart");
                    State.Instance.ExecState = ExecState.Dead;
                }
            });
        }

        /// <summary>
        /// Read the lua midi definitions for internal consumption.
        /// </summary>
        void ReadMidiDefs()
        {
            //var srcDir = MiscUtils.GetSourcePath().Replace("\\", "/");
            //var luaPath = $"{srcDir}/LBOT/?.lua;{srcDir}/lua/?.lua;;";

            List<string> s = [
                "local mid = require('midi_defs')",
                "for _,v in ipairs(mid.gen_list()) do print(v) end"
                ];

            var r = ExecuteLuaChunk(s);

            if (r.ecode == 0)
            {
                foreach (var line in r.sres.SplitByToken(Environment.NewLine))
                {
                    var parts = line.SplitByToken(",");

                    switch (parts[0])
                    {
                        case "instrument": MidiDefs.Instruments.Add(int.Parse(parts[2]), parts[1]); break;
                        case "drum": MidiDefs.Drums.Add(int.Parse(parts[2]), parts[1]); break;
                        case "controller": MidiDefs.Controllers.Add(int.Parse(parts[2]), parts[1]); break;
                        case "kit": MidiDefs.DrumKits.Add(int.Parse(parts[2]), parts[1]); break;
                    }
                }
            }
        }

        /// <summary>
        /// Execute a chunk of lua code. Fixes up lua path and handles errors.
        /// </summary>
        /// <param name="scode"></param>
        /// <returns></returns>
        (int ecode, string sres) ExecuteLuaChunk(List<string> scode)
        {
            var srcDir = MiscUtils.GetSourcePath().Replace("\\", "/");
            var luaPath = $"{srcDir}/LBOT/?.lua;{srcDir}/lua/?.lua;;";
            scode.Insert(0, $"package.path = '{luaPath}' .. package.path");

            var r = Tools.ExecuteLuaCode(string.Join(Environment.NewLine, scode));

            if (r.ecode != 0)
            {
                // Command failed. Capture everything useful.
                List<string> lserr = [];
                lserr.Add($"=== code: {r.ecode}");
                lserr.Add($"=== stderr:");
                lserr.Add($"{r.sret}");

                _logger.Warn(string.Join(Environment.NewLine, lserr));
            }
            return (r.ecode, r.sret);
        }
        #endregion
    }
}
