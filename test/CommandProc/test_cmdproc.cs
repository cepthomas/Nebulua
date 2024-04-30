using System;
using System.Text;
using System.Collections.Generic;
using System.IO;
using Ephemera.NBagOfTricks.PNUT;
using Nebulua.CliApp;
using Nebulua.Common;


namespace Nebulua.Test
{
    /// <summary>Test the simpler functions.</summary>
    public class CMDPROC_BASIC : TestSuite
    {
        public override void RunSuite()
        {
            bool bret;
            UT_STOP_ON_FAIL(true);

            var st = State.Instance;
            st.ValueChangeEvent += (sender, e) => { };

            MockIn min = new();
            MockOut mout = new();
            var cmdProc = new CommandProc(min, mout) { Prompt = "%" };

            ///// Fat fingers.
            mout.Clear();
            min.NextLine = "bbbbb";
            bret = cmdProc.Read();
            UT_TRUE(bret);
            UT_EQUAL(mout.Capture.Count, 2);
            UT_EQUAL(mout.Capture[0], $"Invalid command");
            UT_EQUAL(mout.Capture[1], cmdProc.Prompt);

            mout.Clear();
            min.NextLine = "z";
            bret = cmdProc.Read();
            UT_TRUE(bret);
            UT_EQUAL(mout.Capture.Count, 2);
            UT_EQUAL(mout.Capture[0], $"Invalid command");
            UT_EQUAL(mout.Capture[1], cmdProc.Prompt);

            ///// These next two confirm proper full/short name handling.
            mout.Clear();
            min.NextLine = "help";
            bret = cmdProc.Read();
            UT_TRUE(bret);
            UT_EQUAL(mout.Capture.Count, 15);
            UT_EQUAL(mout.Capture[0], "help|?: available commands");
            UT_EQUAL(mout.Capture[1], "info|i: system information");
            UT_EQUAL(mout.Capture[13], "reload|s: reload current script");
            UT_EQUAL(mout.Capture[14], cmdProc.Prompt);

            mout.Clear();
            min.NextLine = "?";
            bret = cmdProc.Read();
            UT_TRUE(bret);
            UT_EQUAL(mout.Capture.Count, 15);
            UT_EQUAL(mout.Capture[0], "help|?: available commands");

            ///// The rest of the commands.
            mout.Clear();
            min.NextLine = "exit";
            bret = cmdProc.Read();
            UT_TRUE(bret);
            UT_EQUAL(mout.Capture.Count, 2);
            UT_EQUAL(mout.Capture[0], $"goodbye!");

            st.ExecState = ExecState.Idle; // reset
            mout.Clear();
            min.NextLine = "run";
            bret = cmdProc.Read();
            UT_TRUE(bret);
            UT_EQUAL(mout.Capture.Count, 2);
            UT_EQUAL(mout.Capture[0], $"running");

            st.ExecState = ExecState.Idle; // reset
            mout.Clear();
            min.NextLine = "reload";
            bret = cmdProc.Read();
            UT_TRUE(bret);
            UT_EQUAL(mout.Capture.Count, 1);
            UT_EQUAL(mout.Capture[0], cmdProc.Prompt);

            mout.Clear();
            min.NextLine = "tempo";
            bret = cmdProc.Read();
            UT_TRUE(bret);
            UT_EQUAL(mout.Capture.Count, 2);
            UT_EQUAL(mout.Capture[0], "100");

            mout.Clear();
            min.NextLine = "tempo 182";
            bret = cmdProc.Read();
            UT_TRUE(bret);
            UT_EQUAL(mout.Capture.Count, 1);
            UT_EQUAL(mout.Capture[0], cmdProc.Prompt);

            mout.Clear();
            min.NextLine = "tempo 242";
            bret = cmdProc.Read();
            UT_FALSE(bret);
            UT_EQUAL(mout.Capture.Count, 2);
            UT_EQUAL(mout.Capture[0], "invalid tempo: 242");

            mout.Clear();
            min.NextLine = "tempo 39";
            bret = cmdProc.Read();
            UT_FALSE(bret);
            UT_EQUAL(mout.Capture.Count, 2);
            UT_EQUAL(mout.Capture[0], "invalid tempo: 39");

            mout.Clear();
            min.NextLine = "monitor r";
            bret = cmdProc.Read();
            UT_TRUE(bret);
            UT_EQUAL(mout.Capture.Count, 1);
            UT_EQUAL(mout.Capture[0], cmdProc.Prompt);

            mout.Clear();
            min.NextLine = "monitor s";
            bret = cmdProc.Read();
            UT_TRUE(bret);
            UT_EQUAL(mout.Capture.Count, 1);
            UT_EQUAL(mout.Capture[0], cmdProc.Prompt);

            mout.Clear();
            min.NextLine = "monitor o";
            bret = cmdProc.Read();
            UT_TRUE(bret);
            UT_EQUAL(mout.Capture.Count, 1);
            UT_EQUAL(mout.Capture[0], cmdProc.Prompt);

            mout.Clear();
            min.NextLine = "monitor junk";
            bret = cmdProc.Read();
            UT_FALSE(bret);
            UT_EQUAL(mout.Capture.Count, 2);
            UT_EQUAL(mout.Capture[0], "invalid option: junk");

            // Wait for logger to stop.
            Thread.Sleep(100);
        }
    }

