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
    public class SCRIPT_INTERNALS : TestSuite
    {
        public override void RunSuite()
        {
            UT_STOP_ON_FAIL(true);

            EventCollector ecoll = new();
            using Interop interop = new();

            // Checks lua status and throws exception if it failed.
            // interop.EvalLuaStatus(0, "Hello");
            // #define LUA_OK      0
            // #define LUA_YIELD   1
            // #define LUA_ERRRUN  2  "ScriptRunError"
            // #define LUA_ERRSYNTAX   3   "ScriptSyntaxError"
            // #define LUA_ERRMEM  4
            // #define LUA_ERRERR  5
            // #define LUA_ERRFILE LUA_ERRERR+1  "ScriptFileError"
            // default: "AppInteropError"


            // Checks lua interop error and throws exception if it failed.
            // interop.EvalLuaInteropStatus(new string("const char* err"), "const char* info");


            // var c = interop.ToCString("input");
            // interop.Collect();
            // public class Scope
            // {
            // public:
            //     Scope();
            //     virtual ~Scope();
            // };
            // #define SCOPE() Scope _scope;
            // static CRITICAL_SECTION _critsect;
            // Scope::Scope() { EnterCriticalSection(&_critsect); }
            // Scope::~Scope() { Collect(); LeaveCriticalSection(&_critsect); }


            // Exception used for all interop errors.
            // LuaException(String^ message) : Exception(message) {}
        }
    }

    /// <summary>All success operations.</summary>
    public class SCRIPT_HAPPY : TestSuite
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
            var scriptFn = Path.Join(testDir, "script_happy.lua");

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
    public class SCRIPT_FAIL : TestSuite
    {
        public override void RunSuite()
        {
            UT_STOP_ON_FAIL(true);

            // Set up runtime lua environment.
            var testDir = MiscUtils.GetSourcePath();
            var luaPath = $"{testDir}\\?.lua;{testDir}\\..\\LBOT\\?.lua;;";
            var scriptFn = Path.Join(testDir, "script_happy.lua");

            // Program.MyInterop!.Dispose();
            //var interop = Program.MyInterop!;

            var tempfn = "_test_temp.lua";

            // General syntax error during load.
            {
                try
                {
                    using Interop interop = new();
                    File.WriteAllText(tempfn,
                        @"local api = require(""luainterop"")
                    this is a bad statement
                    end");
                    interop.Run(tempfn, luaPath);
                    UT_FAIL("did not throw");
                }
                catch (Exception e)
                {
                    UT_STRING_CONTAINS(e.Message, "syntax error near 'is'");
                }
            }

            // Missing required C2L element - luainterop_Setup(_ltest, &iret);
            {
                try
                {
                    using Interop interop = new();
                    File.WriteAllText(tempfn,
                        @"local api = require(""luainterop"")
                    resx = 345 + 456");
                    interop.Run(tempfn, luaPath);
                    UT_FAIL("did not throw");
                }
                catch (Exception e)
                {
                    UT_STRING_CONTAINS(e.Message, "????");
                }
            }

            // Bad L2C function
            {
                try
                {
                    using Interop interop = new();
                    File.WriteAllText(tempfn,
                        @"local api = require(""luainterop"")
                    function setup()
                        api.no_good(95)
                        return 0
                    end");
                    interop.Run(tempfn, luaPath);
                    UT_FAIL("did not throw");
                }
                catch (Exception e)
                {
                    UT_STRING_CONTAINS(e.Message, "????");
                }
            }

            // General explicit error.
            {
                try
                {
                    using Interop interop = new();
                    File.WriteAllText(tempfn,
                        @"local api = require(""luainterop"")
                    function setup()
                        error(""setup() raises error()"")
                        return 0
                    end");
                    interop.Run(tempfn, luaPath);
                    UT_FAIL("did not throw");
                }
                catch (Exception e)
                {
                    UT_STRING_CONTAINS(e.Message, "????");
                }
            }

            // Runtime error.
            {
                try
                {
                    using Interop interop = new();
                    File.WriteAllText(tempfn,
                        @"local api = require(""luainterop"")
                    function setup()
                        local bad = 123 + ng
                        return 0
                    end");
                    interop.Run(tempfn, luaPath);
                    UT_FAIL("did not throw");
                }
                catch (Exception e)
                {
                    UT_STRING_CONTAINS(e.Message, "????");
                }
            }
        }
    }
}
