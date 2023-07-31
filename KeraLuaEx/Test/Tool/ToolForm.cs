using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using System.IO;
using System.Diagnostics;
using System.Text;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.Json.Serialization;
using KeraLuaEx;


namespace KeraLuaEx.Tool
{
    public partial class ToolForm : Form
    {
        #region Fields

        enum Level { ERR, INF, DBG, SCR };

        readonly Color _backColor = Color.Bisque;

        Dictionary<Level, Color> _colors = new();

        Lua? _lMain;

        readonly LuaFunction _funcPrint = PrintEx;

        readonly string _defaultScriptsPath = @"C:\Dev\repos\Nebulua\KeraLuaEx\Test\scripts";

        static ToolForm? _mf;

        readonly int _maxText = 5000;

        readonly FileSystemWatcher _watcher = new();

        bool _dirty = false;

        //const string _sindent = "    ";

        #endregion

        #region Lifecycle
        /// <summary>
        /// Constructor.
        /// </summary>
        public ToolForm()
        {
            InitializeComponent();

            _mf = this;
        }

        /// <summary>
        /// Post create init.
        /// </summary>
        /// <param name="e"></param>
        protected override void OnLoad(EventArgs e)
        {
            rtbScript.Clear();
            rtbOutput.Clear();

            _colors = new()
            {
                { Level.ERR, Color.Pink },
                { Level.INF, _backColor },
                { Level.DBG, Color.LightGreen },
                { Level.SCR, Color.Magenta },
            };

            var font = new Font("Consolas", 10);
            rtbScript.Font = font;
            rtbOutput.Font = font;
            rtbStack.Font = font;

            Log(Level.INF, "============================ Starting up ===========================");

            // TODO2 temp debug
            string sopen = OpenScriptFile(@"C:\Dev\repos\Nebulua\KeraLuaEx\Test\scripts\luaex.lua");

            rtbScript.KeyDown += (object? _, KeyEventArgs __) => _dirty = true;

            _watcher.EnableRaisingEvents = true;
            _watcher.NotifyFilter = NotifyFilters.LastWrite;
            _watcher.Changed += Watcher_Changed;

            base.OnLoad(e);
        }

        /// <summary>
        /// Clean up on shutdown.
        /// </summary>
        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            if (_dirty)
            {
                //TODO2 ask to save.
            }

            base.OnFormClosing(e);
        }

        /// <summary>
        /// Resource clean up.
        /// </summary>
        /// <param name="disposing"></param>
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                components?.Dispose();
            }

            base.Dispose(disposing);
        }
        #endregion

        #region File handling
        /// <summary>
        /// Allows the user to select a script file.
        /// </summary>
        void Open_Click(object? sender, EventArgs e)
        {
            if (_dirty)
            {
                //TODO2 ask to save.
            }

            using OpenFileDialog openDlg = new()
            {
                Filter = "Lua files | *.lua",
                Title = "Select a Lua file",
                InitialDirectory = _defaultScriptsPath,
            };

            if (openDlg.ShowDialog() == DialogResult.OK)
            {
                string sopen = OpenScriptFile(openDlg.FileName);
            }
        }

        /// <summary>
        /// Common script file opener.
        /// </summary>
        /// <param name="fn">The np file to open.</param>
        /// <returns>Error string or empty if ok.</returns>
        string OpenScriptFile(string fn)
        {
            string ret = "";
            rtbScript.Clear();

            try
            {
                string s = File.ReadAllText(fn);
                rtbScript.AppendText(s);
                Text = $"Testing {fn}";
                Log(Level.INF, $"Opening {fn}");

                _watcher.Path = Path.GetDirectoryName(fn)!;
                _watcher.Filter = Path.GetFileName(fn);
            }
            catch (Exception ex)
            {
                ret = $"Couldn't open the script file {fn} because {ex.Message}";
                Log(Level.ERR, ret);
            }

            return ret;
        }

        /// <summary>
        /// File changed externally.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void Watcher_Changed(object sender, FileSystemEventArgs e)
        {
            if (_dirty)
            {
                //TODO2 ask to save.
            }

            OpenScriptFile(e.FullPath);
        }
        #endregion

        #region Lua calls C# function
        /// <summary>
        /// Called by lua script.
        /// </summary>
        /// <param name="p"></param>
        /// <returns></returns>
        static int PrintEx(IntPtr p)
        {
            var l = Lua.FromIntPtr(p)!;
            _mf!.Log(Level.SCR, $"printex:{l.ToString(-1)!}");
            return 0;
        }



        /// <summary>Add a named chord or scale definition.</summary>
        // list_of_ints = ltoc('whatsis", { 'A', 'B', 'C' } )
        static int LuaCallCsharp(IntPtr p)
        {
            var l = Lua.FromIntPtr(p)!;

            // Get lua func args.
            int numArgs = l.GetTop();

            var name = l.ToString(1);
            // var parts = l.ToString(2);
            var parts = l.ToStringList(2); //TODO support array

            // Do the work.
            int numRes = 1;
            List<int> notes = new() { 3, 55, 909, 1 };// MusicDefinitions.GetNotesFromString(noteString);

            // Return val.
            l.PushList(notes);

            return numRes;
        }
        #endregion



