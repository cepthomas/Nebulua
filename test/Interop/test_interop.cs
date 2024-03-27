using System;
using System.Text;
using System.Collections.Generic;
using System.IO;
using Ephemera.NBagOfTricks;
using Ephemera.NBagOfTricks.PNUT;


namespace Nebulua.Test
{
    /// <summary>All success operations.</summary>
    public class INTEROP_HAPPY : TestSuite
    {
        public override void RunSuite()
        {
            UT_STOP_ON_FAIL(true);
            var _interop = new Interop.Api();
            int stat = _interop.Init();
            UT_EQUAL(stat, Defs.NEB_OK);

            InteropEventCollector events = new(_interop);
            string scrfn = Path.Join(TestUtils.GetFilesDir(), "script_happy.lua");

            State.Instance.PropertyChangeEvent += State_PropertyChangeEvent;

            // Load the script.
            stat = _interop.OpenScript(scrfn);
            UT_EQUAL(stat, Defs.NEB_OK);
            UT_NULL(_interop.Error);

            // Have a look inside.
            UT_EQUAL(events.CollectedEvents.Count, 4);
            foreach (var kv in _interop.SectionInfo) // get sorted
            {
            }

            // Run fake steps.
            events.CollectedEvents.Clear();
            for (int i = 0; i < 99; i++)
            {
                stat = _interop.Step(State.Instance.CurrentTick++);

                if (i % 20 == 0)
                {
                    stat = _interop.InputNote(0x0102, i, (double)i / 100);
                    UT_EQUAL(stat, Defs.NEB_OK);
                }

                if (i % 20 == 5)
                {
                    stat = _interop.InputController(0x0102, i, i);
                    UT_EQUAL(stat, Defs.NEB_OK);
                }
            }

            UT_EQUAL(events.CollectedEvents.Count, 4);
        }

        void State_PropertyChangeEvent(object? sender, string name)
        {
            switch (name)
            {
                case "CurrentTick":
                    // if (sender != this) {}
                    break;
                default:
                    break;
            }
        }
    }

    /// <summary>Test basic failure modes.</summary>
    public class INTEROP_FAIL_1 : TestSuite
    {
        public override void RunSuite()
        {
            UT_STOP_ON_FAIL(true);

            string tempfn = Path.Join(TestUtils.GetFilesDir(), "temp.lua");

            // General syntax error during load.
            {
                var _interop = new Interop.Api();
                int stat = _interop.Init();
                UT_EQUAL(stat, Defs.NEB_OK);

                File.WriteAllText(tempfn,
                    @"local neb = require(""nebulua"")\n
                    this is a bad statement");
                stat = _interop.OpenScript(tempfn);
                UT_EQUAL(stat, Defs.NEB_ERR_SYNTAX); // is 10=NEB_ERR_INTERNAL
                var e = _interop.Error;
                UT_NOT_NULL(e);
                UT_TRUE(e.Contains("syntax error near 'is'"));
            }

            // General syntax error - lua_pcall()
            {
                var _interop = new Interop.Api();
                int stat = _interop.Init();
                UT_EQUAL(stat, Defs.NEB_OK);

                File.WriteAllText(tempfn,
                    @"local neb = require(""nebulua"")\n
                    res1 = 345 + nil_value");
                stat = _interop.OpenScript(tempfn);
                UT_EQUAL(stat, Defs.NEB_OK); // runtime error
                string e = _interop.Error;
                UT_NOT_NULL(e);
                UT_TRUE(e.Contains("attempt to perform arithmetic on a nil value"));
            }

            // Missing required C2L api element - luainterop_Setup(_ltest, &iret);
            {
                var _interop = new Interop.Api();
                int stat = _interop.Init();
                UT_EQUAL(stat, Defs.NEB_OK);

                File.WriteAllText(tempfn,
                    @"local neb = require(""nebulua"")\n
                    resx = 345 + 456");
                stat = _interop.OpenScript(tempfn);
                UT_EQUAL(stat, Defs.NEB_OK); // runtime error
                string e = _interop.Error;
                UT_NOT_NULL(e);
                UT_TRUE(e.Contains("INTEROP_BAD_FUNC_NAME"));
            }

            // Bad L2C api function
            {
                var _interop = new Interop.Api();
                int stat = _interop.Init();
                UT_EQUAL(stat, Defs.NEB_OK);

                File.WriteAllText(tempfn,
                    @"local neb = require(""nebulua"")\n
                    function setup()\n
                        neb.no_good(95)\n
                        return 0\n
                    end");
                stat = _interop.OpenScript(tempfn);
                UT_EQUAL(stat, Defs.NEB_OK); // runtime error
                string e = _interop.Error;
                UT_NOT_NULL(e);
                UT_TRUE(e.Contains("attempt to call a nil value (field 'no_good')"));
            }
        }
    }

    /// <summary>Test fatal error() failure modes.</summary>
    public class INTEROP_FAIL_2 : TestSuite
    {
        public override void RunSuite()
        {
            UT_STOP_ON_FAIL(true);

            string tempfn = Path.Join(TestUtils.GetFilesDir(), "temp.lua");

            // General explicit error.
            {
                var _interop = new Interop.Api();
                int stat = _interop.Init();
                UT_EQUAL(stat, Defs.NEB_OK);

                File.WriteAllText(tempfn,
                    @"local neb = require(""nebulua"")\n
                    function setup()\n
                        error(""setup() raises error()"")\n
                        return 0\n
                    end");
                stat = _interop.OpenScript(tempfn);
                UT_EQUAL(stat, Defs.NEB_OK); // runtime error  // is 10=NEB_ERR_INTERNAL
                string e = _interop.Error;
                UT_NOT_NULL(e);
                UT_TRUE(e.Contains("setup() raises error()"));
            }
        }
    }

    /// <summary>Test fatal internal failure modes.</summary>
    public class INTEROP_FAIL_3 : TestSuite
    {
        public override void RunSuite()
        {
            UT_STOP_ON_FAIL(true);

            string tempfn = Path.Join(TestUtils.GetFilesDir(), "temp.lua");

            // Runtime error.
            {
                var _interop = new Interop.Api();
                int stat = _interop.Init();
                UT_EQUAL(stat, Defs.NEB_OK);

                File.WriteAllText(tempfn,
                    @"local neb = require(""nebulua"")\n
                    function setup()\n
                        local bad = 123 + ng\n
                        return 0\n
                    end");
                stat = _interop.OpenScript(tempfn);
                UT_EQUAL(stat, Defs.NEB_OK); // runtime error // is 10=NEB_ERR_INTERNAL
                string e = _interop.Error;
                UT_NOT_NULL(e);
                UT_TRUE(e.Contains("attempt to perform arithmetic on a nil value (global 'ng')"));
            }
        }
    }

    /// <summary>Test entry.</summary>
    static class Program
    {
        [STAThread]
        static void Main(string[] _)
        {
            TestRunner runner = new(OutputFormat.Readable);
            var cases = new[] { "INTEROP" };
            runner.RunSuites(cases);
            File.WriteAllLines(@"_test.txt", runner.Context.OutputLines);
        }
    }
}
