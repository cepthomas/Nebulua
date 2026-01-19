using System;
using System.Text;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using System.Data;
using Ephemera.NBagOfTricks.PNUT;
using Nebulua;


namespace Test
{
    /// <summary>Odds and ends that have no other home.</summary>
    public class MISC_EXCEPTIONS : TestSuite
    {
        public override void RunSuite()
        {
            {
                var ex = new LuaException(LUA_DEBUG, "message111");
                var (fatal, msg) = Utils.ProcessException(ex);
                UT_FALSE(fatal);
                UT_EQUAL(msg, "Lua/Interop Error: message111");
            }

            {
                var ex = new AppException("message333");
                var (fatal, msg) = Utils.ProcessException(ex);
                UT_FALSE(fatal);
                UT_EQUAL(msg, "App Error: message333");
            }

            {
                var ex = new DuplicateNameException("message444");
                var (fatal, msg) = Utils.ProcessException(ex);
                UT_TRUE(fatal);
                UT_EQUAL(msg, "System.Data.DuplicateNameException: message444");
            }
        }
    }


    #region Script api functions ????

    // /// api.send_midi_note(hnd_strings, note_num, volume)
    // void SendMidiNote(int chnd, int note_num, double volume)

    // /// api.send_midi_controller(hnd_synth, ctrl.Pan, 90)
    // void SendMidiController(int chnd, int controller_id, int value)

    // /// Callback from script: function receive_midi_note(chan_hnd, note_num, volume)
    // void ReceiveMidiNote(int chnd, int note_num, double volume)

    // /// Callback from script: function receive_midi_controller(chan_hnd, controller, value)
    // void ReceiveMidiController(int chnd, int controller_id, int value)
    #endregion
}
