using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Xml.Linq;
using System.Drawing;
using NUnit.Framework;
using KeraLuaEx;


namespace KeraLuaEx.Test
{
    #region Helpers etc.
    class TestCommon
    {
        public static void ExecuteLuaFile(Lua l, string name)
        {
            string path = Path.Combine("Test", "scripts", $"{name}.lua");
            LuaStatus result = l.LoadFile(path);
            Assert.AreEqual(result, LuaStatus.OK, l.ToString(1));
            result = l.PCall(0, -1, 0);
            Assert.AreEqual(result, LuaStatus.OK, l.ToString(1));
        }

        public static int Print(IntPtr p)
        {
            var l = Lua.FromIntPtr(p);
            Console.WriteLine(l.DumpStack());
            return 0;
        }
    }
    #endregion

    #region This is copied from the KeraLua Core.cs tests as regression test.
    [TestFixture]
    public class TestCore
    {
        Lua _lMain;
        static LuaFunction _func_print = TestCommon.Print;

        [SetUp]
        public void Setup()
        {
            _lMain = new Lua();
            _lMain.Register("print", _func_print);
        }

        [TearDown]
        public void TearDown()
        {
            _lMain.Close();
            _lMain = null;
        }

        [Test]
        public void Bisect()
        {
            TestCommon.ExecuteLuaFile(_lMain, "bisect");
        }

        [Test]
        public void CF()
        {
            TestCommon.ExecuteLuaFile(_lMain, "cf");
        }


        [Test]
        public void Factorial()
        {
            TestCommon.ExecuteLuaFile(_lMain, "factorial");
        }

        [Test]
        public void FibFor()
        {
            TestCommon.ExecuteLuaFile(_lMain, "fibfor");
        }

        [Test]
        public void Fib()
        {
            TestCommon.ExecuteLuaFile(_lMain, "fib");
        }

        [Test]
        public void Life()
        {
            TestCommon.ExecuteLuaFile(_lMain, "life");
        }

        [Test]
        public void Printf()
        {
            TestCommon.ExecuteLuaFile(_lMain, "printf");
        }


        [Test]
        public void Sieve()
        {
            TestCommon.ExecuteLuaFile(_lMain, "sieve");
        }

        [Test]
        public void Sort()
        {
            TestCommon.ExecuteLuaFile(_lMain, "sort");
        }
    }
    #endregion


    #region This is copied from the KeraLua Interop.cs tests as regression test.
    [TestFixture]
    public class Interop
    {
        private static LuaHookFunction funcHookCallback = HookCallback;

        private static StringBuilder hookLog;

        [SetUp]
        public void SetUp()
        {
            string path = new Uri(GetType().Assembly.Location).AbsolutePath;
            path = Path.GetDirectoryName(path);
            Environment.CurrentDirectory = path;
        }

        #region PushCFunction
        public static readonly char UnicodeChar = '\uE007';
        public static string UnicodeString => Convert.ToString(UnicodeChar, System.Globalization.CultureInfo.InvariantCulture);
        internal static LuaFunction funcTestUnicodeString = TestUnicodeString;
        internal static LuaFunction funcTestReferenceData = TestReferenceData;
        internal static LuaFunction funcTestValueData = TestValueData;
        private static XDocument gTempDocument;

        [Test]
        public void TestUnicodeString()
        {
            var l = new Lua { Encoding = Encoding.UTF8 };

            l.PushCFunction(funcTestUnicodeString);
            l.SetGlobal("TestUnicodeString");
            l.PushString(UnicodeString);
            l.SetGlobal("unicodeString");
            AssertString("TestUnicodeString(unicodeString)", l);
        }
        private static int TestUnicodeString(IntPtr p)
        {
            var l = Lua.FromIntPtr(p);
            string param = l.ToString(1, false);

            Assert.AreEqual(UnicodeString, param, "#1 ToString()");

            return 0;
        }

