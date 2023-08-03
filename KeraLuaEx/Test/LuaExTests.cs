using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Xml.Linq;
using System.Drawing;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.Json.Serialization;
using NUnit.Framework;
using KeraLuaEx;


namespace KeraLuaEx.Test
{
    // Test my new stuff.
    [TestFixture]
    public class LuaExTests
    {
        Lua? _lMain;
        static readonly LuaFunction _funcPrint = Print;

        [SetUp]
        public void Setup()
        {
            _lMain?.Close();
            _lMain = new Lua();
            _lMain.Register("printex", _funcPrint);
        }

        [TearDown]
        public void TearDown()
        {
            _lMain?.Close();
            _lMain = null;
        }

        [Test]
        public void TestABC()
        {
            string srcPath = Utils.GetSourcePath();
            string scriptsPath = Path.Combine(srcPath, "scripts");
            Utils.SetLuaPath(_lMain!, new() { scriptsPath });
            string scriptFile = Path.Combine(scriptsPath, "luaex.lua");
            LuaStatus lstat = _lMain!.LoadFile(scriptFile);
            Assert.AreEqual(LuaStatus.OK, lstat);
            lstat = _lMain.PCall(0, -1, 0);
            Assert.AreEqual(LuaStatus.OK, lstat);

            var s = Utils.DumpStack(_lMain);
            Debug.WriteLine(s);
            //Debug.WriteLine(FormatDump("Stack", ls, true));

            //ls = Utils.DumpGlobals(_lMain);
            //Debug.WriteLine(FormatDump("Globals", ls, true));

            //ls = Utils.DumpStack(_lMain);
            //Debug.WriteLine(FormatDump("Stack", ls, true));

            //ls = Utils.DumpTable(_lMain, "_G");
            //Debug.WriteLine(FormatDump("_G", ls, true));

            //ls = Utils.DumpGlobalTable(_lMain, "g_table");
            //Debug.WriteLine(FormatDump("g_table", ls, true));

            //ls = Utils.DumpTraceback(_lMain);
            //Debug.WriteLine(FormatDump("Traceback", ls, true));

            //var x = Utils.GetGlobalValue(_lMain, "g_number");
            //Assert.AreEqual(typeof(double), x.type);

            //x = Utils.GetGlobalValue(_lMain, "g_int");
            //Assert.AreEqual(typeof(long), x.type);

            //ls = Utils.DumpGlobalTable(_lMain, "things");
            //Debug.WriteLine(FormatDump("things", ls, true));

            //x = Utils.GetGlobalValue(_lMain, "g_table");
            //Assert.AreEqual(typeof(long), x.type);

            //x = Utils.GetGlobalValue(_lMain, "g_list");
            //Assert.AreEqual(typeof(long), x.type);

            ///// Execute a lua function.
            LuaType gtype = _lMain.GetGlobal("g_func");
            Assert.AreEqual(LuaType.Function, gtype);
            // Push the arguments to the call.
            _lMain.PushString("az9011 birdie");
            // Do the actual call.
            lstat = _lMain.PCall(1, 1, 0);
            Assert.AreEqual(LuaStatus.OK, lstat);
            // Get result.
            var res = _lMain.ToInteger(-1)!;
            Assert.AreEqual(13, res);
        }

        string FormatDump(string name, List<string> lsin, bool indent)
        {
            string sindent = indent ? "    " : "";
            var lines = new List<string> { $"{name}:" };
            lsin.ForEach(s => lines.Add($"{sindent}{s}"));
            var s = string.Join(Environment.NewLine, lines);
            return s;
        }

        static int Print(IntPtr p)
        {
            var l = Lua.FromIntPtr(p)!;
            Debug.WriteLine($"print:{l.ToString(-1)}");
            return 0;
        }
    }
}
