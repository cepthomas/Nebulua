using System;
using System.IO;
using Ephemera.NBagOfTricks.PNUT;


namespace Ephemera.Nebulua.ScriptLua.Test
{
    internal class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Hello World!");

            // Run pnut tests from cmd line.
            TestRunner runner = new(OutputFormat.Readable);
            var cases = new[] { "UTILS_SETTINGS" };
            //var cases = new[] { "PNUT", "UTILS", "CMD", "MMTEX", "IPC", "TOOLS", "JSON", "SLOG" };
            runner.RunSuites(cases);
            File.WriteAllLines(@"..\..\out\test_out.txt", runner.Context.OutputLines);
        }
    }
}
