using System;
using System.Text;
using System.Collections.Generic;
using System.IO;
using Ephemera.NBagOfTricks.PNUT;
using Nebulua;


namespace Nebulua.Test
{
    /// <summary>Test the simpler functions.</summary>
    public class CLI_BASIC : TestSuite
    {
        public override void RunSuite()
        {
            bool bret;
            UT_STOP_ON_FAIL(true);

            var st = State.Instance;
            st.ValueChangeEvent += (sender, e) => { };

            MockIn min = new();
            MockOut mout = new();
            var cli = new Cli("none", min, mout);
            //var cmdProc = new CommandProc(min, mout) { Prompt = "%" };
            string prompt = ">";

            ///// Fat fingers.
            mout.Clear();
            min.NextLine = "bbbbb";
            bret = cli.DoCommand();
            UT_TRUE(bret);
            UT_EQUAL(mout.Capture.Count, 2);
            UT_EQUAL(mout.Capture[0], $"Invalid command");
            UT_EQUAL(mout.Capture[1], prompt);

            mout.Clear();
            min.NextLine = "z";
            bret = cli.DoCommand();
            UT_TRUE(bret);
            UT_EQUAL(mout.Capture.Count, 2);
            UT_EQUAL(mout.Capture[0], $"Invalid command");
            UT_EQUAL(mout.Capture[1], prompt);

            ///// These next two confirm proper full/short name handling.
            mout.Clear();
            min.NextLine = "help";
            bret = cli.DoCommand();
            UT_TRUE(bret);
            UT_EQUAL(mout.Capture.Count, 15);
            UT_EQUAL(mout.Capture[0], "help|?: available commands");
            UT_EQUAL(mout.Capture[1], "info|i: system information");
            UT_EQUAL(mout.Capture[13], "reload|s: reload current script");
            UT_EQUAL(mout.Capture[14], prompt);

            mout.Clear();
            min.NextLine = "?";
            bret = cli.DoCommand();
            UT_TRUE(bret);
            UT_EQUAL(mout.Capture.Count, 15);
            UT_EQUAL(mout.Capture[0], "help|?: available commands");

            ///// The rest of the commands.
            mout.Clear();
            min.NextLine = "exit";
            bret = cli.DoCommand();
            UT_TRUE(bret);
            UT_EQUAL(mout.Capture.Count, 2);
            UT_EQUAL(mout.Capture[0], $"Exit - goodbye!");

            st.ExecState = ExecState.Idle; // reset
            mout.Clear();
            min.NextLine = "run";
            bret = cli.DoCommand();
            UT_TRUE(bret);
            UT_EQUAL(mout.Capture.Count, 2);
            UT_EQUAL(mout.Capture[0], $"running");

            st.ExecState = ExecState.Idle; // reset
            mout.Clear();
            min.NextLine = "reload";
            bret = cli.DoCommand();
            UT_TRUE(bret);
            UT_EQUAL(mout.Capture.Count, 1);
            UT_EQUAL(mout.Capture[0], prompt);

            mout.Clear();
            min.NextLine = "tempo";
            bret = cli.DoCommand();
            UT_TRUE(bret);
            UT_EQUAL(mout.Capture.Count, 2);
            UT_EQUAL(mout.Capture[0], "100");

            mout.Clear();
            min.NextLine = "tempo 182";
            bret = cli.DoCommand();
            UT_TRUE(bret);
            UT_EQUAL(mout.Capture.Count, 1);
            UT_EQUAL(mout.Capture[0], prompt);

            mout.Clear();
            min.NextLine = "tempo 242";
            bret = cli.DoCommand();
            UT_FALSE(bret);
            UT_EQUAL(mout.Capture.Count, 2);
            UT_EQUAL(mout.Capture[0], "invalid tempo: 242");

            mout.Clear();
            min.NextLine = "tempo 39";
            bret = cli.DoCommand();
            UT_FALSE(bret);
            UT_EQUAL(mout.Capture.Count, 2);
            UT_EQUAL(mout.Capture[0], "invalid tempo: 39");

            mout.Clear();
            min.NextLine = "monitor r";
            bret = cli.DoCommand();
            UT_TRUE(bret);
            UT_EQUAL(mout.Capture.Count, 1);
            UT_EQUAL(mout.Capture[0], prompt);

            mout.Clear();
            min.NextLine = "monitor s";
            bret = cli.DoCommand();
            UT_TRUE(bret);
            UT_EQUAL(mout.Capture.Count, 1);
            UT_EQUAL(mout.Capture[0], prompt);

            mout.Clear();
            min.NextLine = "monitor o";
            bret = cli.DoCommand();
            UT_TRUE(bret);
            UT_EQUAL(mout.Capture.Count, 1);
            UT_EQUAL(mout.Capture[0], prompt);

            mout.Clear();
            min.NextLine = "monitor junk";
            bret = cli.DoCommand();
            UT_FALSE(bret);
            UT_EQUAL(mout.Capture.Count, 2);
            UT_EQUAL(mout.Capture[0], "invalid option: junk");

            // Wait for logger to stop.
            Thread.Sleep(100);
        }
    }

