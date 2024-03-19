using System;
using System.Text;
using System.Collections.Generic;
using System.IO;
using Ephemera.NBagOfTricks.PNUT;
using Ephemera.NBagOfTricks.Slog;


namespace Nebulua.Test
{
    // Test the simpler cli functions.
    public class CLI_SIMPLE : TestSuite
    {
        public override void RunSuite()
        {
            int stat;
            List<string> capture;
            UT_STOP_ON_FAIL(true);

            var app = new App();
            app.HookCli();

            ///// Fat fingers.
            app.Clear();
            app.NextLine = "bbbbb";
            stat = app.DoCli();
            UT_EQUAL(stat, Defs.NEB_OK);
            capture = app.CaptureLines;
            UT_EQUAL(capture.Count, 2);
            UT_EQUAL(capture[0], $"Invalid command");
            UT_EQUAL(capture[1], $"->");

            app.Clear();
            app.NextLine = "z";
            stat = app.DoCli();
            UT_EQUAL(stat, Defs.NEB_OK);
            capture = app.CaptureLines;
            UT_EQUAL(capture.Count, 2);
            UT_EQUAL(capture[0], $"Invalid command");
            UT_EQUAL(capture[1], $"->");

            ///// These next two confirm proper full/short name handling.
            app.Clear();
            app.NextLine = "help";
            stat = app.DoCli();
            UT_EQUAL(stat, Defs.NEB_OK);
            capture = app.CaptureLines;
            UT_EQUAL(capture.Count, 12);
            UT_EQUAL(capture[0], "help|?: tell me everything");
            UT_EQUAL(capture[1], "exit|x: exit the application");
            UT_EQUAL(capture[10], "reload|l: re/load current script");
            UT_EQUAL(capture[11], $"->");

            app.Clear();
            app.NextLine = "?";
            stat = app.DoCli();
            UT_EQUAL(stat, Defs.NEB_OK);
            capture = app.CaptureLines;
            UT_EQUAL(capture.Count, 12);
            UT_EQUAL(capture[0], "help|?: tell me everything");

            ///// The rest of the commands.
            app.Clear();
            app.NextLine = "exit";
            stat = app.DoCli();
            UT_EQUAL(stat, Defs.NEB_OK);
            capture = app.CaptureLines;
            UT_EQUAL(capture.Count, 2);
            UT_EQUAL(capture[0], $"goodbye!");

            app.Clear();
            app.NextLine = "run";
            stat = app.DoCli();
            UT_EQUAL(stat, Defs.NEB_OK);
            capture = app.CaptureLines;
            UT_EQUAL(capture.Count, 2);
            UT_EQUAL(capture[0], $"running");

            app.Clear();
            app.NextLine = "reload";
            stat = app.DoCli();
            UT_EQUAL(stat, Defs.NEB_OK);
            capture = app.CaptureLines;
            UT_EQUAL(capture.Count, 1);
            UT_EQUAL(capture[0], $"->");

            app.Clear();
            app.NextLine = "tempo";
            stat = app.DoCli();
            UT_EQUAL(stat, Defs.NEB_OK);
            capture = app.CaptureLines;
            UT_EQUAL(capture.Count, 2);
            UT_EQUAL(capture[0], "100");

            app.Clear();
            app.NextLine = "tempo 182";
            stat = app.DoCli();
            UT_EQUAL(stat, Defs.NEB_ERR_BAD_CLI_ARG);
            capture = app.CaptureLines;
            UT_EQUAL(capture.Count, 1);
            UT_EQUAL(capture[0], $"->");

            app.Clear();
            app.NextLine = "tempo 242";
            stat = app.DoCli();
            UT_EQUAL(stat, Defs.NEB_ERR_BAD_CLI_ARG);
            capture = app.CaptureLines;
            UT_EQUAL(capture.Count, 2);
            UT_EQUAL(capture[0], "invalid tempo: 242");

            app.Clear();
            app.NextLine = "tempo 39";
            stat = app.DoCli();
            UT_EQUAL(stat, Defs.NEB_ERR_BAD_CLI_ARG);
            capture = app.CaptureLines;
            UT_EQUAL(capture.Count, 2);
            UT_EQUAL(capture[0], "invalid tempo: 39");

            app.Clear();
            app.NextLine = "monitor in";
            stat = app.DoCli();
            UT_EQUAL(stat, Defs.NEB_OK);
            capture = app.CaptureLines;
            UT_EQUAL(capture.Count, 1);
            UT_EQUAL(capture[0], $"->");

            app.Clear();
            app.NextLine = "monitor out";
            stat = app.DoCli();
            UT_EQUAL(stat, Defs.NEB_OK);
            capture = app.CaptureLines;
            UT_EQUAL(capture.Count, 1);
            UT_EQUAL(capture[0], $"->");

            app.Clear();
            app.NextLine = "monitor off";
            stat = app.DoCli();
            UT_EQUAL(stat, Defs.NEB_OK);
            capture = app.CaptureLines;
            UT_EQUAL(capture.Count, 1);
            UT_EQUAL(capture[0], $"->");

            app.Clear();
            app.NextLine = "monitor junk";
            stat = app.DoCli();
            UT_EQUAL(stat, Defs.NEB_ERR_BAD_CLI_ARG);
            capture = app.CaptureLines;
            UT_EQUAL(capture.Count, 2);
            UT_EQUAL(capture[0], "invalid option: junk");

            app = null;
        }
    }


    // Test cli functions that require a lua context via script file load.
    public class CLI_CONTEXT : TestSuite
    {
        public override void RunSuite()
        {
            int stat;
            List<string> capture;
            UT_STOP_ON_FAIL(true);

            var app = new App();
            app.HookCli();

            //LogManager.Run("_test_log.txt", 100000);

            app.Run("script_happy.lua");


            ///// Position commands.
            app.Clear();
            app.NextLine = "position";
            stat = app.DoCli();
            UT_EQUAL(stat, Defs.NEB_OK);
            capture = app.CaptureLines;
            UT_EQUAL(capture.Count, 2);
            UT_EQUAL(capture[0], $"0:0:0");
            UT_EQUAL(capture[1], $"->");

            app.Clear();
            app.NextLine = "position 203:2:6";
            stat = app.DoCli();
            UT_EQUAL(stat, Defs.NEB_OK);
            capture = app.CaptureLines;
            UT_EQUAL(capture.Count, 2);
            UT_EQUAL(capture[0], $"203:2:6");
            UT_EQUAL(capture[1], $"->");

            app.Clear();
            app.NextLine = "position";
            stat = app.DoCli();
            UT_EQUAL(stat, Defs.NEB_OK);
            capture = app.CaptureLines;
            UT_EQUAL(capture.Count, 2);
            UT_EQUAL(capture[0], $"203:2:6");
            UT_EQUAL(capture[1], $"->");

            app.Clear();
            app.NextLine = "position 111:9:6";
            stat = app.DoCli();
            UT_EQUAL(stat, Defs.NEB_OK);
            capture = app.CaptureLines;
            UT_EQUAL(capture.Count, 2);
            UT_EQUAL(capture[0], $"invalid position: 111:9:6");
            UT_EQUAL(capture[1], $"->");


            ///// Misc commands.
            app.Clear();
            app.NextLine = "kill";
            stat = app.DoCli();
            UT_EQUAL(stat, Defs.NEB_OK);
            capture = app.CaptureLines;
            UT_EQUAL(capture.Count, 2);
            UT_EQUAL(capture[0], $"");
            UT_EQUAL(capture[1], $"->");
        }
    }    
}
