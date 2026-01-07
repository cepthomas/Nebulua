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


    #region Script api functions TODO1

    // /// api.send_midi_note(hnd_strings, note_num, volume)
    // void SendMidiNote(int chnd, int note_num, double volume)
    // {
    //     if (note_num is < 0 or > MidiDefs.MAX_MIDI) { throw new ArgumentOutOfRangeException(note_num); }

    //     var ch = _mgr.GetOutputChannel(chnd);

    //     if (ch is not null)
    //     {
    //         BaseMidiEvent evt = volume == 0.0 ?
    //             new NoteOff(ChannelHandle.ChannelNumber(chnd), note_num) :
    //             new NoteOn(ChannelHandle.ChannelNumber(chnd), note_num, (int)MathUtils.Constrain(volume * MidiDefs.MAX_MIDI, 0, MidiDefs.MAX_MIDI));
    //         ch.Device.Send(evt);
    //     }
    //     else
    //     {
    //         // error?
    //     }
    // }

    // /// api.send_midi_controller(hnd_synth, ctrl.Pan, 90)
    // void SendMidiController(int chnd, int controller_id, int value)
    // {
    //     if (controller_id is < 0 or > MidiDefs.MAX_MIDI) { throw new ArgumentOutOfRangeException((controller_id)); }
    //     if (value is < 0 or > MidiDefs.MAX_MIDI) { throw new ArgumentOutOfRangeException((value)); }
    //     var ch = _mgr.GetOutputChannel(chnd);
    //     if (ch is not null)
    //     {
    //         BaseMidiEvent evt = new Controller(ChannelHandle.ChannelNumber(chnd), controller_id, value);
    //         ch.Device.Send(evt);
    //     }
    //     else
    //     {
    //         // error?
    //     }
    // }

    // /// Callback from script: function receive_midi_note(chan_hnd, note_num, volume)
    // void ReceiveMidiNote(int chnd, int note_num, double volume)
    // {
    // }

    // /// Callback from script: function receive_midi_controller(chan_hnd, controller, value)
    // void ReceiveMidiController(int chnd, int controller_id, int value)
    // {
    // }
    #endregion


    
}
