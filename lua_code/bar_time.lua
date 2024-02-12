-- Musical time handling.

local ut = require("utils")
local sx = require("stringex")
local v = require('validators')


---------------- these match c code! ----------
-- Only 4/4 time supported.
BEATS_PER_BAR = 4
-- Our resolution = 32nd note. aka midi DeltaTicksPerQuarterNote.
SUBBEATS_PER_BEAT = 8
SUBBEATS_PER_BAR = SUBBEATS_PER_BEAT * BEATS_PER_BAR

MAX_BARS = 1000
MAX_TICKS = MAX_BARS * SUBBEATS_PER_BAR

-- TODO1 incr/decr/math/eq etc/


-----------------------------------------------------------------------------
function BT(tick)
    local d = {}
    d.err = nil
    d.tick = tick
    -- Validate.
    d.err = d.err or v.val_integer(d.tick, 0, MAX_TICKS, 'tick')

    -- Init from explicit parts.
    d.from_bar = function(bar, beat, subbeat)
        d.tick = 0 -- default
        d.err = nil
        d.err = d.err or v.val_integer(bar, 0, MAX_BARS, 'bar')
        d.err = d.err or v.val_integer(beat, 0, BEATS_PER_BAR, 'beat')
        d.err = d.err or v.val_integer(subbeat, 0, SUBBEATS_PER_BEAT, 'subbeat')
        if d.err == nil then
            -- print((bar * SUBBEATS_PER_BAR), (beat * SUBBEATS_PER_BEAT), (subbeat))
            d.tick = (bar * SUBBEATS_PER_BAR) + (beat * SUBBEATS_PER_BEAT) + (subbeat)
        end
    end

    -- Parse from string repr.
    d.parse = function(s)
        -- Validate the parts.
        local valid = true

        if type(s) ~= "string" then
            d.err = "Not a string"
            d.tick = 0
            valid = false
        else
            local parts = sx.strsplit(s, ':', false)
            local bar = 0
            local beat = 0
            local subbeat = 0

            if #parts == 2 then
                beat = tonumber(parts[1], 10)
                subbeat = tonumber(parts[2], 10)
            elseif #parts == 3 then
                bar = tonumber(parts[1], 10)
                beat = tonumber(parts[2], 10)
                subbeat = tonumber(parts[3], 10)
            else
                valid = false
            end

            valid = valid and bar ~= nil and beat ~= nil and subbeat ~= nil

            if valid then
                d.from_bar(bar, beat, subbeat)
            else
                d.tick = 0
                d.err = string.format("Invalid time: %s", s)
            end
        end

        return valid
    end

    -- Check returns valid, error string.
    d.is_valid = function()
        return d.err == nil, d.err
    end

    -- Get the tick.
    d.get_tick = function()
        return d.tick
    end

    -- Get the bar number.
    d.get_bar = function()
        return math.floor(d.tick / SUBBEATS_PER_BAR)
    end

    -- Get the beat number in the bar.
    d.get_beat = function()
        return math.floor(d.tick / SUBBEATS_PER_BEAT % BEATS_PER_BAR)
    end

    -- Get the subbeat in the beat.
    d.get_subbeat = function()
        return math.floor(d.tick % SUBBEATS_PER_BEAT)
    end

    -- Readable.
    setmetatable(d,
    {
        __tostring = function(self)
            return self.err or string.format("%d:%d:%d", self.get_bar(), self.get_beat(), self.get_subbeat())
        end
    } )

    return d
end