    /// <summary>Test functions that interact with running script.</summary>
    public class CLI_CONTEXT : TestSuite
    {
        public override void RunSuite()
        {
            bool bret;
            UT_STOP_ON_FAIL(true);

            var st = State.Instance;
            st.ValueChangeEvent += (sender, e) => { };

            MockIn min = new();
            MockOut mout = new();
            var cli = new Cli("none", min, mout);
            //var cmdProc = new CommandProc(min, mout) { Prompt = "%" };
            string prompt = ">";

            ///// Fake valid loaded script.
            Dictionary<int, string> sectionInfo = new() { [0] = "start", [200] = "middle", [300] = "end", [400] = "LENGTH" };
            State.Instance.InitSectionInfo(sectionInfo);
            UT_EQUAL(State.Instance.SectionInfo.Count, 4);

            ///// Position commands.
            mout.Clear();
            min.NextLine = "position";
            bret = cli.DoCommand();
            UT_TRUE(bret);
            UT_EQUAL(mout.Capture.Count, 2);
            UT_EQUAL(mout.Capture[0], $"0:0:0");
            UT_EQUAL(mout.Capture[1], prompt);

            mout.Clear();
            min.NextLine = "position 10:2:6";
            bret = cli.DoCommand();
            UT_TRUE(bret);
            UT_EQUAL(mout.Capture.Count, 2);
            UT_EQUAL(mout.Capture[0], $"10:2:6");
            UT_EQUAL(mout.Capture[1], prompt);

            mout.Clear();
            min.NextLine = "position 203:2:6"; // too late
            bret = cli.DoCommand();
            UT_FALSE(bret);
            UT_EQUAL(mout.Capture.Count, 2);
            UT_EQUAL(mout.Capture[0], "invalid requested position: 203:2:6");
            UT_EQUAL(mout.Capture[1], prompt);

            mout.Clear();
            min.NextLine = "position";
            bret = cli.DoCommand();
            UT_TRUE(bret);
            UT_EQUAL(mout.Capture.Count, 2);
            UT_EQUAL(mout.Capture[0], $"10:2:6");
            UT_EQUAL(mout.Capture[1], prompt);

            mout.Clear();
            min.NextLine = "position 111:9:6";
            bret = cli.DoCommand();
            UT_FALSE(bret);
            UT_EQUAL(mout.Capture.Count, 2);
            UT_EQUAL(mout.Capture[0], $"invalid requested position: 111:9:6");
            UT_EQUAL(mout.Capture[1], prompt);

            ///// Misc commands.
            mout.Clear();
            min.NextLine = "kill";
            bret = cli.DoCommand();
            UT_TRUE(bret);
            UT_EQUAL(mout.Capture.Count, 1);
            UT_EQUAL(mout.Capture[0], prompt);

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
