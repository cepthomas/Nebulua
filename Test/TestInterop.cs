using System;
using System.Text;
using System.Collections.Generic;
using System.IO;
using Ephemera.NBagOfTricks;
using Ephemera.NBagOfTricks.PNUT;
using Nebulua;


namespace Test
{
    /// <summary>Utility functions.</summary>
    public class INTEROP_INTERNALS : TestSuite
    {
        public override void RunSuite()
        {
            UT_STOP_ON_FAIL(true);

            EventCollector ecoll = new();
            using Interop interop = new();
        }
    }

    /// <summary>All success operations.</summary>
    public class INTEROP_HAPPY : TestSuite
    {
        public override void RunSuite()
        {
            UT_STOP_ON_FAIL(true);

            EventCollector ecoll = new();
            ecoll.AutoRet = true;
            using Interop interop = new();

            // Set up runtime lua environment.
            var testDir = MiscUtils.GetSourcePath();
            var luaPath = $"{testDir}\\?.lua;{testDir}\\..\\LBOT\\?.lua;{testDir}\\..\\lua\\?.lua;;";
            var scriptFn = Path.Join(testDir, "lua", "script_happy.lua");

            interop.Run(scriptFn, luaPath);
            var ret = interop.Setup();

            // Run script steps.
            for (int i = 1; i < 100; i++)
            {
                var stat = interop.Step(State.Instance.CurrentTick++);

                // Inject some received midi events.
                if (i % 20 == 0)
                {
                    stat = interop.ReceiveMidiNote(0x0102, i, (double)i / 100);
                    UT_EQUAL(stat, 0);
                    //C:\Dev\Apps\Nebulua\Test\Utils.cs:
                    //Interop.SendMidiNote += CollectEvent;
                    //SendMidiNoteArgs ne => $"SendMidiNote chan_hnd:{ne.chan_hnd} note_num:{ne.note_num} volume:{ne.volume}",

                }

                if (i % 20 == 5)
                {
                    stat = interop.ReceiveMidiController(0x0102, i, i);
                    UT_EQUAL(stat, 0);
                }
            }
        }
    }

    /// <summary>Test basic failure modes.</summary>
    public class INTEROP_FAIL : TestSuite
    {
        public override void RunSuite()
        {
            UT_STOP_ON_FAIL(true);

            // Set up runtime lua environment.
            var testDir = MiscUtils.GetSourcePath();
            var luaPath = $"{testDir}\\?.lua;{testDir}\\..\\LBOT\\?.lua;;";
            var scriptFn = Path.Join(testDir, "lua", "script_happy.lua");
            var testFn = "_test.lua";

            // General syntax error during load.
            {
                try
                {
                    using Interop interop = new();
                    File.WriteAllText(testFn,
                        @"local api = require(""luainterop"")
                    this is a bad statement
                    end");

                    interop.Run(testFn, luaPath);
                    UT_FAIL("did not throw");
                }
                catch (Exception e)
                {
                    UT_STRING_CONTAINS(e.Message, "syntax error near 'is'");
                }
            }

            // Bad L2C function
            {
                try
                {
                    using Interop interop = new();
                    File.WriteAllText(testFn,
                        @"local api = require(""luainterop"")
                        api.no_good(95)");

                    interop.Run(testFn, luaPath);
                    UT_FAIL("did not throw");
                }
                catch (Exception e)
                {
                    UT_STRING_CONTAINS(e.Message, "attempt to call a nil value (field 'no_good')");
                }
            }

            // General explicit error.
            {
                try
                {
                    using Interop interop = new();
                    File.WriteAllText(testFn,
                        @"local api = require(""luainterop"")
                        error(""setup() raises error()"")");

                    interop.Run(testFn, luaPath);
                    UT_FAIL("did not throw");
                }
                catch (Exception e)
                {
                    UT_STRING_CONTAINS(e.Message, " setup() raises error()");
                }
            }

            // Runtime error.
            {
                try
                {
                    using Interop interop = new();
                    File.WriteAllText(testFn,
                        @"local api = require(""luainterop"")
                        local bad = 123 + ng");

                    interop.Run(testFn, luaPath);
                    UT_FAIL("did not throw");
                }
                catch (Exception e)
                {
                    UT_STRING_CONTAINS(e.Message, "attempt to perform arithmetic on a nil value (global 'ng')");
                }
            }
        }
    }
}
