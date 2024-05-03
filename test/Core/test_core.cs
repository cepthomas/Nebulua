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
    public class CORE_BASIC : TestSuite
    {
        public override void RunSuite()
        {
            NebStatus stat;
            var configFn = Path.Join(TestUtils.GetProjectSourceDir(), "test", "test_config.ini");
            var scriptFn = Path.Join(TestUtils.GetProjectSourceDir(), "test", "script_happy.lua");

            // Load the script.
            Core _core = new(configFn);
            UT_NOT_NULL(_core);
            stat = _core.RunScript(scriptFn);
            UT_EQUAL(stat, NebStatus.Ok);

            // TODO more? look inside private stuff:
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

//            _core.Reload();

            // Clean up.
            _core.Dispose();
        }
    }

    /// <summary>Test life cycles of managed and unmanaged objects.</summary>
    public class CORE_LIFE : TestSuite
    {
        public override void RunSuite()
        {
            NebStatus stat;
            var configFn = Path.Join(TestUtils.GetProjectSourceDir(), "test", "test_config.ini");
            var scriptFn = Path.Join(TestUtils.GetProjectSourceDir(), "test", "script_happy.lua");

            UT_INFO($"DBG 1 CORE_LIFE.CORE_LIFE() this={this.GetHashCode()}");
            Console.WriteLine($"DBG 1 CORE_LIFE.CORE_LIFE() this={this.GetHashCode()}");


            // Load the script.
            Core _core = new(configFn);

Console.WriteLine($"DBG 2 CORE_LIFE.CORE_LIFE() this={this.GetHashCode()} _core={_core.GetHashCode()}");

            UT_NOT_NULL(_core);

            stat = _core.RunScript(scriptFn);
            UT_EQUAL(stat, NebStatus.Ok);

            // TODO1 test this + trace construct/destruct/dispose for leaks.
            // - https://stackoverflow.com/questions/2812071/what-is-a-way-to-reload-lua-scripts-during-run-time
            // - https://stackoverflow.com/questions/9369318/hot-swap-code-in-lua

Console.WriteLine($"DBG 3 CORE_LIFE.CORE_LIFE() this={this.GetHashCode()} _core={_core.GetHashCode()}");

            _core.Reload();
Console.WriteLine($"DBG 4 CORE_LIFE.CORE_LIFE() this={this.GetHashCode()} _core={_core.GetHashCode()}");

            // Clean up.
            _core.Dispose();
        
Console.WriteLine($"DBG 5 CORE_LIFE.CORE_LIFE() this={this.GetHashCode()} _core={_core.GetHashCode()}");
        }
    }

    /*
public App()
{
    _cmdProc = new(Console.In, Console.Out);
    _cmdProc.Write("Greetings from Nebulua!");
Console.WriteLine($"DBG CliApp.CliApp() this={this.GetHashCode()}");
    _core = new Core(configFn);
Console.WriteLine($"DBG CliApp.CliApp()2 this={this.GetHashCode()} _core={_core.GetHashCode()}");
    _core.RunScript(_scriptFn);
}

public void Dispose()
{
Console.WriteLine($"DBG CliApp.Dispose()1 this={this.GetHashCode()} _core={_core.GetHashCode()}");
    if (!_disposed)
    {
        _core?.Dispose();
        _disposed = true;
    }
Console.WriteLine($"DBG CliApp.Dispose()2 this={this.GetHashCode()} _core={_core.GetHashCode()}");
}
*/



    /// <summary>Test entry.</summary>
    static class Program
    {
        [STAThread]
        static void Main(string[] _)
        {
            TestRunner runner = new(OutputFormat.Readable);
            var cases = new[] { "CORE_LIFE" };
            runner.RunSuites(cases);
            File.WriteAllLines(@"_test.txt", runner.Context.OutputLines);
        }
    }
}
