
using Ephemera.NBagOfTricks;

namespace Nebulua
{
    // TODO1 this is a lot of dupe from cpp side. Import/share?

    #region Enums

    /// <summary>Internal status.</summary>
    public enum PlayState { Start, Stop, Rewind, StopRewind }

    public enum midi_event_t
    {
        // Channel events 0x80-0x8F
        MIDI_NOTE_OFF = 0x80,               // 2 - 1 byte pitch, followed by 1 byte velocity
        MIDI_NOTE_ON = 0x90,                // 2 - 1 byte pitch, followed by 1 byte velocity
        MIDI_KEY_AFTER_TOUCH = 0xA0,        // 2 - 1 byte pitch, 1 byte pressure (after-touch)
        MIDI_CONTROL_CHANGE = 0xB0,         // 2 - 1 byte parameter number, 1 byte setting
        MIDI_PATCH_CHANGE = 0xC0,           // 1 byte program selected
        MIDI_CHANNEL_AFTER_TOUCH = 0xD0,    // 1 byte channel pressure (after-touch)
        MIDI_PITCH_WHEEL_CHANGE = 0xE0,     // 2 bytes gives a 14 bit value, least significant 7 bits first
        // System events - no channel.
        MIDI_SYSEX = 0xF0,
        MIDI_EOX = 0xF7,
        MIDI_TIMING_CLOCK = 0xF8,
        MIDI_START_SEQUENCE = 0xFA,
        MIDI_CONTINUE_SEQUENCE = 0xFB,
        MIDI_STOP_SEQUENCE = 0xFC,
        MIDI_AUTO_SENSING = 0xFE,
        MIDI_META_EVENT = 0xFF,
    } 
    #endregion

    #region Definitions
    public class Defs
    {
        ///// App errors start after internal lua errors so they can be handled harmoniously.
        public const int NEB_OK                =   0;  // synonym for LUA_OK and CBOT_ERR_NO_ERR
        public const int NEB_ERR_INTERNAL      =  10;
        public const int NEB_ERR_BAD_CLI_ARG   =  11;
        public const int NEB_ERR_BAD_LUA_ARG   =  12;
        public const int NEB_ERR_BAD_MIDI_CFG  =  13;
        public const int NEB_ERR_SYNTAX        =  14;
        public const int NEB_ERR_MIDI_TX       =  15;
        public const int NEB_ERR_MIDI_RX       =  16;
        public const int NEB_ERR_API           =  17;

        /// Only 4/4 time supported.
        public const int BEATS_PER_BAR = 4;

        /// Our resolution = 32nd note. aka midi DeltaTicksPerQuarterNote.
        public const int SUBS_PER_BEAT = 8;

        /// Convenience.
        public const int SUBS_PER_BAR = (SUBS_PER_BEAT * BEATS_PER_BAR);

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

    public class Utils
    {
        // TODO2 Script lua_State access syncronization. 
        // HANDLE ghMutex; 
        // #define ENTER_CRITICAL_SECTION WaitForSingleObject(ghMutex, INFINITE)
        // #define EXIT_CRITICAL_SECTION ReleaseMutex(ghMutex)
        public static void ENTER_CRITICAL_SECTION() { }

        public static void EXIT_CRITICAL_SECTION() { }


        ///// Channel handle management.
        public static int MAKE_OUT_HANDLE(int index, int chan_num) { return (index << 8) | chan_num | 0x8000; }

        public static int MAKE_IN_HANDLE(int index, int chan_num) { return (index << 8) | chan_num; }

        public static (int index, int chan_num) SPLIT_HANDLE(int chan_hnd) { return (((chan_hnd & ~0x8000) >> 8) & 0xFF, (chan_hnd & ~0x8000) & 0xFF); }


        ///// Musical timing

        /// The bar number.
        public static int BAR(int tick) { return tick / Defs.SUBS_PER_BAR; }

        /// The beat number in the bar.
        public static int BEAT(int tick) { return tick / Defs.SUBS_PER_BEAT % Defs.BEATS_PER_BAR; }

        /// The sub in the beat.
        public static int SUB(int tick) { return tick % Defs.SUBS_PER_BEAT; }

        /// <summary>
        /// Convert a string bar time to absolute position.
        /// </summary>
        /// <param name="sbt">time string can be "1.2.3" or "1.2" or "1".</param>
        /// <returns>Ticks or -1 if invalid input</returns>
        public static int ParseBarTime(string sbt)
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
        /// Convert a position to string bar time.
        /// </summary>
        /// <param name="tick"></param>
        /// <returns></returns>
        public static string FormatBarTime(int tick)
        {
            int bar = BAR(tick);
            int beat = BEAT(tick);
            int sub = SUB(tick);
            var s = $"{bar}:{beat}:{sub}";
            return s;
        }
    }
}
