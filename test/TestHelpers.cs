using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using Ephemera.NBagOfTricks;
using Ephemera.NBagOfTricks.Slog;
using NAudio.Wave;
using Interop;


namespace Nebulua.Test
{
    public class TestUtils
    {
        /// <summary>Get environment.</summary>
        public static string GetTestFilesDir()
        {
            return Path.Join(GetProjectSourceDir(), "test", "files");
        }

        /// <summary>Get environment.</summary>
        public static string GetProjectSourceDir()
        {
            var spath = MiscUtils.GetSourcePath();
            var dir = new DirectoryInfo(spath);
            spath = dir!.Parent!.FullName;
            return spath;
        }

        /// <summary>Get environment.</summary>
        public static string GetProjectSubdir(string which)
        {
            return Path.Join(GetProjectSourceDir(), which);
        }

        /// <summary>Get LUA_PATH components.</summary>
        /// <returns>List of paths if success or null if invalid.</returns>
        public static (bool valid, List<string> lpath) GetLuaPath()
        {
            // Set up lua environment.
            var projDir = GetProjectSourceDir();
            var lbotDir = Environment.GetEnvironmentVariable("LBOT");
            return lbotDir != null ?
                (true, new() { $@"{projDir}\lua_code", $@"{lbotDir}"} ) :
                (false, new() { "Missing LBOT env var" } );
        }
    }

    /// <summary>Used to capture events from test target.</summary>
    public class InteropEventCollector
    {
        public List<string> CollectedEvents { get; set; }

        readonly Interop.Api _interop;

        public InteropEventCollector(Interop.Api interop)
        {
            _interop = interop;
            CollectedEvents = [];

            // Hook script events.
            _interop.CreateChannelEvent += Interop_CreateChannelEvent;
            _interop.SendEvent += Interop_SendEvent;
            _interop.LogEvent += Interop_LogEvent;
            _interop.ScriptEvent += Interop_ScriptEvent;
        }

        void Interop_CreateChannelEvent(object? sender, Interop.CreateChannelEventArgs e)
        {
            string s = $"CreateChannelEvent DevName:{e.DevName} ChanNum:{e.ChanNum} IsOutput:{e.IsOutput} Patch:{e.Patch}";
            CollectedEvents.Add(s);
            e.Ret = 0x0102;
        }

        void Interop_SendEvent(object? sender, Interop.SendEventArgs e)
        {
            string s = $"SendEvent ChanHnd:{e.ChanHnd} IsNote:{e.IsNote} What:{e.What} Value:{e.Value}";
            CollectedEvents.Add(s);
            e.Ret = (int)NebStatus.Ok;
        }

        void Interop_LogEvent(object? sender, Interop.LogEventArgs e)
        {
            string s = $"LogEvent LogLevel:{e.LogLevel} Msg:{e.Msg}";
            CollectedEvents.Add(s);
            e.Ret = (int)NebStatus.Ok;
        }

        void Interop_ScriptEvent(object? sender, Interop.ScriptEventArgs e)
        {
            string s = $"ScriptEvent Bpm:{e.Bpm}";
            CollectedEvents.Add(s);
            e.Ret = (int)NebStatus.Ok;
        }
    }
}