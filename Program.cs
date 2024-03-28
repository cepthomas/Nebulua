using System;
using System.IO;
using System.Linq;

internal class Program
{
    private static void Main(string[] args)
    {
        Console.WriteLine("Hello, World!");

        // Check args.
        if (args.Length != 1)
        {
            Console.WriteLine("Bad cmd line. Use nebulua <file.lua>.");
            Environment.Exit(100);
        }

        string fn = args[0];

        if (!File.Exists(fn))
        {
            Console.WriteLine("Bad lua file name.");
            Environment.Exit(101);
        }

        // Set up runtime lua environment.
        var spath = Environment.CurrentDirectory; // where exe lives
        var lpath = $@";;{spath}\lua_code\?.lua;"; // copied lua files
        Environment.SetEnvironmentVariable("LUA_PATH", lpath);

        var app = new Nebulua.App();
        app.Run(fn);
    }
}
