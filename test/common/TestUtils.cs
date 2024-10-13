using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using Ephemera.NBagOfTricks;
using Nebulua;


namespace Nebulua.Test
{
    /// <summary>Helpers for tests.</summary>
    public class TestUtils
    {
        /// <summary>Get environment.</summary>
        public static string GetProjectSourceDir()
        {
            var spath = MiscUtils.GetSourcePath();
            var dir = new DirectoryInfo(spath);
            spath = dir!.Parent!.Parent!.FullName;
            return spath;
        }

        /// <summary>Get LUA_PATH components.</summary>
        /// <returns>List of paths if success or null if invalid.</returns>
        public static List<string> GetLuaPath()
        {
            // Set up lua environment.
            var projDir = GetProjectSourceDir();
            return [$@"{projDir}\lua", $@"{projDir}\test\lua"];
        }
    }
}