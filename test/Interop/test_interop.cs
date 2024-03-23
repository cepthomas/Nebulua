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

            InteropHelper hlp = new(_api);

            State.Instance.PropertyChangeEvent += State_PropertyChangeEvent;

            // Load the script.
            string fn = @"C:\Dev\repos\Lua\Nebulua\test\script_happy.lua";

            stat = _api.OpenScript(fn);
            UT_EQUAL(stat, Defs.NEB_OK);
            UT_EQUAL(hlp.CollectedEvents.Count, 4);

            var info = _api.SectionInfo;
            UT_EQUAL(info.Count, 4);
// const char* sect_name = scriptinfo_GetSectionName(0);
// int sect_start = scriptinfo_GetSectionStart(0);
// UT_NOT_NULL(sect_name);
// UT_STR_EQUAL(sect_name, "beginning");
// UT_EQUAL(sect_start, 0);

// sect_name = scriptinfo_GetSectionName(1);
// sect_start = scriptinfo_GetSectionStart(1);

// UT_NOT_NULL(sect_name);
// UT_STR_EQUAL(sect_name, "middle");
// UT_EQUAL(sect_start, 646);

// sect_name = scriptinfo_GetSectionName(2);
// sect_start = scriptinfo_GetSectionStart(2);
// UT_NOT_NULL(sect_name);
// UT_STR_EQUAL(sect_name, "ending");
// UT_EQUAL(sect_start, 2118);

