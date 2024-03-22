using System;
using System.Text;
using System.Collections.Generic;
using System.IO;
using Ephemera.NBagOfTricks.PNUT;


namespace Nebulua.Test.Interop
{
    public class INTOP_ONE : TestSuite
    {
        public override void RunSuite()
        {
            int int1 = 321;
            //string str1 = "round and round";
            string str2 = "the mulberry bush";
            double dbl2 = 1.600;

            UT_INFO("Test UT_INFO with args", int1, dbl2);
            UT_EQUAL(str2, "the mulberry bush");
        }
    }

    static class Program
    {
        [STAThread]
        static void Main(string[] _)
        {
            TestRunner runner = new(OutputFormat.Readable);
            var cases = new[] { "INTOP" };
            runner.RunSuites(cases);
            File.WriteAllLines(@"_test.txt", runner.Context.OutputLines);
        }
    }
}
