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
            var _api = new Interop.Api();
            int stat = _api.Init();
            UT_EQUAL(stat, Defs.NEB_OK);

            TestHelper hlp = new(_api);

            State.Instance.PropertyChangeEvent += State_PropertyChangeEvent;

            // Load the script.
            stat = _api.OpenScript(TestHelper.ScriptFn);
            UT_EQUAL(stat, Defs.NEB_OK); // is 10=NEB_ERR_INTERNAL
            UT_NULL(_api.Error);

            // Have a look inside.
            UT_EQUAL(hlp.CollectedEvents.Count, 4);
            foreach (var kv in _api.SectionInfo)// get sorted
            {
            }

            var err = _api.Error;
            UT_NULL(err);

            // Run fake steps.
            hlp.CollectedEvents.Clear();
            for (int i = 0; i < 99; i++)
            {
                stat = _api.Step(State.Instance.CurrentTick++);

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

    /// <summary>Test basic failure modes.</summary>
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

                File.WriteAllText(TestHelper.TempFn,
                    @"local neb = require(""nebulua"")\n
                    this is a bad statement");
                stat = _api.OpenScript(TestHelper.TempFn);
                UT_EQUAL(stat, Defs.NEB_ERR_SYNTAX); // is 10=NEB_ERR_INTERNAL
                var e = _api.Error;
                UT_NOT_NULL(e);
                UT_TRUE(e.Contains("syntax error near 'is'"));
            }

            // General syntax error - lua_pcall()
            {
                var _api = new Interop.Api();
                int stat = _api.Init();
                UT_EQUAL(stat, Defs.NEB_OK);

                File.WriteAllText(TestHelper.TempFn,
                    @"local neb = require(""nebulua"")\n
                    res1 = 345 + nil_value");
                stat = _api.OpenScript(TestHelper.TempFn);
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

                File.WriteAllText(TestHelper.TempFn,
                    @"local neb = require(""nebulua"")\n
                    resx = 345 + 456");
                stat = _api.OpenScript(TestHelper.TempFn);
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

                File.WriteAllText(TestHelper.TempFn,
                    @"local neb = require(""nebulua"")\n
                    function setup()\n
                        neb.no_good(95)\n
                        return 0\n
                    end");
                stat = _api.OpenScript(TestHelper.TempFn);
                UT_EQUAL(stat, Defs.NEB_OK); // runtime error
                string e = _api.Error;
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

            // General explicit error.
            {
                var _api = new Interop.Api();
                int stat = _api.Init();
                UT_EQUAL(stat, Defs.NEB_OK);

                File.WriteAllText(TestHelper.TempFn,
                    @"local neb = require(""nebulua"")\n
                    function setup()\n
                        error(""setup() raises error()"")\n
                        return 0\n
                    end");
                stat = _api.OpenScript(TestHelper.TempFn);
                UT_EQUAL(stat, Defs.NEB_OK); // runtime error  // is 10=NEB_ERR_INTERNAL
                string e = _api.Error;
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

            // Runtime error.
            {
                var _api = new Interop.Api();
                int stat = _api.Init();
                UT_EQUAL(stat, Defs.NEB_OK);

                File.WriteAllText(TestHelper.TempFn,
                    @"local neb = require(""nebulua"")\n
                    function setup()\n
                        local bad = 123 + ng\n
                        return 0\n
                    end");
                stat = _api.OpenScript(TestHelper.TempFn);
                UT_EQUAL(stat, Defs.NEB_OK); // runtime error // is 10=NEB_ERR_INTERNAL
                string e = _api.Error;
                UT_NOT_NULL(e);
                UT_TRUE(e.Contains("attempt to perform arithmetic on a nil value (global 'ng')"));
            }
        }
    }


    /// <summary>Used to capture events from test target.</summary>
    public class TestHelper
    {
        public static string ScriptFn { get { return Path.Join(MiscUtils.GetSourcePath(), "script_happy.lua"); } }
        public static string TempFn { get { return Path.Join(MiscUtils.GetSourcePath(), "temp.lua"); } }
        public List<string> CollectedEvents { get; set; }

        Interop.Api _api;

        public TestHelper(Interop.Api api)
        {
            _api = api;
            CollectedEvents = new();

            // Hook script events.
            _api.CreateChannelEvent += Api_CreateChannelEvent;
            _api.SendEvent += Api_SendEvent;
            _api.LogEvent += Api_LogEvent;
            _api.ScriptEvent += Api_ScriptEvent;
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

        void Api_LogEvent(object? sender, Interop.LogEventArgs e)
        {
            string s = $"LogEvent LogLevel:{e.LogLevel} Msg:{e.Msg}";
            CollectedEvents.Add(s);
            e.Ret = Defs.NEB_OK;
        }

        void Api_ScriptEvent(object? sender, Interop.ScriptEventArgs e)
        {
            string s = $"ScriptEvent Bpm:{e.Bpm}";
            CollectedEvents.Add(s);
            e.Ret = Defs.NEB_OK;
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
