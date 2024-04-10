using System;
using System.Text;
using System.Collections.Generic;
using System.IO;
using Ephemera.NBagOfTricks.PNUT;


namespace Nebulua.Test
{
    /// <summary>Test the simpler cli functions.</summary>
    public class CLI_BASIC : TestSuite
    {
        public override void RunSuite()
        {
            bool bret;
            List<string> capture;
            UT_STOP_ON_FAIL(true);

            var st = State.Instance;
            st.PropertyChangeEvent += (sender, e) => { };

            MockCliIn cliIn = new();
            MockCliOut cliOut = new();
            var cli = new Cli(cliIn, cliOut) { Prompt = "%" };

            ///// Fat fingers.
            cliOut.Clear();
            cliIn.NextLine = "bbbbb";
            bret = cli.Read();
            UT_TRUE(bret);
            capture = cliOut.CaptureLines;
            UT_EQUAL(capture.Count, 2);
            UT_EQUAL(capture[0], $"Invalid command");
            UT_EQUAL(capture[1], cli.Prompt);

            cliOut.Clear();
            cliIn.NextLine = "z";
            bret = cli.Read();
            UT_TRUE(bret);
            capture = cliOut.CaptureLines;
            UT_EQUAL(capture.Count, 2);
            UT_EQUAL(capture[0], $"Invalid command");
            UT_EQUAL(capture[1], cli.Prompt);

            ///// These next two confirm proper full/short name handling.
            cliOut.Clear();
            cliIn.NextLine = "help";
            bret = cli.Read();
            UT_TRUE(bret);
            capture = cliOut.CaptureLines;
            UT_EQUAL(capture.Count, 12);
            UT_EQUAL(capture[0], "help|?: tell me everything");
            UT_EQUAL(capture[1], "exit|x: exit the application");
            UT_EQUAL(capture[10], "reload|l: re/load current script");
            UT_EQUAL(capture[11], cli.Prompt);

            cliOut.Clear();
            cliIn.NextLine = "?";
            bret = cli.Read();
            UT_TRUE(bret);
            capture = cliOut.CaptureLines;
            UT_EQUAL(capture.Count, 12);
            UT_EQUAL(capture[0], "help|?: tell me everything");

            ///// The rest of the commands.
            cliOut.Clear();
            cliIn.NextLine = "exit";
            bret = cli.Read();
            UT_TRUE(bret);
            capture = cliOut.CaptureLines;
            UT_EQUAL(capture.Count, 2);
            UT_EQUAL(capture[0], $"goodbye!");

            st.ExecState = ExecState.Idle; // reset
            cliOut.Clear();
            cliIn.NextLine = "run";
            bret = cli.Read();
            UT_TRUE(bret);
            capture = cliOut.CaptureLines;
            UT_EQUAL(capture.Count, 2);
            UT_EQUAL(capture[0], $"running");

            st.ExecState = ExecState.Idle; // reset
            cliOut.Clear();
            cliIn.NextLine = "reload";
            bret = cli.Read();
            UT_TRUE(bret);
            capture = cliOut.CaptureLines;
            UT_EQUAL(capture.Count, 1);
            UT_EQUAL(capture[0], cli.Prompt);

            cliOut.Clear();
            cliIn.NextLine = "tempo";
            bret = cli.Read();
            UT_TRUE(bret);
            capture = cliOut.CaptureLines;
            UT_EQUAL(capture.Count, 2);
            UT_EQUAL(capture[0], "100");

            cliOut.Clear();
            cliIn.NextLine = "tempo 182";
            bret = cli.Read();
            UT_FALSE(bret);
            capture = cliOut.CaptureLines;
            UT_EQUAL(capture.Count, 1);
            UT_EQUAL(capture[0], cli.Prompt);

            cliOut.Clear();
            cliIn.NextLine = "tempo 242";
            bret = cli.Read();
            UT_FALSE(bret);
            capture = cliOut.CaptureLines;
            UT_EQUAL(capture.Count, 2);
            UT_EQUAL(capture[0], "invalid tempo: 242");

            cliOut.Clear();
            cliIn.NextLine = "tempo 39";
            bret = cli.Read();
            UT_FALSE(bret);
            capture = cliOut.CaptureLines;
            UT_EQUAL(capture.Count, 2);
            UT_EQUAL(capture[0], "invalid tempo: 39");

            cliOut.Clear();
            cliIn.NextLine = "monitor in";
            bret = cli.Read();
            UT_TRUE(bret);
            capture = cliOut.CaptureLines;
            UT_EQUAL(capture.Count, 1);
            UT_EQUAL(capture[0], cli.Prompt);

            cliOut.Clear();
            cliIn.NextLine = "monitor out";
            bret = cli.Read();
            UT_TRUE(bret);
            capture = cliOut.CaptureLines;
            UT_EQUAL(capture.Count, 1);
            UT_EQUAL(capture[0], cli.Prompt);

            cliOut.Clear();
            cliIn.NextLine = "monitor off";
            bret = cli.Read();
            UT_TRUE(bret);
            capture = cliOut.CaptureLines;
            UT_EQUAL(capture.Count, 1);
            UT_EQUAL(capture[0], cli.Prompt);

            cliOut.Clear();
            cliIn.NextLine = "monitor junk";
            bret = cli.Read();
            UT_FALSE(bret);
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
            bool bret;
            List<string> capture;
            UT_STOP_ON_FAIL(true);

            var st = State.Instance;
            st.PropertyChangeEvent += (sender, e) => { };

            MockCliIn cliIn = new();
            MockCliOut cliOut = new();
            var cli = new Cli(cliIn, cliOut) { Prompt = "%" };

            //app.Run("script_happy.lua");

            ///// Position commands. TODO1 fix/test these.
            cliOut.Clear();
            cliIn.NextLine = "position";
            bret = cli.Read();
            UT_TRUE(bret);
            capture = cliOut.CaptureLines;
            UT_EQUAL(capture.Count, 2);
            UT_EQUAL(capture[0], $"0:0:0");
            UT_EQUAL(capture[1], cli.Prompt);

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
            bret = cli.Read();
            UT_TRUE(bret);
            capture = cliOut.CaptureLines;
            UT_EQUAL(capture.Count, 1);
            UT_EQUAL(capture[0], cli.Prompt);

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
