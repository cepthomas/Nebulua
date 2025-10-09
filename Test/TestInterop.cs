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

            interop.RunScript(scriptFn, luaPath);
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
}
