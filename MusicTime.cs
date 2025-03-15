using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Ephemera.NBagOfTricks;


namespace Nebulua
{
    /// <summary>Misc musical timing functions.</summary>
    public class MusicTime
    {
        /// <summary>Only 4/4 time supported.</summary>
        public const int BEATS_PER_BAR = 4;

        /// <summary>GOur resolution = 32nd note. aka midi DeltaTicksPerQuarterNote.</summary>
        public const int SUBS_PER_BEAT = 8;

        /// <summary>Convenience.</summary>
        public const int SUBS_PER_BAR = SUBS_PER_BEAT * BEATS_PER_BAR;

        /// <summary>Get the bar number.</summary>
        public static int BAR(int tick) { return tick / SUBS_PER_BAR; }

        /// <summary>Get the beat number in the bar.</summary>
        public static int BEAT(int tick) { return tick / SUBS_PER_BEAT % BEATS_PER_BAR; }

        /// <summary>Get the sub in the beat.</summary>
        public static int SUB(int tick) { return tick % SUBS_PER_BEAT; }

        /// <summary>
        /// Convert a string bar time to absolute position/tick.
        /// </summary>
        /// <param name="sbt">time string can be "1.2.3" or "1.2" or "1".</param>
        /// <returns>Ticks or -1 if invalid input</returns>
        public static int Parse(string sbt)
        {
            int tick = 0;
            var parts = StringUtils.SplitByToken(sbt, ".");

            if (tick >= 0 && parts.Count > 0)
            {
                tick = (int.TryParse(parts[0], out int v) && v >= 0 && v <= 9999) ? tick + v * SUBS_PER_BAR : -1;
            }

            if (tick >= 0 && parts.Count > 1)
            {
                tick = (int.TryParse(parts[1], out int v) && v >= 0 && v <= BEATS_PER_BAR - 1) ? tick + v * SUBS_PER_BEAT : -1;
            }

            if (tick >= 0 && parts.Count > 2)
            {
                tick = (int.TryParse(parts[2], out int v) && v >= 0 && v <= SUBS_PER_BEAT - 1) ? tick + v : -1;
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
            if (tick >= 0)
            {
                int bar = BAR(tick);
                int beat = BEAT(tick);
                int sub = SUB(tick);
                return $"{bar}.{beat}.{sub}";
            }
            else
            {
                return "Invalid";
            }
        }
    }
}
