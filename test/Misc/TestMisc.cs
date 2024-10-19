using System;
using System.Text;
using System.Collections.Generic;
using System.IO;
using Ephemera.NBagOfTricks.PNUT;
using Nebulua;
using System.Runtime.CompilerServices;
using System.Data;


namespace Nebulua.Test
{
    /// <summary>Odds and ends that have no other home.</summary>
    public class MISC_COMMON : TestSuite
    {
        public override void RunSuite()
        {
            int bt = MusicTime.Parse("23:2:6");
            UT_EQUAL(bt, 23 * MusicTime.SUBS_PER_BAR + 2 * MusicTime.SUBS_PER_BEAT + 6);
            bt = MusicTime.Parse("146:1");
            UT_EQUAL(bt, 146 * MusicTime.SUBS_PER_BAR + 1 * MusicTime.SUBS_PER_BEAT);
            bt = MusicTime.Parse("71");
            UT_EQUAL(bt, 71 * MusicTime.SUBS_PER_BAR);
            bt = MusicTime.Parse("49:55:8");
            UT_EQUAL(bt, -1);
            bt = MusicTime.Parse("111:3:88");
            UT_EQUAL(bt, -1);
            bt = MusicTime.Parse("invalid");
            UT_EQUAL(bt, -1);
            string sbt = MusicTime.Format(12345);
            UT_EQUAL(sbt, "385:3:1");

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
                var ex = new ApiException("message111", "Bad API");
                var (fatal, msg) = Utils.ProcessException(ex);
                UT_FALSE(fatal);
                UT_STRING_CONTAINS(msg, "message111");
                UT_STRING_CONTAINS(msg, "Bad API");
            }

            {
                var ex = new ScriptSyntaxException("message222");
                var (fatal, msg) = Utils.ProcessException(ex);
                UT_FALSE(fatal);
                UT_STRING_CONTAINS(msg, "Script Syntax Error: message222");
            }

            {
                var ex = new ApplicationArgumentException("message333");
                var (fatal, msg) = Utils.ProcessException(ex);
                UT_TRUE(fatal);
                UT_STRING_CONTAINS(msg, "Application Argument Error: message333");
            }

            {
                var ex = new DuplicateNameException("message444");
                var (fatal, msg) = Utils.ProcessException(ex);
                UT_TRUE(fatal);
                UT_STRING_CONTAINS(msg, "System.Data.DuplicateNameException: message444");
            }
        }
    }


    public class Verify
    {
        //https://andrewlock.net/exploring-dotnet-6-part-11-callerargumentexpression-and-throw-helpers/
        public static void IsTrue(bool value, [CallerArgumentExpression("value")] string expression = "")
        {
            if (!value)
            {
                Throw(expression);
            }
        }

        private static void Throw(string expression) => throw new ArgumentException($"{expression} must be True, but was False");
    }

    public class CallerStuff
    {
        public static void DoThis([CallerFilePath] string file = "", [CallerLineNumber] int line = 0, [CallerMemberName] string member = "")
        {
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
