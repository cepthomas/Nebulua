using System;
using System.Text;
using System.Collections.Generic;
using System.IO;
using System.Diagnostics;
using Ephemera.NBagOfTricks.PNUT;
using Nebulua;
using Nebulua.Interop;


namespace Nebulua.Test
{
    /// <summary>Test core functions.</summary>
    public class CORE_BASIC : TestSuite
    {
        public override void RunSuite()
        {
            NebStatus stat;
            var scriptFn = Path.Join(Utils.GetAppRoot(), "test", "lua", "script_happy.lua");

            // Load the script.
            Core core = new();
            UT_NOT_NULL(core);
            stat = core.LoadScript(scriptFn);
            UT_EQUAL(stat, NebStatus.Ok);

            // Look inside private stuff:
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

            // Reload. Need real interop to test this.
            core.LoadScript();

            // Clean up.
            core.Dispose();
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
