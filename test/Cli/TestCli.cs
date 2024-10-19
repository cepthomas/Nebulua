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
            console.Reset();
            console.NextReadLine = "bbbbb";
            bret = cli.DoCommand();
            UT_TRUE(bret);
            UT_EQUAL(console.Capture.Count, 2);
            UT_EQUAL(console.Capture[0], $"Invalid command");
            UT_EQUAL(console.Capture[1], prompt);

            console.Reset();
            console.NextReadLine = "z";
            bret = cli.DoCommand();
            UT_TRUE(bret);
            UT_EQUAL(console.Capture.Count, 2);
            UT_EQUAL(console.Capture[0], $"Invalid command");
            UT_EQUAL(console.Capture[1], prompt);

            ///// These next two confirm proper full/short name handling.
            console.Reset();
            console.NextReadLine = "help";
            bret = cli.DoCommand();
            UT_TRUE(bret);
            UT_EQUAL(console.Capture.Count, 15);
            UT_EQUAL(console.Capture[0], "help|?: available commands");
            UT_EQUAL(console.Capture[1], "info|i: system information");
            UT_EQUAL(console.Capture[13], "reload|s: reload current script");
            UT_EQUAL(console.Capture[14], prompt);

            console.Reset();
            console.NextReadLine = "?";
            bret = cli.DoCommand();
            UT_TRUE(bret);
            UT_EQUAL(console.Capture.Count, 15);
            UT_EQUAL(console.Capture[0], "help|?: available commands");

            ///// The rest of the commands.
            console.Reset();
            console.NextReadLine = "exit";
            bret = cli.DoCommand();
            UT_TRUE(bret);
            UT_EQUAL(console.Capture.Count, 2);
            UT_EQUAL(console.Capture[0], $"Exit - goodbye!");

            st.ExecState = ExecState.Idle; // reset
            console.Reset();
            console.NextReadLine = "run";
            bret = cli.DoCommand();
            UT_TRUE(bret);
            UT_EQUAL(console.Capture.Count, 2);
            UT_EQUAL(console.Capture[0], $"running");

            st.ExecState = ExecState.Idle; // reset
            console.Reset();
            console.NextReadLine = "reload";
            bret = cli.DoCommand();
            UT_TRUE(bret);
            UT_EQUAL(console.Capture.Count, 1);
            UT_EQUAL(console.Capture[0], prompt);

            console.Reset();
            console.NextReadLine = "tempo";
            bret = cli.DoCommand();
            UT_TRUE(bret);
            UT_EQUAL(console.Capture.Count, 2);
            UT_EQUAL(console.Capture[0], "100");

            console.Reset();
            console.NextReadLine = "tempo 182";
            bret = cli.DoCommand();
            UT_TRUE(bret);
            UT_EQUAL(console.Capture.Count, 1);
            UT_EQUAL(console.Capture[0], prompt);

            console.Reset();
            console.NextReadLine = "tempo 242";
            bret = cli.DoCommand();
            UT_FALSE(bret);
            UT_EQUAL(console.Capture.Count, 2);
            UT_EQUAL(console.Capture[0], "invalid tempo: 242");

            console.Reset();
            console.NextReadLine = "tempo 39";
            bret = cli.DoCommand();
            UT_FALSE(bret);
            UT_EQUAL(console.Capture.Count, 2);
            UT_EQUAL(console.Capture[0], "invalid tempo: 39");

            console.Reset();
            console.NextReadLine = "monitor r";
            bret = cli.DoCommand();
            UT_TRUE(bret);
            UT_EQUAL(console.Capture.Count, 1);
            UT_EQUAL(console.Capture[0], prompt);

            console.Reset();
            console.NextReadLine = "monitor s";
            bret = cli.DoCommand();
            UT_TRUE(bret);
            UT_EQUAL(console.Capture.Count, 1);
            UT_EQUAL(console.Capture[0], prompt);

            console.Reset();
            console.NextReadLine = "monitor o";
            bret = cli.DoCommand();
            UT_TRUE(bret);
            UT_EQUAL(console.Capture.Count, 1);
            UT_EQUAL(console.Capture[0], prompt);

            // Test immediate spacebar.
            console.Reset();
            State.Instance.ExecState = ExecState.Idle;
            console.NextReadLine = " ";
            bret = cli.DoCommand();
            UT_TRUE(bret);
            UT_EQUAL(console.Capture.Count, 2);
            UT_EQUAL(console.Capture[0], $"running");

            console.Reset();
            console.NextReadLine = "monitor junk";
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
            console.Reset();
            console.NextReadLine = "position";
            bret = cli.DoCommand();
            UT_TRUE(bret);
            UT_EQUAL(console.Capture.Count, 2);
            UT_EQUAL(console.Capture[0], $"0:0:0");
            UT_EQUAL(console.Capture[1], prompt);

            console.Reset();
            console.NextReadLine = "position 10:2:6";
            bret = cli.DoCommand();
            UT_TRUE(bret);
            UT_EQUAL(console.Capture.Count, 2);
            UT_EQUAL(console.Capture[0], $"10:2:6");
            UT_EQUAL(console.Capture[1], prompt);

            console.Reset();
            console.NextReadLine = "position 203:2:6"; // too late
            bret = cli.DoCommand();
            UT_FALSE(bret);
            UT_EQUAL(console.Capture.Count, 2);
            UT_EQUAL(console.Capture[0], "invalid requested position: 203:2:6");
            UT_EQUAL(console.Capture[1], prompt);

            console.Reset();
            console.NextReadLine = "position";
            bret = cli.DoCommand();
            UT_TRUE(bret);
            UT_EQUAL(console.Capture.Count, 2);
            UT_EQUAL(console.Capture[0], $"10:2:6");
            UT_EQUAL(console.Capture[1], prompt);

            console.Reset();
            console.NextReadLine = "position 111:9:6";
            bret = cli.DoCommand();
            UT_FALSE(bret);
            UT_EQUAL(console.Capture.Count, 2);
            UT_EQUAL(console.Capture[0], $"invalid requested position: 111:9:6");
            UT_EQUAL(console.Capture[1], prompt);

            ///// Misc commands.
            console.Reset();
            console.NextReadLine = "kill";
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
