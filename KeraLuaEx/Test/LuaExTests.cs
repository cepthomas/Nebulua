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
            _lMain.Register("print", _funcPrint);
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
            //TestCommon.ExecuteLuaFile(_lMain, "interop");

            using Lua l = new();

            string srcPath = Lua.GetSourcePath();
            string scriptsPath = Path.Combine(srcPath, "scripts");
            l.SetLuaPath(new() { scriptsPath });
            string scriptFile = Path.Combine(scriptsPath, "luaex.lua");
            LuaStatus lstat = l.LoadFile(scriptFile);
            Assert.AreEqual(LuaStatus.OK, lstat);
            lstat = l.PCall(0, -1, 0);

            var ls = l.DumpStack();
            Debug.WriteLine(FormatDump("Stack", ls, true));

            //ls = l.DumpGlobals();
            //Debug.WriteLine(FormatDump("Globals", ls, true));

            //ls = l.DumpStack();
            //Debug.WriteLine(FormatDump("Stack", ls, true));

            //ls = l.DumpTable("_G");
            //Debug.WriteLine(FormatDump("_G", ls, true));

            ls = l.DumpTable("g_table");
            Debug.WriteLine(FormatDump("g_table", ls, true));

            ls = l.DumpTraceback();
            Debug.WriteLine(FormatDump("Traceback", ls, true));

            var x = l.GetGlobalValue("g_number");
            Assert.AreEqual(typeof(double), x.type);

            x = l.GetGlobalValue("g_int");
            Assert.AreEqual(typeof(int), x.type);

            ls = l.DumpTable("things");
            Debug.WriteLine(FormatDump("things", ls, true));

            //x = l.GetGlobalValue("g_table");
            //Assert.AreEqual(typeof(int), x.type);

            //x = l.GetGlobalValue("g_list");
            //Assert.AreEqual(typeof(int), x.type);


            ///// json stuff TODOA
            x = l.GetGlobalValue("things_json");//TODOA
            Assert.AreEqual(typeof(string), x.type);
            var jdoc = JsonDocument.Parse(x.val.ToString());
            var jrdr = new Utf8JsonReader();
            //{
            //  TUNE = { type = "midi_in", channel = 1,  },
            //  TRIG = { type = "virt_key", channel = 2, adouble = 1.234 },
            //  WHIZ = { type = "bing_bong", channel = 10, abool = true }
            //}
            // >>>>>>>
            //{
            //    "TRIG": {
            //        "channel": 2,
            //        "type": "virt_key",
            //        "adouble": 1.234
            //    },
            //    "WHIZ": {
            //        "channel": 10,
            //        "abool": true,
            //        "type": "bing_bong"
            //    },
            //    "TUNE": {
            //        "type": "midi_in",
            //        "channel": 1
            //    }
            //}


            ///// Execute a lua function.
            LuaType gtype = l.GetGlobal("g_func");
            Assert.AreEqual(LuaType.Function, gtype);
            // Push the arguments to the call.
            l.PushString("az9011 birdie");
            // Do the actual call.
            lstat = l.PCall(1, 1, 0);
            Assert.AreEqual(LuaStatus.OK, lstat);
            // Get result.
            int res = (int)l.ToInteger(-1);
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
            var l = Lua.FromIntPtr(p);
            Debug.WriteLine($"print:{l.ToString(-1)}");
            return 0;
        }
    }
}
