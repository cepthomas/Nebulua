using System;
using System.Text;
using System.Collections.Generic;
using System.IO;
using Ephemera.NBagOfTricks.PNUT;
using Nebulua.Common;
using Nebulua.Interop;


namespace Nebulua.Test
{
    /// <summary>Test core functions.</summary>
    public class CORE_HAPPY : TestSuite
    {
        public override void RunSuite()
        {
            NebStatus stat = NebStatus.Ok;

            var configFn = Path.Join(TestUtils.GetTestLuaDir(), "test_config.ini");
            var scriptFn = Path.Join(TestUtils.GetTestLuaDir(), "script_happy.lua");

            Core _core = new Core(configFn);
            UT_NOT_NULL(_core);
            stat = _core.RunScript(scriptFn);
            UT_EQUAL(stat, NebStatus.Ok);


            _core.Reload();

            _core.Dispose();

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
