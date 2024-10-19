using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Threading;
using System.Diagnostics;
using NAudio.Midi;
using Ephemera.NBagOfTricks;
using Ephemera.NBagOfTricks.Slog;


namespace Nebulua
{
    public class Cli : IDisposable
    {
        #region Fields
        /// <summary>App logger.</summary>
        readonly Logger _logger = LogManager.CreateLogger("Cli");

        /// <summary>Common functionality.</summary>
        readonly Core _core = new();

        /// <summary>Resource management.</summary>
        bool _disposed = false;

        /// <summary>All the commands.</summary>
        readonly CommandDescriptor[] _commands;

        /// <summary>CLI.</summary>
        readonly IConsole _console;

        /// <summary>CLI prompt.</summary>
        readonly string _prompt = ">";
        #endregion

        #region Types
        /// <summary>Command descriptor.</summary>
        readonly record struct CommandDescriptor
        (
            /// <summary>If you like to type.</summary>
            string LongName,
            /// <summary>If you don't.</summary>
            char ShortName,
            /// <summary>Free text for command description.</summary>
            string Info,
            /// <summary>Free text for args description.</summary>
            string Args,
            /// <summary>The runtime handler.</summary>
            CommandHandler Handler
        );

        /// <summary>Command handler.</summary>
        delegate bool CommandHandler(CommandDescriptor cmd, List<string> args);
        #endregion

