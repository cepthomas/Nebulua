using System;
using System.Text;
using System.Collections.Generic;
using System.IO;
using Ephemera.NBagOfTricks.PNUT;
using Nebulua.Common;
using Nebulua.Interop;


namespace Nebulua.Test
{
    /// <summary>Test core functions. TODO1 more</summary>
    public class CORE_HAPPY : TestSuite
    {
        public override void RunSuite()
        {
            NebStatus stat = NebStatus.Ok;
            var configFn = Path.Join(TestUtils.GetProjectSourceDir(), "test", "test_config.ini");
            var scriptFn = Path.Join(TestUtils.GetProjectSourceDir(), "test", "script_happy.lua");

            Core _core = new Core(configFn);
            UT_NOT_NULL(_core);
            stat = _core.RunScript(scriptFn);
            UT_EQUAL(stat, NebStatus.Ok);


            //// Load the script.
            //NebStatus stat = interop.OpenScript(scrfn);
            //UT_EQUAL(interop.Error, "");
            //UT_EQUAL(stat, NebStatus.Ok);
            //UT_EQUAL(events.CollectedEvents.Count, 6);

            //// Have a look inside.
            //UT_EQUAL(State.Instance.SectionInfo.Count, 4);
            //UT_EQUAL(State.Instance.SectionInfo[0].tick, 0);
            //UT_EQUAL(State.Instance.SectionInfo[0].name, "beginning");
            //UT_EQUAL(State.Instance.SectionInfo[1].tick, 256);
            //UT_EQUAL(State.Instance.SectionInfo[1].name, "middle");
            //UT_EQUAL(State.Instance.SectionInfo[2].tick, 512);
            //UT_EQUAL(State.Instance.SectionInfo[2].name, "ending");
            //UT_EQUAL(State.Instance.SectionInfo[3].tick, 768);
            //UT_EQUAL(State.Instance.SectionInfo[3].name, "_LENGTH");





            _core.Reload();

            _core.Dispose();

            // private stuff:
            //void CallbackError(Exception e)
            //void Interop_CreateChannel(object? sender, CreateChannelArgs e)
            //void Interop_Log(object? sender, LogArgs e)
            //void Interop_PropertyChange(object? sender, PropertyArgs e)
            //void Interop_Send(object? sender, SendArgs e)
            //void KillAll()
            //void Midi_ReceiveEvent(object? sender, MidiEvent e)
            //void MmTimer_Callback(double totalElapsed, double periodElapsed)
            //void SetTimer(int tempo)
            //void State_ValueChangeEvent(object? sender, string name)
        }
    }

    /// <summary>Test entry.</summary>
    static class Program
    {
        [STAThread]
        static void Main(string[] _)
        {
            TestRunner runner = new(OutputFormat.Readable);
            var cases = new[] { "CORE" };
            runner.RunSuites(cases);
            File.WriteAllLines(@"_test.txt", runner.Context.OutputLines);
        }
    }
}
