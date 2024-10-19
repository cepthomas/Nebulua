using System;
using System.IO;
using Ephemera.NBagOfTricks;


namespace Nebulua
{
    #region Exceptions
    /// <summary>Lua script syntax error.</summary>
    public class ScriptSyntaxException(string message) : Exception(message) { }

    /// <summary>Api error.</summary>
    public class ApiException(string message, string apiError) : Exception(message)
    {
        public string ApiError { get; init; } = apiError;
    }

    /// <summary>App command line error.</summary>
    public class ApplicationArgumentException(string message) : Exception(message) { }
    #endregion

    #region Console abstraction to support testing
    public interface IConsole
    {
        void Write(string text);
        void WriteLine(string text);
        string? ReadLine();
        ConsoleKeyInfo ReadKey(bool intercept);
        bool KeyAvailable { get; }
        bool CursorVisible { get; set; }
        string Title { get; set; }
        (int left, int top) GetCursorPosition();
        void SetCursorPosition(int left, int top);
    }
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
                case ApiException ex:
                    msg = $"Api Error: {ex.Message}:{Environment.NewLine}{ex.ApiError}";
                    break;

                case ScriptSyntaxException ex:
                    msg = $"Script Syntax Error: {ex.Message}";
                    break;

                case ApplicationArgumentException ex:
                    msg = $"Application Argument Error: {ex.Message}";
                    fatal = true;
                    break;

                default: // other, probably fatal
                    msg = $"{e.GetType()}: {e.Message}{Environment.NewLine}{e.StackTrace}";
                    fatal = true;
                    break;
            }

            return (fatal, msg);
        }

        /// <summary>
        /// Get the directory name where the application lives.
        /// </summary>
        /// <returns></returns>
        public static string GetAppRoot()
        {
            if (_rootDir is null)
            {
                DirectoryInfo dinfo = new(MiscUtils.GetSourcePath());
                while (dinfo.Name! != "Nebulua")
                {
                    dinfo = dinfo.Parent!;
                }
                _rootDir = dinfo.FullName;
            }

            return _rootDir!;
        }
        static string? _rootDir = null; // cache
    }
}
