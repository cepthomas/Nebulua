using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using Ephemera.NBagOfTricks;
using Nebulua.Common;


namespace Nebulua.Test
{
    public class TestUtils
    {
        /// <summary>Get environment.</summary>
        public static string GetTestLuaDir()
        {
            return Path.Join(GetProjectSourceDir(), "test", "lua");
        }

        /// <summary>Get environment.</summary>
        public static string GetProjectSourceDir()
        {
            var spath = MiscUtils.GetSourcePath();
            var dir = new DirectoryInfo(spath);
            spath = dir!.Parent!.Parent!.FullName;
            return spath;
        }

        /// <summary>Get environment.</summary>
        public static string GetProjectSubdir(string which)
        {
            return Path.Join(GetProjectSourceDir(), which);
        }

        /// <summary>Get LUA_PATH components.</summary>
        /// <returns>List of paths if success or null if invalid.</returns>
        public static List<string> GetLuaPath()
        {
            // Set up lua environment.
            var projDir = GetProjectSourceDir();
            return [$@"{projDir}\lua_code", $@"{projDir}\lbot", $@"{projDir}\test\lua"];
        }

        /// <summary>
        /// Setup the state stuff tests need to see.
        /// </summary>
        public static void SetupFakeScript()
        {
            // Fake valid loaded script.
            List<(int tick, string name)> sinfo = [];
            sinfo.Add((0, "start"));
            sinfo.Add((200, "middle"));
            sinfo.Add((300, "end"));
            sinfo.Add((400, "LENGTH"));
            State.Instance.SectionInfo = sinfo;
            State.Instance.LoopStart = -1;
            State.Instance.LoopEnd = -1;
        }
    }
}