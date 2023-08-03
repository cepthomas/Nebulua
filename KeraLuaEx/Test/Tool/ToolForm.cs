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
using System.Text.Json.Nodes;
using System.ComponentModel;


namespace KeraLuaEx.Tool
{
    public partial class ToolForm : Form
    {
        #region Types
        enum Level { ERR, INF, DBG, SCR };
        #endregion

        #region Fields
        Lua? _lMain;

        readonly LuaFunction _funcPrint = PrintEx;
        readonly LuaFunction _funcStartTimer = StartTimer;
        readonly LuaFunction _funcStopTimer = StopTimer;

        readonly string _defaultScriptsPath = @"C:\Dev\repos\Nebulua\KeraLuaEx\Test\scripts";

        static ToolForm? _mf;

        readonly Color _backColor = Color.Bisque;

        Dictionary<Level, Color> _logColors = new();

        readonly int _maxText = 5000;

        readonly FileSystemWatcher _watcher = new();

        bool _dirty = false;

        readonly Stopwatch _sw = new();

        long _startTicks = 0;
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
            StartPosition = FormStartPosition.Manual;
            Location = new(20, 20);
            ClientSize = new(1300, 950);

            rtbScript.Clear();
            rtbOutput.Clear();

            _logColors = new()
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

            // TODO1 temp debug
            string sopen = OpenScriptFile(@"C:\Dev\repos\Nebulua\KeraLuaEx\Test\scripts\luaex.lua");

            rtbScript.KeyDown += (object? _, KeyEventArgs __) => _dirty = true;

            _watcher.EnableRaisingEvents = true;
            _watcher.NotifyFilter = NotifyFilters.LastWrite;
            _watcher.Changed += Watcher_Changed;

            _sw.Start();

            base.OnLoad(e);
        }

        /// <summary>
        /// Clean up on shutdown.
        /// </summary>
        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            if (_dirty)
            {
                //TODO1 ask to save.
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
                _sw.Stop();

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
                //TODO1 ask to save.
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
                //TODO1 ask to save.
            }

            this.InvokeIfRequired(_ => { OpenScriptFile(e.FullPath); });
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

        /// <summary>
        /// Called by lua script.
        /// </summary>
        /// <param name="p"></param>
        /// <returns></returns>
        static int StartTimer(IntPtr p)
        {
            var l = Lua.FromIntPtr(p)!;
            _mf!.StartTimer();
            return 0;
        }

        /// <summary>
        /// Called by lua script.
        /// </summary>
        /// <param name="p"></param>
        /// <returns></returns>
        static int StopTimer(IntPtr p)
        {
            var l = Lua.FromIntPtr(p)!;
            var msec = _mf!.StopTimer();

            // Return val.
            l.PushNumber(msec);

            return 1;
        }
        #endregion

