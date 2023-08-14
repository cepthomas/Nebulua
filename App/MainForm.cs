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


        Lua _lMain;
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
            _mf.tvOutput.AppendLine($"printex >>> {l.ToStringL(-1)!}");
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

            _lMain.LoadString(s);

            _lMain.PCall(0, -1, 0);





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
