using System;
using System.Drawing;
using System.Windows.Forms;
using NAudio.Midi;
using Ephemera.NBagOfTricks;
using Ephemera.NBagOfTricks.Slog;
using Ephemera.NBagOfUis;
using Nebulua.Common;
//using Interop;


namespace Nebulua.UiApp
{
    public partial class MainForm : Form
    {
        #region Fields
        /// <summary>App logger.</summary>
        readonly Logger _logger = LogManager.CreateLogger("Ui");

        /// <summary>Current script.</summary>
        readonly string? _scriptFn = null;

        /// <summary>Common functionality.</summary>
        Core _core;

        /// <summary>Detect changed script files.</summary>
        readonly MultiFileWatcher _watcher = new();
        #endregion

        #region Lifecycle
        /// <summary>
        /// Srart here.
        /// </summary>
        public MainForm()
        {
            SetStyle(ControlStyles.OptimizedDoubleBuffer | ControlStyles.SupportsTransparentBackColor, true);
            InitializeComponent();
            KeyPreview = true; // for routing kbd strokes properly

            //Location = new Point(_settings.FormGeometry.X, _settings.FormGeometry.Y);
            //Size = new Size(_settings.FormGeometry.Width, _settings.FormGeometry.Height);

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

                // Cosmetics.
                Color _foreclr = Color.Aqua;
                Color _backclr = Color.Pink;
                Color _selclr = Color.Green;

                timeBar.BackColor = _backclr;
                timeBar.ProgressColor = _foreclr;
                timeBar.MarkerColor = Color.Black;

                cliIn.BackColor = _backclr;

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

                sldVolume.BackColor = _backclr;
                sldVolume.ForeColor = _foreclr;

                sldTempo.BackColor = _backclr;
                sldTempo.ForeColor = _foreclr;

                // Behavior.
                timeBar.CurrentTimeChanged += TimeBar_CurrentTimeChanged;
                chkPlay.Click += Play_Click;
                btnRewind.Click += Rewind_Click;
                btnAbout.Click += About_Click;
                btnKill.Click += Kill_Click;
                chkLoop.Click += (_, __) => State.Instance.DoLoop = chkLoop.Checked;
                chkMonRcv.Click += (_, __) => State.Instance.MonRcv = chkMonRcv.Checked;
                chkMonSnd.Click += (_, __) => State.Instance.MonSnd = chkMonSnd.Checked;
                sldVolume.ValueChanged += (_, __) => State.Instance.Volume = sldVolume.Value;
                sldTempo.ValueChanged += (_, __) => State.Instance.Tempo = (int)sldTempo.Value;
                //sldTempo.Label = "|-|-|";

                traffic.MatchColors.Add(" SND ", Color.Purple);
                traffic.MatchColors.Add(" RCV ", Color.Green);
                traffic.BackColor = _backclr;
                traffic.Font = new("Cascadia Mono", 9);
                traffic.Prompt = "->";

                _watcher.Clear();
                _watcher.FileChange += Watcher_Changed;

                // OK so far.
                _core = new Core(configFn);

                LogManager.LogMessage += LogManager_LogMessage;

                _core.Run(_scriptFn);

                // Update file watcher. TODO1 also any required files in script.
                _watcher.Add(_scriptFn);

                Text = $"Nebulua {MiscUtils.GetVersionString()} - {_scriptFn}";

                // State change handler.
                State.Instance.ValueChangeEvent += State_ValueChangeEvent;

            }
            // Anything that throws is fatal.
            catch (Exception ex)
            {
                FatalError(ex, "App constructor failed.");
            }
        }





        //////// ================ Core stuff custom ===================

        protected override void OnLoad(EventArgs e)
        {
            try
            {
                //KeyPreview = true; // for routing kbd strokes properly
                //_watcher.Clear();
                //_watcher.FileChange += Watcher_Changed;
                //// OK so far.
                //_core = new Core(configFn);
                //LogManager.LogMessage += LogManager_LogMessage;
                //_core.Run(_scriptFn);
                //// Update file watcher. TODO1 also any required files in script.
                //_watcher.Add(_scriptFn);
                //Text = $"Nebulua {MiscUtils.GetVersionString()} - {_scriptFn}";
                //// State change handler.
                //State.Instance.ValueChangeEvent += State_ValueChangeEvent;
            }
            // Anything that throws is fatal. TODO1 generic handling or custom? also log error below.
            catch (Exception ex)
            {
                FatalError(ex, "OnLoad() failed.");
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
                _core.Dispose();
            }
            base.Dispose(disposing);
        }
        #endregion

