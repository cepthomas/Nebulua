using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using System.IO;
using System.Diagnostics;
using NAudio.Midi;
using Ephemera.NBagOfTricks;
using Ephemera.NBagOfTricks.Slog;
using Ephemera.NBagOfUis;
using Ephemera.MidiLib;
//using static Ephemera.Nebulua.Common;

// https://www.codeproject.com/Articles/1204629/DryWetMIDI-Notes-Quantization


namespace Ephemera.Nebulua
{
    public partial class MainForm : Form
    {
        #region Types
        /// <summary>Internal status.</summary>
        enum PlayCommand { Start, Stop, Rewind, StopRewind, UpdateUiTime }
        #endregion

        #region Fields
        /// <summary>App logger.</summary>
        readonly Logger _logger = LogManager.CreateLogger("Main");

        /// <summary>App settings.</summary>
        readonly UserSettings _settings;

        /// <summary>Fast timer.</summary>
        readonly MmTimerEx _mmTimer = new();

        /// <summary>Current np file name.</summary>
        string _scriptFileName = "";

        /// <summary>The current script.</summary>
        readonly Script _script = new();

        /// <summary>All the channel UI controls.</summary>
        readonly List<ChannelControl> _channelControls = new();

        /// <summary>Longest length of channels in subbeats.</summary>
        int _totalSubbeats = 0;

        /// <summary>Persisted internal values for current script file.</summary>
        Bag _nppVals = new();

        /// <summary>Current step time clock.</summary>
        BarTime _stepTime = new();

        /// <summary>Real time.</summary>
        readonly Stopwatch _sw = new();

        /// <summary>Real time.</summary>
        long _startTicks = 0;

        /// <summary>Hack for dirty shutdown issue.</summary>
        bool _shuttingDown = false;

        ///// <summary>Diagnostics for timing measurement.</summary>
        //TimingAnalyzer _tan = new TimingAnalyzer() { SampleSize = 100 };
        #endregion

        #region Lifecycle
        /// <summary>
        /// Constructor.
        /// </summary>
        public MainForm()
        {
            // Must do this first before initializing.
            string appDir = MiscUtils.GetAppDataDir("Nebulua", "Ephemera");
            _settings = (UserSettings)SettingsCore.Load(appDir, typeof(UserSettings));
            MidiSettings.LibSettings = _settings.MidiSettings;
            // Force the resolution for this application.
            MidiSettings.LibSettings.InternalPPQ = BarTime.LOW_RES_PPQ;

            InitializeComponent();

            // Init logging.
            string logFileName = Path.Combine(appDir, "log.txt");
            LogManager.MinLevelFile = _settings.FileLogLevel;
            LogManager.MinLevelNotif = _settings.NotifLogLevel;
            LogManager.LogMessage += LogManager_LogMessage;
            LogManager.Run(logFileName, 100000);

            #region Init UI from settings
            toolStrip1.Renderer = new NBagOfUis.CheckBoxRenderer() { SelectedColor = _settings.SelectedColor };

            // Main form.
            Location = _settings.FormGeometry.Location;
            Size = _settings.FormGeometry.Size;
            WindowState = FormWindowState.Normal;
            BackColor = _settings.BackColor;

            // The rest of the controls.
            textViewer.WordWrap = false;
            textViewer.BackColor = _settings.BackColor;
            textViewer.MatchColors.Add("ERR", Color.LightPink);
            textViewer.MatchColors.Add("WRN", Color.Plum);
            textViewer.Prompt = "> ";

            btnMonIn.Image = GraphicsUtils.ColorizeBitmap((Bitmap)btnMonIn.Image, _settings.IconColor);
            btnMonOut.Image = GraphicsUtils.ColorizeBitmap((Bitmap)btnMonOut.Image, _settings.IconColor);
            btnKillComm.Image = GraphicsUtils.ColorizeBitmap((Bitmap)btnKillComm.Image, _settings.IconColor);
            fileDropDownButton.Image = GraphicsUtils.ColorizeBitmap((Bitmap)fileDropDownButton.Image, _settings.IconColor);
            btnRewind.Image = GraphicsUtils.ColorizeBitmap((Bitmap)btnRewind.Image, _settings.IconColor);
            btnCompile.Image = GraphicsUtils.ColorizeBitmap((Bitmap)btnCompile.Image, _settings.IconColor);
            btnAbout.Image = GraphicsUtils.ColorizeBitmap((Bitmap)btnAbout.Image, _settings.IconColor);
            btnSettings.Image = GraphicsUtils.ColorizeBitmap((Bitmap)btnSettings.Image, _settings.IconColor);

            btnMonIn.Checked = _settings.MonitorInput;
            btnMonOut.Checked = _settings.MonitorOutput;

            chkPlay.Image = GraphicsUtils.ColorizeBitmap((Bitmap)chkPlay.Image, _settings.IconColor);
            chkPlay.BackColor = _settings.BackColor;
            chkPlay.FlatAppearance.CheckedBackColor = _settings.SelectedColor;

            sldTempo.DrawColor = _settings.ControlColor;
            sldTempo.Invalidate();
            sldVolume.DrawColor = _settings.ControlColor;
            sldVolume.Invalidate();

            // Time controller.
            barBar.ProgressColor = _settings.ControlColor;
            barBar.CurrentTimeChanged += BarBar_CurrentTimeChanged;
            barBar.Invalidate();

            textViewer.WordWrap = _settings.WordWrap;

            btnKillComm.Click += (_, __) => { KillAll(); };
            #endregion
        }

