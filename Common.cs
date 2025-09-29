using System;


namespace Nebulua
{
    #region Types
    /// <summary>Lua script syntax error.</summary>
    public class SyntaxException(string message) : Exception(message) { }

    /// <summary>Channel playing.</summary>
    public enum PlayState { Normal, Solo, Mute }

    /// <summary>Channel direction.</summary>
    public enum Direction { None, Input, Output }
    #endregion

    /// <summary>Defines one channel. Supports translation to/from script int handle.</summary>
    /// <param name="DeviceId"></param>
    /// <param name="ChannelNumber"></param>
    /// <param name="Output"></param>
    public record struct ChannelDef(int DeviceId, int ChannelNumber, Direction Direction)
    {
        const int OUTPUT_FLAG = 0x8000;

        /// <summary>Create from int handle.</summary>
        /// <param name="handle"></param>
        public ChannelDef(int handle) : this(-1, -1, Direction.None)
        {
            Direction = (handle & OUTPUT_FLAG) > 0 ? Direction.Output : Direction.Input;
            DeviceId = ((handle & ~OUTPUT_FLAG) >> 8) & 0xFF;
            ChannelNumber = (handle & ~OUTPUT_FLAG) & 0xFF;
        }

        /// <summary>Operator to convert to int handle.</summary>
        /// <param name="ch"></param>
        public static implicit operator int(ChannelDef ch)
        {
            return (ch.DeviceId << 8) | ch.ChannelNumber | (ch.Direction == Direction.Output ? OUTPUT_FLAG : OUTPUT_FLAG);
        }
    }

    /// <summary>General definitions.</summary>
    public class Common
    {
        #region General definitions
        /// <summary>Midi constant.</summary>
        public const int MIDI_VAL_MIN = 0;

        /// <summary>Midi constant.</summary>
        public const int MIDI_VAL_MAX = 127;

        /// <summary>Per device.</summary>
        public const int NUM_MIDI_CHANNELS = 16;

        /// <summary>Corresponds to midi velocity = 0.</summary>
        public const double VOLUME_MIN = 0.0;

        /// <summary>Corresponds to midi velocity = 127.</summary>
        public const double VOLUME_MAX = 1.0;

        /// <summary>Default value.</summary>
        public const double VOLUME_DEFAULT = 0.8;

        /// <summary>Allow UI controls some more headroom.</summary>
        public const double MAX_GAIN = 2.0;
        #endregion

        #region Utils
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
                    msg = $"{e.GetType()}: {e.Message}";
                    if (e.StackTrace is not null)
                    {
                        msg += $"{Environment.NewLine}{e.StackTrace}";
                    }
                    fatal = true;
                    break;
            }

            return (fatal, msg);
        }
        #endregion
    }

    #region Console abstraction to support testing
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
