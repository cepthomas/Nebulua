using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using Ephemera.NBagOfTricks;
using Ephemera.NBagOfTricks.Slog;


namespace Nebulua
{
    #region Exceptions
    /// <summary>Lua script syntax error.</summary>
    public class SyntaxException(string message) : Exception(message) { }
    #endregion

    public class Utils
    {
        static readonly Logger _logger = LogManager.CreateLogger("COM");
        static long _startTick = 0;
        static long _lastTick = 0;

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

        /// <summary>
        /// Measure some times.
        /// </summary>
        /// <param name="msg"></param>
        public static void TimeIt(string msg)
        {
            long tick = Stopwatch.GetTimestamp();

            if (_startTick == 0)
            {
                _startTick = tick;
                _lastTick = tick;
            }

            double durMsec = 1000.0 * (tick - _lastTick) / Stopwatch.Frequency;
            double totMsec = 1000.0 * (tick - _startTick) / Stopwatch.Frequency;
            var s = $"dur:{durMsec} tot:{totMsec} {msg}";
            //_logger.Info(s);
            // Console.WriteLine(s);
            _lastTick = tick;
        }
    }

    #region Console abstraction to support testing
    public interface IConsole
    {
        bool KeyAvailable { get; }
        bool CursorVisible { get; set; }
        string Title { get; set; }
        int BufferWidth { get; set; }
        void Write(string text);
        void WriteLine(string text);
        string? ReadLine();
        ConsoleKeyInfo ReadKey(bool intercept);
        (int left, int top) GetCursorPosition();
        void SetCursorPosition(int left, int top);
    }
    #endregion

}
