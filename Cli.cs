using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using NAudio.Midi;
using Ephemera.NBagOfTricks;
using Ephemera.NBagOfTricks.Slog;


namespace Nebulua
{
    public class Cli
    {
        #region Commands
        /// <summary>Cli command descriptor.</summary>
        readonly record struct CommandDescriptor
        (
            /// <summary>If you like to type.</summary>
            string LongName,
            /// <summary>If you don't.</summary>
            char ShortName,
            /// <summary>TODO3 Optional single char for immediate execution (no CR required). Can be ^(ctrl) or ~(alt) in conjunction with short_name.</summary>
            char ImmediateKey,
            /// <summary>Free text for command description.</summary>
            string Info,
            /// <summary>Free text for args description.</summary>
            string Args,
            /// <summary>The runtime handler.</summary>
            CommandHandler Handler
        );

        /// <summary>Cli command handler.</summary>
        delegate int CommandHandler(CommandDescriptor cmd, List<string> args);

        /// <summary>All the commands.</summary>
        readonly CommandDescriptor[]? _commands;

        /// <summary>CLI prompt.</summary>
        readonly string _prompt = "?";
        #endregion

        #region IO
        /// <summary>CLI.</summary>
        readonly TextWriter _cliOut;

        /// <summary>CLI.</summary>
        readonly TextReader _cliIn;
        #endregion

        /// <summary>
        /// Set up command table.
        /// </summary>
        public Cli(TextReader cliIn, TextWriter cliOut, string prompt)
        {
            _cliIn = cliIn;
            _cliOut = cliOut;
            _prompt = prompt;

            _commands =
            [
                new("help",     '?',    '\0',   "tell me everything",                       "",                         UsageCmd),
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
            _cliOut.Write(_prompt);
        }

        /// <summary>
        /// Process user input.
        /// </summary>
        /// <returns>Status</returns>
        public int Read()
        {
            int stat = Defs.NEB_OK;

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
                            // Lock access to lua context.
                            Utils.ENTER_CRITICAL_SECTION();
                            stat = cmd.Handler(cmd, args);
                            // ok = _EvalStatus(stat, "handler failed: %s", cmd->desc.long_name);
                            Utils.EXIT_CRITICAL_SECTION();
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

        //--------------------------------------------------------//
        int TempoCmd(CommandDescriptor cmd, List<string> args)
        {
            int stat = Defs.NEB_ERR_BAD_CLI_ARG;

            if (args.Count == 1) // get
            {
                Write($"{State.Instance.Tempo}");
                stat = Defs.NEB_OK;
            }
            else if (args.Count == 2) // set
            {
                if (int.TryParse(args[1], out int t) && t >= 40 && t <= 240)
                {
                    State.Instance.Tempo = t;
                    Write("");
                }
                else
                {
                    Write($"invalid tempo: {args[1]}");
                }
            }

            return stat;
        }

        //--------------------------------------------------------//
        int RunCmd(CommandDescriptor cmd, List<string> args)
        {
            int stat = Defs.NEB_ERR_BAD_CLI_ARG;

            if (args.Count == 1) // no args
            {
                stat = Defs.NEB_OK;
                Write(State.Instance.ExecState == ExecState.Run ? "running" : "stopped");
            }
            else
            {
                Write($"invalid command");
            }

            return stat;
        }

        //--------------------------------------------------------//
        int ExitCmd(CommandDescriptor cmd, List<string> args)
        {
            int stat = Defs.NEB_OK;

            State.Instance.ExecState = ExecState.Exit;
            Write($"goodbye!");

            return stat;
        }

        //--------------------------------------------------------//
        int MonCmd(CommandDescriptor cmd, List<string> args)
        {
            int stat = Defs.NEB_ERR_BAD_CLI_ARG;

            if (args.Count == 2) // set
            {
                switch(args[1])
                {
                    case "in":
                        State.Instance.MonInput = !State.Instance.MonInput;
                        stat = Defs.NEB_OK;
                        Write("");
                        break;

                    case "out":
                        State.Instance.MonOutput = !State.Instance.MonOutput;
                        stat = Defs.NEB_OK;
                        Write("");
                        break;

                    case "off":
                        State.Instance.MonInput = false;
                        State.Instance.MonOutput = false;
                        stat = Defs.NEB_OK;
                        Write("");
                        break;

                    default:
                        Write($"invalid option: {args[1]}");
                        break;
                }
            }

            return stat;
        }

        //--------------------------------------------------------//
        int KillCmd(CommandDescriptor cmd, List<string> args)
        {
            int stat = Defs.NEB_ERR_BAD_CLI_ARG;

            if (args.Count == 1) // no args
            {
                State.Instance.ExecState = ExecState.Kill;
                stat = Defs.NEB_OK;
            }
            Write("");

            return stat;
        }

        //--------------------------------------------------------//
        int PositionCmd(CommandDescriptor cmd, List<string> args)
        {
            int stat = Defs.NEB_ERR_BAD_CLI_ARG;

            if (args.Count == 1) // get
            {
                Write(Utils.FormatBarTime(State.Instance.CurrentTick));
                stat = Defs.NEB_OK;
            }
            else if (args.Count == 2) // set
            {
                int position = Utils.ParseBarTime(args[1]);
                if (position < 0)
                {
                    Write($"invalid position: {args[1]}");
                }
                else
                {
                    // Limit range maybe.
                    int start = State.Instance.LoopStart == -1 ? 0 : State.Instance.LoopStart;
                    int end = State.Instance.LoopEnd == -1 ? State.Instance.Length : State.Instance.LoopEnd;
                    State.Instance.CurrentTick = MathUtils.Constrain(position, start, end);

                    Write(Utils.FormatBarTime(State.Instance.CurrentTick)); // echo
                    stat = Defs.NEB_OK;
                }
            }

            return stat;
        }

        //--------------------------------------------------------//
        int ReloadCmd(CommandDescriptor cmd, List<string> args)
        {
            int stat = Defs.NEB_ERR_BAD_CLI_ARG;

            if (args.Count == 1) // no args
            {
                // TODO2 do something to reload script =>
                // - https://stackoverflow.com/questions/2812071/what-is-a-way-to-reload-lua-scripts-during-run-time
                // - https://stackoverflow.com/questions/9369318/hot-swap-code-in-lua
                stat = Defs.NEB_OK;
            }
            Write("");

            return stat;
        }

        //--------------------------------------------------------//
        int UsageCmd(CommandDescriptor _, List<string> __)
        {
            int stat = Defs.NEB_OK;

            foreach (var cmd in _commands!)
            {
                _cliOut.WriteLine($"{cmd.LongName}|{cmd.ShortName}: {cmd.Info}");
                if (cmd.Args.Length > 0)
                {
                    // Maybe multiline args.
                    var parts = StringUtils.SplitByToken(cmd.Args, Environment.NewLine);
                    foreach(var arg in parts)
                    {
                        _cliOut.WriteLine($"    {arg}");
                    }
                }
            }
            Write("");

            return stat;
        }
    }
}