using System;
using System.Text;
using System.Collections.Generic;
using System.IO;
using Ephemera.NBagOfTricks;
using Ephemera.NBagOfTricks.PNUT;


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
                var stat = interop.Step(i);

                // Inject some received midi events.
                if (i % 20 == 0)
                {
                    stat = interop.ReceiveMidiNote(0x0102, i, (double)i / 100);
                    UT_EQUAL(stat, 0);
                    //Nebulua\Test\Utils.cs:
                    //Interop.SendMidiNote += CollectEvent;
                    //SendMidiNoteArgs ne => $"SendMidiNote chan_hnd:{ne.chan_hnd} note_num:{ne.note_num} volume:{ne.volume}",
                }

                if (i % 20 == 5)
                {
                    stat = interop.ReceiveMidiController(0x0102, i, i);
                    UT_EQUAL(stat, 0);
                }
            }

            // Script api functions ????

            // /// api.send_midi_note(hnd_strings, note_num, volume)
            // void SendMidiNote(int chnd, int note_num, double volume)

            // /// api.send_midi_controller(hnd_synth, ctrl.Pan, 90)
            // void SendMidiController(int chnd, int controller_id, int value)

            // /// Callback from script: function receive_midi_note(chan_hnd, note_num, volume)
            // void ReceiveMidiNote(int chnd, int note_num, double volume)

            // /// Callback from script: function receive_midi_controller(chan_hnd, controller, value)
            // void ReceiveMidiController(int chnd, int controller_id, int value)
        }
    }


    /// <summary>Hook used to capture events from test target.</summary>
    public class EventCollector
    {
        public List<EventArgs> Events { get; } = [];
        public List<string> Strings { get; } = [];
        public bool AutoRet { get; set; } = false;
        public int Ret { get; set; } = 0;
        public void Reset()
        {
            Events.Clear();
            Strings.Clear();
            AutoRet = false;
            Ret = 0;
        }

        public EventCollector()
        {
            // Hook script callbacks.
            Interop.Log += CollectEvent;
            Interop.OpenMidiInput += CollectEvent;
            Interop.OpenMidiOutput += CollectEvent;
            Interop.SendMidiNote += CollectEvent;
            Interop.SendMidiController += CollectEvent;
            Interop.SetTempo += CollectEvent;
        }

        public void CollectEvent(object? sender, EventArgs e)
        {
            // Fix up the event value.
            if (AutoRet) Ret++;

            switch (e)
            {
                case LogArgs le: le.ret = Ret; break;
                case OpenMidiInputArgs le: le.ret = Ret; break;
                case OpenMidiOutputArgs le: le.ret = Ret; break;
                case SendMidiNoteArgs le: le.ret = Ret; break;
                case SendMidiControllerArgs le: le.ret = Ret; break;
                case SetTempoArgs le: le.ret = Ret; break;
                default: break; // handle?
            }

            Events.Add(e);
            Strings.Add(Format(e));
        }

        public string Format(EventArgs e)
        {
            return e switch
            {
                LogArgs le => $"Log() level:{le.level} msg:{le.msg} ret:{le.ret}",
                OpenMidiInputArgs ie => $"OpenMidiInput() dev_name:{ie.dev_name} chan_num:{ie.chan_num} ret:{ie.ret}",
                OpenMidiOutputArgs oe => $"OpenMidiOutput() dev_name:{oe.dev_name} chan_num: {oe.chan_num} patch:{oe.patch} ret:{oe.ret}",
                SendMidiNoteArgs ne => $"SendMidiNote() chan_hnd:{ne.chan_hnd} note_num:{ne.note_num} volume:{ne.volume} ret:{ne.ret}",
                SendMidiControllerArgs ce => $"SendMidiController() chan_hnd:{ce.chan_hnd} controller:{ce.controller} value:{ce.value} ret:{ce.ret}",
                SetTempoArgs te => $"SetTempo() bpm:{te.bpm} ret:{te.ret}",
                _ => throw new Exception("???"),
            };
        }
    }
}
