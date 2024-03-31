using System;
using System.Text;
using System.Collections.Generic;
using System.IO;
using Ephemera.NBagOfTricks.PNUT;
using Ephemera.NBagOfTricks.Slog;
using Interop;


namespace Nebulua.Test
{
    /// <summary>Test the simpler cli functions.</summary>
    public class CLI_BASIC : TestSuite
    {
        public override void RunSuite()
        {
            NebStatus stat;
            List<string> capture;
            UT_STOP_ON_FAIL(true);

            var st = State.Instance;
            st.PropertyChangeEvent += (sender, e) => { };

            MockCliIn cliIn = new();
            MockCliOut cliOut = new();
            var prompt = "%";
            var cli = new Nebulua.Cli(cliIn, cliOut, prompt);

            ///// Fat fingers.
            cliOut.Clear();
            cliIn.NextLine = "bbbbb";
            stat = cli.Read();
            UT_EQUAL(stat, NebStatus.Ok);
            capture = cliOut.CaptureLines;
            UT_EQUAL(capture.Count, 2);
            UT_EQUAL(capture[0], $"Invalid command");
            UT_EQUAL(capture[1], prompt);

            cliOut.Clear();
            cliIn.NextLine = "z";
            stat = cli.Read();
            UT_EQUAL(stat, NebStatus.Ok);
            capture = cliOut.CaptureLines;
            UT_EQUAL(capture.Count, 2);
            UT_EQUAL(capture[0], $"Invalid command");
            UT_EQUAL(capture[1], prompt);

            ///// These next two confirm proper full/short name handling.
            cliOut.Clear();
            cliIn.NextLine = "help";
            stat = cli.Read();
            UT_EQUAL(stat, NebStatus.Ok);
            capture = cliOut.CaptureLines;
            UT_EQUAL(capture.Count, 12);
            UT_EQUAL(capture[0], "help|?: tell me everything");
            UT_EQUAL(capture[1], "exit|x: exit the application");
            UT_EQUAL(capture[10], "reload|l: re/load current script");
            UT_EQUAL(capture[11], prompt);

            cliOut.Clear();
            cliIn.NextLine = "?";
            stat = cli.Read();
            UT_EQUAL(stat, NebStatus.Ok);
            capture = cliOut.CaptureLines;
            UT_EQUAL(capture.Count, 12);
            UT_EQUAL(capture[0], "help|?: tell me everything");

            ///// The rest of the commands.
            cliOut.Clear();
            cliIn.NextLine = "exit";
            stat = cli.Read();
            UT_EQUAL(stat, NebStatus.Ok);
            capture = cliOut.CaptureLines;
            UT_EQUAL(capture.Count, 2);
            UT_EQUAL(capture[0], $"goodbye!");

            st.ExecState = ExecState.Idle; // reset
            cliOut.Clear();
            cliIn.NextLine = "run";
            stat = cli.Read();
            UT_EQUAL(stat, NebStatus.Ok);
            capture = cliOut.CaptureLines;
            UT_EQUAL(capture.Count, 2);
            UT_EQUAL(capture[0], $"running");

            st.ExecState = ExecState.Idle; // reset
            cliOut.Clear();
            cliIn.NextLine = "reload";
            stat = cli.Read();
            UT_EQUAL(stat, NebStatus.Ok);
            capture = cliOut.CaptureLines;
            UT_EQUAL(capture.Count, 1);
            UT_EQUAL(capture[0], prompt);

            cliOut.Clear();
            cliIn.NextLine = "tempo";
            stat = cli.Read();
            UT_EQUAL(stat, NebStatus.Ok);
            capture = cliOut.CaptureLines;
            UT_EQUAL(capture.Count, 2);
            UT_EQUAL(capture[0], "100");

            cliOut.Clear();
            cliIn.NextLine = "tempo 182";
            stat = cli.Read();
            UT_EQUAL(stat, NebStatus.BadCliArg);
            capture = cliOut.CaptureLines;
            UT_EQUAL(capture.Count, 1);
            UT_EQUAL(capture[0], prompt);

            cliOut.Clear();
            cliIn.NextLine = "tempo 242";
            stat = cli.Read();
            UT_EQUAL(stat, NebStatus.BadCliArg);
            capture = cliOut.CaptureLines;
            UT_EQUAL(capture.Count, 2);
            UT_EQUAL(capture[0], "invalid tempo: 242");

            cliOut.Clear();
            cliIn.NextLine = "tempo 39";
            stat = cli.Read();
            UT_EQUAL(stat, NebStatus.BadCliArg);
            capture = cliOut.CaptureLines;
            UT_EQUAL(capture.Count, 2);
            UT_EQUAL(capture[0], "invalid tempo: 39");

            cliOut.Clear();
            cliIn.NextLine = "monitor in";
            stat = cli.Read();
            UT_EQUAL(stat, NebStatus.Ok);
            capture = cliOut.CaptureLines;
            UT_EQUAL(capture.Count, 1);
            UT_EQUAL(capture[0], prompt);

            cliOut.Clear();
            cliIn.NextLine = "monitor out";
            stat = cli.Read();
            UT_EQUAL(stat, NebStatus.Ok);
            capture = cliOut.CaptureLines;
            UT_EQUAL(capture.Count, 1);
            UT_EQUAL(capture[0], prompt);

            cliOut.Clear();
            cliIn.NextLine = "monitor off";
            stat = cli.Read();
            UT_EQUAL(stat, NebStatus.Ok);
            capture = cliOut.CaptureLines;
            UT_EQUAL(capture.Count, 1);
            UT_EQUAL(capture[0], prompt);

            cliOut.Clear();
            cliIn.NextLine = "monitor junk";
            stat = cli.Read();
            UT_EQUAL(stat, NebStatus.BadCliArg);
            capture = cliOut.CaptureLines;
            UT_EQUAL(capture.Count, 2);
            UT_EQUAL(capture[0], "invalid option: junk");

            // Wait for logger to stop.
            Thread.Sleep(100);
        }
    }


