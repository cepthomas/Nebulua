using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
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

            /// <summary>Free text for command description.</summary>
            string Info,

            /// <summary>Free text for args description.</summary>
            string Args,
            
            /// <summary>The runtime handler.</summary>
            CommandHandler Handler
        );

        /// <summary>Cli command handler.</summary>
        delegate bool CommandHandler(CommandDescriptor cmd, List<string> args);
        #endregion

        #region Main work
        /// <summary>
        /// Set up command handler. TODO other stream I/O e.g. socket.
        /// </summary>
        public Cli(TextReader cliIn, TextWriter cliOut)
        {
            _cliIn = cliIn;
            _cliOut = cliOut;

            _commands =
            [
                new("help",     '?',    "available commands",                       "",                         UsageCmd),
                new("info",     'i',    "system information",                       "",                         InfoCmd),
                new("exit",     'x',    "exit the application",                     "",                         ExitCmd),
                new("run",      'r',    "toggle running the script",                "",                         RunCmd),
                new("tempo",    't',    "get or set the tempo",                     "(bpm): 40-240",            TempoCmd),
                new("monitor",  'm',    "toggle monitor midi traffic",              "(rx|tx|off): action",      MonCmd),
                new("kill",     'k',    "stop all midi",                            "",                         KillCmd),
                new("position", 'p',    "set position to where or tell current",    "(where): bar:beat:sub",    PositionCmd),
                new("reload",   'l',    "re/load current script",                   "",                         ReloadCmd)
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
            _cliOut.WriteLine(s);
            _cliOut.Write(Prompt);
        }


        public bool Read_XXX()
        {
            bool ret = true;

            //StringBuilder sb = new();

            //while (_cliIn.Peek() > -1)
            //{
            //    Console.Write(_cliIn.Read());
            //}
            //return true;

            //Seems that this is a known bug in StreamReader implementation, as suggested in this answer.The workaround in your case is to use asynchronous stream methods like StreamReader.ReadAsync or StreamReader.ReadLineAsync.
            //https://stackoverflow.com/questions/4557591/alternative-to-streamreader-peek-and-thread-interrupt
            int ch = _cliIn.Peek();

            switch (ch)
            {
                case -1:
                    Thread.Sleep(1);
                    break;

                case ' ':
                    RunCmd(new(), new() { "this is stupid" });
                    _cliIn.Read(); // flush
                    break;

                default:
                    DoCmd();
                    break;
            }


            void DoCmd()
            {
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
            }

            return ret;
        }




        /// <summary>
        /// Process user input. Blocks until new line. TODO1 like to peek for spacebar
        /// </summary>
        /// <returns>Success</returns>
        public bool Read_orig()
        {
            bool ret = true;

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
            Write($"goodbye!");

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
                        case "rx":
                            State.Instance.MonRx = !State.Instance.MonRx;
                            Write("");
                            break;

                        case "tx":
                            State.Instance.MonTx = !State.Instance.MonTx;
                            Write("");
                            break;

                        case "off":
                            State.Instance.MonRx = false;
                            State.Instance.MonTx = false;
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
                    int position = MusicTime.Parse(args[1]);
                    if (position < 0)
                    {
                        Write($"invalid position: {args[1]}");
                        ret = false;
                    }
                    else
                    {
                        // Limit range maybe.
                        int start = State.Instance.LoopStart == -1 ? 0 : State.Instance.LoopStart;
                        int end = State.Instance.LoopEnd == -1 ? State.Instance.Length : State.Instance.LoopEnd;
                        State.Instance.CurrentTick = MathUtils.Constrain(position, start, end);

                        Write(MusicTime.Format(State.Instance.CurrentTick)); // echo
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
        bool ReloadCmd(CommandDescriptor cmd, List<string> args)
        {
            bool ret = true;

            switch (args.Count)
            {
                case 1: // no args
                    // TODO1 do something to reload script without exiting app. App detect/indicate changed file?
                    // - https://stackoverflow.com/questions/2812071/what-is-a-way-to-reload-lua-scripts-during-run-time
                    // - https://stackoverflow.com/questions/9369318/hot-swap-code-in-lua
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
            _cliOut.WriteLine($"Midi output devices:");
            for (int i = 0; i < MidiOut.NumberOfDevices; i++)
            {
                _cliOut.WriteLine("  " + MidiOut.DeviceInfo(i).ProductName);
            }

            _cliOut.WriteLine($"Midi input devices:");
            for (int i = 0; i < MidiIn.NumberOfDevices; i++)
            {
                _cliOut.WriteLine("  " + MidiIn.DeviceInfo(i).ProductName);
            }
            Write("");

            return true;
        }

        //--------------------------------------------------------//
        bool UsageCmd(CommandDescriptor _, List<string> __)
        {
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

            return true;
        }
        #endregion
    }
}