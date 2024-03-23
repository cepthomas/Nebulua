using System;
using System.Text;
using System.Collections.Generic;
using System.IO;
using Ephemera.NBagOfTricks.PNUT;


namespace Nebulua.Test
{
    public class INTEROP_HAPPY : TestSuite
    {
        public override void RunSuite()
        {
            UT_STOP_ON_FAIL(true);
            var _api = new Interop.Api();
            int stat = _api.Init();
            UT_EQUAL(stat, Defs.NEB_OK);

            TestHelper hlp = new(_api);

            State.Instance.PropertyChangeEvent += State_PropertyChangeEvent;

            // Load the script.
            stat = _api.OpenScript(TestHelper.fn);
            UT_EQUAL(stat, Defs.NEB_OK);

            // Have a look inside.
            UT_EQUAL(hlp.CollectedEvents.Count, 4);
            foreach (var kv in hlp.CollectedEvents)
            {


            }

            var err = _api.Error;
            UT_NULL(err);

            hlp.CollectedEvents.Clear();
            for (int i = 0; i < 99; i++)
            {
                stat = _api.Step(State.Instance.CurrentTick);

                if (i % 20 == 0)
                {
                    stat = _api.InputNote(0x0102, i, (double)i / 100);
                    UT_EQUAL(stat, Defs.NEB_OK);
                }

                if (i % 20 == 5)
                {
                    stat = _api.InputController(0x0102, i, i);
                    UT_EQUAL(stat, Defs.NEB_OK);
                }
            }
            stat = _api.Step(State.Instance.CurrentTick);
            UT_EQUAL(hlp.CollectedEvents.Count, 4);
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


    public class TestHelper
    {
        public const string fn = @"C:\Dev\repos\Lua\Nebulua\test\script_happy.lua";

        public List<string> CollectedEvents { get; set; }

        Interop.Api _api;

        public TestHelper(Interop.Api api)
        {
            _api = api;
            CollectedEvents = new();

            // Hook script events.
            _api.CreateChannelEvent += Api_CreateChannelEvent;
            _api.SendEvent += Api_SendEvent;
            _api.MiscInternalEvent += Api_MiscInternalEvent;
        }

        void Api_CreateChannelEvent(object? sender, Interop.CreateChannelEventArgs e)
        {
            string s = $"CreateChannelEvent DevName:{e.DevName} ChanNum:{e.ChanNum} IsOutput:{e.IsOutput} Patch:{e.Patch}";
            CollectedEvents.Add(s);
            e.Ret = 0x0102;
        }

        void Api_SendEvent(object? sender, Interop.SendEventArgs e)
        {
            string s = $"SendEvent ChanHnd:{e.ChanHnd} IsNote:{e.IsNote} What:{e.What} Value:{e.Value}";
            CollectedEvents.Add(s);
            e.Ret = Defs.NEB_OK;
        }

        void Api_MiscInternalEvent(object? sender, Interop.MiscInternalEventArgs e)
        {
            string s = $"MiscInternalEvent LogLevel:{e.LogLevel} Bpm:{e.Bpm} Msg:{e.Msg}";
            CollectedEvents.Add(s);
            e.Ret = Defs.NEB_OK;
        }
    }


    // Test basic failure modes.
    public class INTEROP_FAIL_1 : TestSuite
    {
        public override void RunSuite()
        {
            UT_STOP_ON_FAIL(true);

            // General syntax error during load.
            {
                var _api = new Interop.Api();
                int stat = _api.Init();
                UT_EQUAL(stat, Defs.NEB_OK);

                File.WriteAllText(TestHelper.fn,
                    @"local neb = require(""nebulua"")\n
                    this is a bad statement");
                stat = _api.OpenScript(TestHelper.fn);
                UT_EQUAL(stat, Defs.NEB_ERR_SYNTAX);
                var e = _api.Error;
                UT_NOT_NULL(e);
                UT_TRUE(e.Contains("syntax error near 'is'"));
            }

            // General syntax error - lua_pcall()
            {
                var _api = new Interop.Api();
                int stat = _api.Init();
                UT_EQUAL(stat, Defs.NEB_OK);

                File.WriteAllText(TestHelper.fn,
                    @"local neb = require(""nebulua"")\n
                    res1 = 345 + nil_value");
                stat = _api.OpenScript(TestHelper.fn);
                UT_EQUAL(stat, Defs.NEB_OK); // runtime error
                string e = _api.Error;
                UT_NOT_NULL(e);
                UT_TRUE(e.Contains("attempt to perform arithmetic on a nil value"));
            }

            // Missing required C2L api element - luainterop_Setup(_ltest, &iret);
            {
                var _api = new Interop.Api();
                int stat = _api.Init();
                UT_EQUAL(stat, Defs.NEB_OK);

                File.WriteAllText(TestHelper.fn,
                    @"local neb = require(""nebulua"")\n
                    resx = 345 + 456");
                stat = _api.OpenScript(TestHelper.fn);
                UT_EQUAL(stat, Defs.NEB_OK); // runtime error
                string e = _api.Error;
                UT_NOT_NULL(e);
                UT_TRUE(e.Contains("INTEROP_BAD_FUNC_NAME"));
            }

            // Bad L2C api function
            {
                var _api = new Interop.Api();
                int stat = _api.Init();
                UT_EQUAL(stat, Defs.NEB_OK);

                File.WriteAllText(TestHelper.fn,
                    @"local neb = require(""nebulua"")\n
                    function setup()\n
                        neb.no_good(95)\n
                        return 0\n
                    end");
                stat = _api.OpenScript(TestHelper.fn);
                UT_EQUAL(stat, Defs.NEB_OK); // runtime error
                string e = _api.Error;
                UT_NOT_NULL(e);
                UT_TRUE(e.Contains("attempt to call a nil value (field 'no_good')"));
            }
        }
    }

    // Test fatal error() failure modes.
    public class INTEROP_FAIL_2 : TestSuite
    {
        public override void RunSuite()
        {
            UT_STOP_ON_FAIL(true);

            // General explicit error.
            {
                var _api = new Interop.Api();
                int stat = _api.Init();
                UT_EQUAL(stat, Defs.NEB_OK);

                File.WriteAllText(TestHelper.fn,
                    @"local neb = require(""nebulua"")\n
                    function setup()\n
                        error(""setup() raises error()"")\n
                        return 0\n
                    end");
                stat = _api.OpenScript(TestHelper.fn);
                UT_EQUAL(stat, Defs.NEB_OK); // runtime error
                string e = _api.Error;
                UT_NOT_NULL(e);
                UT_TRUE(e.Contains("setup() raises error()"));
            }
        }
    }

    // Test fatal internal failure modes.
    public class INTEROP_FAIL_3 : TestSuite
    {
        public override void RunSuite()
        {
            UT_STOP_ON_FAIL(true);

            // Runtime error.
            {
                var _api = new Interop.Api();
                int stat = _api.Init();
                UT_EQUAL(stat, Defs.NEB_OK);

                File.WriteAllText(TestHelper.fn,
                    @"local neb = require(""nebulua"")\n
                    function setup()\n
                        local bad = 123 + ng\n
                        return 0\n
                    end");
                stat = _api.OpenScript(TestHelper.fn);
                UT_EQUAL(stat, Defs.NEB_OK); // runtime error
                string e = _api.Error;
                UT_NOT_NULL(e);
                UT_TRUE(e.Contains("attempt to perform arithmetic on a nil value (global 'ng')"));
            }
        }
    }


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
