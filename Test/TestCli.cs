using System;
using System.Text;
using System.Collections.Generic;
using System.IO;
using Ephemera.NBagOfTricks.PNUT;
using Nebulua;
using Ephemera.NBagOfTricks;


namespace Test
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
            UT_EQUAL(console.Capture.Count, 16);
            UT_EQUAL(console.Capture[0], "help|?: available commands");
            UT_EQUAL(console.Capture[1], "info|i: system information");
            UT_EQUAL(console.Capture[14], "reload|s: reload current script");
            UT_EQUAL(console.Capture[15], prompt);

            console.Reset();
            console.NextReadLine = "?";
            bret = cli.DoCommand();
            UT_TRUE(bret);
            UT_EQUAL(console.Capture.Count, 16);
            UT_EQUAL(console.Capture[0], "help|?: available commands");

            ///// The rest of the commands.
            console.Reset();
            console.NextReadLine = "exit";
            bret = cli.DoCommand();
            UT_TRUE(bret);
            UT_EQUAL(console.Capture.Count, 2);
            UT_EQUAL(console.Capture[0], $"Goodbye!");

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
            UT_EQUAL(console.Capture.Count, 2);
            UT_EQUAL(console.Capture[0], "tempo set to 182");
            UT_EQUAL(console.Capture[1], prompt);

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
            UT_EQUAL(console.Capture.Count, 2);
            UT_EQUAL(console.Capture[0], "monitor rcv");

            console.Reset();
            console.NextReadLine = "monitor s";
            bret = cli.DoCommand();
            UT_TRUE(bret);
            UT_EQUAL(console.Capture.Count, 2);
            UT_EQUAL(console.Capture[0], "monitor snd");

            console.Reset();
            console.NextReadLine = "monitor o";
            bret = cli.DoCommand();
            UT_TRUE(bret);
            UT_EQUAL(console.Capture.Count, 2);
            UT_EQUAL(console.Capture[0], "monitor off");

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
            UT_EQUAL(console.Capture[0], $"0.0.0");
            UT_EQUAL(console.Capture[1], prompt);

            console.Reset();
            console.NextReadLine = $"position 10.2.6";
            bret = cli.DoCommand();
            UT_TRUE(bret);
            UT_EQUAL(console.Capture.Count, 2);
            UT_EQUAL(console.Capture[0], $"10.2.6");
            UT_EQUAL(console.Capture[1], prompt);

            console.Reset();
            console.NextReadLine = $"position 203.2.6"; // too late
            bret = cli.DoCommand();
            UT_FALSE(bret);
            UT_EQUAL(console.Capture.Count, 2);
            UT_EQUAL(console.Capture[0], $"invalid requested position 203.2.6");
            UT_EQUAL(console.Capture[1], prompt);

            console.Reset();
            console.NextReadLine = "position";
            bret = cli.DoCommand();
            UT_TRUE(bret);
            UT_EQUAL(console.Capture.Count, 2);
            UT_EQUAL(console.Capture[0], $"10.2.6");
            UT_EQUAL(console.Capture[1], prompt);

            console.Reset();
            console.NextReadLine = $"position 111.9.6";
            bret = cli.DoCommand();
            UT_FALSE(bret);
            UT_EQUAL(console.Capture.Count, 2);
            UT_EQUAL(console.Capture[0], $"invalid requested position 111.9.6");
            UT_EQUAL(console.Capture[1], prompt);

            ///// Misc commands.
            console.Reset();
            console.NextReadLine = "kill";
            bret = cli.DoCommand();
            UT_TRUE(bret);
            UT_EQUAL(console.Capture.Count, 2);
            UT_EQUAL(console.Capture[0], "killed all");
            UT_EQUAL(console.Capture[1], prompt);

            // Wait for logger to stop.
            Thread.Sleep(100);
        }
    }

    public class MockConsole : IConsole
    {
        #region Fields
        readonly StringBuilder _capture = new();
        int _left = 0;
        int _top = 0;
        #endregion

        #region Internals
        public List<string> Capture { get { return StringUtils.SplitByTokens(_capture.ToString(), Environment.NewLine); } }
        public string NextReadLine { get; set; } = "";
        public void Reset()
        {
            _capture.Clear();
        }
        #endregion

        #region IConsole implementation
        public bool KeyAvailable { get => NextReadLine.Length > 0; }
        public bool CursorVisible { get; set; } = true;
        public string Title { get; set; } = "";
        public int BufferWidth { get; set; }

        public string? ReadLine()
        {
            if (NextReadLine == "")
            {
                return null;
            }
            else
            {
                var ret = NextReadLine;
                NextReadLine = "";
                return ret;
            }
        }
        public ConsoleKeyInfo ReadKey(bool intercept)
        {
            if (KeyAvailable)
            {
                var key = NextReadLine[0];
                NextReadLine = NextReadLine.Substring(1);
                return new ConsoleKeyInfo(key, (ConsoleKey)key, false, false, false);
            }
            else
            {
                throw new InvalidOperationException();
            }
        }

        public void Write(string text)
        {
            _capture.Append(text);
        }

        public void WriteLine(string text)
        {
            _capture.Append(text + Environment.NewLine);
        }

        public void SetCursorPosition(int left, int top)
        {
            _left = left;
            _top = top;
        }

        public (int left, int top) GetCursorPosition()
        {
            return (_left, _top);
        }
        #endregion
    }

}
