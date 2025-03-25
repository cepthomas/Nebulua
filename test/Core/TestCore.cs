using System;
using System.Text;
using System.Collections.Generic;
using System.IO;
using System.Diagnostics;
using Ephemera.NBagOfTricks;
using Ephemera.NBagOfTricks.PNUT;
using Nebulua;
using Script;


namespace TestCore
{
    /// <summary> Test core functions.</summary>
    public class CORE_BASIC : TestSuite
    {
        public override void RunSuite()
        {
            var scriptFn = Path.Join(MiscUtils.GetSourcePath(), "..", "lua", "script_happy.lua");

            Interop.CreateOutputChannel += Interop_CreateOutputChannel;

            // Load the script.
            Core core = new();
            UT_NOT_NULL(core);
            core.LoadScript(scriptFn);
            //stat = core.LoadScript(scriptFn);
            //UT_EQUAL(stat, NebStatus.Ok);

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

        private void Interop_CreateOutputChannel(object? sender, CreateOutputChannelArgs e)
        {
            throw new NotImplementedException();
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