        /// <summary>
        /// Capture bad events and display them to the user. If error shut down.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void LogManager_LogMessage(object? sender, LogMessageEventArgs e)
        {
            traffic.AppendLine(e.ToString());

            switch (e.Level)
            {
                case LogLevel.Error:
                    traffic.AppendLine(e.Message);
                    // Fatal, shut down.
                    State.Instance.ExecState = ExecState.Exit;
                    break;

                case LogLevel.Warn:
                    traffic.AppendLine(e.Message);
                    break;

                default:
                    // ignore
                    break;
            }
        }



        ///////////////////////// TODO1 run management //////////////////



        /// <summary>
        /// Handler for state changes. Some may originate in this component, others from elsewhere.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void State_ValueChangeEvent(object? sender, string name)
        {
            switch (name)
            {
                case "CurrentTick": // from Core or UI
                    if (sender != this) { } // TODO1 Do state handling with TimeBar_CurrentTimeChanged()
                    break;

                case "ExecState":
                    switch (State.Instance.ExecState)
                    {
                        case ExecState.Idle:
                        case ExecState.Run:
                        case ExecState.Exit:
                            break;

                        case ExecState.Kill:
                            //KillAll();
                            // State.Instance.ExecState = ExecState.Idle;
                            break;
                    }
                    break;
            }
        }









        void _set_position(int position)
        {
            //bool PositionCmd(CommandDescriptor cmd, List<string> args)
            // Limit range maybe.
            int start = State.Instance.LoopStart == -1 ? 0 : State.Instance.LoopStart;
            int end = State.Instance.LoopEnd == -1 ? State.Instance.Length : State.Instance.LoopEnd;

            State.Instance.CurrentTick = MathUtils.Constrain(position, start, end);



        }



        /// <summary>
        /// User has moved the time.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void TimeBar_CurrentTimeChanged(object? sender, EventArgs e)
        {
            // do time mgmt
            //_stepTime = new(barBar.Current.Sub);
            //ProcessPlay(PlayCommand.UpdateUiTime);

            // Stop and rewind.
            traffic.AppendLine("done"); // handle with state change
            State.Instance.ExecState = ExecState.Idle;
        }
        // /// <summary>
        // /// User has changed the time.
        // /// </summary>
        // /// <param name="sender"></param>
        // /// <param name="e"></param>
        // void BarBar_CurrentTimeChanged(object? sender, EventArgs e)
        // {
        //     _stepTime = new(barBar.Current.Sub);
        //     ProcessPlay(PlayCommand.UpdateUiTime);
        // }
        // #endregion



        void Play_Click(object? sender, EventArgs e)
        {

            //         case PlayCommand.Start:
            //             bool ok = !_needCompile || CompileScript();
            //             if (ok)
            //             {
            //                 _startTime = DateTime.Now;
            //                 chkPlay.Checked = true;
            //                 _mmTimer.Start();
            //             }
            //             else
            //             {
            //                 chkPlay.Checked = false;
            //                 ret = false;
            //             }
            //             break;
            //         case PlayCommand.Stop:
            //             chkPlay.Checked = false;
            //             _mmTimer.Stop();
            //             // Send midi stop all notes just in case.
            //             KillAll();
            //             break;


        }
        //bool RunCmd(CommandDescriptor cmd, List<string> args)
        //{
        //    switch (State.Instance.ExecState)
        //    {
        //        case ExecState.Idle:
        //            State.Instance.ExecState = ExecState.Run;
        //            Write("running");
        //            break;

        //        case ExecState.Run:
        //            State.Instance.ExecState = ExecState.Idle;
        //            Write("stopped");
        //            break;

