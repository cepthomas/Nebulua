using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Ephemera.NBagOfTricks;
using Interop;
using NAudio.Midi;


namespace Nebulua
{
    public class Cli
    {
        #region Fields
        /// <summary>All the commands.</summary>
        readonly CommandDescriptor[]? _commands;

        /// <summary>CLI.</summary>
        readonly TextWriter _cliOut;

        /// <summary>CLI.</summary>
        readonly TextReader _cliIn;
        #endregion

        #region Properties
        /// <summary>CLI prompt.</summary>
        public string Prompt { get; set; } = ">";
        #endregion

        #region Infrastructure
        /// <summary>Cli command descriptor.</summary>
        readonly record struct CommandDescriptor
        (
            /// <summary>If you like to type.</summary>
            string LongName,

            /// <summary>If you don't.</summary>
            char ShortName,

            /// <summary>TODO1 Optional single char for immediate execution (no CR required). Can be ^(ctrl) or ~(alt) in conjunction with short_name.</summary>
            char ImmediateKey,

            /// <summary>Free text for command description.</summary>
            string Info,

            /// <summary>Free text for args description.</summary>
            string Args,
            
            /// <summary>The runtime handler.</summary>
            CommandHandler Handler
        );

        /// <summary>Cli command handler.</summary>
        delegate NebStatus CommandHandler(CommandDescriptor cmd, List<string> args);
        #endregion

        #region Main work
        /// <summary>
        /// Set up command handler. TODOF other stream I/O e.g. socket.
        /// </summary>
        public Cli(TextReader cliIn, TextWriter cliOut)
        {
            _cliIn = cliIn;
            _cliOut = cliOut;

            _commands =
            [
                new("help",     '?',    '\0',   "available commands",                       "",                         UsageCmd),
                new("info",     'i',    '\0',   "system information",                       "",                         InfoCmd),
                new("exit",     'x',    '\0',   "exit the application",                     "",                         ExitCmd),
                new("run",      'r',    ' ',    "toggle running the script",                "",                         RunCmd),
                new("tempo",    't',    '\0',   "get or set the tempo",                     "(bpm): 40-240",            TempoCmd),
                new("monitor",  'm',    '^',    "toggle monitor midi traffic",              "(in|out|off): action",     MonCmd),
                new("kill",     'k',    '~',    "stop all midi",                            "",                         KillCmd),
                new("position", 'p',    '\0',   "set position to where or tell current",    "(where): bar:beat:sub",    PositionCmd),
                new("reload",   'l',    '\0',   "re/load current script",                   "",                         ReloadCmd)
            ];
        }

        /// <summary>
        /// Takes care of prompt.
        /// </summary>
        /// <param name="s"></param>
        public void Write(string s)
        {
            _cliOut.WriteLine(s);
            _cliOut.Write(Prompt);
        }

        /// <summary>
        /// Process user input. Blocks until new line.
        /// </summary>
        /// <returns>Status</returns>
        public NebStatus Read()
        {
            NebStatus stat = NebStatus.Ok;

            // Listen.
            string? res = _cliIn.ReadLine();

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

                            stat = cmd.Handler(cmd, args);
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

            return stat;
        }
        #endregion

        #region Command handlers
        //--------------------------------------------------------//
        NebStatus TempoCmd(CommandDescriptor cmd, List<string> args)
        {
            NebStatus stat;

            switch (args.Count)
            {
                case 1: // get
                    Write($"{State.Instance.Tempo}");
                    stat = NebStatus.Ok;
                    break;

                case 2: // set
                    if (int.TryParse(args[1], out int t) && t >= 40 && t <= 240)
                    {
                        State.Instance.Tempo = t;
                        stat = NebStatus.Ok;
                        Write("");
                    }
                    else
                    {
                        stat = NebStatus.InvalidCliArg;
                        Write($"invalid tempo: {args[1]}");
                    }
                    break;

                default:
                    stat = NebStatus.InvalidCliArg;
                    break;
            }

            return stat;
        }

        //--------------------------------------------------------//
        NebStatus RunCmd(CommandDescriptor cmd, List<string> args)
        {
            NebStatus stat;

            switch (args.Count)
            {
                case 1:  // no args
                    switch (State.Instance.ExecState)
                    {
                        case ExecState.Idle:
                            State.Instance.ExecState = ExecState.Run;
                            Write("running");
                            stat = NebStatus.Ok;
                            break;

                        case ExecState.Run:
                            State.Instance.ExecState = ExecState.Idle;
                            Write("stopped");
                            stat = NebStatus.Ok;
                            break;

                        default:
                            Write("invalid state");
                            stat = NebStatus.InvalidCliArg;
                            break;
                    }
                    break;

                default:
                    Write($"invalid command");
                    stat = NebStatus.InvalidCliArg;
                    break;
            }

            return stat;
        }

