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
        var exePath = Environment.CurrentDirectory; // where exe lives
        var codePath = $@"{exePath}\lua_code"; // copied lua files
        var lbotPath = Environment.GetEnvironmentVariable("LBOT"); // TODO1 copy/submodule? fix dependency for runtime? needs utils.lua, stringex.lua, validators.lua. 
        // Environment.SetEnvironmentVariable("LUA_PATH", codePath);

        var app = new Nebulua.App([lbotPath, codePath]);
        app.Run(fn);
    }
}