        //        default:
        //            Write("invalid state");
        //            ret = false;
        //            break;
        //    }
        //}
        // #region Play control
        // /// <summary>
        // /// Update UI state per param.
        // /// </summary>
        // /// <param name="cmd">The command.</param>
        // /// <returns>Indication of success.</returns>
        // bool ProcessPlay(PlayCommand cmd)
        // {
        //     bool ret = true;
        //     switch (cmd)
        //     {
        //         case PlayCommand.Start:
        //             bool ok = !_needCompile || CompileScript();
        //             if (ok)
        //             {
        //                 _startTime = DateTime.Now;
        //                 chkPlay.Checked = true;
        //                 _mmTimer.Start();
        //             }
        //             else
        //             {
        //                 chkPlay.Checked = false;
        //                 ret = false;
        //             }
        //             break;
        //         case PlayCommand.Stop:
        //             chkPlay.Checked = false;
        //             _mmTimer.Stop();
        //             // Send midi stop all notes just in case.
        //             KillAll();
        //             break;
        //         case PlayCommand.Rewind:
        //             _stepTime.Reset();
        //             barBar.Current = new();
        //             break;
        //         case PlayCommand.StopRewind:
        //             chkPlay.Checked = false;
        //             _stepTime.Reset();
        //             break;
        //         case PlayCommand.UpdateUiTime:
        //             // See below.
        //             break;
        //     }
        //     // Always do this.
        //     barBar.Current = new(_stepTime.TotalSubs);
        //     return ret;
        // }


        void Kill_Click(object? sender, EventArgs e)
        {

        }

        void Rewind_Click(object? sender, EventArgs e)
        {

        }







        /// <summary>
        /// Do some global key handling. Space bar is used for stop/start playing. TODO1 ?
        /// </summary>
        /// <param name="e"></param>
        protected override void OnKeyDown(KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Space)
            {
                // Handle start/stop toggle.
                //ProcessPlay(chkPlay.Checked ? PlayCommand.Stop : PlayCommand.Start);
                e.Handled = true;
            }
            base.OnKeyDown(e);
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
            //TODO1 MiscUtils.ShowReadme("Nebulator");
            // btnAbout.Click += About_Click;
            // > MiscUtils.ShowReadme("Nebulator");
            // this.showDefinitionsToolStripMenuItem.Click += new System.EventHandler(this.ShowDefinitions_Click);
            // > var docs = MidiDefs.FormatDoc();
            // > docs.AddRange(MusicDefinitions.FormatDoc());
            // > Tools.MarkdownToHtml(docs, Color.LightYellow, new Font("arial", 16), true);

            //bool InfoCmd(CommandDescriptor _, List<string> __)
            //_out.WriteLine($"Midi output devices:");
            //for (int i = 0; i < MidiOut.NumberOfDevices; i++)
            //{
            //    _out.WriteLine("  " + MidiOut.DeviceInfo(i).ProductName);
            //}
            //_out.WriteLine($"Midi input devices:");
            //for (int i = 0; i < MidiIn.NumberOfDevices; i++)
            //{
            //    _out.WriteLine("  " + MidiIn.DeviceInfo(i).ProductName);
            //}

        }

        #region Private functions
        /// <summary>
        /// General purpose handler for fatal errors.
        /// </summary>
        /// <param name="ex">The exception</param>
        /// <param name="info">Extra info</param>
        void FatalError(Exception e, string info)
        {
            string serr;

            switch (e)
            {
                case ApiException ex:
                    serr = $"Api Error: {ex.Message}: {info}{Environment.NewLine}{ex.ApiError}";
                    //// Could remove unnecessary detail for user.
                    //int pos = ex.ApiError.IndexOf("stack traceback");
                    //var s = pos > 0 ? StringUtils.Left(ex.ApiError, pos) : ex.ApiError;
                    //serr = $"Api Error: {ex.Message}{Environment.NewLine}{s}";
                    //// Log the detail.
                    //_logger.Debug($">>>>{ex.ApiError}");
                    break;

                case ConfigException ex:
                    serr = $"Config File Error: {ex.Message}: {info}";
                    break;

                case ScriptSyntaxException ex:
                    serr = $"Script Syntax Error: {ex.Message}: {info}";
                    break;

                case ApplicationArgumentException ex:
                    serr = $"Application Argument Error: {ex.Message}: {info}";
                    break;

                default:
                    serr = $"Other error: {e}{Environment.NewLine}{e.StackTrace}";
                    break;
            }

            // This will cause the app to exit.
            _logger.Error(serr);
        }
        #endregion
    }
}
