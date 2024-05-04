using System;
using System.Drawing;
using System.Windows.Forms;
using System.Collections.Generic;
using System.Reflection;
using System.Diagnostics;
using NAudio.Midi;
using Ephemera.NBagOfTricks;
using Ephemera.NBagOfTricks.Slog;
using Ephemera.NBagOfUis;
using Nebulua.Common;


namespace Nebulua.UiApp
{
    public partial class MainForm : Form
    {
        #region Fields
        /// <summary>App logger.</summary>
        readonly Logger _logger = LogManager.CreateLogger("UiApp");

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
            Debug.WriteLine($"*** MainForm.MainForm() this={this}");

            SetStyle(ControlStyles.OptimizedDoubleBuffer | ControlStyles.SupportsTransparentBackColor, true);
            InitializeComponent();
            KeyPreview = true; // for routing kbd strokes properly
            Location = new Point(100, 100);
            Size = new Size(1200, 600);

            // Gets the icon associated with the currently executing assembly.
            Icon = Icon.ExtractAssociatedIcon(Assembly.GetExecutingAssembly().Location);

            // Hook loog writes.
            LogManager.LogMessage += LogManager_LogMessage;

            // Behavior.
            //timeBar.CurrentTimeChanged += (object? sender, EventArgs e) => { State.Instance.CurrentTick = timeBar.CurrentTick; };
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

            // State change handler.
            State.Instance.ValueChangeEvent += State_ValueChangeEvent;
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
                _core.RunScript(_scriptFn);

                Debug.WriteLine($"*** MainForm.OnLoad() this={this} _core={_core}");

                timeBar.Invalidate();

                Text = $"Nebulua {MiscUtils.GetVersionString()} - {_scriptFn}";
            }
            // Anything that throws is fatal.
            catch (Exception ex)
            {
                State.Instance.ExecState = ExecState.Dead;
                var serr = $"Fatal error in {_scriptFn} - please restart application.{Environment.NewLine}{ex}";
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
            Debug.WriteLine($"*** MainForm.Dispose() this={this} _core={_core} disposing={disposing}");

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
