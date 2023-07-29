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
using NAudio.Midi;
using NAudio.Wave;
using Ephemera.NBagOfTricks;
using Ephemera.NBagOfTricks.ScriptCompiler;
using Ephemera.NBagOfTricks.Slog;
using Ephemera.NBagOfUis;
using Ephemera.MidiLib;
using Ephemera.Nebulator.Script;
using KeraLuaEx;



namespace Ephemera.Nebulua.App
{
    public partial class MainForm : Form
    {
        #region Fields
        /// <summary>App logger.</summary>
        //readonly Logger _logger = LogManager.CreateLogger("Main");

        ///// <summary>App settings.</summary>
        //UserSettings _settings;
        #endregion

        #region Lifecycle
        /// <summary>
        /// Constructor.
        /// </summary>
        public MainForm()
        {
            // Must do this first before initializing.
            //string appDir = MiscUtils.GetAppDataDir("Nebulua", "Ephemera");
            // _settings = (UserSettings)SettingsCore.Load(appDir, typeof(UserSettings));

            InitializeComponent();

            // Init logging.
            //string logFileName = Path.Combine(appDir, "log.txt");
            //LogManager.MinLevelFile = _settings.FileLogLevel;
            //LogManager.MinLevelNotif = _settings.NotifLogLevel;
            //LogManager.LogMessage += LogManager_LogMessage;
            //LogManager.Run(logFileName, 100000);

            _mf = this;
        }

        /// <summary>
        /// Post create init.
        /// </summary>
        /// <param name="e"></param>
        protected override void OnLoad(EventArgs e)
        {
            rtbScript.Clear();
            tvOutput.Clear();
            rtbScript.Font = tvOutput.Font;

            tvOutput.AppendLine("============================ Starting up ===========================");

            base.OnLoad(e);
        }

        /// <summary>
        /// Clean up on shutdown.
        /// </summary>
        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            LogManager.Stop();

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
                tvOutput.AppendLine(sopen);
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

                Text = $"Nebulator {MiscUtils.GetVersionString()} - {fn}";
            }
            catch (Exception ex)
            {
                ret = $"Couldn't open the script file: {fn} because: {ex.Message}";
                tvOutput.AppendLine(ret);
            }

            return ret;
        }
        #endregion


        Lua? _lMain;
        LuaFunction _funcPrint = Print;
        string _defaultScriptsPath = @"C:\Dev\repos\Nebulua\KeraLuaEx\Test\scripts";
        static MainForm _mf;




        /// <summary>
        /// 
        /// </summary>
        /// <param name="p"></param>
        /// <returns></returns>
        static int Print(IntPtr p)
        {
            var l = Lua.FromIntPtr(p)!;
            _mf.tvOutput.AppendLine($"printex >>> {l.ToString(-1)!}");
            return 0;
        }


        /// <summary>
        /// 
        /// </summary>
        void ShowStack()
        {
            var ls = _lMain.DumpStack();
            rtbStack.Text = ls.Count > 0 ? FormatDump("Stack", ls, true) : "Empty";
        }


        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void Go1_Click(object sender, EventArgs e)
        {
            tvOutput.Clear();
            tvOutput.AppendLine("============================ Here we go!!! ===========================");

            //Setup();
            _lMain?.Close();
            _lMain = new Lua();
            _lMain.Register("printex", _funcPrint);

            _lMain.SetLuaPath(new() { _defaultScriptsPath });
            string s = rtbScript.Text;
            LuaStatus lstat = _lMain.LoadString(s);
            _lMain.CheckLuaStatus(lstat);
            lstat = _lMain.PCall(0, -1, 0);
            _lMain.CheckLuaStatus(lstat);

            List<string>? ls = null;

            ShowStack();

            //ls = _lMain.DumpStack();
            //tvOutput.AppendLine(FormatDump("Stack", ls, true));

            //ls = _lMain.DumpGlobals();
            //tvOutput.AppendLine(FormatDump("Globals", ls, true));

            //ls = l.DumpStack();
            //tvOutput.AppendLine(FormatDump("Stack", ls, true));

            //ls = l.DumpTable("_G");
            //tvOutput.AppendLine(FormatDump("_G", ls, true));

            ls = _lMain.DumpTable("g_table");
            tvOutput.AppendLine(FormatDump("g_table", ls, true));

            //ls = _lMain.DumpTraceback();
            //tvOutput.AppendLine(FormatDump("Traceback", ls, true));

            var x = _lMain.GetGlobalValue("g_number");
            //Assert.AreEqual(typeof(double), x.type);

            x = _lMain.GetGlobalValue("g_int");
            //Assert.AreEqual(typeof(int), x.type);


            ls = _lMain.DumpTable("things");
            tvOutput.AppendLine(FormatDump("things", ls, true));

            ShowStack(); 

            //x = l.GetGlobalValue("g_table");
            //Assert.AreEqual(typeof(int), x.type);

            //x = l.GetGlobalValue("g_list");
            //Assert.AreEqual(typeof(int), x.type);


            ///// json stuff
            x = _lMain.GetGlobalValue("things_json");
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
            LuaType gtype = _lMain.GetGlobal("g_func");
            //Assert.AreEqual(LuaType.Function, gtype);
            // Push the arguments to the call.
            _lMain.PushString("az9011 birdie");
            // Do the actual call.
            lstat = _lMain.PCall(1, 1, 0);
            //Assert.AreEqual(LuaStatus.OK, lstat);
            // Get result.
            int res = (int)_lMain.ToInteger(-1)!;
            //Assert.AreEqual(13, res);
            tvOutput.AppendLine($"Function returned:{res}");

            //TearDown();
            _lMain?.Close();
            _lMain = null;
        }

        string FormatDump(string name, List<string> lsin, bool indent)
        {
            string sindent = indent ? "  " : "";
            var lines = new List<string> { $"{name}:" };
            lsin.ForEach(s => lines.Add($"{sindent}{s}"));
            var s = string.Join(Environment.NewLine, lines);
            return s;
        }
    }
}
