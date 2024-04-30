using System;
using System.Text;
using System.Collections.Generic;
using System.IO;
using Ephemera.NBagOfTricks.PNUT;
using Nebulua.Common;


namespace Nebulua.Test
{
    /// <summary>Odds and ends that have no other home.</summary>
    public class MISC_COMMON : TestSuite
    {
        public override void RunSuite()
        {
            int bt = MusicTime.Parse("23:2:6");
            UT_EQUAL(bt, 23 * Defs.SUBS_PER_BAR + 2 * Defs.SUBS_PER_BEAT + 6);
            MusicTime.Parse("146:1");
            UT_EQUAL(bt, 146 * Defs.SUBS_PER_BAR + 1 * Defs.SUBS_PER_BEAT);
            MusicTime.Parse("71");
            UT_EQUAL(bt, 71 * Defs.SUBS_PER_BAR);
            MusicTime.Parse("49:55:8");
            UT_EQUAL(bt, -1);
            MusicTime.Parse("111:3:88");
            UT_EQUAL(bt, -1);
            MusicTime.Parse("invalid");
            UT_EQUAL(bt, -1);
            string sbt = MusicTime.Format(12345);
            UT_EQUAL(sbt, "385:3:1");

            //string smidi = Utils.FormatMidiStatus(MMSYSERR_INVALFLAG);
            //UT_STR_EQUAL(smidi, "An invalid flag was passed to a system function.");

            //smidi = Utils.FormatMidiStatus(90909);
            //UT_STR_EQUAL(smidi, "MidiStatus:90909");
        }
    }

    /// <summary>Test entry.</summary>
    static class Program
    {
        [STAThread]
        static void Main(string[] _)
        {
            TestRunner runner = new(OutputFormat.Readable);
            var cases = new[] { "MISC" };
            runner.RunSuites(cases);
            File.WriteAllLines(@"_test.txt", runner.Context.OutputLines);
        }
    }
}
