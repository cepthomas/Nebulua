using System;
using System.Drawing;
using System.Windows.Forms;
using NAudio.Midi;
using Ephemera.NBagOfTricks;
using Ephemera.NBagOfTricks.Slog;
using Ephemera.NBagOfUis;
using Interop;


namespace Ephemera.Nebulua.UiApp
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



/*  command handlers TODO1

        bool TempoCmd(CommandDescriptor cmd, List<string> args)
        if (int.TryParse(args[1], out int t) && t >= 40 && t <= 240)
        {
            State.Instance.Tempo = t;
            Write("");
        }
        else
        {
            ret = false;
            Write($"invalid tempo: {args[1]}");
        }



        bool RunCmd(CommandDescriptor cmd, List<string> args)
        {
            switch (State.Instance.ExecState)
            {
                case ExecState.Idle:
                    State.Instance.ExecState = ExecState.Run;
                    Write("running");
                    break;

                case ExecState.Run:
                    State.Instance.ExecState = ExecState.Idle;
                    Write("stopped");
                    break;

                default:
                    Write("invalid state");
                    ret = false;
                    break;
            }
        }


        bool PositionCmd(CommandDescriptor cmd, List<string> args)
        // Limit range maybe.
        int start = State.Instance.LoopStart == -1 ? 0 : State.Instance.LoopStart;
        int end = State.Instance.LoopEnd == -1 ? State.Instance.Length : State.Instance.LoopEnd;
        State.Instance.CurrentTick = MathUtils.Constrain(position, start, end);



        bool MonCmd(CommandDescriptor cmd, List<string> args)
        case "r":
            State.Instance.MonRcv = !State.Instance.MonRcv;

        case "s":
            State.Instance.MonSnd = !State.Instance.MonSnd;

        case "o":
            State.Instance.MonRcv = false;
            State.Instance.MonSnd = false;



        bool ReloadCmd(CommandDescriptor cmd, List<string> args)
        // TODO1 do something to reload script without exiting app. App detect/indicate changed file? see _watcher.
        // - https://stackoverflow.com/questions/2812071/what-is-a-way-to-reload-lua-scripts-during-run-time
        // - https://stackoverflow.com/questions/9369318/hot-swap-code-in-lua



        bool InfoCmd(CommandDescriptor _, List<string> __)
        _out.WriteLine($"Midi output devices:");
        for (int i = 0; i < MidiOut.NumberOfDevices; i++)
        {
            _out.WriteLine("  " + MidiOut.DeviceInfo(i).ProductName);
        }
        _out.WriteLine($"Midi input devices:");
        for (int i = 0; i < MidiIn.NumberOfDevices; i++)
        {
            _out.WriteLine("  " + MidiIn.DeviceInfo(i).ProductName);
        }
*/

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
                Color _fclr = Color.Aqua;
                Color _bclr = Color.Pink;

                timeBar.BackColor = _bclr;
                timeBar.ProgressColor = _fclr;
                timeBar.MarkerColor = Color.Black;

                cliIn.BackColor = _bclr;

                // btnMonIn.Image = GraphicsUtils.ColorizeBitmap((Bitmap)btnMonIn.Image, _settings.IconColor);
                chkPlay.Image = GraphicsUtils.ColorizeBitmap((Bitmap)chkPlay.Image, _bclr);
                //chkPlay.BackColor = _bclr;
                chkPlay.ForeColor = _fclr;

                btnRewind.BackColor = _bclr;
                btnRewind.ForeColor = _fclr;

                btnAbout.BackColor = _bclr;
                btnAbout.ForeColor = _fclr;

                btnMonRcv.BackColor = _bclr;
                btnMonRcv.ForeColor = _fclr;

                btnMonSnd.BackColor = _bclr;
                btnMonSnd.ForeColor = _fclr;

                btnKill.BackColor = _bclr;
                btnKill.ForeColor = _fclr;

                sldVolume.BackColor = _bclr;
                sldVolume.ForeColor = _fclr;

                sldTempo.BackColor = _bclr;
                sldTempo.ForeColor = _fclr;

                // Behavior.
                timeBar.CurrentTimeChanged += TimeBar_CurrentTimeChanged;

                traffic.MatchColors.Add(" SND ", Color.Purple);
                traffic.MatchColors.Add(" RCV ", Color.Green);
                traffic.BackColor = _bclr;
                traffic.Font = new("Cascadia Mono", 9);
                traffic.Prompt = "->";

                // sliders like:
                //slider1.ValueChanged += (_, __) => Tell($"Slider value: {slider1.Value}");
                //slider1.Minimum = 0;
                //slider1.Maximum = 100;
                //slider1.Resolution = 5;
                //slider1.Value = 40;
                //slider1.Label = "|-|-|";
                // sldVolume.ValueChanged += new System.EventHandler(this.Volume_ValueChanged);
                // this.sldTempo.ValueChanged += new System.EventHandler(this.Speed_ValueChanged); > SetFastTimerPeriod();

                // btnMonIn.Click += Monitor_Click;
                // btnMonOut.Click += Monitor_Click;

                // ProcessPlay(PlayCommand cmd):
                // this.btnRewind.Click += new System.EventHandler(this.Rewind_Click);  
                // this.chkPlay.Click += new System.EventHandler(this.Play_Click);  ProcessPlay(PlayCommand cmd)
                // void BarBar_CurrentTimeChanged(object? sender, EventArgs e)

                // btnAbout.Click += About_Click;
                // > MiscUtils.ShowReadme("Nebulator");
                // this.showDefinitionsToolStripMenuItem.Click += new System.EventHandler(this.ShowDefinitions_Click);
                // > var docs = MidiDefs.FormatDoc();
                // > docs.AddRange(MusicDefinitions.FormatDoc());
                // > Tools.MarkdownToHtml(docs, Color.LightYellow, new Font("arial", 16), true);

                // btnKillComm.Click += (object? _, EventArgs __) => { KillAll(); };


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
                State.Instance.PropertyChangeEvent += State_PropertyChangeEvent;

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
                //State.Instance.PropertyChangeEvent += State_PropertyChangeEvent;
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

        /// <summary>
        /// Handler for state changes. Some may originate in this component, others from elsewhere.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void State_PropertyChangeEvent(object? sender, string name)
        {
            switch (name)
            {
                case "CurrentTick":
                    if (sender != this) { } // TODO1 Do state handling with TimeBar_CurrentTimeChanged()
                    break;

                case "ScriptState":
                    switch (State.Instance.ExecState)
                    {
                        case ExecState.Idle:
                        case ExecState.Run:
                        case ExecState.Exit:
                            break;

                        case ExecState.Kill:
                            //KillAll();
                            State.Instance.ExecState = ExecState.Idle;
                            break;
                    }
                    break;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void TimeBar_CurrentTimeChanged(object? sender, EventArgs e)
        {
            // TODO1 do time mgmt
            //_stepTime = new(barBar.Current.Sub);
            //ProcessPlay(PlayCommand.UpdateUiTime);

            // Stop and rewind.
            traffic.AppendLine("done"); // handle with state change
            State.Instance.ExecState = ExecState.Idle;
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
