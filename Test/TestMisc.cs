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
    public class MISC_COMMON : TestSuite
    {
        public override void RunSuite()
        {
            int bt = MusicTime.Parse("23.2.6");
            UT_EQUAL(bt, 23 * MusicTime.SUBS_PER_BAR + 2 * MusicTime.SUBS_PER_BEAT + 6);
            bt = MusicTime.Parse("146.1");
            UT_EQUAL(bt, 146 * MusicTime.SUBS_PER_BAR + 1 * MusicTime.SUBS_PER_BEAT);
            bt = MusicTime.Parse("71");
            UT_EQUAL(bt, 71 * MusicTime.SUBS_PER_BAR);
            bt = MusicTime.Parse("49.55.8");
            UT_EQUAL(bt, -1);
            bt = MusicTime.Parse("111.3.88");
            UT_EQUAL(bt, -1);
            bt = MusicTime.Parse("invalid");
            UT_EQUAL(bt, -1);
            string sbt = MusicTime.Format(12345);
            UT_EQUAL(sbt, "385.3.1");

            //string smidi = Utils.FormatMidiStatus(MMSYSERR_INVALFLAG);
            //UT_STR_EQUAL(smidi, "An invalid flag was passed to a system function.");

            //smidi = Utils.FormatMidiStatus(90909);
            //UT_STR_EQUAL(smidi, "MidiStatus:90909");
        }
    }

    /// <summary>Odds and ends that have no other home.</summary>
    public class MISC_EXCEPTIONS : TestSuite
    {
        public override void RunSuite()
        {
            {
                var ex = new LuaException("message111");
                var (fatal, msg) = Utils.ProcessException(ex);
                UT_FALSE(fatal);
                UT_EQUAL(msg, "Lua/Interop Error: message111");
            }

            {
                var ex = new SyntaxException("message222");
                var (fatal, msg) = Utils.ProcessException(ex);
                UT_FALSE(fatal);
                UT_EQUAL(msg, "Script Syntax Error: message222");
            }

            {
                var ex = new ArgumentException("message333");
                var (fatal, msg) = Utils.ProcessException(ex);
                UT_FALSE(fatal);
                UT_EQUAL(msg, "Argument Error: message333");
            }

            {
                var ex = new DuplicateNameException("message444");
                var (fatal, msg) = Utils.ProcessException(ex);
                UT_TRUE(fatal);
                UT_EQUAL(msg, "System.Data.DuplicateNameException: message444");
            }
        }
    }
}
