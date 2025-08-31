using System;


namespace Nebulua
{
    #region Types
    /// <summary>Lua script syntax error.</summary>
    public class SyntaxException(string message) : Exception(message) { }

    /// <summary>Channel state.</summary>
    public enum ChannelState { Normal, Solo, Mute }
    #endregion

    /// <summary>Channel info.</summary>
    public class ChannelSpec(ChannelSpec.ChannelDirection direction, int deviceId, string deviceName, int channelNumber, int patch)
    {
        /// <summary>Output or input.</summary>
        public enum ChannelDirection { Output, Input }

        /// <summary>Output or input.</summary>
        public ChannelDirection Direction { get; init; } = direction;

        /// <summary>Device identifier - internal.</summary>
        public int DeviceId { get; init; } = deviceId;

        /// <summary>Device name from system.</summary>
        public string DeviceName { get; init; } = deviceName;

        /// <summary>Midi channel number 1-based.</summary>
        public int ChannelNumber { get; init; } = channelNumber;

        /// <summary>Optional patch.</summary>
        public int Patch { get; init; } = patch;

        /// <summary>Corresponding handle.</summary>
        public int Handle { get { return (DeviceId << 8) | ChannelNumber | (Direction == ChannelDirection.Output ? 0x8000 : 0x0000); }}

        public static ChannelSpec FromHandle(int handle)
        {
            ChannelDirection direction = (handle & 0x8000) > 0 ? ChannelDirection.Output : ChannelDirection.Input;
            int deviceId = ((handle & ~0x8000) >> 8) & 0xFF;
            int channelNumber = (handle & ~0x8000) & 0xFF;
            return new(direction, deviceId, "TODO", channelNumber, 9999); 
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
