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
using Nebulua.Interop;


namespace Nebulua
{
    public class Cli : IDisposable // TODO1 combine with App or make a new csproj. 
    {
        #region Fields
        /// <summary>App logger.</summary>
        readonly Logger _logger = LogManager.CreateLogger("Cli");

        /// <summary>App settings.</summary>
        readonly UserSettings _settings;

        /// <summary>Talks to the user.</summary>
        readonly CommandProc _cmdProc = new(Console.In, Console.Out);

        /// <summary>Current script.</summary>
        readonly string _scriptFn = "";

        /// <summary>Resource management.</summary>
        bool _disposed = false;

        /// <summary>Common functionality.</summary>
        readonly Core _core = new();
        #endregion

        #region Lifecycle
        /// <summary>
        /// Constructor inits stuff.
        /// </summary>
        public Cli()
        {
            string appDir = MiscUtils.GetAppDataDir("Nebulua", "Ephemera");
            _settings = (UserSettings)SettingsCore.Load(appDir, typeof(UserSettings));

            try
            {
                LogManager.LogMessage += LogManager_LogMessage;
                LogManager.MinLevelFile = _settings.FileLogLevel;
                LogManager.MinLevelNotif = _settings.NotifLogLevel;
                LogManager.Run(Path.Combine(appDir, "log.txt"), 50000);

                State.Instance.ValueChangeEvent += State_ValueChangeEvent;

                // Process cmd line args.
                var args = Environment.GetCommandLineArgs();
                if (args.Length == 2 && args[1].EndsWith(".lua") && Path.Exists(args[1]))
                {
                    _scriptFn = args[1];
                }
                else
                {
                    throw new ApplicationArgumentException($"Invalid nebulua script file: {args[1]}");
                }

                // OK so far. Run it.
                _logger.Info($"Running script file {_scriptFn}");
                _core.RunScript(_scriptFn);

                // Loop forever doing cmdproc requests. Should not throw. Command processor will take care of its own errors.
                while (State.Instance.ExecState != ExecState.Exit)
                {
                    _cmdProc.Read();
                }

                // Normal done. Wait a bit in case there are some lingering events or logging.
                Thread.Sleep(100);
            }
            catch (Exception e) // Anything that throws is fatal.
            {
                State.Instance.ExecState = ExecState.Dead;
                string serr = e switch
                {
                    ApiException ex => $"Api Error: {ex.Message}:{Environment.NewLine}{ex.ApiError}",
                    ConfigException ex => $"Config File Error: {ex.Message}",
                    ScriptSyntaxException ex => $"Script Syntax Error: {ex.Message}",
                    ApplicationArgumentException ex => $"Application Argument Error: {ex.Message}",
                    _ => $"Other error: {e}{Environment.NewLine}{e.StackTrace}",
                };

                // Logging an error will cause the app to exit.
                _logger.Error(serr);
            }
        }

        /// <summary>Clean up.</summary>
        public void Dispose()
        {
            if (!_disposed)
            {
                _core?.Dispose();
                _disposed = true;
            }
        }
        #endregion

        #region Private functions
        /// <summary>
        /// Handler for state changes
        /// </summary>
        /// <param name="_"></param>
        /// <param name="name">Specific State value.</param>
        void State_ValueChangeEvent(object? _, string name)
        {
            if (name == "CurrentTick")
            {
                // TODO display running tick/bartime somewhere in console?
            }
        }

        /// <summary>
        /// Capture bad events and display them to the user. If error shut down.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void LogManager_LogMessage(object? sender, LogMessageEventArgs e)
        {
            if (e.Level == LogLevel.Error)
            {
                _cmdProc.Write($"Fatal! shutting down.{Environment.NewLine}{e.Message}");
                State.Instance.ExecState = ExecState.Exit;
            }
            else
            {
                _cmdProc.Write(e.Message);
            }
        }
        #endregion
    }

    public class CommandProc
    {
        #region Fields
        /// <summary>All the commands.</summary>
        readonly CommandDescriptor[]? _commands;

        /// <summary>CLI input.</summary>
        readonly TextReader _in;

        /// <summary>CLI output.</summary>
        readonly TextWriter _out;

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

        #region Main work
        /// <summary>
        /// Set up command handler. Could support alternate streams e.g. socket.
        /// </summary>
        public CommandProc(TextReader @in, TextWriter @out)
        {
            _in = @in;
            _out = @out;

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

            // Show info now.
            // InfoCmd(new(), []);
        }

        /// <summary>
        /// Takes care of prompt.
        /// </summary>
        /// <param name="s"></param>
        public void Write(string s)
        {
            _out.WriteLine(s);
            _out.Write(_prompt);
        }

        /// <summary>
        /// Process user input. Blocks until new line.
        /// TODO Would like to .Peek() for spacebar but it's broken. Read() doesn't seem to work either. Maybe something like Console.KeyAvailable.
        /// </summary>
        /// <returns>Success</returns>
        public bool Read()
        {
            bool ret = true;

            // Listen.
            string? res = _in.ReadLine();

            if (res != null)
            {
                // Process the line. Chop up the raw command line into args.
                List<string> args = StringUtils.SplitByToken(res, " ");

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
                            // ok = _EvalStatus(stat, "handler failed: %s", cmd->desc.long_name);
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

            return ret;
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
            State.Instance.ExecState = ExecState.Kill;
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
        bool LoopCmd(CommandDescriptor cmd, List<string> args) // TODO loops
        {
            bool ret = true;

            // switch (args.Count)
            // {
            // }

            return ret;
        }

        //--------------------------------------------------------//
        bool ReloadCmd(CommandDescriptor cmd, List<string> args)
        {
            bool ret = true;

            switch (args.Count)
            {
                case 1: // no args
                    State.Instance.ExecState = ExecState.Reload;
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
            Write("");

            return true;
        }

        //--------------------------------------------------------//
        bool UsageCmd(CommandDescriptor _, List<string> __)
        {
            foreach (var cmd in _commands!)
            {
                _out.WriteLine($"{cmd.LongName}|{cmd.ShortName}: {cmd.Info}");
                if (cmd.Args.Length > 0)
                {
                    // Maybe multiline args.
                    var parts = StringUtils.SplitByToken(cmd.Args, Environment.NewLine);
                    foreach (var arg in parts)
                    {
                        _out.WriteLine($"    {arg}");
                    }
                }
            }
            Write("");

            return true;
        }
        #endregion
    }

    
}
