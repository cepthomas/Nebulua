using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using Ephemera.NBagOfTricks;
using Ephemera.NBagOfTricks.Slog;


namespace Nebulua.Test
{
    public class TestUtils
    {
        public static string GetFilesDir()
        {
            return Path.Join(MiscUtils.GetSourcePath(), "files");
        }

        public static string GetProjectSubdir(string which)
        {
            string spath = MiscUtils.GetSourcePath();
            spath = Path.Join(spath, which);
            return spath;
        }

        // public static string ScriptFn { get { return Path.Join(GetProjectSubdir("files"), "script_happy.lua"); } }
        // public static string TempFn { get { return Path.Join(GetProectSubdir("files"), "temp.lua"); } }
    }

    /// <summary>Used to capture events from test target.</summary>
    public class InteropEventCollector//TODO1
    {
        public List<string> CollectedEvents { get; set; }

        Interop.Api _api;

        public InteropEventCollector(Interop.Api api)
        {
            _api = api;
            CollectedEvents = new();

            // Hook script events.
            _api.CreateChannelEvent += Api_CreateChannelEvent;
            _api.SendEvent += Api_SendEvent;
            _api.LogEvent += Api_LogEvent;
            _api.ScriptEvent += Api_ScriptEvent;
        }

        void Api_CreateChannelEvent(object? sender, Interop.CreateChannelEventArgs e)
        {
            string s = $"CreateChannelEvent DevName:{e.DevName} ChanNum:{e.ChanNum} IsOutput:{e.IsOutput} Patch:{e.Patch}";
            CollectedEvents.Add(s);
            e.Ret = 0x0102;
        }

        void Api_SendEvent(object? sender, Interop.SendEventArgs e)
        {
            string s = $"SendEvent ChanHnd:{e.ChanHnd} IsNote:{e.IsNote} What:{e.What} Value:{e.Value}";
            CollectedEvents.Add(s);
            e.Ret = Defs.NEB_OK;
        }

        void Api_LogEvent(object? sender, Interop.LogEventArgs e)
        {
            string s = $"LogEvent LogLevel:{e.LogLevel} Msg:{e.Msg}";
            CollectedEvents.Add(s);
            e.Ret = Defs.NEB_OK;
        }

        void Api_ScriptEvent(object? sender, Interop.ScriptEventArgs e)
        {
            string s = $"ScriptEvent Bpm:{e.Bpm}";
            CollectedEvents.Add(s);
            e.Ret = Defs.NEB_OK;
        }
    }
}