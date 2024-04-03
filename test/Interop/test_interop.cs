using System;
using System.Text;
using System.Collections.Generic;
using System.IO;
using Ephemera.NBagOfTricks;
using Ephemera.NBagOfTricks.PNUT;
using Interop;


namespace Nebulua.Test
{
    //public class TestContext
    //{
    //    public static Api MyInterop;

    //    public TestContext()
    //    {
    //        // Create interop.
    //        MyInterop = Api.Instance;

    //        var p = TestUtils.GetLuaPath();
    //        if (p.valid)
    //        {
    //            if (MyInterop.Init(p.lpath) != NebStatus.Ok)
    //            {
    //                Console.WriteLine($"Init interop failed: {p.lpath}");
    //                Environment.Exit(2);
    //            }
    //        }
    //        else
    //        {
    //            Console.WriteLine("Init interop failed");
    //            Environment.Exit(1);
    //        }
    //    }
    //}


    /// <summary>All success operations.</summary>
    public class INTEROP_HAPPY : TestSuite
    {
        public override void RunSuite()
        {
            UT_STOP_ON_FAIL(true);

            // Create interop.
            var (valid, lpath) = TestUtils.GetLuaPath();
            var interop = new(lpath);

            InteropEventCollector events = new(interop);
            string scrfn = Path.Join(TestUtils.GetTestFilesDir(), "script_happy.lua");
            State.Instance.PropertyChangeEvent += State_PropertyChangeEvent;

            // Load the script.
            NebStatus stat = interop.OpenScript(scrfn);
            UT_EQUAL(stat, NebStatus.Ok);
            UT_EQUAL(interop.Error.Length, 0);

            // Have a look inside. TODO2
            UT_EQUAL(interop.SectionInfo.Count, 4);
            foreach (var kv in interop.SectionInfo)
            {
            }
            UT_EQUAL(events.CollectedEvents.Count, 6);

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

            UT_EQUAL(events.CollectedEvents.Count, 40);
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

            Program.MyInterop!.Dispose();

            var interop = Program.MyInterop!;

            var tempfn = "_test_temp.lua";

            // General syntax error during load.
            {
                File.WriteAllText(tempfn,
                    @"local neb = require(""nebulua"")
                    this is a bad statement
                    end");
                NebStatus stat = interop.OpenScript(tempfn);
                UT_EQUAL(stat, NebStatus.SyntaxError);
                UT_STRING_CONTAINS(interop.Error, "syntax error near 'is'");
            }

            // Missing required C2L api element - luainterop_Setup(_ltest, &iret);
            {
                File.WriteAllText(tempfn,
                    @"local neb = require(""nebulua"")
                    resx = 345 + 456");
                NebStatus stat = interop.OpenScript(tempfn);
                UT_EQUAL(stat, NebStatus.SyntaxError); // TODO1 fails, says ok
                UT_STRING_CONTAINS(interop.Error, "INTEROP_BAD_FUNC_NAME");

                // <eof> expected near 'end'] does not contain [INTEROP_BAD_FUNC_NAME]
            }

            // Bad L2C api function
            {
                File.WriteAllText(tempfn,
                    @"local neb = require(""nebulua"")
                    function setup()
                        neb.no_good(95)
                        return 0
                    end");
                NebStatus stat = interop.OpenScript(tempfn);
                UT_EQUAL(stat, NebStatus.SyntaxError); // runtime error
                UT_STRING_CONTAINS(interop.Error, "attempt to call a nil value (field 'no_good')");
            }
        }
    }

    /// <summary>Test fatal error() failure modes.</summary>
    public class INTEROP_FAIL_2 : TestSuite //TODO1 combine all these?
    {
        public override void RunSuite()
        {
            UT_STOP_ON_FAIL(true);
            var interop = Program.MyInterop!;
            var tempfn = "_test_temp.lua";

            // General explicit error.
            {
                File.WriteAllText(tempfn,
                    @"local neb = require(""nebulua"")
                    function setup()
                        error(""setup() raises error()"")
                        return 0
                    end");
                NebStatus stat = interop.OpenScript(tempfn);
                UT_EQUAL(stat, NebStatus.SyntaxError);
                UT_STRING_CONTAINS(interop.Error, "setup() raises error()");
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
            var tempfn = "_test_temp.lua";

            // Runtime error.
            {
                File.WriteAllText(tempfn,
                    @"local neb = require(""nebulua"")
                    function setup()
                        local bad = 123 + ng
                        return 0
                    end");
                NebStatus stat = interop.OpenScript(tempfn);
                UT_EQUAL(stat, NebStatus.SyntaxError); // runtime error
                UT_STRING_CONTAINS(interop.Error, "attempt to perform arithmetic on a nil value (global 'ng')");
            }
        }
    }

    /// <summary>Test entry.</summary>
    static class Program
    {
        [STAThread]
        static void Main(string[] _)
        {
           // // Create interop.
           //// MyInterop = Api.Instance;

           // var p = TestUtils.GetLuaPath();
           // if (p.valid)
           // {
           //     MyInterop = new(p.lpath);
           //     //if (MyInterop = new(p.lpath));
           //     //{
           //     //    Console.WriteLine($"Init interop failed: {p.lpath}");
           //     //    Environment.Exit(2);
           //     //}
           // }
           // else
           // {
           //     Console.WriteLine("Init interop failed");
           //     Environment.Exit(1);
           // }

            TestRunner runner = new(OutputFormat.Readable);
            var cases = new[] { "INTEROP" };
            runner.RunSuites(cases);
            File.WriteAllLines(@"_test.txt", runner.Context.OutputLines);
        }

        //public static Api? MyInterop;
    }
}
