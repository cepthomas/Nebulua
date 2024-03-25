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

        // TODO1 best way to handle LUA_PATH? other than cmd file? see below.
        //  <PropertyGroup Condition="'$(Configuration)|$(Platform)'=='Debug|x64'">
        //    <LocalDebuggerEnvironment>LUA_PATH=;;C:\Dev\repos\Lua\Nebulua\lua_code\?.lua;C:\Dev\repos\Lua\Nebulua\test\?.lua;C:\Dev\repos\Lua\LuaBagOfTricks\?.lua;$(LocalDebuggerEnvironment)</LocalDebuggerEnvironment>
        //    <DebuggerFlavor>WindowsLocalDebugger</DebuggerFlavor>
        //    <LocalDebuggerCommandArguments>"C:\Dev\repos\Lua\Nebulua\test\script_happy.lua"</LocalDebuggerCommandArguments>
        //    <LocalDebuggerDebuggerType>NativeWithManagedCore</LocalDebuggerDebuggerType>
        //  </PropertyGroup>



        var app = new Nebulua.App();

        app.Run(fn);
    }
}
