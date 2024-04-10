using System;
using Ephemera.NBagOfTricks;

namespace Nebulua
{
    #region Definitions
    public class Defs
    {
        /// Only 4/4 time supported.
        public const int BEATS_PER_BAR = 4;

        /// Our resolution = 32nd note. aka midi DeltaTicksPerQuarterNote.
        public const int SUBS_PER_BEAT = 8;

        /// Convenience.
        public const int SUBS_PER_BAR = SUBS_PER_BEAT * BEATS_PER_BAR;

        /// Arbitrary setting.
        public const int MAX_SECTIONS = 32;

        // Midi caps.
        public const int MIDI_VAL_MIN = 0;

        // Midi caps.
        public const int MIDI_VAL_MAX = 127;

        // Midi per device.
        public const int NUM_MIDI_CHANNELS = 16;
    }
    #endregion

    #region Exceptions
    /// <summary>Lua script syntax error.</summary>
    public class SyntaxException : Exception
    {
        public SyntaxException(string message) : base(message) { }
    }

    /// <summary>Api error.</summary>
    public class ApiException : Exception
    {
        public string ApiError { get; init; } = "None";
        public ApiException(string message, string apiError) : base(message) { ApiError = apiError; }
    }

    /// <summary>App command line error.</summary>
    public class ApplicationArgumentException : Exception
    {
        public ApplicationArgumentException(string message) : base(message) { }
    }

    /// <summary>Config file error.</summary>
    public class ConfigException : Exception
    {
        public ConfigException(string message) : base(message) { }
    }
    #endregion

    /// <summary>Manipulate the internal handle format.</summary>
    public class ChannelHandle
    {
        /// <summary>Make a standard output handle.</summary>
        public static int MakeOutHandle(int index, int chan_num) { return (index << 8) | chan_num | 0x8000; }

        /// <summary>Make a standard input handle.</summary>
        public static int MakeInHandle(int index, int chan_num) { return (index << 8) | chan_num; }

        /// <summary>Take apart a standard in/out handle.</summary>
        public static (int index, int chan_num) DeconstructHandle(int chan_hnd) { return (((chan_hnd & ~0x8000) >> 8) & 0xFF, (chan_hnd & ~0x8000) & 0xFF); }
    }

    /// <summary>Misc musical timing functions.</summary>
    public class MusicTime
    {
        /// <summary>Get the bar number.</summary>
        public static int BAR(int tick) { return tick / Defs.SUBS_PER_BAR; }

        /// <summary>Get the beat number in the bar.</summary>
        public static int BEAT(int tick) { return tick / Defs.SUBS_PER_BEAT % Defs.BEATS_PER_BAR; }

        /// <summary>Get the sub in the beat.</summary>
        public static int SUB(int tick) { return tick % Defs.SUBS_PER_BEAT; }

        /// <summary>
        /// Convert a string bar time to absolute position/tick.
        /// </summary>
        /// <param name="sbt">time string can be "1:2:3" or "1:2" or "1".</param>
        /// <returns>Ticks or -1 if invalid input</returns>
        public static int Parse(string sbt)
        {
            int tick = 0;
            var parts = StringUtils.SplitByToken(sbt, ":");

            if (tick >= 0 && parts.Count > 0)
            {
                tick = (int.TryParse(parts[0], out int v) && v >= 0 && v <= 9999) ? tick + v * Defs.SUBS_PER_BAR : -1;
            }

            if (tick >= 0 && parts.Count > 1)
            {
                tick = (int.TryParse(parts[1], out int v) && v >= 0 && v <= Defs.BEATS_PER_BAR - 1) ? tick + v * Defs.SUBS_PER_BEAT : -1;
            }

            if (tick >= 0 && parts.Count > 2)
            {
                tick = (int.TryParse(parts[2], out int v) && v >= 0 && v <= Defs.SUBS_PER_BEAT - 1) ? tick + v : -1;
            }

            return tick;
        }

        /// <summary>
        /// Convert a position/tick to string bar time.
        /// </summary>
        /// <param name="tick"></param>
        /// <returns></returns>
        public static string Format(int tick)
        {
            int bar = BAR(tick);
            int beat = BEAT(tick);
            int sub = SUB(tick);
            var s = $"{bar}:{beat}:{sub}";
            return s;
        }
    }
}
