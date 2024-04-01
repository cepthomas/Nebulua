using System;
using System.Text;
using System.Collections.Generic;
using System.IO;
using Ephemera.NBagOfTricks;
using Ephemera.NBagOfTricks.PNUT;
using Interop;


namespace Nebulua.Test
{
    /// <summary>All success operations.</summary>
    public class INTEROP_HAPPY : TestSuite
    {
        public override void RunSuite()
        {
            UT_STOP_ON_FAIL(true);
            var interop = Program.MyInterop!;
            InteropEventCollector events = new(interop);
            string scrfn = Path.Join(TestUtils.GetTestFilesDir(), "script_happy.lua");
            State.Instance.PropertyChangeEvent += State_PropertyChangeEvent;

            // Load the script.
            NebStatus stat = interop.OpenScript(scrfn);
            UT_EQUAL(stat, NebStatus.Ok);
            UT_NULL(interop.Error);

            // Have a look inside.
            UT_EQUAL(events.CollectedEvents.Count, 4);
            foreach (var kv in interop.SectionInfo) // get sorted
            {
            }

            // Run fake steps.
            events.CollectedEvents.Clear();
            for (int i = 0; i < 99; i++)
            {
                stat = interop.Step(State.Instance.CurrentTick++);

                if (i % 20 == 0)
                {
                    stat = interop.InputNote(0x0102, i, (double)i / 100);
                    UT_EQUAL(stat, NebStatus.Ok);
                }

                if (i % 20 == 5)
                {
                    stat = interop.InputController(0x0102, i, i);
                    UT_EQUAL(stat, NebStatus.Ok);
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
            var interop = Program.MyInterop!;
            string tempfn = Path.Join(TestUtils.GetTestFilesDir(), "temp.lua");

            // General syntax error during load.
            {
                File.WriteAllText(tempfn,
                    @"local neb = require(""nebulua"")\n
                    this is a bad statement");
                NebStatus stat = interop.OpenScript(tempfn);
                UT_EQUAL(stat, NebStatus.Syntax);
                var e = interop.Error;
                UT_NOT_NULL(e);
                UT_TRUE(e.Contains("syntax error near 'is'"));
            }

            // General syntax error - lua_pcall()
            {
                NebStatus stat = interop.OpenScript(tempfn);
                UT_EQUAL(stat, NebStatus.Ok); // runtime error
                string e = interop.Error;
                UT_NOT_NULL(e);
                UT_TRUE(e.Contains("attempt to perform arithmetic on a nil value"));
            }

            // Missing required C2L api element - luainterop_Setup(_ltest, &iret);
            {
                File.WriteAllText(tempfn,
                    @"local neb = require(""nebulua"")\n
                    resx = 345 + 456");
                NebStatus stat = interop.OpenScript(tempfn);
                UT_EQUAL(stat, NebStatus.Ok); // runtime error
                string e = interop.Error;
                UT_NOT_NULL(e);
                UT_TRUE(e.Contains("INTEROP_BAD_FUNC_NAME"));
            }

            // Bad L2C api function
            {
                File.WriteAllText(tempfn,
                    @"local neb = require(""nebulua"")\n
                    function setup()\n
                        neb.no_good(95)\n
                        return 0\n
                    end");
                NebStatus stat = interop.OpenScript(tempfn);
                UT_EQUAL(stat, NebStatus.Ok); // runtime error
                string e = interop.Error;
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
            var interop = Program.MyInterop!;
            string tempfn = Path.Join(TestUtils.GetTestFilesDir(), "temp.lua");

            // General explicit error.
            {
                File.WriteAllText(tempfn,
                    @"local neb = require(""nebulua"")\n
                    function setup()\n
                        error(""setup() raises error()"")\n
                        return 0\n
                    end");
                NebStatus stat = interop.OpenScript(tempfn);
                UT_EQUAL(stat, NebStatus.Ok); // runtime error
                string e = interop.Error;
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
            var interop = Program.MyInterop!;

            string tempfn = Path.Join(TestUtils.GetTestFilesDir(), "temp.lua");

            // Runtime error.
            {
                File.WriteAllText(tempfn,
                    @"local neb = require(""nebulua"")\n
                    function setup()\n
                        local bad = 123 + ng\n
                        return 0\n
                    end");
                NebStatus stat = interop.OpenScript(tempfn);
                UT_EQUAL(stat, NebStatus.Ok); // runtime error
                string e = interop.Error;
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
            // Create interop.
            MyInterop = Interop.Api.Instance;

            var p = TestUtils.GetLuaPath();
            if (p.valid)
            {
                if (MyInterop.Init(p.lpath) != NebStatus.Ok)
                {
                    Console.WriteLine($"Init interop failed: {p.lpath}");
                    Environment.Exit(2);
                }
            }
            else
            {
                Console.WriteLine("Init interop failed");
                Environment.Exit(1);
            }

            TestRunner runner = new(OutputFormat.Readable);
            var cases = new[] { "INTEROP_HAPPY" };
            runner.RunSuites(cases);
            File.WriteAllLines(@"_test.txt", runner.Context.OutputLines);
        }

        public static Interop.Api? MyInterop;
    }
}