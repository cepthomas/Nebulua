using System;
using System.Text;
using System.Collections.Generic;
using System.IO;
using Ephemera.NBagOfTricks;
using Ephemera.NBagOfTricks.PNUT;
using Nebulua.Interop;


namespace Nebulua.Test
{
    /// <summary>All success operations.</summary>
    public class INTEROP_HAPPY : TestSuite
    {
        public override void RunSuite()
        {
            UT_STOP_ON_FAIL(true);

            // Create interop.
            var lpath = Utils.GetLuaPath();
            Api interop = new(lpath);

            EventCollector events = new(interop);
            string scrfn = Path.Join(Utils.GetAppRoot(), "test", "lua", "script_happy.lua");
            State.Instance.ValueChangeEvent += State_ValueChangeEvent;

            // Load the script.
            NebStatus stat = interop.OpenScript(scrfn);
            UT_EQUAL(interop.Error, "");
            UT_EQUAL(stat, NebStatus.Ok);
            UT_EQUAL(events.CollectedEvents.Count, 7);

            // Have a look inside.
            UT_EQUAL(interop.SectionInfo.Count, 4);

            // Fake valid loaded script.
            State.Instance.InitSectionInfo(interop.SectionInfo);

            // Run script steps.
            events.CollectedEvents.Clear();
            for (int i = 0; i < 99; i++)
            {
                stat = interop.Step(State.Instance.CurrentTick++);

                // Inject some received midi events.
                if (i % 20 == 0)
                {
                    stat = interop.RcvNote(0x0102, i, (double)i / 100);
                    UT_EQUAL(stat, NebStatus.Ok);
                }

                if (i % 20 == 5)
                {
                    stat = interop.RcvController(0x0102, i, i);
                    UT_EQUAL(stat, NebStatus.Ok);
                }
            }

            UT_EQUAL(events.CollectedEvents.Count, 115);
        }

        void State_ValueChangeEvent(object? sender, string name)
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
    public class INTEROP_FAIL : TestSuite
    {
        public override void RunSuite()
        {
            UT_STOP_ON_FAIL(true);

            // Program.MyInterop!.Dispose();
            //var interop = Program.MyInterop!;

            var tempfn = "_test_temp.lua";

            // General syntax error during load.
            {
                var lpath = Utils.GetLuaPath();
                Api interop = new(lpath);

                File.WriteAllText(tempfn,
                    @"local neb = require(""nebulua"")
                    this is a bad statement
                    end");
                NebStatus stat = interop.OpenScript(tempfn);
                UT_STRING_CONTAINS(interop.Error, "syntax error near 'is'");
                UT_EQUAL(stat, NebStatus.SyntaxError);
            }

            // Missing required C2L api element - luainterop_Setup(_ltest, &iret);
            {
                var lpath = Utils.GetLuaPath();
                Api interop = new(lpath);

                File.WriteAllText(tempfn,
                    @"local neb = require(""nebulua"")
                    resx = 345 + 456");
                NebStatus stat = interop.OpenScript(tempfn);
                UT_STRING_CONTAINS(interop.Error, "Bad function name: setup()");
                UT_EQUAL(stat, NebStatus.SyntaxError);
            }

            // Bad L2C api function
            {
                var lpath = Utils.GetLuaPath();
                Api interop = new(lpath);

                File.WriteAllText(tempfn,
                    @"local neb = require(""nebulua"")
                    function setup()
                        neb.no_good(95)
                        return 0
                    end");
                NebStatus stat = interop.OpenScript(tempfn);
                UT_STRING_CONTAINS(interop.Error, "attempt to call a nil value (field 'no_good')");
                UT_EQUAL(stat, NebStatus.SyntaxError); // runtime error
            }

            // General explicit error.
            {
                var lpath = Utils.GetLuaPath();
                Api interop = new(lpath);

                File.WriteAllText(tempfn,
                    @"local neb = require(""nebulua"")
                    function setup()
                        error(""setup() raises error()"")
                        return 0
                    end");
                NebStatus stat = interop.OpenScript(tempfn);
                UT_STRING_CONTAINS(interop.Error, "setup() raises error()");
                UT_EQUAL(stat, NebStatus.SyntaxError);
            }

            // Runtime error.
            {
                var lpath = Utils.GetLuaPath();
                Api interop = new(lpath);

                File.WriteAllText(tempfn,
                    @"local neb = require(""nebulua"")
                    function setup()
                        local bad = 123 + ng
                        return 0
                    end");
                NebStatus stat = interop.OpenScript(tempfn);
                UT_STRING_CONTAINS(interop.Error, "attempt to perform arithmetic on a nil value (global 'ng')");
                UT_EQUAL(stat, NebStatus.SyntaxError); // runtime error
            }
        }
    }

    /// <summary>Hook used to capture events from test target.</summary>
    internal class EventCollector
    {
        public List<string> CollectedEvents { get; set; }

        readonly Api _api;

        public EventCollector(Api api)
        {
            _api = api;
            CollectedEvents = [];

            // Hook script events.
            Api.CreateChannel += Interop_CreateChannel;
            Api.Send += Interop_Send;
            Api.Log += Interop_Log;
            Api.PropertyChange += Interop_PropertyChange;
        }

        void Interop_CreateChannel(object? sender, CreateChannelArgs e)
        {
            string s = $"CreateChannel DevName:{e.DevName} ChanNum:{e.ChanNum} IsOutput:{e.IsOutput} Patch:{e.Patch}";
            CollectedEvents.Add(s);
            e.Ret = 0x0102;
        }

        void Interop_Send(object? sender, SendArgs e)
        {
            string s = $"Send ChanHnd:{e.ChanHnd} IsNote:{e.IsNote} What:{e.What} Value:{e.Value}";
            CollectedEvents.Add(s);
            e.Ret = (int)NebStatus.Ok;
        }

        void Interop_Log(object? sender, LogArgs e)
        {
            string s = $"Log LogLevel:{e.LogLevel} Msg:{e.Msg}";
            CollectedEvents.Add(s);
            e.Ret = (int)NebStatus.Ok;
        }

        void Interop_PropertyChange(object? sender, PropertyArgs e)
        {
            string s = $"PropertyChange Bpm:{e.Bpm}";
            CollectedEvents.Add(s);
            e.Ret = (int)NebStatus.Ok;
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
