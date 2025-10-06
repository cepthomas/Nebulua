using Ephemera.NBagOfTricks;
using System;
using System.Linq;


namespace Nebulua
{
    #region Types
    /// <summary>Application error. Includes function args etc.</summary>
    public class AppException(string message) : Exception(message) { }

    /// <summary>Channel playing.</summary>
    public enum PlayState { Normal, Solo, Mute }

    /// <summary>Channel direction.</summary>
    public enum Direction { None, Input, Output }
    #endregion

    /// <summary>References one channel. Supports translation to/from script unique int handle.</summary>
    /// <param name="DeviceId">Index in internal list</param>
    /// <param name="ChannelNumber">Midi channel 1-based</param>
    /// <param name="Output">T or F</param>
    public record struct ChannelHandle(int DeviceId, int ChannelNumber, Direction Direction)
    {
        const int OUTPUT_FLAG = 0x8000;

        /// <summary>Create from int handle.</summary>
        /// <param name="handle"></param>
        public ChannelHandle(int handle) : this(-1, -1, Direction.None)
        {
            Direction = (handle & OUTPUT_FLAG) > 0 ? Direction.Output : Direction.Input;
            DeviceId = ((handle & ~OUTPUT_FLAG) >> 8) & 0xFF;
            ChannelNumber = (handle & ~OUTPUT_FLAG) & 0xFF;
        }

        /// <summary>Operator to convert to int handle.</summary>
        /// <param name="ch"></param>
        public static implicit operator int(ChannelHandle ch)
        {
            return (ch.DeviceId << 8) | ch.ChannelNumber | (ch.Direction == Direction.Output ? OUTPUT_FLAG : OUTPUT_FLAG);
        }
    }

    /// <summary>General stuff.</summary>
    public class Utils
    {
        /// <summary>Generic exception processor for other threads that throw.</summary>
        /// <param name="e"></param>
        /// <returns>(bool fatal, string msg)</returns>
        public static (bool fatal, string msg) ProcessException_XXX(Exception e)
        {
            bool fatal = false;
            string msg;



            switch (e)
            {
                case LuaException ex: // script or lua errors but could originate anywhwere

                    // Common stuff.
                    msg = ex.Message; // default
                    //Console.WriteLine($"status:{ex.Status} info:{ex.Info}] context:[{ex.Context}]");

                    if (ex.Context.Length > 0)
                    {
                        // Make a synopsis - dissect the stack from luaLerror().
                        var parts = ex.Context.SplitByTokens("\r\n\t");

                        if (parts[1] == "stack traceback:")
                        {
                            //C:\Dev\Apps\Nebulua\lua\script_api.lua:95: Invalid arg type for chan_name
                            var errdesc = parts[0].SplitByToken(":").Last();
                            //  C  \Dev\Apps\Nebulua\lua\script_api.lua  95  Invalid arg type for chan_name

                            //	C:\Dev\Apps\Nebulua\examples\example.lua:33: in main chunk
                            var src = parts.Last().SplitByToken(":");
                            //	C  \Dev\Apps\Nebulua\examples\example.lua  33  in main chunk
                            //var srcfile = $"{src[0]}:{src[1]}({src[2]})";

                            msg = $"{ex.Status} {errdesc} => {src[0]}:{src[1]}({src[2]})";
                        }
                        else
                        {
                            msg = $"{ex.Status} {ex.Message}";
                        }
                    }

                    switch (ex.Status)
                    {
                        case LuaStatus.ERRRUN:
                        case LuaStatus.ERRMEM:
                        case LuaStatus.ERRERR:
                            State.Instance.ExecState = ExecState.Dead_XXX;
                            fatal = true;
                            break;

                        case LuaStatus.ERRSYNTAX:
                            State.Instance.ExecState = ExecState.Dead_XXX;
                            break;

                        case LuaStatus.ERRFILE:
                            State.Instance.ExecState = ExecState.Dead_XXX;
                            break;

                        case LuaStatus.ERRARG:
                            State.Instance.ExecState = ExecState.Dead_XXX;
                            break;

                        case LuaStatus.INTEROP:
                            State.Instance.ExecState = ExecState.Dead_XXX;
                            break;

                        case LuaStatus.DEBUG:
                            break;

                        case LuaStatus.OK:
                        case LuaStatus.YIELD:
                            // Normal, ignore.
                            break;
                    }
                    break;

                case AppException ex: // from app 
                    msg = $"TODO1 App Error {ex.Message}";
                    break;

                default: // other, probably fatal
                    msg = $"{e.GetType()} {e.Message}";
                    if (e.StackTrace is not null)
                    {
                        msg += $"{Environment.NewLine}{e.StackTrace}";
                    }
                    fatal = true;
                    break;
            }

            return (fatal, msg);
        }
    }

    #region Console abstraction to support testing TODO?
    public interface IConsole
    {
        bool KeyAvailable { get; }
        string Title { get; set; }
        void Write(string text);
        void WriteLine(string text);
        string? ReadLine();
        ConsoleKeyInfo ReadKey(bool intercept);
    }
    #endregion
}
