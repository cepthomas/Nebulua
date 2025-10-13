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

    /// <summary>Utility stuff.</summary>
    public class Utils
    {
        /// <summary>General exception processor.</summary>
        /// <param name="e"></param>
        /// <returns>(bool fatal, string msg)</returns>
        public static (bool fatal, string msg) ProcessException(Exception e)
        {
            bool fatal = false;
            string msg = e.Message; // default

            switch (e)
            {
                case LuaException ex:
                    if (ex.Error.Contains("FATAL")) // bad lua internal error
                    {
                        fatal = true;
                        State.Instance.ExecState = ExecState.Dead;
                    }
                    break;

                case AppException ex: // from app - not fatal
                    break;

                default: // other - assume fatal
                    fatal = true;
                    if (e.StackTrace is not null)
                    {
                        msg += $"{Environment.NewLine}{e.StackTrace}";
                    }
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
