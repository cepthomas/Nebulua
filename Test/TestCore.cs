using System;
using System.Text;
using System.Collections.Generic;
using System.IO;
using Ephemera.NBagOfTricks;
using Ephemera.NBagOfTricks.PNUT;
using Nebulua;

// TODOF needs more tests.

namespace Test
{
    /// <summary>All success operations.</summary>
    public class CORE_HAPPY : TestSuite
    {
        public override void RunSuite()
        {
            UT_STOP_ON_FAIL(true);

            //////// some of HostCore api /////////////
            // public HostCore()
            // public void Dispose()
            // public void LoadScript(string? scriptFn = null)
            // public void InjectReceiveEvent(string devName, int channel, int noteNum, int velocity)
            // public void KillAll()

            // Internals:
            // void State_ValueChangeEvent(object? _, string name)
            // void MmTimer_Callback(double totalElapsed, double periodElapsed)
            // void Midi_ReceiveEvent(object? sender, MidiEvent e)
            // void Interop_OpenMidiInput(object? _, OpenMidiInputArgs e)
            // void Interop_OpenMidiOutput(object? _, OpenMidiOutputArgs e)
            // void Interop_SendMidiNote(object? _, SendMidiNoteArgs e)
            // void Interop_send_midi_controller(object? _, send_midi_controllerArgs e)
            // void Interop_SetTempo(object? _, SetTempoArgs e)
            // void Interop_Log(object? _, LogArgs e)
            // void ResetIo()
            // void SetTimer(int tempo)
            // void CallbackError(Exception ex)
            // string FormatMidiEvent(MidiEvent evt, int tick, int chan_hnd)
            // int MakeOutHandle(int index, int chan_num)
            // int MakeInHandle(int index, int chan_num)
            // (int index, int chan_num) DeconstructHandle(int chan_hnd)


            EventCollector ecoll = new();

            // Load the script. 
            var scriptFn = Path.Join(MiscUtils.GetSourcePath(), "lua", "script_happy.lua");
            HostCore hostCore = new();
            hostCore.LoadScript(scriptFn); // may throw

            UT_NOT_NULL(hostCore);

            //// Have a look inside.
            //UT_EQUAL(interop.SectionInfo.Count, 4);
            //// Fake valid loaded script.
            //State.Instance.InitSectionInfo(interop.SectionInfo);

            UT_EQUAL(ecoll.Events.Count, 7);
        }
    }
}