        #region C# calls Lua function
        //public void LikeThis(int bar, int beat, int subdiv)
        //{
        //    // Get the function to be called. Check return.
        //    LuaType gtype = _lMain!.GetGlobal("step"); // !!! check these.
        //    // Push the arguments to the call.
        //    _lMain.PushInteger(bar);
        //    _lMain.PushInteger(beat);
        //    _lMain.PushInteger(subdiv);
        //    // Do the actual call.
        //    LuaStatus lstat = _lMain.PCall(3, 0, 0);
        //    _lMain.CheckLuaStatus(lstat);
        //    // Get the results from the stack.
        //    // None.
        //}
        #endregion

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void GoMain_Click(object sender, EventArgs e)
        {
            Setup();
            try
            {
                string s = rtbScript.Text;
                _lMain!.LoadString(s);
                _lMain.PCall(0, -1, 0);

                List<string>? ls = new();

                ShowStack();

                //ls = Utils.DumpStack(_lMain);
                //Log(Level.INF, FormatDump("Stack", ls, true));

                var x = Utils.GetGlobalValue(_lMain, "g_table");
                var table = x.val as Table;
                Log(Level.INF, table!.Format("g_table"));
                //g_table:
                //  dev_type(String):bing_bong
                //  abool(Boolean):true
                //  channel(Number):10

                x = Utils.GetGlobalValue(_lMain, "g_number");
                Log(Level.INF, Utils.FormatCsharpVal("g_number", x.val));

                x = Utils.GetGlobalValue(_lMain, "g_int");
                Log(Level.INF, Utils.FormatCsharpVal("g_int", x.val));

                //x = Utils.GetGlobalValue(_lMain, "_G");
                //table = x.val as Table;
                //Log(Level.INF, table.Format("_G"));
                //public static List<string> DumpGlobals(Lua l)
                //{
                //    // Get global table.
                //    l.PushGlobalTable();
                //    var ls = DumpTable(l);
                //    // Remove global table(-1).
                //    l.Pop(1);
                //    return ls;
                //}

                x = Utils.GetGlobalValue(_lMain, "g_list_int");
                table = x.val as Table;
                Log(Level.INF, table!.Format("g_list_int"));
                //g_list_int:
                //  1(Number):2
                //  2(Number):56
                //  3(Number):98
                //  4(Number):2

                x = Utils.GetGlobalValue(_lMain, "things");
                table = x.val as Table;
                Log(Level.INF, table!.Format("things"));


                ///// Execute a lua function.
                LuaType gtype = _lMain.GetGlobal("g_func");
                // Push the arguments to the call.
                _lMain.PushString("az9011 birdie");
                // Do the actual call.
                _lMain.PCall(1, 1, 0);
                // Get result.
                var res = _lMain.ToInteger(-1)!;
                Log(Level.DBG, $"Function returned {res} should be 13");


                // Tables in/out TODO1


                // Json TODO2
                //json.lua:
                // json.encode(value)
                // Returns a string representing value encoded in JSON.
                // json.encode({ 1, 2, 3, { x = 10 } }) -- Returns '[1,2,3,{"x":10}]'
                //
                // json.decode(str)
                // Returns a value representing the decoded JSON string.
                // json.decode('[1,2,3,{"x":10}]') -- Returns { 1, 2, 3, { x = 10 } }
                // {"TUNE":{"channel":1,"dev_type":"midi_in"},"WHIZ":{"channel":10,"abool":true,"dev_type":"bing_bong"},"TRIG":{"channel":2,"adouble":1.234,"dev_type":"virt_key"}}
            }
            catch (Exception ex)
            {
                Log(Level.ERR, $"{ex}");
            }

            TearDown();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void GoJson_Click(object sender, EventArgs e)
        {
            Setup();

            string s = rtbScript.Text;
            _lMain!.LoadString(s);
            _lMain!.PCall(0, -1, 0);

            List<string>? ls = new();

            //https://marcroussy.com/2020/08/17/deserialization-with-system-text-json/


            var sjson = @"{""TUNE"":{""channel"":1,""dev_type"":""midi_in""},""WHIZ"":{""channel"":10,""alist"":[2,56,98,2],""dev_type"":""bing_bong""},""TRIG"":{""channel"":2,""adouble"":1.234,""dev_type"":""virt_key""}}";

            // Uses Utf8JsonReader  See Table. TODO2

            TearDown();
        }

        #region Internal functions
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

            rtbOutput.SelectionBackColor = _logColors[level];
            rtbOutput.AppendText(text);
            rtbOutput.ScrollToCaret();
        }

        /// <summary>
        /// Show the contents of the stack.
        /// </summary>
        void ShowStack()
        {
            var s = Utils.DumpStack(_lMain!);
            rtbStack.Text = s;
        }

        /// <summary>
        /// Start the elapsed timer.
        /// </summary>
        void StartTimer()
        {
            _startTicks = _sw.ElapsedTicks; // snap
        }

        /// <summary>
        /// Stop the elapsed timer and return msec.
        /// </summary>
        /// <returns></returns>
        double StopTimer()
        {
            double totalMsec = double.NaN;
            if (_startTicks > 0)
            {
                long t = _sw.ElapsedTicks; // snap
                totalMsec = (t - _startTicks) * 1000D / Stopwatch.Frequency;
            }
            return totalMsec;
        }

        /// <summary>
        /// Pretend unit test function.
        /// </summary>
        void Setup()
        {
            rtbOutput.Clear();

            _lMain?.Close();
            _lMain = new Lua();
            _lMain.Register("printex", _funcPrint);
            _lMain.Register("start_timer", _funcStartTimer);
            _lMain.Register("stop_timer", _funcStopTimer);

            Utils.SetLuaPath(_lMain, new() { _defaultScriptsPath });
        }

        /// <summary>
        /// Pretend unit test function.
        /// </summary>
        void TearDown()
        {
            _lMain?.Close();
            _lMain = null;
        }
        #endregion
    }

    static class Extensions
    {
        /// <summary>
        /// Invoke helper, maybe.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="obj"></param>
        /// <param name="action"></param>
        public static void InvokeIfRequired<T>(this T obj, InvokeIfRequiredDelegate<T> action) where T : ISynchronizeInvoke
        {
            if (obj is not null)
            {
                if (obj.InvokeRequired)
                {
                    obj.Invoke(action, new object[] { obj });
                }
                else
                {
                    action(obj);
                }
            }
        }
        public delegate void InvokeIfRequiredDelegate<T>(T obj) where T : ISynchronizeInvoke;
    }
}
