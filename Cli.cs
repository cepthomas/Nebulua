using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NAudio.Midi;
using Ephemera.NBagOfTricks;
using Ephemera.NBagOfTricks.Slog;


namespace Nebulua
{
    public partial class App
    {
        /// <summary>Cli command descriptor.</summary>
        readonly record struct CommandDescriptor
        (
            /// <summary>If you like to type.</summary>
            string long_name,
            /// <summary>If you don't.</summary>
            char short_name,
            /// <summary>FUTURE Optional single char for immediate execution (no CR required). Can be ^(ctrl) or ~(alt) in conjunction with short_name.</summary>
            char immediate_key,
            /// <summary>Free text for command description.</summary>
            string info,
            /// <summary>Free text for args description.</summary>
            string args,
            /// <summary>The runtime handler.</summary>
            CommandHandler handler
        );

        /// <summary>Cli command handler.</summary>
        delegate int CommandHandler(CommandDescriptor cmd, List<string> args);

        /// <summary>All the commands.</summary>
        CommandDescriptor[] _commands;

        /// <summary>CLI prompt.</summary>
        string _prompt = "$";

        /// <summary>
        /// Set up command table.
        /// </summary>
        void InitCommands()
        {
            _commands =
            [
                new("help", '?', '\0', "tell me everything", "", _Usage),
                new("exit", 'x', '\0', "exit the application", "", _ExitCmd),
                new("run", 'r', ' ', "toggle running the script", "", _RunCmd),
                new("tempo", 't', '\0', "get or set the tempo", "(bpm): 40-240", _TempoCmd),
                new("monitor", 'm', '^', "toggle monitor midi traffic", "(in|out|off): action", _MonCmd),
                new("kill", 'k', '~', "stop all midi", "", _KillCmd),
                new("position", 'p', '\0', "set position to where or tell current", "(where): bar:beat:sub", _PositionCmd),
                new("reload", 'l', '\0', "re/load current script", "", _ReloadCmd)
            ];
        }

        /// <summary>
        /// Process user input.
        /// </summary>
        /// <returns>Status</returns>
        int DoCli()
        {
            int stat = Defs.NEB_OK;

            // Prompt.
            _cliOut.Write(_prompt);

            // Listen.
            string res = _cliIn.ReadLine();

            if (res != null)
            {
                // Process the line. Chop up the raw command line into args.
                List<string> args = StringUtils.SplitByToken(res, " ");

                // Process the command and its options.
                bool valid = false;
                if (args.Count > 0)
                {
                    foreach (var cmd in _commands)
                    {
                        if (args[0] == cmd.long_name || (args[0].Length == 1 && args[0][0] == cmd.short_name))
                        {
                            // Execute the command. They handle any errors internally.
                            valid = true;
                            // Lock access to lua context.
                            ENTER_CRITICAL_SECTION();
                            stat = cmd.handler(cmd, args);
                            // ok = _EvalStatus(stat, "handler failed: %s", cmd->desc.long_name);
                            EXIT_CRITICAL_SECTION();
                            break;
                        }
                    }

                    if (!valid)
                    {
                        _cliOut.WriteLine("Invalid command");
                    }
                }
            }
            else
            {
                // Assume finished.
                _appRunning = false;
            }

            return stat;
        }

        //--------------------------------------------------------//
        int _TempoCmd(CommandDescriptor cmd, List<string> args)
        {
            int stat = Defs.NEB_ERR_BAD_CLI_ARG;

            if (args.Count == 1) // get
            {
                _cliOut.WriteLine($"{_tempo}");
                stat = Defs.NEB_OK;
            }
            else if (args.Count == 2) // set
            {
                if (int.TryParse(args[1], out int t) && t >= 40 && t <= 240)
                {
                    _tempo = t;
                    SetTimer(_tempo);
                }
                else
                {
                    _cliOut.WriteLine($"invalid tempo: {args[1]}");
                }
            }

            return stat;
        }

