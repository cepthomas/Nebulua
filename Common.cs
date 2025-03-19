using System;
using System.Collections.Generic;
using System.IO;
using Ephemera.NBagOfTricks;
using InteropCore;


namespace Nebulua
{
    #region Exceptions
    /// <summary>Lua script syntax error.</summary>
    public class SyntaxException(string message) : Exception(message) { }
    #endregion

    public class Utils
    {
        /// <summary>Generic exception processor for callback threads that throw.</summary>
        /// <param name="e"></param>
        /// <returns>(bool fatal, string msg)</returns>
        public static (bool fatal, string msg) ProcessException(Exception e)
        {
            bool fatal = false;
            string msg;

            switch (e)
            {
                case SyntaxException ex:
                    msg = $"Script Syntax Error: {ex.Message}";
                    break;

                case ArgumentException ex:
                    msg = $"Argument Error: {ex.Message}";
                    //? fatal = true;
                    break;

                case LuaException ex:
                    msg = $"Lua/Interop Error: {ex.Message}";
                    //? fatal = true;
                    break;

                default: // other, probably fatal
                    msg = $"{e.GetType()}: {e.Message}{Environment.NewLine}{e.StackTrace}";
                    fatal = true;
                    break;
            }

            return (fatal, msg);
        }
    }
}
