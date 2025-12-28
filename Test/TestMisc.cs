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
    // /// <summary>Odds and ends that have no other home.</summary>
    // public class MISC_COMMON : TestSuite
    // {
    //     public override void RunSuite()
    //     {
    //         int bt = MusicTime.Parse("23.2.6");
    //         UT_EQUAL(bt, 23 * MusicTime.SUBS_PER_BAR + 2 * MusicTime.SUBS_PER_BEAT + 6);
    //         bt = MusicTime.Parse("146.1");
    //         UT_EQUAL(bt, 146 * MusicTime.SUBS_PER_BAR + 1 * MusicTime.SUBS_PER_BEAT);
    //         bt = MusicTime.Parse("71");
    //         UT_EQUAL(bt, 71 * MusicTime.SUBS_PER_BAR);
    //         bt = MusicTime.Parse("49.55.8");
    //         UT_EQUAL(bt, -1);
    //         bt = MusicTime.Parse("111.3.88");
    //         UT_EQUAL(bt, -1);
    //         bt = MusicTime.Parse("invalid");
    //         UT_EQUAL(bt, -1);
    //         string sbt = MusicTime.Format(12345);
    //         UT_EQUAL(sbt, "385.3.1");

    //         //string smidi = Common.FormatMidiStatus(MMSYSERR_INVALFLAG);
    //         //UT_STR_EQUAL(smidi, "An invalid flag was passed to a system function.");

    //         //smidi = Common.FormatMidiStatus(90909);
    //         //UT_STR_EQUAL(smidi, "MidiStatus:90909");
    //     }
    // }

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
    //     if (note_num is < 0 or > MidiDefs.MAX_MIDI) { throw new ArgumentOutOfRangeException(nameof(note_num)); }

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
    //     if (controller_id is < 0 or > MidiDefs.MAX_MIDI) { throw new ArgumentOutOfRangeException(nameof(controller_id)); }
    //     if (value is < 0 or > MidiDefs.MAX_MIDI) { throw new ArgumentOutOfRangeException(nameof(value)); }
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