// sect_name = scriptinfo_GetSectionName(3);
// sect_start = scriptinfo_GetSectionStart(3);
// UT_NULL(sect_name);
// UT_EQUAL(sect_start, -1);

            var err = _api.Error;
            UT_NUL(err);

            hlp.CollectedEvents.Clear();
            for (int i = 0; i < 99; i++)
            {
                stat = _api.Step(State.Instance.CurrentTick);

                if (i % 20 == 0)
                {
                    stat = _api.InputNote(0x0102, i, (double)i / 100);
                    UT_EQUAL(stat, NEB_OK);
                }

                if (i % 20 == 5)
                {
                    stat = _api.InputController(0x0102, i, i);
                    UT_EQUAL(stat, NEB_OK);
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


    public class InteropHelper
    {
        List<string> CollectedEvents { get; set; } = new();

        Interop.Api _api = new();

        public void Init(Interop.Api api)
        {
            _api = api;
            CollectedEvents.Clear();

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


    public class INTEROP_XXX1 : TestSuite //UT_SUITE(EXEC_ERR1, "Test basic failure modes.")
    {
        public override void RunSuite()
        {
            UT_STOP_ON_FAIL(true);
            string fn = @"temp_script.lua";

            var _api = new Interop.Api();
            int stat = _api.Init();
            UT_EQUAL(stat, Defs.NEB_OK);




            // Load the script.

            stat = _api.OpenScript(fn);
            UT_EQUAL(stat, Defs.NEB_OK);
            UT_EQUAL(hlp.CollectedEvents.Count, 4);



            /// General syntax error during load.
            File.WriteAllText(fn,
                "local neb = require(\"nebulua\")\n"
                "this is a bad statement\n");
            stat = _api.OpenScript(fn);
            UT_EQUAL(stat, LUA_ERRSYNTAX);
            const char* e = nebcommon_EvalStatus(_ltest, stat, "ERR1");
            UT_STR_CONTAINS(e, "syntax error near 'is'");



            ///// General syntax error - lua_pcall(_ltest, 0, LUA_MULTRET, 0);
            s1 =
                "local neb = require(\"nebulua\")\n"
                "res1 = 345 + nil_value\n";
            stat = luaL_loadstring(_ltest, s1);
            UT_EQUAL(stat, LUA_OK);
            stat = lua_pcall(_ltest, 0, LUA_MULTRET, 0);
            UT_EQUAL(stat, LUA_ERRRUN); // runtime error
            e = nebcommon_EvalStatus(_ltest, stat, "ERR2");
            UT_STR_CONTAINS(e, "attempt to perform arithmetic on a nil value");




            ///// Missing required C2L api element - luainterop_Setup(_ltest, &iret);
            s1 =
                "local neb = require(\"nebulua\")\n"
                "resx = 345 + 456\n";
            stat = luaL_loadstring(_ltest, s1);
            UT_EQUAL(stat, LUA_OK);
            stat = lua_pcall(_ltest, 0, LUA_MULTRET, 0);
            UT_EQUAL(stat, LUA_OK);
            stat = luainterop_Setup(_ltest, &iret);
            UT_EQUAL(stat, INTEROP_BAD_FUNC_NAME);
            e = nebcommon_EvalStatus(_ltest, stat, "ERR3");
            UT_STR_CONTAINS(e, "INTEROP_BAD_FUNC_NAME");


            ///// Bad L2C api function
            s1 =
                "local neb = require(\"nebulua\")\n"
                "function setup()\n"
                "    neb.no_good(95)\n"
                "    return 0\n"
                "end\n";
            stat = luaL_loadstring(_ltest, s1);
            UT_EQUAL(stat, LUA_OK);
            stat = lua_pcall(_ltest, 0, LUA_MULTRET, 0);
            UT_EQUAL(stat, LUA_OK);
            stat = luainterop_Setup(_ltest, &iret);
            UT_EQUAL(stat, LUA_ERRRUN);
            e = nebcommon_EvalStatus(_ltest, stat, "ERR4");
            UT_STR_CONTAINS(e, "attempt to call a nil value (field 'no_good')");

        }
    }
}


    public class INTEROP_XXX2 : TestSuite //UT_SUITE(EXEC_ERR2, "Test error() failure modes.")
    {
        public override void RunSuite()
        {
            UT_STOP_ON_FAIL(true);
            var _api = new Interop.Api();
            int stat = _api.Init();
            UT_EQUAL(stat, Defs.NEB_OK);

            InteropHelper hlp = new(_api);

            State.Instance.PropertyChangeEvent += State_PropertyChangeEvent;

            // Load the script.
            string fn = @"C:\Dev\repos\Lua\Nebulua\test\script_happy.lua";

            stat = _api.OpenScript(fn);
            UT_EQUAL(stat, Defs.NEB_OK);
            UT_EQUAL(hlp.CollectedEvents.Count, 4);

        }
    }
    // // Load lua.
    // lua_State* _ltest = luaL_newstate();
    // luaL_openlibs(_ltest);
    // luainterop_Load(_ltest);
    // lua_pop(_ltest, 1);

    // ///// General explicit error.
    // const char* s1 =
    //     "local neb = require(\"nebulua\")\n"
    //     "function setup()\n"
    //     "    error(\"setup() raises error()\")\n"
    //     "    return 0\n"
    //     "end\n";
    // stat = luaL_loadstring(_ltest, s1);
    // UT_EQUAL(stat, LUA_OK);
    // stat = lua_pcall(_ltest, 0, LUA_MULTRET, 0);
    // UT_EQUAL(stat, LUA_OK);
    // stat = luainterop_Setup(_ltest, &iret);
    // UT_EQUAL(stat, LUA_ERRRUN);
    // const char* e = nebcommon_EvalStatus(_ltest, stat, "ERR5");
    // UT_STR_CONTAINS(e, "setup() raises error()");

    // lua_close(_ltest);



    public class INTEROP_XXX3 : TestSuite //UT_SUITE(EXEC_ERR3, "Test fatal internal failure modes.")
    {
        public override void RunSuite()
        {
            UT_STOP_ON_FAIL(true);
            var _api = new Interop.Api();
            int stat = _api.Init();
            UT_EQUAL(stat, Defs.NEB_OK);

            InteropHelper hlp = new(_api);

            State.Instance.PropertyChangeEvent += State_PropertyChangeEvent;

            // Load the script.
            string fn = @"C:\Dev\repos\Lua\Nebulua\test\script_happy.lua";

            stat = _api.OpenScript(fn);
            UT_EQUAL(stat, Defs.NEB_OK);
            UT_EQUAL(hlp.CollectedEvents.Count, 4);

        }
    }
    // // Load lua.
    // lua_State* _ltest = luaL_newstate();
    // luaL_openlibs(_ltest);
    // luainterop_Load(_ltest);
    // lua_pop(_ltest, 1);

    // ///// Runtime error.
    // const char* s1 =
    //     "local neb = require(\"nebulua\")\n"
    //     "function setup()\n" 
    //     "    local bad = 123 + ng\n"
    //     "    return 0\n"
    //     "end\n";
    //  stat = luaL_loadstring(_ltest, s1);
    // UT_EQUAL(stat, LUA_OK);
    // stat = lua_pcall(_ltest, 0, LUA_MULTRET, 0);
    // UT_EQUAL(stat, LUA_OK);
    // stat = luainterop_Setup(_ltest, &iret);
    // UT_EQUAL(stat, LUA_ERRRUN);
    // const char* e = nebcommon_EvalStatus(_ltest, stat, "ERR6");
    // UT_STR_CONTAINS(e, "attempt to perform arithmetic on a nil value (global 'ng')");

    // lua_close(_ltest);



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
