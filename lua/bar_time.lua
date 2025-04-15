-- Musical time handling.

local ut  = require("lbot_utils")
local lt  = require("lbot_types")
local sx  = require("stringex")


local M = {}


-------------------- Definitions - must match C code! --------------
-- Only 4/4 time supported.
M.BEATS_PER_BAR = 4

-- Our resolution = 32nd note. aka midi DeltaTicksPerQuarterNote.
M.SUBS_PER_BEAT = 8
M.SUBS_PER_BAR = M.SUBS_PER_BEAT * M.BEATS_PER_BAR

M.MAX_BAR = 1000
M.MAX_BEAT = M.MAX_BAR * M.BEATS_PER_BAR
M.MAX_TICK = M.MAX_BAR * M.SUBS_PER_BAR


-----------------------------------------------------------------------------
--- Convert proper components to tick.
-- returns tick or nil
function M.bt_to_tick(bar, beat, sub)
    lt.val_integer(bar, 0, M.MAX_BAR)
    lt.val_integer(beat, 0, M.BEATS_PER_BAR-1)
    lt.val_integer(sub, 0, M.SUBS_PER_BEAT-1)

    local tick = bar * M.SUBS_PER_BAR + beat * M.SUBS_PER_BEAT + sub
    return tick
end

-----------------------------------------------------------------------------
--- Convert total beats and subs to tick.
-- returns tick or nil
function M.beats_to_tick(beats, sub)
    lt.val_integer(beats, 0, M.MAX_BEAT-1)
    lt.val_integer(sub, 0, M.SUBS_PER_BEAT-1)

    local tick = beats * M.SUBS_PER_BEAT + sub
    return tick
end

-----------------------------------------------------------------------------
--- Convert string representation to tick.
-- returns tick or nil
function M.str_to_tick(str)
    lt.val_string(str)
    local tick = nil

    local parts = sx.strsplit(str, '.', false)
    if #parts == 2 then
        -- Duration form.
        local beat = ut.tointeger(parts[1])
        local sub = ut.tointeger(parts[2])
        tick = M.beats_to_tick(beat, sub)
    elseif #parts == 3 then
        -- Absolute form.
        local bar = ut.tointeger(parts[1])
        local beat = ut.tointeger(parts[2])
        local sub = ut.tointeger(parts[3])
        tick = M.bt_to_tick(bar, beat, sub)
    end

    return tick
end

-----------------------------------------------------------------------------
--- Convert tick to components.
-- returns bar,beat,sub or nil
function M.tick_to_bt(tick)
    lt.val_integer(tick, 0, M.MAX_TICK)
    local bar = math.floor(tick / M.SUBS_PER_BAR)
    local beat = math.floor(tick / M.SUBS_PER_BEAT % M.BEATS_PER_BAR)
    local sub = math.floor(tick % M.SUBS_PER_BEAT)
    return bar, beat, sub
end

-----------------------------------------------------------------------------
--- Convert tick to string representation
-- returns string or nil
function M.tick_to_str(tick)
    lt.val_integer(tick, 0, M.MAX_TICK)
    local bar, beat, sub = M.tick_to_bt(tick)
    return string.format("%d.%d.%d", bar, beat, sub)
end


-- Return the module.
return M
