using System;
using System.Text;
using System.Collections.Generic;
using System.IO;
using Ephemera.NBagOfTricks;
using Ephemera.NBagOfTricks.PNUT;
using Nebulua;
//using Script;


namespace Test
{
    /// <summary>All success operations.</summary>
    public class SCRIPT_HAPPY : TestSuite
    {
        public override void RunSuite()
        {
            UT_STOP_ON_FAIL(true);

            EventCollector events = new();
            //_core.LoadScript(_scriptFn); // may throw
            var scriptFn = Path.Join(MiscUtils.GetSourcePath(), "..", "lua", "script_happy.lua");

            // Load the script. 
            HostCore hostCore = new();
            hostCore.LoadScript(scriptFn); // may throw
           // hostCore.

            UT_NOT_NULL(hostCore);

            //// Have a look inside.
            //UT_EQUAL(interop.SectionInfo.Count, 4);
            //// Fake valid loaded script.
            //State.Instance.InitSectionInfo(interop.SectionInfo);

            // Run script steps.
            events.CollectedEvents.Clear();
            for (int i = 0; i < 99; i++)
            {
                stat = hostCore._interop.Step(State.Instance.CurrentTick++);

                // Inject some received midi events.
                if (i % 20 == 0)
                {
                    stat = interop.RcvNote(0x0102, i, (double)i / 100);
                    //UT_EQUAL(stat, NebStatus.Ok);
                }

                if (i % 20 == 5)
                {
                    stat = interop.RcvController(0x0102, i, i);
                    //UT_EQUAL(stat, NebStatus.Ok);
                }
            }

            UT_EQUAL(events.CollectedEvents.Count, 122);
        }
    }

    /// <summary>Test basic failure modes.</summary>
    public class SCRIPT_FAIL : TestSuite
    {
        public override void RunSuite()
        {
            UT_STOP_ON_FAIL(true);

            // Program.MyInterop!.Dispose();
            //var interop = Program.MyInterop!;

            var tempfn = "_test_temp.lua";

            // General syntax error during load.
            {
                File.WriteAllText(tempfn,
                    @"local api = require(""script_api"")
                    this is a bad statement
                    end");
                NebStatus stat = interop.OpenScript(tempfn);
                UT_STRING_CONTAINS(interop.Error, "syntax error near 'is'");
                UT_EQUAL(stat, NebStatus.SyntaxError);
            }

            // Missing required C2L element - luainterop_Setup(_ltest, &iret);
            {
                File.WriteAllText(tempfn,
                    @"local api = require(""script_api"")
                    resx = 345 + 456");
                NebStatus stat = interop.OpenScript(tempfn);
                UT_STRING_CONTAINS(interop.Error, "Bad function name: setup()");
                UT_EQUAL(stat, NebStatus.SyntaxError);
            }

            // Bad L2C function
            {
                File.WriteAllText(tempfn,
                    @"local api = require(""script_api"")
                    function setup()
                        api.no_good(95)
                        return 0
                    end");
                NebStatus stat = interop.OpenScript(tempfn);
                UT_STRING_CONTAINS(interop.Error, "attempt to call a nil value (field 'no_good')");
                UT_EQUAL(stat, NebStatus.SyntaxError); // runtime error
            }

            // General explicit error.
            {
                File.WriteAllText(tempfn,
                    @"local api = require(""script_api"")
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
                File.WriteAllText(tempfn,
                    @"local api = require(""script_api"")
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

        public EventCollector()
        {
            CollectedEvents = [];

            // Hook script callbacks.
            Interop.Log += Interop_Log;
            Interop.CreateInputChannel += Interop_CreateInputChannel;
            Interop.CreateOutputChannel += Interop_CreateOutputChannel;
            Interop.SendNote += Interop_SendNote;
            Interop.SendController += Interop_SendController;
            Interop.SetTempo += Interop_SetTempo;
        }

        void Interop_Log(object? sender, LogArgs e)
        {
            string s = $"Log LogLevel:{e.level} Message:{e.msg}";
            CollectedEvents.Add(s);
            e.ret = 0;
        }

        void Interop_CreateInputChannel(object? sender, CreateInputChannelArgs e)
        {
            string s = $"CreateInputChannel DevName:{e.dev_name} chan_num:{e.chan_num}";
            CollectedEvents.Add(s);
            e.ret = 0x0102;
        }

        void Interop_CreateOutputChannel(object? sender, CreateOutputChannelArgs e)
        {
            string s = $"CreateOutputChannel DevName:{e.dev_name} chan_num: {e.chan_num} patch:{e.patch}";
            CollectedEvents.Add(s);
            e.ret = 0x0102;
        }

        void Interop_SendNote(object? sender, SendNoteArgs e)
        {
            string s = $"SendNote ChanHnd:{e.chan_hnd} note_num:{e.note_num} volume:{e.volume}";
            CollectedEvents.Add(s);
            e.ret = 0;
        }

        void Interop_SendController(object? sender, SendControllerArgs e)
        {
            string s = $"Send SendController:{e.chan_hnd} controller:{e.controller} value:{e.value}";
            CollectedEvents.Add(s);
            e.ret = 0;
        }

        void Interop_SetTempo(object? sender, SetTempoArgs e)
        {
            string s = $"SetTempo Bpm:{e.bpm}";
            CollectedEvents.Add(s);
            e.ret = 0;
        }
    }
}
