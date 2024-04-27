using System;
using System.Drawing;
using System.Windows.Forms;
using NAudio.Midi;
using Ephemera.NBagOfTricks;
using Ephemera.NBagOfTricks.Slog;
using Ephemera.NBagOfUis;
using Nebulua.Common;
using System.Collections.Generic;


namespace Nebulua.UiApp
{
    public partial class MainForm : Form
    {
        #region Fields
        /// <summary>App logger.</summary>
        readonly Logger _logger = LogManager.CreateLogger("Ui");

        /// <summary>Current script.</summary>
        string? _scriptFn = null;

        /// <summary>Common functionality.</summary>
        Core? _core;

        /// <summary>Detect changed script files.</summary>
        readonly MultiFileWatcher _watcher = new();
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

            Location = new Point(100, 100);
            //Size = new Size(_settings.FormGeometry.Width, _settings.FormGeometry.Height);

            // Hook loog writes.
            LogManager.LogMessage += LogManager_LogMessage;

            // Behavior.
            timeBar.CurrentTimeChanged += (object? sender, EventArgs e) => { State.Instance.CurrentTick = timeBar.Current; };
            chkPlay.Click += Play_Click;
            btnRewind.Click += Rewind_Click;
            btnAbout.Click += About_Click;
            btnKill.Click += (_, __) => { State.Instance.ExecState = ExecState.Kill; };
            btnReload.Click += (_, __) => { State.Instance.ExecState = ExecState.Reload; };
;
            chkLoop.Click += (_, __) => State.Instance.DoLoop = chkLoop.Checked;
            chkMonRcv.Click += (_, __) => State.Instance.MonRcv = chkMonRcv.Checked;
            chkMonSnd.Click += (_, __) => State.Instance.MonSnd = chkMonSnd.Checked;
            sldVolume.ValueChanged += (_, __) => State.Instance.Volume = sldVolume.Value;
            sldTempo.ValueChanged += (_, __) => State.Instance.Tempo = (int)sldTempo.Value;

            _watcher.Clear();
            _watcher.FileChange += Watcher_Changed;
        }

        /// <summary>
        /// Inits control appearance. Opens config and script. Can throw.
        /// </summary>
        /// <param name="e"></param>
        protected override void OnLoad(EventArgs e)
        {
            try
            {
                // Process cmd line args.
                string? configFn = null;
                _scriptFn = null;
                var args = StringUtils.SplitByToken(Environment.CommandLine, " ");
                args.RemoveAt(0); // remove the binary name

                foreach (var arg in args)
                {
                    if (arg.EndsWith(".ini"))
                    {
                        configFn = arg;
                    }
                    else if (arg.EndsWith(".lua"))
                    {
                        _scriptFn = arg;
                    }
                    else
                    {
                        throw new ApplicationArgumentException($"Invalid command line: {arg}");
                    }
                }

                if (_scriptFn is null)
                {
                    throw new ApplicationArgumentException($"Missing nebulua script file");
                }

                #region Cosmetics TODO get from config?
                Color _backclr = Color.LightYellow;
                Color _foreclr = Color.DodgerBlue;
                Color _selclr = Color.Moccasin;

                timeBar.BackColor = _backclr;
                timeBar.ProgressColor = _foreclr;
                timeBar.MarkerColor = Color.Black;

                chkPlay.Image = GraphicsUtils.ColorizeBitmap((Bitmap)chkPlay.Image!, _foreclr);
                chkPlay.BackColor = _backclr;
                chkPlay.FlatAppearance.CheckedBackColor = _selclr;

                chkLoop.Image = GraphicsUtils.ColorizeBitmap((Bitmap)chkLoop.Image!, _foreclr);
                chkLoop.BackColor = _backclr;
                chkLoop.FlatAppearance.CheckedBackColor = _selclr;

                chkMonRcv.BackColor = _backclr;
                chkMonRcv.Image = GraphicsUtils.ColorizeBitmap((Bitmap)chkMonRcv.Image!, _foreclr);
                chkMonRcv.FlatAppearance.CheckedBackColor = _selclr;

                chkMonSnd.BackColor = _backclr;
                chkMonSnd.Image = GraphicsUtils.ColorizeBitmap((Bitmap)chkMonSnd.Image!, _foreclr);
                chkMonSnd.FlatAppearance.CheckedBackColor = _selclr;

                btnRewind.BackColor = _backclr;
                btnRewind.Image = GraphicsUtils.ColorizeBitmap((Bitmap)btnRewind.Image!, _foreclr);

                btnAbout.BackColor = _backclr;
                btnAbout.Image = GraphicsUtils.ColorizeBitmap((Bitmap)btnAbout.Image!, _foreclr);

                btnKill.BackColor = _backclr;
                btnKill.Image = GraphicsUtils.ColorizeBitmap((Bitmap)btnKill.Image!, _foreclr);

                btnReload.BackColor = _backclr;
                btnReload.Image = GraphicsUtils.ColorizeBitmap((Bitmap)btnReload.Image!, _foreclr);

                sldVolume.BackColor = _backclr;
                sldVolume.ForeColor = _foreclr;

                sldTempo.BackColor = _backclr;
                sldTempo.ForeColor = _foreclr;

                // Text display.
                traffic.BackColor = _backclr;
                traffic.MatchColors.Add(" SND ", Color.Purple);
                traffic.MatchColors.Add(" RCV ", Color.Green);
                traffic.Font = new("Cascadia Mono", 9);
                traffic.Prompt = "";
                #endregion

                // OK so far. Assemble the engine.
                _core = new Core(configFn);
                _core.Run(_scriptFn);

                // Update file watcher. TODO1 also any required files in script.
                _watcher.Add(_scriptFn);

                Text = $"Nebulua {MiscUtils.GetVersionString()} - {_scriptFn}";
            }
            // Anything that throws is fatal.
            catch (Exception ex)
            {
                State.Instance.ExecState = ExecState.Dead;
                var serr = $"Fatal error - restart application.{Environment.NewLine}{ex}{Environment.NewLine}{ex.StackTrace}";
                traffic.AppendLine(serr);
            }

            base.OnLoad(e);
        }