    /// <summary>Test functions that interact with running script.</summary>
    public class CMDPROC_CONTEXT : TestSuite
    {
        public override void RunSuite()
        {
            bool bret;
            UT_STOP_ON_FAIL(true);

            var st = State.Instance;
            st.ValueChangeEvent += (sender, e) => { };

            MockIn min = new();
            MockOut mout = new();
            var cmdProc = new CommandProc(min, mout) { Prompt = "%" };

            //app.Run("script_happy.lua");

            ///// Position commands. TODO fix/test these - sim Api?
            mout.Clear();
            min.NextLine = "position";
            bret = cmdProc.Read();
            UT_TRUE(bret);
            UT_EQUAL(mout.Capture.Count, 2);
            UT_EQUAL(mout.Capture[0], $"0:0:0");
            UT_EQUAL(mout.Capture[1], cmdProc.Prompt);

            // Fake valid loaded script.
            // pos 6518 <-> 203:2:6
            State.Instance.LoopStart = 100;
            State.Instance.LoopEnd = 7000;

            mout.Clear();
            min.NextLine = "position 203:2:6";
            bret = cmdProc.Read();
            UT_TRUE(bret);
            UT_EQUAL(mout.Capture.Count, 2);
            UT_EQUAL(mout.Capture[0], $"203:2:6");
            UT_EQUAL(mout.Capture[1], cmdProc.Prompt);

            mout.Clear();
            min.NextLine = "position";
            bret = cmdProc.Read();
            UT_TRUE(bret);
            UT_EQUAL(mout.Capture.Count, 2);
            UT_EQUAL(mout.Capture[0], $"203:2:6");
            UT_EQUAL(mout.Capture[1], cmdProc.Prompt);

            mout.Clear();
            min.NextLine = "position 111:9:6";
            bret = cmdProc.Read();
            UT_FALSE(bret);
            UT_EQUAL(mout.Capture.Count, 2);
            UT_EQUAL(mout.Capture[0], $"invalid position: 111:9:6");
            UT_EQUAL(mout.Capture[1], cmdProc.Prompt);

            ///// Misc commands.
            mout.Clear();
            min.NextLine = "kill";
            bret = cmdProc.Read();
            UT_TRUE(bret);
            UT_EQUAL(mout.Capture.Count, 1);
            UT_EQUAL(mout.Capture[0], cmdProc.Prompt);

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
            var cases = new[] { "CMDPROC" };
            runner.RunSuites(cases);
            File.WriteAllLines(@"_test.txt", runner.Context.OutputLines);
        }
    }
}