    /// <summary>Test cli functions that require a loaded script.</summary>
    public class CLI_CONTEXT : TestSuite
    {
        public override void RunSuite()
        {
            NebStatus stat;
            List<string> capture;
            UT_STOP_ON_FAIL(true);

            var st = State.Instance;
            st.PropertyChangeEvent += (sender, e) => { };

            MockCliIn cliIn = new();
            MockCliOut cliOut = new();
            var prompt = "%";
            var cli = new Nebulua.Cli(cliIn, cliOut, prompt);

            //app.Run("script_happy.lua");

            ///// Position commands. TODO1 fix these.
            cliOut.Clear();
            cliIn.NextLine = "position";
            stat = cli.Read();
            UT_EQUAL(stat, NebStatus.Ok);
            capture = cliOut.CaptureLines;
            UT_EQUAL(capture.Count, 2);
            UT_EQUAL(capture[0], $"0:0:0");
            UT_EQUAL(capture[1], prompt);

            //app.Clear();
            //cliIn.NextLine = "position 203:2:6";
            //stat = cli.Read();
            //UT_EQUAL(stat, NebStatus.Ok);
            //capture = cliOut.CaptureLines;
            //UT_EQUAL(capture.Count, 2);
            //UT_EQUAL(capture[0], $"203:2:6");
            //UT_EQUAL(capture[1], prompt);

            //app.Clear();
            //cliIn.NextLine = "position";
            //stat = cli.Read();
            //UT_EQUAL(stat, NebStatus.Ok);
            //capture = cliOut.CaptureLines;
            //UT_EQUAL(capture.Count, 2);
            //UT_EQUAL(capture[0], $"203:2:6");
            //UT_EQUAL(capture[1], prompt);

            //app.Clear();
            //cliIn.NextLine = "position 111:9:6";
            //stat = cli.Read();
            //UT_EQUAL(stat, NebStatus.Ok);
            //capture = cliOut.CaptureLines;
            //UT_EQUAL(capture.Count, 2);
            //UT_EQUAL(capture[0], $"invalid position: 111:9:6");
            //UT_EQUAL(capture[1], prompt);


            ///// Misc commands.
            cliOut.Clear();
            cliIn.NextLine = "kill";
            stat = cli.Read();
            UT_EQUAL(stat, NebStatus.Ok);
            capture = cliOut.CaptureLines;
            UT_EQUAL(capture.Count, 1);
            UT_EQUAL(capture[0], prompt);

            // Wait for logger to stop.
            Thread.Sleep(100);
        }
    }    

    /// <summary>Test entry.</summary>
    static class Program
    {
        [STAThread]
        static void Main(string[] _)
        {
            TestRunner runner = new(OutputFormat.Readable);
            var cases = new[] { "CLI" };
            runner.RunSuites(cases);
            File.WriteAllLines(@"_test.txt", runner.Context.OutputLines);
        }
    }
}
