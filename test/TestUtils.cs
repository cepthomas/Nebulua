using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using Ephemera.NBagOfTricks;
//using Interop;


namespace Ephemera.Nebulua.Test
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
        public static List<string> GetLuaPath()
        {
            // Set up lua environment.
            var projDir = GetProjectSourceDir();
            return [$@"{projDir}\lua_code", $@"{projDir}\lbot"];
        }
    }
}