        /// <summary>
        /// Post create init.
        /// </summary>
        /// <param name="e"></param>
        protected override void OnLoad(EventArgs e)
        {
            _logger.Info("============================ Starting up ===========================");

            PopulateRecentMenu();

            bool ok = CreateDevices();

            if(ok)
            {
                // Fast mm timer.
                SetFastTimerPeriod();
                _mmTimer.Start();

                _startTicks = 0;
                _sw.Start();
            
                KeyPreview = true; // for routing kbd strokes properly

                // Look for filename passed in. @"C:\Dev\repos\Nebulua\Examples\example.lua"
                string? serr = "";
                string[] args = Environment.GetCommandLineArgs();
                if (args.Length > 1)
                {
                    serr = OpenScriptFile(args[1]);
                }

                if (serr is null)
                {
                    ProcessPlay(PlayCommand.Stop);
                }
                else
                {
                    _logger.Error($"Couldn't open script file: {serr}");
                    Text = $"Nebulua {MiscUtils.GetVersionString()} - No file loaded";
                }
            }

            base.OnLoad(e);
        }

        /// <summary>
        /// Clean up on shutdown.
        /// </summary>
        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            _shuttingDown = true;
            Debug.WriteLine("closing");

            LogManager.Stop();

            ProcessPlay(PlayCommand.Stop);

            // Just in case.
            KillAll();

            // Save user settings.
            _settings.FormGeometry = new()
            {
                X = Location.X,
                Y = Location.Y,
                Width = Width,
                Height = Height
            };
            _settings.WordWrap = textViewer.WordWrap;
            _settings.Save();

            SaveProjectValues();

            base.OnFormClosing(e);
        }

        /// <summary>
        /// Resource clean up.
        /// </summary>
        /// <param name="disposing"></param>
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _mmTimer.Stop();
                _mmTimer.Dispose();

                // Wait a bit in case there are some lingering events.
                System.Threading.Thread.Sleep(100);

                DestroyDevices();