        /// <summary>
        ///  Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
                _core?.Dispose();
            }
            base.Dispose(disposing);
        }
        #endregion

        #region Run control
        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void Play_Click(object? sender, EventArgs e)
        {
            if (chkPlay.Checked && State.Instance.ExecState == ExecState.Idle)
            {
                State.Instance.ExecState = ExecState.Run;
            }
            else if (!chkPlay.Checked && State.Instance.ExecState == ExecState.Run)
            {
                State.Instance.ExecState = ExecState.Idle;
            }
            // else other?? { Idle, Run, Kill, Exit, Dead }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void Rewind_Click(object? sender, EventArgs e)
        {
            State.Instance.CurrentTick = 0;
            // Current tick may have been corrected for loop.
            timeBar.Current = State.Instance.CurrentTick;
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
                e.Handled = true;
            }
            base.OnKeyDown(e);
        }
        #endregion

        #region Private functions
        /// <summary>
        /// Capture bad events and display them to the user.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void LogManager_LogMessage(object? sender, LogMessageEventArgs e)
        {
            traffic.AppendLine(e.ToString()!);

            if (e.Level == LogLevel.Error)
            {
                traffic.AppendLine("Fatal error, you must restart");
                State.Instance.ExecState = ExecState.Dead;
            }
        }

        /// <summary>
        /// One or more files have changed so reload/compile.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void Watcher_Changed(object? sender, MultiFileWatcher.FileChangeEventArgs e)
        {
            //e.FileNames.ForEach(fn => _logger.Debug($"Watcher_Changed {fn}"));

            //// Kick over to main UI thread.
            //this.InvokeIfRequired(_ =>
            //{
            //    if (_settings.AutoCompile)
            //    {
            //        CompileScript();
            //    }
            //    else
            //    {
            //        SetCompileStatus(false);
            //    }
            //});
        }

        /// <summary>
        /// The meaning of life.
        /// </summary>
        void About_Click(object? sender, EventArgs e)
        {
            List<string> ls = [];

            MiscUtils.ShowReadme("Nebulua");

            ls.Add($"Midi output devices:");
            for (int i = 0; i < MidiOut.NumberOfDevices; i++)
            {
               ls.Add("  " + MidiOut.DeviceInfo(i).ProductName);
            }
            ls.Add($"Midi input devices:");
            for (int i = 0; i < MidiIn.NumberOfDevices; i++)
            {
                ls.Add("  " + MidiIn.DeviceInfo(i).ProductName);
            }

            traffic.AppendLine(string.Join(Environment.NewLine, ls));
        }
        #endregion
    }
}