// json.encode(value)
// Returns a string representing value encoded in JSON.
// json.encode({ 1, 2, 3, { x = 10 } }) -- Returns '[1,2,3,{"x":10}]'

// json.decode(str)
// Returns a value representing the decoded JSON string.
// json.decode('[1,2,3,{"x":10}]') -- Returns { 1, 2, 3, { x = 10 } }


        public static void ToStringList(this Lua l, List<int> ints) // overloads for doubles, strings


        /// <summary>
        /// Push a list of ints onto the stack (as C# function return).
        /// </summary>
        /// <param name="l"></param>
        /// <param name="ints"></param>
        public static void PushList(this Lua l, List<int> ints) // overloads for doubles, strings
        {
            //https://stackoverflow.com/a/18487635

            l.NewTable();

            for (int i = 0; i < ints.Count; i++)
            {
                l.NewTable();
                l.PushInteger(i + 1);
                l.RawSetInteger(-2, 1);
                l.PushInteger(ints[i]);
                l.RawSetInteger(-2, 2);
                l.RawSetInteger(-2, i + 1);
            }
        }
// typedef struct Point { int x, y; } Point;
// static int returnImageProxy(lua_State *L)
// {
//     Point points[3] = {{11, 12}, {21, 22}, {31, 32}};
//     lua_newtable(L);
//     for (int i = 0; i < 3; i++) {
//         lua_newtable(L);
//         lua_pushnumber(L, points[i].x);
//         lua_rawseti(L, -2, 1);
//         lua_pushnumber(L, points[i].y);
//         lua_rawseti(L, -2, 2);
//         lua_rawseti(L, -2, i+1);
//     }
//     return 1;   // I want to return a Lua table like :{{11, 12}, {21, 22}, {31, 32}}
// }



        #region C# calls Lua function
        public static void Step(int bar, int beat, int subdiv)
        {
            // Get the function to be called. Check return.
            LuaType gtype = _lMain.GetGlobal("step"); // TODOE check these.

            // Push the arguments to the call.
            _lMain.PushInteger(bar);
            _lMain.PushInteger(beat);
            _lMain.PushInteger(subdiv);

            // Do the actual call.
            LuaStatus lstat = _lMain.PCall(3, 0, 0);
            _lMain.CheckLuaStatus(lstat);

            // Get the results from the stack.
            // None.
        }


        #endregion


        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void Go1_Click(object sender, EventArgs e)
        {
            rtbOutput.Clear();
            Log(Level.INF, "============================ Here we go!!! ===========================");

            //Setup();
            _lMain?.Close();
            _lMain = new Lua();
            _lMain.Register("printex", _funcPrint);

            Utils.SetLuaPath(_lMain, new() { _defaultScriptsPath });
            string s = rtbScript.Text;
            LuaStatus lstat = _lMain.LoadString(s);
            _lMain.CheckLuaStatus(lstat);
            lstat = _lMain.PCall(0, -1, 0);
            _lMain.CheckLuaStatus(lstat);

            List<string>? ls = new();

            ShowStack();

            //ls = Utils.DumpStack(_lMain);
            //Log(Level.INF, FormatDump("Stack", ls, true));

            //ls = Utils.DumpGlobals(_lMain);
            //Log(Level.INF, FormatDump("Globals", ls, true));

            //ls = Utils.DumpStack(_lMain);
            //Log(Level.INF, FormatDump("Stack", ls, true));

            //ls = Utils.DumpTable(_lMain, "_G");
            //Log(Level.INF, FormatDump("_G", ls, true));

            ls = Utils.DumpGlobalTable(_lMain, "g_table");
            Log(Level.INF, FormatDump("g_table", ls, 1));
            //g_table:
            //  dev_type(String):bing_bong
            //  abool(Boolean):true
            //  channel(Number):10

            //ls = Utils.DumpTraceback(_lMain);
            //Log(Level.INF, FormatDump("Traceback", ls, true));

            var x = Utils.GetGlobalValue(_lMain, "g_number");
            Log(Level.INF, FormatVal("g_number", x.val));

            x = Utils.GetGlobalValue(_lMain, "g_int");
            Log(Level.INF, FormatVal("g_int", x.val));

            //var ttype = _lMain.GetGlobal("things");
            //ShowStack();
            //_lMain.Pop(1);


            ls = Utils.DumpGlobalTable(_lMain, "things");
            Log(Level.INF, FormatDump("things", ls, 1));
            //things:
            //  WHIZ(Table):table: 000002096923BF30
            //  TRIG(Table):table: 000002096923C3F0
            //  TUNE(Table):table: 000002096923CBF0


            //x = Utils.GetGlobalValue(_lMain, "g_list");
            //Log(Level.INF, $"g_list:{x}");

            ls = Utils.DumpGlobalTable(_lMain, "g_list");
            Log(Level.INF, FormatDump("g_list", ls, 1));
            //g_list:
            //  1(Number):2
            //  2(Number):56
            //  3(Number):98
            //  4(Number):2

            // Tables in/out TODO1


            ///// Execute a lua function.
            LuaType gtype = _lMain.GetGlobal("g_func"); //Function?
            // Push the arguments to the call.
            _lMain.PushString("az9011 birdie");
            // Do the actual call.
            lstat = _lMain.PCall(1, 1, 0); // OK?
            // Get result.
            int res = (int)_lMain.ToInteger(-1)!;
            Log(Level.DBG, $"Function returned {res} should be 13");

            // TearDown();
            _lMain?.Close();
            _lMain = null;
        }

        /// <summary>
        /// Show the contents of the stack.
        /// </summary>
        void ShowStack()
        {
            var ls = Utils.DumpStack(_lMain!);
            rtbStack.Text = FormatDump("Stack", ls, 1);
        }

        /// <summary>
        /// Format value for display.
        /// </summary>
        /// <param name="name"></param>
        /// <param name="val"></param>
        /// <returns></returns>
        /// <exception cref="SyntaxException"></exception>
        string FormatVal(string name, object? val)
        {
            string sval = "???";
            
            switch (val)
            {
                case int _: sval = $"{name}:{val}(int)"; break;
                case double _: sval = $"{name}:{val}(double)"; break;
                case bool _: sval = $"{name}:{val}(bool)"; break;
                case string _: sval = $"{name}:{val}(string)"; break;
                case null: sval = $"{name}:(null)"; break;
                //case table: sval = $"{name}:{val}(table)"; break; // TODO1 deeper
                default: throw new SyntaxException($"Unsupported type:{val} for {name}");
            };

            return sval;
        }

        /// <summary>
        /// Format list for display.
        /// </summary>
        /// <param name="name"></param>
        /// <param name="ls"></param>
        /// <param name="indent">Indent level</param>
        /// <returns></returns>
        string FormatDump(string name, List<string> ls, int indent)
        {
            var sind = new string(' ', indent * 4);
            var lines = new List<string> { $"{name}:" };
            ls.ForEach(s => lines.Add($"{sind}{s}"));
            var s = string.Join(Environment.NewLine, lines);
            return s;
        }

        /// <summary>
        /// Log it.
        /// </summary>
        /// <param name="level"></param>
        /// <param name="msg"></param>
        void Log(Level level, string msg)
        {
            string text = $"> {msg}{Environment.NewLine}";

            // Trim buffer.
            if (_maxText > 0 && rtbOutput.TextLength > _maxText)
            {
                rtbOutput.Select(0, _maxText / 5);
                rtbOutput.SelectedText = "";
            }

            rtbOutput.SelectionBackColor = _colors[level];
            rtbOutput.AppendText(text);
            rtbOutput.ScrollToCaret();
        }
    }
}
