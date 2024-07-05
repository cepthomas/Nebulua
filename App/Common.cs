using System;
using System.Runtime.CompilerServices;
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

    public class Utils
    {
        /// <summary>Generic exception processor.</summary>
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

                default: // other
                    msg = $"{e.GetType()}: {e.Message}{Environment.NewLine}{e.StackTrace}";
                    fatal = true;
                    break;
            }

            return (fatal, msg);
        }
    }
    #endregion
}