        //--------------------------------------------------------//
        int _RunCmd(CommandDescriptor cmd, List<string> args)
        {
            int stat = Defs.NEB_ERR_BAD_CLI_ARG;

            if (args.Count == 1) // no args
            {
                _scriptRunning = !_scriptRunning;
                stat = Defs.NEB_OK;
            }

            return stat;
        }

        //--------------------------------------------------------//
        int _ExitCmd(CommandDescriptor cmd, List<string> args)
        {
            int stat = Defs.NEB_ERR_BAD_CLI_ARG;

            if (args.Count == 1) // no args
            {
                _scriptRunning = false;
                _appRunning = false;
                stat = Defs.NEB_OK;
            }

            return stat;
        }

        //--------------------------------------------------------//
        int _MonCmd(CommandDescriptor cmd, List<string> args)
        {
            int stat = Defs.NEB_ERR_BAD_CLI_ARG;

            if (args.Count == 2) // set
            {
                switch(args[1])
                {
                    case "in":
                        _monInput = !_monInput;
                        stat = Defs.NEB_OK;
                        break;

                    case "out":
                        _monOutput = !_monOutput;
                        stat = Defs.NEB_OK;
                        break;

                    case "off":
                        _monInput = false;
                        _monOutput = false;
                        stat = Defs.NEB_OK;
                        break;

                    default:
                        _cliOut.WriteLine("invalid option: %s", args[1]);
                        break;
                }
            }

            return stat;
        }

        //--------------------------------------------------------//
        int _KillCmd(CommandDescriptor cmd, List<string> args)
        {
            int stat = Defs.NEB_ERR_BAD_CLI_ARG;

            if (args.Count == 1) // no args
            {
                KillAll();
                stat = Defs.NEB_OK;
            }

            return stat;
        }

        //--------------------------------------------------------//
        int _PositionCmd(CommandDescriptor cmd, List<string> args)
        {
            int stat = Defs.NEB_ERR_BAD_CLI_ARG;

            if (args.Count == 1) // get
            {
                _cliOut.WriteLine("%s", Utils.FormatBarTime(_currentTick));
                stat = Defs.NEB_OK;
            }
            else if (args.Count == 2) // set
            {
                int position = Utils.ParseBarTime(args[1]);
                if (position < 0)
                {
                    _cliOut.WriteLine($"invalid position: {args[1]}");
                }
                else
                {
                    // Limit range maybe.
                    int start = _loopStart == -1 ? 0 : _loopStart;
                    int end = _loopEnd == -1 ? _length : _loopEnd;
                    position = MathUtils.Constrain(position, start, end);

                    _cliOut.WriteLine("%s", Utils.FormatBarTime(_currentTick)); // echo
                    stat = Defs.NEB_OK;
                }
            }

            return stat;
        }

        //--------------------------------------------------------//
        int _ReloadCmd(CommandDescriptor cmd, List<string> args)
        {
            int stat = Defs.NEB_ERR_BAD_CLI_ARG;

            if (args.Count == 1) // no args
            {
                // TODO2 do something to reload script =>
                // - https://stackoverflow.com/questions/2812071/what-is-a-way-to-reload-lua-scripts-during-run-time
                // - https://stackoverflow.com/questions/9369318/hot-swap-code-in-lua
                stat = Defs.NEB_OK;
            }

            return stat;
        }

        //--------------------------------------------------------//
        int _Usage(CommandDescriptor _, List<string> __)
        {
            int stat = Defs.NEB_OK;

            foreach (var cmd in _commands)
            {
                _cliOut.WriteLine($"{cmd.long_name}|{cmd.short_name}: {cmd.info}");
                if (cmd.args.Length > 0)
                {
                    // Maybe multiline args.
                    var parts = StringUtils.SplitByToken(cmd.args, Environment.NewLine);
                    foreach(var arg in parts)
                    {
                        _cliOut.WriteLine($"    {arg}");
                    }
                }
            }

            return stat;
        }
    }
}