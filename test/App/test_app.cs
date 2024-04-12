using System;
using System.Text;
using System.Collections.Generic;
using System.IO;
using Ephemera.NBagOfTricks.PNUT;


// void _MidiInHandler(HMIDIIN hMidiIn, UINT wMsg, DWORD_PTR dwInstance, DWORD_PTR dwParam1, DWORD_PTR dwParam2);
// void _MidiClockHandler(double msec);
// int exec_Main(const char* script_fn);

// const char* _my_midi_in1  = "loopMIDI Port";
// const char* _my_midi_out1 = "Microsoft GS Wavetable Synth";


namespace Nebulua.Test
{
    /// <summary>Test application functions. TODO1 Doesn't do anything yet.
    /// </summary>
    public class APP_ONE : TestSuite
    {
        public override void RunSuite()
        {
            // int stat = 0;

            // //////
            // HMIDIIN hMidiIn = 0;
            // UINT wMsg = 0;
            // DWORD_PTR dwInstance = 0;
            // DWORD_PTR dwParam1 = 0;
            // DWORD_PTR dwParam2 = 0;
            // _MidiInHandler(hMidiIn, wMsg, dwInstance, dwParam1, dwParam2);

            // //////
            // double msec = 12.34;
            // _MidiClockHandler(msec);

            // //////
            // // stat = exec_Main(script_fn);
            // // Needs luapath.

            // lua_close(_l);

            // return 0;
        }
    }

    //// Insert some hooks to support testing.
    //public partial class AppXXX
    //{
    //    // Fake cli output.
    //    CliTextWriter _myCliOut = new();

    //    // Fake cli input.
    //    CliTextReader _myCliIn = new();

    //    public List<string> CaptureLines
    //    {
    //        get { return StringUtils.SplitByTokens(_myCliOut.Capture.ToString(), "\r\n"); }
    //    }

    //    public string NextLine
    //    {
    //        get { return _myCliIn.NextLine; }
    //        set { _myCliIn.NextLine = value; }
    //    }

    //    public string Prompt
    //    {
    //        get { return _prompt; }
    //    }

    //    public void Clear()
    //    {
    //        _myCliOut.Capture.Clear();
    //        _myCliIn.NextLine = "";
    //    }

    //    public string GetPrompt()
    //    {
    //        return _prompt;
    //    }

    //    public void HookCli()
    //    {
    //        _cliOut = _myCliOut;
    //        _cliIn = _myCliIn;
    //    }
    //}

    /// <summary>Test entry.</summary>
    static class Program
    {
        [STAThread]
        static void Main(string[] _)
        {
            TestRunner runner = new(OutputFormat.Readable);
            var cases = new[] { "APP" };
            runner.RunSuites(cases);
            File.WriteAllLines(@"_test.txt", runner.Context.OutputLines);
        }
    }
}
