using System;
using System.Text;
using System.Collections.Generic;
using System.IO;
using Ephemera.NBagOfTricks.PNUT;


namespace Nebulua.Test
{
    public class INTEROP_ONE : TestSuite
    {
        public override void RunSuite()
        {
            int int1 = 321;
            //string str1 = "round and round";
            string str2 = "the mulberry bush";
            double dbl2 = 1.600;

            UT_INFO("Test UT_INFO with args", int1, dbl2);
            UT_EQUAL(str2, "the mulberry bush");






            // Create script api.
            int stat = _api.Init();
            if (stat != Defs.NEB_OK)
            {
                _logger.Error(_api.Error);
                Environment.Exit(1);
            }

            // Hook script events.
            _api.CreateChannelEvent += Api_CreateChannelEvent;
            _api.SendEvent += Api_SendEvent;
            _api.MiscInternalEvent += Api_MiscInternalEvent;





            // Load the script.
            stat = _api.OpenScript(fn);
            if (stat != Defs.NEB_OK)
            {
                _logger.Error(_api.Error);
            }

            _api.SectionInfo;


            _api.Error;


            stat = _api.Step(State.Instance.CurrentTick);


            stat = _api.InputNote(chan_hnd, evt.NoteNumber, (double)evt.Velocity / Defs.MIDI_VAL_MAX);


            stat = _api.InputNote(chan_hnd, evt.NoteNumber, (double)evt.Velocity / Defs.MIDI_VAL_MAX);


            stat = _api.InputController(chan_hnd, (int)evt.Controller, evt.ControllerValue);


        }


        void Api_CreateChannelEvent(object? sender, Interop.CreateChannelEventArgs e)
        {

        }

        void Api_SendEvent(object? sender, Interop.SendEventArgs e)
        {

        }

        void Api_MiscInternalEvent(object? sender, Interop.MiscInternalEventArgs e)
        {

        }

    }







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
