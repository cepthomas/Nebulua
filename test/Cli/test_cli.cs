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

            MockConsole console = new();
            var cli = new Cli("none", console);
            string prompt = ">";

            ///// Fat fingers.
            console.Clear();
            console.NextLine = "bbbbb";
            bret = cli.DoCommand();
            UT_TRUE(bret);
            UT_EQUAL(console.Capture.Count, 2);
            UT_EQUAL(console.Capture[0], $"Invalid command");
            UT_EQUAL(console.Capture[1], prompt);

            console.Clear();
            console.NextLine = "z";
            bret = cli.DoCommand();
            UT_TRUE(bret);
            UT_EQUAL(console.Capture.Count, 2);
            UT_EQUAL(console.Capture[0], $"Invalid command");
            UT_EQUAL(console.Capture[1], prompt);

            ///// These next two confirm proper full/short name handling.
            console.Clear();
            console.NextLine = "help";
            bret = cli.DoCommand();
            UT_TRUE(bret);
            UT_EQUAL(console.Capture.Count, 15);
            UT_EQUAL(console.Capture[0], "help|?: available commands");
            UT_EQUAL(console.Capture[1], "info|i: system information");
            UT_EQUAL(console.Capture[13], "reload|s: reload current script");
            UT_EQUAL(console.Capture[14], prompt);

            console.Clear();
            console.NextLine = "?";
            bret = cli.DoCommand();
            UT_TRUE(bret);
            UT_EQUAL(console.Capture.Count, 15);
            UT_EQUAL(console.Capture[0], "help|?: available commands");

            ///// The rest of the commands.
            console.Clear();
            console.NextLine = "exit";
            bret = cli.DoCommand();
            UT_TRUE(bret);
            UT_EQUAL(console.Capture.Count, 2);
            UT_EQUAL(console.Capture[0], $"Exit - goodbye!");

            st.ExecState = ExecState.Idle; // reset
            console.Clear();
            console.NextLine = "run";
            bret = cli.DoCommand();
            UT_TRUE(bret);
            UT_EQUAL(console.Capture.Count, 2);
            UT_EQUAL(console.Capture[0], $"running");

            st.ExecState = ExecState.Idle; // reset
            console.Clear();
            console.NextLine = "reload";
            bret = cli.DoCommand();
            UT_TRUE(bret);
            UT_EQUAL(console.Capture.Count, 1);
            UT_EQUAL(console.Capture[0], prompt);

            console.Clear();
            console.NextLine = "tempo";
            bret = cli.DoCommand();
            UT_TRUE(bret);
            UT_EQUAL(console.Capture.Count, 2);
            UT_EQUAL(console.Capture[0], "100");

            console.Clear();
            console.NextLine = "tempo 182";
            bret = cli.DoCommand();
            UT_TRUE(bret);
            UT_EQUAL(console.Capture.Count, 1);
            UT_EQUAL(console.Capture[0], prompt);

            console.Clear();
            console.NextLine = "tempo 242";
            bret = cli.DoCommand();
            UT_FALSE(bret);
            UT_EQUAL(console.Capture.Count, 2);
            UT_EQUAL(console.Capture[0], "invalid tempo: 242");

            console.Clear();
            console.NextLine = "tempo 39";
            bret = cli.DoCommand();
            UT_FALSE(bret);
            UT_EQUAL(console.Capture.Count, 2);
            UT_EQUAL(console.Capture[0], "invalid tempo: 39");

            console.Clear();
            console.NextLine = "monitor r";
            bret = cli.DoCommand();
            UT_TRUE(bret);
            UT_EQUAL(console.Capture.Count, 1);
            UT_EQUAL(console.Capture[0], prompt);

            console.Clear();
            console.NextLine = "monitor s";
            bret = cli.DoCommand();
            UT_TRUE(bret);
            UT_EQUAL(console.Capture.Count, 1);
            UT_EQUAL(console.Capture[0], prompt);

            console.Clear();
            console.NextLine = "monitor o";
            bret = cli.DoCommand();
            UT_TRUE(bret);
            UT_EQUAL(console.Capture.Count, 1);
            UT_EQUAL(console.Capture[0], prompt);

            console.Clear();
            console.NextLine = "monitor junk";
            bret = cli.DoCommand();
            UT_FALSE(bret);
            UT_EQUAL(console.Capture.Count, 2);
            UT_EQUAL(console.Capture[0], "invalid option: junk");

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

            MockConsole console = new();
            var cli = new Cli("none", console);
            string prompt = ">";

            ///// Fake valid loaded script.
            Dictionary<int, string> sectionInfo = new() { [0] = "start", [200] = "middle", [300] = "end", [400] = "LENGTH" };
            State.Instance.InitSectionInfo(sectionInfo);
            UT_EQUAL(State.Instance.SectionInfo.Count, 4);

            ///// Position commands.
            console.Clear();
            console.NextLine = "position";
            bret = cli.DoCommand();
            UT_TRUE(bret);
            UT_EQUAL(console.Capture.Count, 2);
            UT_EQUAL(console.Capture[0], $"0:0:0");
            UT_EQUAL(console.Capture[1], prompt);

            console.Clear();
            console.NextLine = "position 10:2:6";
            bret = cli.DoCommand();
            UT_TRUE(bret);
            UT_EQUAL(console.Capture.Count, 2);
            UT_EQUAL(console.Capture[0], $"10:2:6");
            UT_EQUAL(console.Capture[1], prompt);

            console.Clear();
            console.NextLine = "position 203:2:6"; // too late
            bret = cli.DoCommand();
            UT_FALSE(bret);
            UT_EQUAL(console.Capture.Count, 2);
            UT_EQUAL(console.Capture[0], "invalid requested position: 203:2:6");
            UT_EQUAL(console.Capture[1], prompt);

            console.Clear();
            console.NextLine = "position";
            bret = cli.DoCommand();
            UT_TRUE(bret);
            UT_EQUAL(console.Capture.Count, 2);
            UT_EQUAL(console.Capture[0], $"10:2:6");
            UT_EQUAL(console.Capture[1], prompt);

            console.Clear();
            console.NextLine = "position 111:9:6";
            bret = cli.DoCommand();
            UT_FALSE(bret);
            UT_EQUAL(console.Capture.Count, 2);
            UT_EQUAL(console.Capture[0], $"invalid requested position: 111:9:6");
            UT_EQUAL(console.Capture[1], prompt);

            ///// Misc commands.
            console.Clear();
            console.NextLine = "kill";
            bret = cli.DoCommand();
            UT_TRUE(bret);
            UT_EQUAL(console.Capture.Count, 1);
            UT_EQUAL(console.Capture[0], prompt);

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
