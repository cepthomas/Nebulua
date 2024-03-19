using System;
using System.Text;
using System.Collections.Generic;
using System.IO;
using Ephemera.NBagOfTricks.PNUT;


namespace Nebulua.Test
{
    public class NEBCOM_MAIN : TestSuite
    {
        public override void RunSuite()
        {
            int bt = Utils.ParseBarTime("23:2:6");
            UT_EQUAL(bt, 23 * Defs.SUBS_PER_BAR + 2 * Defs.SUBS_PER_BEAT + 6);
            bt = Utils.ParseBarTime("146:1");
            UT_EQUAL(bt, 146 * Defs.SUBS_PER_BAR + 1 * Defs.SUBS_PER_BEAT);
            bt = Utils.ParseBarTime("71");
            UT_EQUAL(bt, 71 * Defs.SUBS_PER_BAR);
            bt = Utils.ParseBarTime("49:55:8");
            UT_EQUAL(bt, -1);
            bt = Utils.ParseBarTime("111:3:88");
            UT_EQUAL(bt, -1);
            bt = Utils.ParseBarTime("invalid");
            UT_EQUAL(bt, -1);
            string sbt = Utils.FormatBarTime(12345);
            UT_EQUAL(sbt, "385:3:1");

            //string smidi = Utils.FormatMidiStatus(MMSYSERR_INVALFLAG);
            //UT_STR_EQUAL(smidi, "An invalid flag was passed to a system function.");

            //smidi = Utils.FormatMidiStatus(90909);
            //UT_STR_EQUAL(smidi, "MidiStatus:90909");
        }
    }
}