        [Test]
        public void TestPushReferenceData()
        {
            var document = XDocument.Parse(@"<users>
                                                    <user name=""John Doe"" age=""42"" />
                                                    <user name=""Jane Doe"" age=""39"" />
                                                  </users>");
            var l = new Lua();

            l.PushObject<XDocument>(null);
            l.SetGlobal("foo");

            l.PushObject(document);
            l.SetGlobal("bar");

            l.PushCFunction(funcTestReferenceData);
            l.SetGlobal("TestReferenceData");

            gTempDocument = document;
            AssertString("TestReferenceData(foo, bar)", l);
        }
        private static int TestReferenceData(IntPtr p)
        {
            var l = Lua.FromIntPtr(p);

            XDocument param1 = l.ToObject<XDocument>(1);
            XDocument param2 = l.ToObject<XDocument>(2);

            Assert.IsNull(param1, "#1");
            Assert.AreEqual(param2, gTempDocument, "#2");

            return 0;
        }

        [Test]
        public void TestPushValueData()
        {
            var l = new Lua();

            l.PushObject<Rectangle?>(null);
            l.SetGlobal("foo");

            l.PushObject(new Rectangle(10, 10, 100, 100));
            l.SetGlobal("bar");

            l.PushObject(new DateTime(2018, 10, 10, 0, 0, 0));
            l.SetGlobal("date");

            l.PushCFunction(funcTestValueData);
            l.SetGlobal("TestValueData");

            AssertString("TestValueData(foo, bar, date)", l);
        }
        private static int TestValueData(IntPtr p)
        {
            var l = Lua.FromIntPtr(p);

            Rectangle? param1 = l.ToObject<Rectangle?>(1);
            Rectangle param2 = l.ToObject<Rectangle>(2);
            DateTime param3 = l.ToObject<DateTime>(3);

            Assert.IsNull(param1, "#1");
            Assert.AreEqual(param2, new Rectangle(10, 10, 100, 100), "#2");
            Assert.AreEqual(param3, new DateTime(2018, 10, 10, 0, 0, 0), "#3");

            return 0;
        }
        #endregion

        private static void AssertString(string chunk, Lua l)
        {
            string error = string.Empty;

            LuaStatus result = l.LoadString(chunk);

            if (result != LuaStatus.OK)
                error = l.ToString(1, false);

            Assert.True(result == LuaStatus.OK, "Fail loading string: " + chunk + "ERROR:" + error);

            result = l.PCall(0, -1, 0);

            if (result != 0)
                error = l.ToString(1, false);

            Assert.True(result == 0, "Fail calling chunk: " + chunk + " ERROR: " + error);
        }

        #region Debug hooks
        [Test]
        public void TestLuaHook()
        {
            var l = new Lua();
            hookLog = new StringBuilder();
            l.SetHook(funcHookCallback, LuaHookMask.Line, 0);
            l.DoFile("main.l");
            string output = hookLog.ToString();
            string expected =
@"main.l-main.l:2 (main)
foo.l-foo.l:2 (main)
module1.l-module1.l:3 (main)
module1.l-module1.l:9 (main)
module1.l-module1.l:5 (main)
module1.l-module1.l:11 (main)
foo.l-foo.l:8 (main)
foo.l-foo.l:4 (main)
foo.l-foo.l:14 (main)
foo.l-foo.l:10 (main)
foo.l-foo.l:14 (main)
main.l-main.l:4 (main)
main.l-main.l:5 (main)
main.l-main.l:7 (main)
main.l-main.l:8 (main)
foo.l-foo.l:5 (Lua)
foo.l-foo.l:6 (Lua)
foo.l-foo.l:7 (Lua)
module1.l-module1.l:6 (Lua)
module1.l-module1.l:7 (Lua)
module1.l-module1.l:8 (Lua)
foo.l-foo.l:8 (Lua)
main.l-main.l:11 (main)
";
            expected = expected.Replace("\r", "");
            expected = expected.Replace('/', Path.DirectorySeparatorChar);
            output = output.Replace("\r", "");
            Assert.AreEqual(expected, output, "#1");
            Assert.IsNotNull(l.Hook);
        }
        private static void HookCallback(IntPtr p, IntPtr ar)
        {
            var l = Lua.FromIntPtr(p);
            var debug = LuaDebug.FromIntPtr(ar);

            Assert.NotNull(l, "#l shouldn't be null");
            Assert.NotNull(debug, "#debug shouldn't be null");

            if (debug.Event != LuaHookEvent.Line)
                return;

            if (!l.GetInfo("Snlu", ar))
                return;

            debug = LuaDebug.FromIntPtr(ar);

            string source = debug.Source.Substring(1);
            string shortSource = Path.GetFileName(debug.ShortSource);

            source = Path.GetFileName(source);
            hookLog.AppendLine($"{shortSource}-{source}:{debug.CurrentLine} ({debug.What})");
        }


        [Test]
        public void TestLuaHookStruct()
        {
            funcHookCallback = HookCalbackStruct;
            var l = new Lua();
            hookLog = new StringBuilder();

            l.SetHook(funcHookCallback, LuaHookMask.Line, 0);

            Assert.AreEqual(funcHookCallback, l.Hook, "#1");

            l.DoFile("main.l");
            string output = hookLog.ToString();
            string expected =
@"main.l-main.l:2 (main)
foo.l-foo.l:2 (main)
module1.l-module1.l:3 (main)
module1.l-module1.l:9 (main)
module1.l-module1.l:5 (main)
module1.l-module1.l:11 (main)
foo.l-foo.l:8 (main)
foo.l-foo.l:4 (main)
foo.l-foo.l:14 (main)
foo.l-foo.l:10 (main)
foo.l-foo.l:14 (main)
main.l-main.l:4 (main)
main.l-main.l:5 (main)
main.l-main.l:7 (main)
main.l-main.l:8 (main)
foo.l-foo.l:5 (Lua)
foo.l-foo.l:6 (Lua)
foo.l-foo.l:7 (Lua)
module1.l-module1.l:6 (Lua)
module1.l-module1.l:7 (Lua)
module1.l-module1.l:8 (Lua)
foo.l-foo.l:8 (Lua)
main.l-main.l:11 (main)
";
            expected = expected.Replace('/', Path.DirectorySeparatorChar);
            expected = expected.Replace("\r", "");
            output = output.Replace("\r", "");

            Assert.AreEqual(expected, output, "#2");

            l.SetHook(funcHookCallback, LuaHookMask.Disabled, 0);

            Assert.IsNull(l.Hook, "#3");
        }
        private static void HookCalbackStruct(IntPtr p, IntPtr ar)
        {
            var l = Lua.FromIntPtr(p);
            var debug = new LuaDebug();

            l.GetStack(0, ref debug);

            if (!l.GetInfo("Snlu", ref debug))
                return;

            string shortSource = Path.GetFileName(debug.ShortSource);
            string source = debug.Source.Substring(1);
            source = Path.GetFileName(source);
            hookLog.AppendLine($"{shortSource}-{source}:{debug.CurrentLine} ({debug.What})");
        }

        #endregion

        [Test]
        public void TypeNameReturn()
        {
            using (var l = new Lua())
            {
                l.PushInteger(28);
                string name = l.TypeName(-1);

                Assert.AreEqual("number", name, "#1");
            }
        }

        [Test]
        public void SettingUpValueDoesntCrash()
        {
            using (var l = new Lua())
            {
                l.LoadString("hello = 1");
                l.NewTable();
                string result = l.SetUpValue(-2, 1);

                Assert.AreEqual("_ENV", result, "#1");
            }
        }

        [Test]
        public void TestUnref()
        {
            var l = new Lua();
            l.DoString("function f() end");
            LuaType type = l.GetGlobal("f");
            Assert.AreEqual(LuaType.Function, type, "#1");

            l.PushCopy(-1);
            l.Ref(LuaRegistry.Index);
            l.Close();
        }

        public static int Func(IntPtr p)
        {
            var l = Lua.FromIntPtr(p);
            long param1 = l.CheckInteger(1);
            long param2 = l.CheckInteger(2);

            l.PushInteger(param1 + param2);
            return 1;
        }

        [Test]
        public void TestThreadFromToPtr()
        {
            using (var l = new Lua())
            {
                l.Register("func1", Func);

                Lua thread = l.NewThread();

                thread.DoString("func1(10,10)");
                thread.DoString("func1(10,10)");
            }
        }

        [Test]
        public void TestCoroutineCallback()
        {
            using (var l = new Lua())
            {
                l.Register("func1", Func);

                string script = @"function yielder() 
                                a=1; 
                                coroutine.yield();
                                a = func1(3,2);
                                a = func1(4,2);
                                coroutine.yield();
                                a=2;
                                coroutine.yield();
                             end
                             co_routine = coroutine.create(yielder);
                             while coroutine.resume(co_routine) do end;";
                l.DoString(script);
                l.DoString(script);

                l.GetGlobal("a");
                long a = l.ToInteger(-1);
                Assert.AreEqual(a, 2d);
            }
        }

        [Test]
        public void TestToStringStack()
        {
            using (var l = new Lua())
            {
                l.PushNumber(3);
                l.PushInteger(4);

                int currentTop = l.GetTop();

                string four = l.ToString(-1);

                int newTop = l.GetTop();

                Assert.AreEqual("4", four, "#1.1");
                Assert.AreEqual(currentTop, newTop, "#1.2");
            }
        }

        [Test]
        public void GettingUpValueDoesntCrash()
        {
            using (var l = new Lua())
            {
                l.LoadString("hello = 1");
                string result = l.GetUpValue(-1, 1);

                Assert.AreEqual("_ENV", result, "#1");
            }
        }

        [Test]
        public void ResumeAcceptsNull()
        {
            using (var l = new Lua())
            {
                l.LoadString("hello = 1");
                LuaStatus result = l.Resume(null, 0);

                Assert.AreEqual(LuaStatus.OK, result);
            }
        }

        public static void MyWarning(IntPtr ud, IntPtr msg, int tocont)
        {
            var l = Lua.FromIntPtr(ud);
            StringBuilder sb = l.ToObject<StringBuilder>(-1);
            string message = System.Runtime.InteropServices.Marshal.PtrToStringAnsi(msg);
            sb.Append(message);
        }

        [Test]
        public void TestWarning()
        {
            using (var l = new Lua())
            {
                LuaWarnFunction warnFunction = MyWarning;
                var sb = new StringBuilder();

                l.PushObject(sb);
                l.SetWarningFunction(warnFunction, l.Handle);

                l.Warning("Ola um dois tres", false);

                Assert.AreEqual("Ola um dois tres", sb.ToString(), "#1");
            }
        }

        private static readonly LuaRegister[] fooReg =
        {
            new LuaRegister { name = "foo", function = Foo, },
            new LuaRegister { name = null,  function = null, }
        };

        [Test]
        public static void TestNewLib()
        {
            var l = new Lua();

            l.RequireF("foobar", OpenFoo, true);

            l.DoString("s = foobar.foo()");

            l.GetGlobal("s");

            bool check = l.IsString(-1);
            string s = l.ToString(-1, false);

            Assert.IsTrue(check, "#1");
            Assert.AreEqual("bar", s, "#2");

            l.Dispose();
        }

        private static int OpenFoo(IntPtr p)
        {
            var l = Lua.FromIntPtr(p);
            l.NewLib(fooReg);
            return 1;
        }

        private static int Foo(IntPtr p)
        {
            var l = Lua.FromIntPtr(p);

            l.PushString("bar");
            return 1;
        }
    }
    #endregion
}
