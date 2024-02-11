-- Class for time handling.

local ut = require("utils")
local v = require('validators')
require('class')


-- STEP_TYPE = { NONE = 0, NOTE = 1, CONTROLLER = 2, FUNCTION = 3 }


-- #define BEATS_PER_BAR 4

-- // /// Internal/app resolution aka DeltaTicksPerQuarterNote.
-- // #define INTERNAL_PPQ 8

-- #define SUBBEATS_PER_BEAT 8

-- /// Convenience.
-- #define SUBBEATS_PER_BAR (SUBBEATS_PER_BEAT * BEATS_PER_BAR)

-- /// Total.
-- #define TOTAL_BEATS(tick) (tick / SUBBEATS_PER_BEAT)



---------------- these match c code! ----------
-- /// Only 4/4 time supported.
BEATS_PER_BAR = 4
-- /// Our resolution = 32nd note. aka midi DeltaTicksPerQuarterNote.
SUBBEATS_PER_BEAT = 8
SUBBEATS_PER_BAR = SUBBEATS_PER_BEAT * BEATS_PER_BAR




-----------------------------------------------------------------------------
-- base class
BT = class(
    function(t, tick)
        t.tick = 0 -- default
        t.err = v.val_integer(tick, 0, 999999, 'tick')
        if t.err == nil then
            t.tick = tick
        end
    end)

-- Init from bars.
function BT:from_bar(bar, beat, subbeat)
    self.tick = 0 -- default
    self.err = nil
    self.err = self.err or v.val_integer(bar, 0, 9999, 'bar')
    self.err = self.err or v.val_integer(beat, 0, BEATS_PER_BAR, 'beat')
    self.err = self.err or v.val_integer(subbeat, 0, SUBBEATS_PER_BAR, 'subbeat')
    if self.err == nil then
        self.tick = bar * SUBBEATS_PER_BAR + beat * SUBBEATS_PER_BEAT + subbeat
    end
end


-- Parse from string repr. TODO1
function BT:parse(s)
--     int tick = 0;
--     bool valid = false;
--     int v;

--     // Make writable copy and tokenize it.
--     char cp[32];
--     strncpy(cp, s, sizeof(cp));

--     char* tok = strtok(cp, ".");
--     if (tok != NULL)
--     {
--         valid = nebcommon_ParseInt(tok, &v, 0, 9999);
--         if (!valid) goto nogood;
--         tick += v * SUBBEATS_PER_BAR;
--     }

--     tok = strtok(NULL, ".");
--     if (tok != NULL)
--     {
--         valid = nebcommon_ParseInt(tok, &v, 0, BEATS_PER_BAR-1);
--         if (!valid) goto nogood;
--         tick += v * SUBBEATS_PER_BEAT;
--     }

--     tok = strtok(NULL, ".");
--     if (tok != NULL)
--     {
--         valid = nebcommon_ParseInt(tok, &v, 0, SUBBEATS_PER_BEAT-1);
--         if (!valid) goto nogood;
--         tick += v;
--     }
end

-- Get the tick.
-- function BT:get_tick()
--     return self.tick
-- end

-- Get the bar number.
function BT:get_bar()
    return self.tick / SUBBEATS_PER_BAR
end

-- Get the beat number in the bar.
function BT:get_beat()
    return self.tick / SUBBEATS_PER_BEAT % BEATS_PER_BAR
end

-- Get the subbeat in the beat.
function BT:get_subbeat()
    return self.tick % SUBBEATS_PER_BEAT
end

function BT:__tostring()
    return self.err or string.format("%d.%d.%d", self.get_bar(), self.get_beat(), self.get_subbeat)
end
