using System;
using System.Text;
using System.Collections.Generic;
using System.IO;
using System.Drawing;
using Ephemera.NBagOfTricks;
using Ephemera.NBagOfTricks.PNUT;
//using Nebulua;


namespace Test // TODO useful?
{
    /// <summary>Test entry.</summary>
    static class Program
    {
        [STAThread]
        static void Main(string[] _)
        {
            TestRunner runner = new(OutputFormat.Readable);
            // "INTEROP"
            var torun = new[] { "INTEROP" };
            runner.RunSuites(torun);
            File.WriteAllLines(@"_test.txt", runner.Context.OutputLines);
        }
    }
}
