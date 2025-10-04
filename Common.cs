using Ephemera.NBagOfTricks;
using System;
using System.Linq;


namespace Nebulua
{
    #region Types
    /// <summary>Lua script syntax error.</summary>
    public class SyntaxException(string message) : Exception(message) { }

    /// <summary>Application error. Includes ArgumentException etc.</summary>
    public class AppException(string message) : Exception(message) { }

    /// <summary>Channel playing.</summary>
    public enum PlayState { Normal, Solo, Mute }

    /// <summary>Channel direction.</summary>
    public enum Direction { None, Input, Output }
    #endregion

    /// <summary>Defines one channel. Supports translation to/from script unique int handle.</summary>
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
        /// <summary>Generic exception processor for callback threads that throw.</summary>
        /// <param name="e"></param>
        /// <returns>(bool fatal, string msg)</returns>
        public static (bool fatal, string msg) ProcessException(Exception e)
        {
            bool fatal = false;
            string msg;

            switch (e)
            {
                case SyntaxException ex: // from script
                    // TODO1 Make a synopsis - dissect the stack from luaLerror().
                    // msg:
                    //ScriptRunError
                    //Execute script failed.
                    //C:\Dev\Apps\Nebulua\lua\script_api.lua:95: Invalid arg type for chan_name
                    //stack traceback:
                    //	[C]: in function 'luainterop.open_midi_input'
                    //	C:\Dev\Apps\Nebulua\lua\script_api.lua:95: in function 'script_api.open_midi_input'
                    //	C:\Dev\Apps\Nebulua\examples\example.lua:33: in main chunk
                    // ==>
                    // C:\Dev\Apps\Nebulua\examples\example.lua:33 Execute script failed. Invalid arg type for: chan_name.
                    var parts = e.Message.SplitByTokens("\r\n\t");

                    var src = parts.Last().SplitByToken(":");
                    //	C  \Dev\Apps\Nebulua\examples\example.lua  33  in main chunk
                    var api = parts[2].SplitByToken(":");
                    //  C  \Dev\Apps\Nebulua\lua\script_api.lua  95  Invalid arg type for chan_name
                    var lerr = parts[0];
                    // LUA_ERRRUN
                    var info = parts[1];
                    // Execute script failed.

                    msg = $"Script Syntax Error {ex.Message}";
                    break;

                case AppException ex: // from app
                    msg = $"App Error {ex.Message}";
                    break;

                case LuaException ex: // from interop
                    msg = $"Lua Error {ex.Message}";
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