        //--------------------------------------------------------//
        NebStatus ExitCmd(CommandDescriptor cmd, List<string> args)
        {
            NebStatus stat = NebStatus.Ok;

            State.Instance.ExecState = ExecState.Exit;
            Write($"goodbye!");

            return stat;
        }

        //--------------------------------------------------------//
        NebStatus MonCmd(CommandDescriptor cmd, List<string> args)
        {
            NebStatus stat;

            switch (args.Count)
            {
                case 2: // set
                    switch (args[1])
                    {
                        case "rx":
                            State.Instance.MonRcv = !State.Instance.MonRcv;
                            stat = NebStatus.Ok;
                            Write("");
                            break;

                        case "tx":
                            State.Instance.MonSend = !State.Instance.MonSend;
                            stat = NebStatus.Ok;
                            Write("");
                            break;

                        case "off":
                            State.Instance.MonRcv = false;
                            State.Instance.MonSend = false;
                            stat = NebStatus.Ok;
                            Write("");
                            break;

                        default:
                            stat = NebStatus.InvalidCliArg;
                            Write($"invalid option: {args[1]}");
                            break;
                    }
                    break;

                default:
                    stat = NebStatus.InvalidCliArg;
                    Write("");
                    break;
            }

            return stat;
        }

        //--------------------------------------------------------//
        NebStatus KillCmd(CommandDescriptor cmd, List<string> args)
        {
            NebStatus stat = NebStatus.InvalidCliArg;

            State.Instance.ExecState = ExecState.Kill;
            stat = NebStatus.Ok;
            Write("");

            return stat;
        }

        //--------------------------------------------------------//
        NebStatus PositionCmd(CommandDescriptor cmd, List<string> args)
        {
            NebStatus stat;

            switch (args.Count)
            {
                case 1: // get
                    Write(Utils.FormatBarTime(State.Instance.CurrentTick));
                    stat = NebStatus.Ok;
                    break;

                case 2: // set
                    int position = Utils.ParseBarTime(args[1]);
                    if (position < 0)
                    {
                        Write($"invalid position: {args[1]}");
                        stat = NebStatus.InvalidCliArg;
                    }
                    else
                    {
                        // Limit range maybe.
                        int start = State.Instance.LoopStart == -1 ? 0 : State.Instance.LoopStart;
                        int end = State.Instance.LoopEnd == -1 ? State.Instance.Length : State.Instance.LoopEnd;
                        State.Instance.CurrentTick = MathUtils.Constrain(position, start, end);

                        Write(Utils.FormatBarTime(State.Instance.CurrentTick)); // echo
                        stat = NebStatus.Ok;
                    }
                    break;

                default:
                    Write("");
                    stat = NebStatus.InvalidCliArg;
                    break;
            }

            return stat;
        }

        //--------------------------------------------------------//
        NebStatus ReloadCmd(CommandDescriptor cmd, List<string> args)
        {
            NebStatus stat;

            switch (args.Count)
            {
                case 1: // no args
                    // TODO1 do something to reload script => ?? The application will watch for changes you make and indicate that reload is needed.
                    // - https://stackoverflow.com/questions/2812071/what-is-a-way-to-reload-lua-scripts-during-run-time
                    // - https://stackoverflow.com/questions/9369318/hot-swap-code-in-lua
                    stat = NebStatus.Ok;
                    break;

                default:
                    stat = NebStatus.InvalidCliArg;
                    break;
            }
            Write("");

            return stat;
        }

        //--------------------------------------------------------//
        NebStatus InfoCmd(CommandDescriptor _, List<string> __)
        {
            NebStatus stat = NebStatus.Ok;

            _cliOut.WriteLine($"Midi output devices");
            for (int i = 0; i < MidiOut.NumberOfDevices; i++)
            {
                _cliOut.WriteLine(MidiOut.DeviceInfo(i).ProductName);
            }

            _cliOut.WriteLine($"Midi input devices");
            for (int i = 0; i < MidiIn.NumberOfDevices; i++)
            {
                _cliOut.WriteLine(MidiIn.DeviceInfo(i).ProductName);
            }
            Write("");

            return stat;
        }

        //--------------------------------------------------------//
        NebStatus UsageCmd(CommandDescriptor _, List<string> __)
        {
            NebStatus stat = NebStatus.Ok;

            foreach (var cmd in _commands!)
            {
                _cliOut.WriteLine($"{cmd.LongName}|{cmd.ShortName}: {cmd.Info}");
                if (cmd.Args.Length > 0)
                {
                    // Maybe multiline args.
                    var parts = StringUtils.SplitByToken(cmd.Args, Environment.NewLine);
                    foreach (var arg in parts)
                    {
                        _cliOut.WriteLine($"    {arg}");
                    }
                }
            }
            Write("");

            return stat;
        }
        #endregion
    }
}