        #region Lifecycle
        /// <summary>
        /// Constructor inits stuff.
        /// </summary>
        /// <param name="scriptFn">Cli version requires cl script name.</param>
        /// <param name="tin">Stream in</param>
        /// <param name="tout">Stream out</param>
        public Cli(string scriptFn, IConsole console)
        {
            _console = console;

            string appDir = MiscUtils.GetAppDataDir("Nebulua", "Ephemera");
            UserSettings.Current = (UserSettings)SettingsCore.Load(appDir, typeof(UserSettings));
            LogManager.LogMessage += LogManager_LogMessage;
            LogManager.MinLevelFile = UserSettings.Current.FileLogLevel;
            LogManager.MinLevelNotif = UserSettings.Current.NotifLogLevel;
            LogManager.Run(Path.Combine(appDir, "log.txt"), 50000);

            State.Instance.ValueChangeEvent += State_ValueChangeEvent;
            
            _commands =
            [
                new("help",     '?',    "available commands",               "",                         UsageCmd),
                new("info",     'i',    "system information",               "",                         InfoCmd),
                new("exit",     'q',    "exit the application",             "",                         ExitCmd),
                new("run",      'r',    "toggle running the script",        "",                         RunCmd),
                new("position", 'p',    "set position or tell current",     "(bt)",                     PositionCmd),
                new("loop",     'l',    "set loop or tell current",         "(start-bt end-bt)|(r)",    LoopCmd),
                new("tempo",    't',    "get or set the tempo",             "(40-240)",                 TempoCmd),
                new("monitor",  'm',    "toggle monitor midi traffic",      "(r|s|o): rcv|snd|off",     MonCmd),
                new("kill",     'k',    "stop all midi",                    "",                         KillCmd),
                new("reload",   's',    "reload current script",            "",                         ReloadCmd)
            ];

            try
            {
                // Script file validity checked in LoadScript().
                _logger.Info($"Loading script file {scriptFn}");
                _core.LoadScript(scriptFn);

                // Done. Wait a bit in case there are some lingering events or logging.
                Thread.Sleep(100);
            }
            catch (Exception ex)
            {
                var (fatal, msg) = Utils.ProcessException(ex);
                if (fatal)
                {
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

        /// <summary>
        /// Loop forever doing cmdproc requests. Should not throw. Command processor will take care of its own errors.
        /// </summary>
        public void Run()
        {
            while (State.Instance.ExecState != ExecState.Exit)
            {
                DoCommand();
            }
        }

        /// <summary>
        /// Clean up.
        /// </summary>
        public void Dispose()
        {
            if (!_disposed)
            {
                _core?.Dispose();
                _disposed = true;
            }
        }
        #endregion

        #region Event handlers
        /// <summary>
        /// Handler for state changes
        /// </summary>
        /// <param name="_"></param>
        /// <param name="name">Specific State value.</param>
        void State_ValueChangeEvent(object? _, string name)
        {
            if (name == "CurrentTick")
            {
                int tick = State.Instance.CurrentTick;
                int sub = MusicTime.SUB(tick);

                if (sub == 0)
                {
                    // Display time.
                    int bar = MusicTime.BAR(tick);
                    int beat = MusicTime.BEAT(tick);
                    string st = $"{bar:D3}:{beat:D1}";

                    bool inTab = true;
                    if (inTab)
                    {
                        _console.Title = st;
                    }
                    else
                    {
                        var (left, top) = _console.GetCursorPosition();
                        _console.CursorVisible = false;
                        _console.SetCursorPosition(0, 0);
                        _console.Write(st.PadRight(_console.BufferWidth - 1));
                        _console.SetCursorPosition(left, top);
                        _console.CursorVisible = true;
                    }
                }
            }
        }

        /// <summary>
        /// Log events. If error shut down.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void LogManager_LogMessage(object? sender, LogMessageEventArgs e)
        {
            if (e.Level == LogLevel.Error)
            {
                Write($"Fatal! shutting down.{Environment.NewLine}{e.Message}");
                State.Instance.ExecState = ExecState.Exit;
            }
            else
            {
                Write(e.Message);
            }
        }
        #endregion

        #region Private functions
        /// <summary>
        /// Process user input. Blocks until new line or spacebar.
        /// </summary>
        /// <returns>Success</returns>
        public bool DoCommand()
        {
            bool ret = true;
            string? ucmd;

            // Check for something to do.
            if (_console.KeyAvailable)
            {
                // Console.ReadKey blocks and consumes the buffer immediately.
                var key = _console.ReadKey(false);

                if (key.KeyChar == ' ')
                {
                    // Toggle run.
                    ucmd = "r";
                    _console.WriteLine("");
                }
                else
                {
                    // Get the rest.
                    var res = _console.ReadLine();
                    ucmd = res is null ? key.KeyChar.ToString() : key.KeyChar + res;
                }

                if (ucmd is not null)
                {
                    // Process the line. Chop up the raw command line into args.
                    List<string> args = StringUtils.SplitByToken(ucmd, " ");

                    // Process the command and its options.
                    bool valid = false;
                    if (args.Count > 0)
                    {
                        foreach (var cmd in _commands!)
                        {
                            if (args[0] == cmd.LongName || (args[0].Length == 1 && args[0][0] == cmd.ShortName))
                            {
                                // Execute the command. They handle any errors internally.
                                valid = true;

                                ret = cmd.Handler(cmd, args);
                                break;
                            }
                        }

                        if (!valid)
                        {
                            Write("Invalid command");
                        }
                    }
                }
                else
                {
                    // Assume finished.
                    State.Instance.ExecState = ExecState.Exit;
                }
            }

            return ret;
        }

        /// <summary>
        /// Write to user. Takes care of prompt.
        /// </summary>
        /// <param name="s"></param>
        void Write(string s)
        {
            _console.WriteLine(s);
            _console.Write(_prompt);
        }
        #endregion

        #region Command handlers

        //--------------------------------------------------------//
        bool TempoCmd(CommandDescriptor cmd, List<string> args)
        {
            bool ret = true;

            switch (args.Count)
            {
                case 1: // get
                    Write($"{State.Instance.Tempo}");
                    break;

                case 2: // set
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
                    break;

                default:
                    ret = false;
                    break;
            }

            return ret;
        }

        //--------------------------------------------------------//
        bool RunCmd(CommandDescriptor cmd, List<string> args)
        {
            bool ret = true;

            switch (args.Count)
            {
                case 1:  // no args
                    switch (State.Instance.ExecState)
                    {
                        case ExecState.Idle:
                            State.Instance.ExecState = ExecState.Run;
                            Write("running");
                            break;

                        case ExecState.Run:
                            State.Instance.ExecState = ExecState.Idle;
                            Write("stopped");
                            _core.KillAll();
                            break;

                        default:
                            Write("invalid state");
                            ret = false;
                            break;
                    }
                    break;

                default:
                    Write($"invalid command");
                    ret = false;
                    break;
            }

            return ret;
        }

        //--------------------------------------------------------//
        bool ExitCmd(CommandDescriptor cmd, List<string> args)
        {
            State.Instance.ExecState = ExecState.Exit;
            Write($"Exit - goodbye!");

            return true;
        }

        //--------------------------------------------------------//
        bool MonCmd(CommandDescriptor cmd, List<string> args)
        {
            bool ret = true;

            switch (args.Count)
            {
                case 2: // set
                    switch (args[1])
                    {
                        case "r":
                            UserSettings.Current.MonitorRcv = !UserSettings.Current.MonitorRcv;
                            Write("");
                            break;

                        case "s":
                            UserSettings.Current.MonitorSnd = !UserSettings.Current.MonitorSnd;
                            Write("");
                            break;

                        case "o":
                            UserSettings.Current.MonitorRcv = false;
                            UserSettings.Current.MonitorSnd = false;
                            Write("");
                            break;

                        default:
                            ret = false;
                            Write($"invalid option: {args[1]}");
                            break;
                    }
                    break;

                default:
                    ret = false;
                    Write("");
                    break;
            }

            return ret;
        }

        //--------------------------------------------------------//
        bool KillCmd(CommandDescriptor cmd, List<string> args)
        {
            // State.Instance.ExecState = ExecState.Kill;
            _core.KillAll();
            Write("");

            return true;
        }

        //--------------------------------------------------------//
        bool PositionCmd(CommandDescriptor cmd, List<string> args)
        {
            bool ret = true;

            switch (args.Count)
            {
                case 1: // get
                    Write(MusicTime.Format(State.Instance.CurrentTick));
                    break;

                case 2: // set
                    int curpos = State.Instance.CurrentTick;
                    int reqpos = MusicTime.Parse(args[1]);
                    if (reqpos < 0)
                    {
                        Write($"invalid requested position: {args[1]}");
                        ret = false;
                    }
                    else
                    {
                        // State will validate the requested position.
                        State.Instance.CurrentTick = reqpos;
                        int actpos = State.Instance.CurrentTick;
                        if (actpos != reqpos)
                        {
                            Write($"invalid requested position: {args[1]}");
                            State.Instance.CurrentTick = curpos;
                            ret = false;
                        }
                        else
                        {
                            Write(MusicTime.Format(State.Instance.CurrentTick)); // echo
                        }
                    }
                    break;

                default:
                    Write("");
                    ret = false;
                    break;
            }

            return ret;
        }

        //--------------------------------------------------------//
        bool LoopCmd(CommandDescriptor cmd, List<string> args)
        {
            bool ret = true;

            // switch (args.Count)
            // {
            // }

            return ret;
        }

        // TODO1 loops - ("loop",     'l',    "set loop or tell current",         "(start-bt end-bt)|(r)",    LoopCmd),
        // Core does this:
        // if (State.Instance.IsComposition)
        // {
        //     // Bump time and check state.
        //     int start = State.Instance.LoopStart == -1 ? 0 : State.Instance.LoopStart;
        //     int end = State.Instance.LoopEnd == -1 ? State.Instance.Length : State.Instance.LoopEnd;

        //     if (++State.Instance.CurrentTick >= end) // done
        //     {
        //         // Keep going? else stop/rewind.
        //         if (State.Instance.DoLoop)
        //         {
        //             // Keep going.
        //             State.Instance.CurrentTick = start;
        //         }
        //         else
        //         {
        //             // Stop and rewind.
        //             State.Instance.ExecState = ExecState.Idle;
        //             State.Instance.CurrentTick = start;

        //             // just in case
        //             KillAll();
        //         }
        //     }
        // }
        // else // dynamic script
        // {
        //     ++State.Instance.CurrentTick;
        // }

        // C:\Dev\repos\Apps\Nebulua\App\TimeBar.cs:
        //    95:                 int mstart = GetClientFromTick(State.Instance.LoopStart);
        //   105:             pe.Graphics.DrawString(MusicTime.Format(State.Instance.LoopStart), FontSmall, Brushes.Black, ClientRectangle, _format);
        //   121:                 State.Instance.LoopStart = -1;
        //   140:                 //_toolTip.SetToolTip(this, $"{State.Instance.CurrentTick}: 0 {State.Instance.LoopStart} {State.Instance.LoopEnd} {State.Instance.Length} ");
        //   156:                 int lstart = State.Instance.LoopStart;
        //   164:                         State.Instance.LoopStart = newval;




        //--------------------------------------------------------//
        bool ReloadCmd(CommandDescriptor cmd, List<string> args)
        {
            bool ret = true;

            switch (args.Count)
            {
                case 1: // no args
                    _core.LoadScript();
                    break;

                default:
                    ret = false;
                    break;
            }
            Write("");

            return ret;
        }

        //--------------------------------------------------------//
        bool InfoCmd(CommandDescriptor _, List<string> __)
        {
            _console.WriteLine($"Midi output devices:");
            for (int i = 0; i < MidiOut.NumberOfDevices; i++)
            {
                _console.WriteLine("  " + MidiOut.DeviceInfo(i).ProductName);
            }

            _console.WriteLine($"Midi input devices:");
            for (int i = 0; i < MidiIn.NumberOfDevices; i++)
            {
                _console.WriteLine("  " + MidiIn.DeviceInfo(i).ProductName);
            }
            Write("");

            return true;
        }

        //--------------------------------------------------------//
        bool UsageCmd(CommandDescriptor _, List<string> __)
        {
            // Talk about muself.
            foreach (var cmd in _commands!)
            {
                _console.WriteLine($"{cmd.LongName}|{cmd.ShortName}: {cmd.Info}");
                if (cmd.Args.Length > 0)
                {
                    // Maybe multiline args.
                    var parts = StringUtils.SplitByToken(cmd.Args, Environment.NewLine);
                    foreach (var arg in parts)
                    {
                        _console.WriteLine($"    {arg}");
                    }
                }
            }
            Write("");

            return true;
        }
        #endregion
    }
}
