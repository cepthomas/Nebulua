using System;
using System.Text;
using System.Collections.Generic;
using System.IO;
using Ephemera.NBagOfTricks.PNUT;


namespace Ephemera.Nebulua.Test
{
    /// <summary>Test the simpler functions.</summary>
    public class CMDPROC_BASIC : TestSuite
    {
        public override void RunSuite()
        {
            bool bret;
            UT_STOP_ON_FAIL(true);

            var st = State.Instance;
            st.PropertyChangeEvent += (sender, e) => { };

            MockIn in = new();
            MockOut out = new();
            var cmdProc = new CommandProc(in, out) { Prompt = "%" };

            ///// Fat fingers.
            out.Clear();
            in.NextLine = "bbbbb";
            bret = cmdProc.Read();
            UT_TRUE(bret);
            UT_EQUAL(out.Capture.Count, 2);
            UT_EQUAL(out.Capture[0], $"Invalid command");
            UT_EQUAL(out.Capture[1], cmdProc.Prompt);

            out.Clear();
            in.NextLine = "z";
            bret = cmdProc.Read();
            UT_TRUE(bret);
            UT_EQUAL(out.Capture.Count, 2);
            UT_EQUAL(out.Capture[0], $"Invalid command");
            UT_EQUAL(out.Capture[1], cmdProc.Prompt);

            ///// These next two confirm proper full/short name handling.
            out.Clear();
            in.NextLine = "help";
            bret = cmdProc.Read();
            UT_TRUE(bret);
            UT_EQUAL(out.Capture.Count, 15);
            UT_EQUAL(out.Capture[0], "help|?: available commands");
            UT_EQUAL(out.Capture[1], "info|i: system information");
            UT_EQUAL(out.Capture[13], "reload|s: reload current script");
            UT_EQUAL(out.Capture[14], cmdProc.Prompt);

            out.Clear();
            in.NextLine = "?";
            bret = cmdProc.Read();
            UT_TRUE(bret);
            UT_EQUAL(out.Capture.Count, 15);
            UT_EQUAL(out.Capture[0], "help|?: available commands");

            ///// The rest of the commands.
            out.Clear();
            in.NextLine = "exit";
            bret = cmdProc.Read();
            UT_TRUE(bret);
            UT_EQUAL(out.Capture.Count, 2);
            UT_EQUAL(out.Capture[0], $"goodbye!");

            st.ExecState = ExecState.Idle; // reset
            out.Clear();
            in.NextLine = "run";
            bret = cmdProc.Read();
            UT_TRUE(bret);
            UT_EQUAL(out.Capture.Count, 2);
            UT_EQUAL(out.Capture[0], $"running");

            st.ExecState = ExecState.Idle; // reset
            out.Clear();
            in.NextLine = "reload";
            bret = cmdProc.Read();
            UT_TRUE(bret);
            UT_EQUAL(out.Capture.Count, 1);
            UT_EQUAL(out.Capture[0], cmdProc.Prompt);

            out.Clear();
            in.NextLine = "tempo";
            bret = cmdProc.Read();
            UT_TRUE(bret);
            UT_EQUAL(out.Capture.Count, 2);
            UT_EQUAL(out.Capture[0], "100");

            out.Clear();
            in.NextLine = "tempo 182";
            bret = cmdProc.Read();
            UT_TRUE(bret);
            UT_EQUAL(out.Capture.Count, 1);
            UT_EQUAL(out.Capture[0], cmdProc.Prompt);

            out.Clear();
            in.NextLine = "tempo 242";
            bret = cmdProc.Read();
            UT_FALSE(bret);
            UT_EQUAL(out.Capture.Count, 2);
            UT_EQUAL(out.Capture[0], "invalid tempo: 242");

            out.Clear();
            in.NextLine = "tempo 39";
            bret = cmdProc.Read();
            UT_FALSE(bret);
            UT_EQUAL(out.Capture.Count, 2);
            UT_EQUAL(out.Capture[0], "invalid tempo: 39");

            out.Clear();
            in.NextLine = "monitor r";
            bret = cmdProc.Read();
            UT_TRUE(bret);
            UT_EQUAL(out.Capture.Count, 1);
            UT_EQUAL(out.Capture[0], cmdProc.Prompt);

            out.Clear();
            in.NextLine = "monitor s";
            bret = cmdProc.Read();
            UT_TRUE(bret);
            UT_EQUAL(out.Capture.Count, 1);
            UT_EQUAL(out.Capture[0], cmdProc.Prompt);

            out.Clear();
            in.NextLine = "monitor o";
            bret = cmdProc.Read();
            UT_TRUE(bret);
            UT_EQUAL(out.Capture.Count, 1);
            UT_EQUAL(out.Capture[0], cmdProc.Prompt);

            out.Clear();
            in.NextLine = "monitor junk";
            bret = cmdProc.Read();
            UT_FALSE(bret);
            UT_EQUAL(out.Capture.Count, 2);
            UT_EQUAL(out.Capture[0], "invalid option: junk");

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
            st.PropertyChangeEvent += (sender, e) => { };

            MockIn in = new();
            MockOut out = new();
            var cmdProc = new CommandProc(in, out) { Prompt = "%" };

            //app.Run("script_happy.lua");

            ///// Position commands. TODO fix/test these - sim Api?
            out.Clear();
            in.NextLine = "position";
            bret = cmdProc.Read();
            UT_TRUE(bret);
            UT_EQUAL(out.Capture.Count, 2);
            UT_EQUAL(out.Capture[0], $"0:0:0");
            UT_EQUAL(out.Capture[1], cmdProc.Prompt);

            // Fake valid loaded script.
            // pos 6518 <-> 203:2:6
            State.Instance.LoopStart = 100;
            State.Instance.LoopEnd = 7000;

            out.Clear();
            in.NextLine = "position 203:2:6";
            bret = cmdProc.Read();
            UT_TRUE(bret);
            UT_EQUAL(out.Capture.Count, 2);
            UT_EQUAL(out.Capture[0], $"203:2:6");
            UT_EQUAL(out.Capture[1], cmdProc.Prompt);

            out.Clear();
            in.NextLine = "position";
            bret = cmdProc.Read();
            UT_TRUE(bret);
            UT_EQUAL(out.Capture.Count, 2);
            UT_EQUAL(out.Capture[0], $"203:2:6");
            UT_EQUAL(out.Capture[1], cmdProc.Prompt);

            out.Clear();
            in.NextLine = "position 111:9:6";
            bret = cmdProc.Read();
            UT_FALSE(bret);
            UT_EQUAL(out.Capture.Count, 2);
            UT_EQUAL(out.Capture[0], $"invalid position: 111:9:6");
            UT_EQUAL(out.Capture[1], cmdProc.Prompt);

            ///// Misc commands.
            out.Clear();
            in.NextLine = "kill";
            bret = cmdProc.Read();
            UT_TRUE(bret);
            UT_EQUAL(out.Capture.Count, 1);
            UT_EQUAL(out.Capture[0], cmdProc.Prompt);

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
