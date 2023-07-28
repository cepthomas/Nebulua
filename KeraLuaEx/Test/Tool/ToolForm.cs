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


/* TODOA monitor external script file, save editor to file.

FileSystemWatcher watcher = new()
{
    Path = npath,
    Filter = Path.GetFileName(path),
    EnableRaisingEvents = true,
    NotifyFilter = NotifyFilters.LastWrite
};

watcher.Changed += Watcher_Changed;

void Watcher_Changed(object sender, FileSystemEventArgs e)
{
    _touchedFiles.Add(e.FullPath);
    // Reset timer.
    _timer.Interval = DELAY;
}
*/



namespace KeraLuaEx.Tool
{
    public partial class ToolForm : Form
    {
        #region Fields

        enum Level { ERR, INF, DBG, SCR };

        readonly Color _backColor = Color.Bisque;

        Dictionary<Level, Color> _colors = new();

        Lua? _lMain;

        readonly LuaFunction _funcPrint = Print;

        readonly string _defaultScriptsPath = @"C:\Dev\repos\Nebulua\KeraLuaEx\Test\scripts";

        static ToolForm? _mf;

        readonly int _maxText = 5000;
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

            // TODOA temp debug
            string sopen = OpenScriptFile(@"C:\Dev\repos\Nebulua\KeraLuaEx\Test\scripts\luaex.lua");

            base.OnLoad(e);
        }

        /// <summary>
        /// Clean up on shutdown.
        /// </summary>
        protected override void OnFormClosing(FormClosingEventArgs e)
        {
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
        /// Allows the user to select a np file from file system.
        /// </summary>
        void Open_Click(object? sender, EventArgs e)
        {
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

            try
            {
                string s = File.ReadAllText(fn);

                rtbScript.AppendText(s);

                Text = $"Testing {fn}";

                Log(Level.INF, $"Opening {fn}");
            }
            catch (Exception ex)
            {
                ret = $"Couldn't open the script file {fn} because {ex.Message}";
                Log(Level.ERR, ret);
            }

            return ret;
        }
        #endregion

        /// <summary>
        /// 
        /// </summary>
        /// <param name="p"></param>
        /// <returns></returns>
        static int Print(IntPtr p)
        {
            var l = Lua.FromIntPtr(p)!;
            _mf!.Log(Level.SCR, $"printex said: {l.ToString(-1)!}");
            return 0;
        }

        /// <summary>
        /// 
        /// </summary>
        void ShowStack()
        {
            var ls = Utils.DumpStack(_lMain!);
            rtbStack.Text = ls.Count > 0 ? FormatDump("Stack", ls, true) : "Empty";
        }

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

            ls = Utils.DumpTable(_lMain, "g_table");
            Log(Level.INF, FormatDump("g_table", ls, true));

            //ls = Utils.DumpTraceback(_lMain);
            //Log(Level.INF, FormatDump("Traceback", ls, true));

            var x = Utils.GetGlobalValue(_lMain, "g_number");
            //Assert.AreEqual(typeof(double), x.type);

            x = Utils.GetGlobalValue(_lMain, "g_int");
            //Assert.AreEqual(typeof(int), x.type);


            ls = Utils.DumpTable(_lMain, "things");
            Log(Level.INF, FormatDump("things", ls, true));

            ShowStack();

            //x = Utils.GetGlobalValue(_lMain, "g_table");
            //Assert.AreEqual(typeof(int), x.type);

            //x = Utils.GetGlobalValue(_lMain, "g_list");
            //Assert.AreEqual(typeof(int), x.type);


            ///// json stuff TODOA
            x = Utils.GetGlobalValue(_lMain, "things_json");//TODOA
            //Assert.AreEqual(typeof(string), x.type);
            var jdoc = JsonDocument.Parse(x.val!.ToString()!);
            //var jrdr = new Utf8JsonReader();
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
            LuaType gtype = _lMain.GetGlobal("g_func"); //Function?
            // Push the arguments to the call.
            _lMain.PushString("az9011 birdie");
            // Do the actual call.
            lstat = _lMain.PCall(1, 1, 0); // OK?
            // Get result.
            int res = (int)_lMain.ToInteger(-1)!;
            Log(Level.DBG, $"Function returned:{res} 13?");

            //TearDown();
            _lMain?.Close();
            _lMain = null;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="name"></param>
        /// <param name="lsin"></param>
        /// <param name="indent"></param>
        /// <returns></returns>
        string FormatDump(string name, List<string> lsin, bool indent)
        {
            string sindent = indent ? "    " : "";
            var lines = new List<string> { $"{name}:" };
            lsin.ForEach(s => lines.Add($"{sindent}{s}"));
            var s = string.Join(Environment.NewLine, lines);
            return s;
        }

        /// <summary>
        /// 
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