                components?.Dispose();
            }

            base.Dispose(disposing);
        }
        #endregion

        #region Process script
        /// <summary>
        /// What used to be called Compile.
        /// </summary>
        /// <returns></returns>
        bool ProcessScript()
        {
            bool ok = true;

            // Clean up old script stuff.
            ProcessPlay(PlayCommand.StopRewind);

            // Clean out our current elements.
            _channelControls.ForEach(c =>
            {
                Controls.Remove(c);
                c.Dispose();
            });
            _channelControls.Clear();
            Common.OutputChannels.Clear();
            Common.InputChannels.Clear();
            _totalSubbeats = 0;
            barBar.Reset();

            try
            {
                _script.LoadScript(_scriptFileName, _settings.LuaPaths);  // throws
            }
            catch (Exception ex)
            {
                ok = false;
                _logger.Error($"{ex.Message}");
            }

            ///// Script loaded ok.
            if (ok)
            {
                // Need exception handling here to protect from user script errors.
                try
                {
                    // Init shared vars.
                    InitRuntime();

                    // Setup script. This builds the sequences and sections.
                    _script.Setup(); // throws
                }
                catch (Exception ex)
                {
                    ok = false;
                    _logger.Error($"{ex.Message}");
                }

                // Create channels and controls from event sets.
                const int CONTROL_SPACING = 10;
                int x = btnRewind.Left;
                int y = barBar.Bottom + CONTROL_SPACING;

                foreach (var ch in Common.OutputChannels.Values)
                {
                    // Fill in the blanks.
                    ch.Volume = _nppVals.GetDouble(ch.ChannelName, "volume", MidiLibDefs.VOLUME_DEFAULT);
                    ch.State = (ChannelState)_nppVals.GetInteger(ch.ChannelName, "state", (int)ChannelState.Normal);
                    ch.Selected = false;
                    ch.Device = Common.OutputDevices[ch.DeviceId];
                    ch.AddNoteOff = true;

                    // Make new control and bind to channel.
                    ChannelControl control = new()
                    {
                        Location = new(x, y),
                        BorderStyle = BorderStyle.FixedSingle,
                        BoundChannel = ch
                    };
                    control.ChannelChange += Control_ChannelChange;
                    Controls.Add(control);
                    _channelControls.Add(control);

                    // Good time to send initial patch.
                    ch.SendPatch();

                    // Adjust positioning for next iteration.
                    y += control.Height + 5;
                }

                // Build the events.
                _script.BuildSteps();

                // Store the steps in the channel objects.
                MidiTimeConverter _mt = new(BarTime.LOW_RES_PPQ, _settings.MidiSettings.DefaultTempo);
                foreach (var channel in Common.OutputChannels.Values)
                {
                    var chEvents = _script.GetEvents().Where(e => e.ChannelName == channel.ChannelName &&
                        (e.RawEvent is NoteEvent || e.RawEvent is NoteOnEvent));

                    // Scale time and give to channel.
                    chEvents.ForEach(e => e.ScaledTime = _mt!.MidiToInternal(e.AbsoluteTime));
                    channel.SetEvents(chEvents);

                    // Round total up to next beat.
                    BarTime bs = new();
                    bs.SetRounded(channel.MaxSubbeat, SnapType.Beat, true);
                    _totalSubbeats = Math.Max(_totalSubbeats, bs.TotalSubbeats);
                }

                // Init the timeclock.
                if (_totalSubbeats > 0) // sequences
                {
                    barBar.TimeDefs = _script.GetSectionMarkers();
                    barBar.Length = new(_totalSubbeats);
                    barBar.Start = new(0);
                    barBar.End = new(_totalSubbeats - 1);
                    barBar.Current = new(0);
                }
                else // free form
                {
                    barBar.Length = new(0);
                    barBar.Start = new(0);
                    barBar.End = new(0);
                    barBar.Current = new(0);
                }

                // Start the clock.
                SetFastTimerPeriod();
            }

            if(!ok)
            {
                _logger.Error("Process script failed.");
            }

            return ok;
        }
        #endregion

        #region Device management
        /// <summary>
        /// Create all I/O devices from user settings.
        /// </summary>
        /// <returns>Success</returns>
        bool CreateDevices()
        {
            bool ok = true;

            // First...
            DestroyDevices();

            foreach(var dev in _settings.MidiSettings.InputDevices)
            {
                switch (dev.DeviceName)
                {
                    case nameof(VirtualKeyboard):
                        {
                            VirtualKeyboard vkey = new()
                            {
                                Dock = DockStyle.Fill,
                            };
                            vkey.InputReceive += Device_InputReceive;
                            Common.InputDevices.Add(dev.DeviceId, vkey);

                            using Form f = new()
                            {
                                Text = "Virtual Keyboard",
                                ClientSize = vkey.Size,
                                FormBorderStyle = FormBorderStyle.SizableToolWindow,
                                ShowIcon = false,
                                ShowInTaskbar = false
                            };
                            f.Controls.Add(vkey);
                            f.Show();
                        }
                        break;

                    case nameof(BingBong):
                        {
                            BingBong bb = new()
                            {
                                Dock = DockStyle.Fill,
                            };
                            bb.InputReceive += Device_InputReceive;
                            Common.InputDevices.Add(dev.DeviceId, bb);

                            using Form f = new()
                            {
                                Text = "Bing Bong",
                                ClientSize = bb.Size,
                                FormBorderStyle = FormBorderStyle.SizableToolWindow,
                                ShowIcon = false,
                                ShowInTaskbar = false
                            };
                            f.Controls.Add(bb);
                            f.Show();
                        }
                        break;

                    case "":
                        // None specified.
                        _logger.Warn($"Missing device for {dev.DeviceId}");
                        break;

                    default:
                        // Try midi or OSC.
                        try
                        {
                            var min = new MidiInput(dev.DeviceName);
                            var mosc = new OscInput(dev.DeviceName);
                            if (min.Valid)
                            {
                                min.InputReceive += Device_InputReceive;
                                Common.InputDevices.Add(dev.DeviceId, min);
                            }
                            else if (mosc.Valid)
                            {
                                mosc.InputReceive += Device_InputReceive;
                                Common.InputDevices.Add(dev.DeviceId, mosc);
                            }
                            else
                            {
                                ok = false;
                            }
                        }
                        catch
                        {
                            ok = false;
                        }

                        if (!ok)
                        {
                            _logger.Error($"Something wrong with your input device:{dev.DeviceName} id:{dev.DeviceId}");
                        }
                        break;
                }
            }

            foreach (var dev in _settings.MidiSettings.OutputDevices)
            {
                switch (dev.DeviceName)
                {
                    default:
                        // Try midi or OSC.
                        try
                        {
                            var mout = new MidiOutput(dev.DeviceName);
                            var mosc = new OscOutput(dev.DeviceName);
                            if (mout.Valid)
                            {
                                Common.OutputDevices.Add(dev.DeviceId, mout);
                            }
                            else if (mosc.Valid)
                            {
                                Common.OutputDevices.Add(dev.DeviceId, mosc);
                            }
                            else
                            {
                                ok = false;
                            }
                        }
                        catch
                        {
                            ok = false;
                        }

                        if (!ok)
                        {
                            _logger.Error($"Something wrong with your output device:{dev.DeviceName} id:{dev.DeviceId}");
                        }

                        break;
                }
            }

            Common.OutputDevices.Values.ForEach(d => d.LogEnable = _settings.MonitorOutput);

            return ok;
        }

        /// <summary>
        /// Clean up.
        /// </summary>
        void DestroyDevices()
        {
            Common.InputDevices.Values.ForEach(d => d.Dispose());
            Common.InputDevices.Clear();
            Common.OutputDevices.Values.ForEach(d => d.Dispose());
            Common.OutputDevices.Clear();
        }
        #endregion

        #region Channel controls
        /// <summary>
        /// UI changed something.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void Control_ChannelChange(object? sender, ChannelChangeEventArgs e)
        {
            ChannelControl chc = (ChannelControl)sender!;

            if (e.StateChange)
            {
                switch (chc.State)
                {
                    case ChannelState.Normal:
                        break;

                    case ChannelState.Solo:
                        // Mute any other non-solo channels.
                        Common.OutputChannels.Values.ForEach(ch =>
                        {
                            if (ch.ChannelName != chc.BoundChannel.ChannelName && chc.State != ChannelState.Solo)
                            {
                                chc.BoundChannel.Kill();
                            }
                        });
                        break;

                    case ChannelState.Mute:
                        chc.BoundChannel.Kill();
                        break;
                }
            }

            if (e.PatchChange && chc.Patch >= 0)
            {
                chc.BoundChannel.SendPatch();
            }
        }
        #endregion

        #region Real time
        /// <summary>
        /// Multimedia timer tick handler.
        /// </summary>
        void MmTimerCallback(double totalElapsed, double periodElapsed)
        {
            // Do some stats gathering for measuring jitter.
            //if (_tan.Grab())
            //{
            //    _logger.Info($"Midi timing: {_tan.Mean}");
            //}

            // Kick over to main UI thread.
            if (!_shuttingDown)
            {
                Debug.WriteLine("mmcb");

                this.InvokeIfRequired(_ =>
                {
                    if (_script is not null)
                    {
                        NextStep();
                    }
                });
            }
        }

        /// <summary>
        /// Output steps for next time increment.
        /// </summary>
        void NextStep()
        {
            if (_script is not null && chkPlay.Checked)// && !_needCompile)
            {
                //_tan.Arm();

                InitRuntime();

                // Kick the script. Note: Need exception handling here to protect from user script errors.
                try
                {
                    _script.Step(_stepTime.Bar, _stepTime.Beat, _stepTime.Subbeat); // throws
                }
                catch (Exception ex)
                {
                    _logger.Error($"{ex.Message}");
                    ProcessPlay(PlayCommand.Stop);
                }

                //if (_tan.Grab())
                //{
                //    _logger.Info($"NEB tan: {_tan.Mean}");
                //}

                bool anySolo = Common.OutputChannels.AnySolo();

                // Process any sequence steps.
                foreach (var ch in Common.OutputChannels.Values)
                {
                    // Is it ok to play now?
                    bool play = ch.State == ChannelState.Solo || (ch.State == ChannelState.Normal && !anySolo);

                    if (play)
                    {
                        // Need exception handling here to protect from user script errors.
                        try
                        {
                            ch.DoStep(_stepTime.TotalSubbeats);
                        }
                        catch (Exception ex)
                        {
                            _logger.Error($"{ex.Message}");
                            ProcessPlay(PlayCommand.Stop);
                        }
                    }
                }

                ///// Bump time.
                _stepTime.Increment(1);
                bool done = barBar.IncrementCurrent(1);
                // Check for end of play. If no steps or not selected, free running mode so always keep going.
                if (barBar.TimeDefs.Count > 1)
                {
                    // Check for end.
                    if (done)
                    {
                        Common.OutputChannels.Values.ForEach(ch => ch.Flush(_stepTime.TotalSubbeats));
                        ProcessPlay(PlayCommand.StopRewind);
                        KillAll(); // just in case
                    }
                }
                // else keep going

                ProcessPlay(PlayCommand.UpdateUiTime);
            }
        }

        /// <summary>
        /// Process input event.
        /// </summary>
        void Device_InputReceive(object? sender, InputReceiveEventArgs e)
        {
            this.InvokeIfRequired(_ =>
            {
                if (_script is not null && sender is not null)
                {
                    var dev = (IInputDevice)sender;

                    // Hand over to the script.
                    var chName = Common.InputChannels.ChannelName; check for invalid
                    try
                    {
                        if (e.Note != -1)
                        {
                            _script.InputNote(chName, e.Note, e.Value); // throws?
                        }
                        else if (e.Controller != -1)
                        {
                            _script.InputController(chName, e.Controller, e.Value); // throws?
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.Error($"{ex.Message}");
                        ProcessPlay(PlayCommand.Stop);
                    }
                }
            });
        }
        #endregion

        #region Runtime interop
        /// <summary>
        /// Package up the shared runtime stuff the script may need. Call this before any script updates.
        /// </summary>
        void InitRuntime()
        {
            if (_script is not null)
            {
                _script.Playing = chkPlay.Checked;
                _script.Tempo = (int)sldTempo.Value;
                _script.MasterVolume = sldVolume.Value;
                // What time is it?
                double totalMsec = _startTicks > 0 ? (_sw.ElapsedTicks - _startTicks) * 1000D / Stopwatch.Frequency : 0;
                _script.RealTime = totalMsec;
            }
        }
        #endregion

        #region File handling
        /// <summary>
        /// The user has asked to open a recent file.
        /// </summary>
        void Recent_Click(object? sender, EventArgs e)
        {
            string? fn = sender!.ToString();
            string? serr = OpenScriptFile(fn!);
            if (serr is not null)
            {
                _logger.Error(serr);
            }
        }

        /// <summary>
        /// Allows the user to select a np file from file system.
        /// </summary>
        void Open_Click(object? sender, EventArgs e)
        {
            string dir = ""; 
            if (_settings.ScriptPath != "")
            {
                if(Directory.Exists(_settings.ScriptPath))
                {
                    dir = _settings.ScriptPath;
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
                string? serr = OpenScriptFile(openDlg.FileName);
                if (serr is not null)
                {
                    _logger.Error(serr);
                }
            }
        }

        /// <summary>
        /// Common script file opener.
        /// </summary>
        /// <param name="fn">The np file to open.</param>
        /// <returns>Error string or null if ok.</returns>
        string? OpenScriptFile(string fn)
        {
            string? ret = null;

            try
            {
                // Clean up the old.
                SaveProjectValues();
                barBar.TimeDefs.Clear();

                if (File.Exists(fn))
                {
                    _logger.Info($"Opening {fn}");
                    _scriptFileName = fn;

                    // Get the persisted properties.
                    _nppVals = Bag.Load(fn.Replace(".lua", ".luap"));
                    sldTempo.Value = _nppVals.GetDouble("master", "speed", 100.0);
                    sldVolume.Value = _nppVals.GetDouble("master", "volume", MidiLibDefs.VOLUME_DEFAULT);

                    AddToRecentDefs(fn);
                    bool ok = ProcessScript();
                    Text = $"Nebulua {MiscUtils.GetVersionString()} - {fn}";
                }
                else
                {
                    ret = $"Invalid file: {fn}";
                }
            }
            catch (Exception ex)
            {
                ret = $"Couldn't open the script file: {fn} because: {ex.Message}";
                _logger.Error(ret);
            }

            // Update bar.
            barBar.Start = new();
            barBar.Current = new();

            return ret;
        }

        /// <summary>
        /// Create the menu with the recently used files.
        /// </summary>
        void PopulateRecentMenu()
        {
            ToolStripItemCollection menuItems = recentToolStripMenuItem.DropDownItems;
            menuItems.Clear();
            _settings.RecentFiles.ForEach(f =>
            {
                ToolStripMenuItem menuItem = new(f, null, new EventHandler(Recent_Click));
                menuItems.Add(menuItem);
            });
        }

        /// <summary>
        /// Update the mru with the user selection.
        /// </summary>
        /// <param name="fn">The selected file.</param>
        void AddToRecentDefs(string fn)
        {
            if (File.Exists(fn))
            {
                _settings.UpdateMru(fn);
                PopulateRecentMenu();
            }
        }
        #endregion

        #region Main toolbar controls
        /// <summary>
        /// Go or stop button.
        /// </summary>
        void Play_Click(object? sender, EventArgs e)
        {
            ProcessPlay(chkPlay.Checked ? PlayCommand.Start : PlayCommand.Stop);
        }

        /// <summary>
        /// Update multimedia timer period.
        /// </summary>
        void Speed_ValueChanged(object? sender, EventArgs e)
        {
            SetFastTimerPeriod();
        }

        /// <summary>
        /// Go back jack.
        /// </summary>
        void Rewind_Click(object? sender, EventArgs e)
        {
            ProcessPlay(PlayCommand.Rewind);
        }

        /// <summary>
        /// Manual recompile.
        /// </summary>
        void Compile_Click(object? sender, EventArgs e)
        {
            ProcessScript();
            ProcessPlay(PlayCommand.StopRewind);
        }

        /// <summary>
        /// Monitor comm messages. Note that monitoring slows down processing so use judiciously.
        /// </summary>
        void Monitor_Click(object? sender, EventArgs e)
        {
            _settings.MonitorInput = btnMonIn.Checked;
            _settings.MonitorOutput = btnMonOut.Checked;

            Common.OutputDevices.Values.ForEach(d => d.LogEnable = _settings.MonitorOutput);
        }

        /// <summary>
        /// The meaning of life.
        /// </summary>
        void About_Click(object? sender, EventArgs e)
        {
            MiscUtils.ShowReadme("Nebulua");
        }

        /// <summary>
        /// Edit the common options in a property grid.
        /// </summary>
        void Settings_Click(object? sender, EventArgs e)
        {
            var changes = SettingsEditor.Edit(_settings, "User Settings", 500);

            // Detect changes of interest.
            bool restart = false;

            foreach (var (name, cat) in changes)
            {
                switch (name)
                {
                    case "MidiInDevice":
                    case "MidiOutDevice":
                    case "InternalPPQ":
                    case "ControlColor":
                    case "SelectedColor":
                    case "BackColor":
                        restart = true;
                        break;
                }
            }

            if (restart)
            {
                MessageBox.Show("Restart required for device changes to take effect");
            }
        }
        #endregion

        #region Play control
        /// <summary>
        /// Update UI state per param.
        /// </summary>
        /// <param name="cmd">The command.</param>
        /// <returns>Indication of success.</returns>
        bool ProcessPlay(PlayCommand cmd)
        {
            bool ret = true;

            switch (cmd)
            {
                case PlayCommand.Start:
                    bool ok = ProcessScript();
                    if (ok)
                    {
                        _startTicks = _sw.ElapsedTicks;
                        chkPlay.Checked = true;
                        _mmTimer.Start();
                    }
                    else
                    {
                        _startTicks = 0;
                        chkPlay.Checked = false;
                        ret = false;
                    }
                    break;

                case PlayCommand.Stop:
                    chkPlay.Checked = false;
                    _startTicks = 0;
                    _mmTimer.Stop();
                    // Send midi stop all notes just in case.
                    KillAll();
                    break;

                case PlayCommand.Rewind:
                    _stepTime.Reset();
                    barBar.Current = new();
                    break;

                case PlayCommand.StopRewind:
                    chkPlay.Checked = false;
                    _stepTime.Reset();
                    break;

                case PlayCommand.UpdateUiTime:
                    // See below.
                    break;
            }

            // Always do this.
            barBar.Current = new(_stepTime.TotalSubbeats);

            return ret;
        }

        /// <summary>
        /// User has changed the time.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void BarBar_CurrentTimeChanged(object? sender, EventArgs e)
        {
            _stepTime = new(barBar.Current.TotalSubbeats);
            ProcessPlay(PlayCommand.UpdateUiTime);
        }
        #endregion

        #region Stuff
        /// <summary>
        /// Show log events.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void LogManager_LogMessage(object? sender, LogMessageEventArgs e)
        {
            // Usually come from a different thread.
            if (!_shuttingDown)// IsHandleCreated) //TODO2??
            {
                Debug.WriteLine("log");
                this.InvokeIfRequired(_ => { textViewer.AppendLine($"{e.Message}"); });
            }
        }

        /// <summary>
        /// Save current values.
        /// </summary>
        void SaveProjectValues()
        {
            _nppVals.Clear();
            _nppVals.SetValue("master", "speed", sldTempo.Value);
            _nppVals.SetValue("master", "volume", sldVolume.Value);

            Common.OutputChannels.Values.ForEach(ch =>
            {
                if(ch.NumEvents > 0)
                {
                    _nppVals.SetValue(ch.ChannelName, "volume", ch.Volume);
                    _nppVals.SetValue(ch.ChannelName, "state", ch.State);
                }
            });

            _nppVals.Save();
        }

        /// <summary>
        /// Do some global key handling. Space bar is used for stop/start playing.
        /// </summary>
        /// <param name="e"></param>
        protected override void OnKeyDown(KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Space)
            {
                // Handle start/stop toggle.
                ProcessPlay(chkPlay.Checked ? PlayCommand.Stop : PlayCommand.Start);
                e.Handled = true;
            }
            base.OnKeyDown(e);
        }

        /// <summary>
        /// Common func.
        /// </summary>
        void SetFastTimerPeriod()
        {
            // Make a transformer.
            MidiTimeConverter mt = new(_settings.MidiSettings.SubbeatsPerBeat, sldTempo.Value);
            var per = mt.RoundedInternalPeriod();
            _mmTimer.SetTimer(per, MmTimerCallback);
        }

        /// <summary>
        /// Kill em all.
        /// </summary>
        void KillAll()
        {
            chkPlay.Checked = false;
            Common.OutputChannels.Values.ForEach(ch => ch.Kill());
        }
        #endregion

        #region Midi utilities
        /// <summary>
        /// Export to a midi file.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void ExportMidi_Click(object? sender, EventArgs e)
        {
            if(_script is not null)
            {
                using SaveFileDialog saveDlg = new()
                {
                    Filter = "Midi files (*.mid)|*.mid",
                    Title = "Export to midi file",
                    FileName = Path.GetFileName(_scriptFileName.Replace(".lua", ".mid"))
                };

                if (saveDlg.ShowDialog() == DialogResult.OK)
                {
                    // Make a Pattern object and call the formatter.
                    IEnumerable<Channel> channels = Common.OutputChannels.Values.Where(ch => ch.NumEvents > 0);

                    PatternInfo pattern = new("export", _settings.MidiSettings.SubbeatsPerBeat,
                        _script.GetEvents(), channels, (int)sldTempo.Value);// _script.Tempo);

                    Dictionary<string, int> meta = new()
                    {
                        { "MidiFileType", 0 },
                        { "DeltaTicksPerQuarterNote", _settings.MidiSettings.SubbeatsPerBeat },
                        { "NumTracks", 1 }
                    };

                    MidiExport.ExportMidi(saveDlg.FileName, pattern, channels, meta);
                }
            }
        }

        /// <summary>
        /// Dump human readable.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void ExportCsv_Click(object sender, EventArgs e)
        {
            if (_script is not null)
            {
                // Make a Pattern object and call the formatter.
                IEnumerable<Channel> channels = Common.OutputChannels.Values.Where(ch => ch.NumEvents > 0);

                var fn = Path.GetFileName(_scriptFileName.Replace(".lua", ".csv"));

                PatternInfo pattern = new("export", _settings.MidiSettings.SubbeatsPerBeat, _script.GetEvents(), channels, (int)sldTempo.Value);// _script.Tempo);

                Dictionary<string, int> meta = new()
                {
                    { "MidiFileType", 0 },
                    { "DeltaTicksPerQuarterNote", _settings.MidiSettings.SubbeatsPerBeat },
                    { "NumTracks", 1 }
                };

                MidiExport.ExportCsv(fn, new List<PatternInfo>() { pattern }, channels, meta);
                _logger.Info($"Exported to {fn}");
            }
        }

        /// <summary>
        /// Show the builtin definitions.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void ShowDefinitions_Click(object sender, EventArgs e)
        {
            var docs = MidiDefs.FormatDoc();
            docs.AddRange(MusicDefinitions.FormatDoc());
            Tools.MarkdownToHtml(docs, Color.LightYellow, new Font("arial", 16), true);
        }
        #endregion
    }
}
