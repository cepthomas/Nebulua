using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using Ephemera.NBagOfTricks.PNUT;


namespace Nebulua.Test
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main(string[] _)
        {
            // Run pnut tests from cmd line.
            TestRunner runner = new(OutputFormat.Readable);
            //var cases = new[] { "MISC" };
            var cases = new[] { "MISC", "INTOP", "APP", "CLI" };

            // Init system before running tests.
            // FILE* fp_log = fopen("_log.txt", "w");
            // logger_Init(fp_log);

            runner.RunSuites(cases);
            File.WriteAllLines(@"_test.txt", runner.Context.OutputLines);

            // fclose(fp_log);
        }
    }
}
