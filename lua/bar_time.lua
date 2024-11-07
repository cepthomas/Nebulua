-- Musical time handling.

local ut  = require("lbot_utils")
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

    local valid = true

    valid = valid and ut.val_integer(bar, 0, M.MAX_BAR)
    valid = valid and ut.val_integer(beat, 0, M.BEATS_PER_BAR-1)
    valid = valid and ut.val_integer(sub, 0, M.SUBS_PER_BEAT-1)

    if not valid then error(string.format('Invalid bartime %s %s %s', bar, beat, sub)) end

    if valid then
        local tick = bar * M.SUBS_PER_BAR + beat * M.SUBS_PER_BEAT + sub
        return tick
    else
        return nil
    end
end

-----------------------------------------------------------------------------
--- Convert total beats and subs to tick.
-- returns tick or nil
function M.beats_to_tick(beats, sub)

    local valid = true

    valid = valid and ut.val_integer(beats, 0, M.MAX_BEAT-1)
    valid = valid and ut.val_integer(sub, 0, M.SUBS_PER_BEAT-1)

    if not valid then error(string.format('Invalid bartime %s %s', beats, sub)) end

    if valid then
        local tick = beats * M.SUBS_PER_BEAT + sub
        return tick
    else
        return nil
    end
end

-----------------------------------------------------------------------------
--- Convert string representation to tick.
-- returns tick or nil
function M.str_to_tick(str)

    local tick = nil

    if str ~= nil and type(str) == 'string' then
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
    end

    if tick == nil then error(string.format('Invalid bartime %s', str)) end

    return tick
end

-----------------------------------------------------------------------------
--- Convert tick to components.
-- returns bar,beat,sub or nil
function M.tick_to_bt(tick)

    local valid = ut.val_integer(tick, 0, M.MAX_TICK)

    if valid then
        local bar = math.floor(tick / M.SUBS_PER_BAR)
        local beat = math.floor(tick / M.SUBS_PER_BEAT % M.BEATS_PER_BAR)
        local sub = math.floor(tick % M.SUBS_PER_BEAT)
        return bar, beat, sub
    else
        error('Invalid tick '..tick)
    end
end

-----------------------------------------------------------------------------
--- Convert tick to string representation
-- returns string or nil
function M.tick_to_str(tick)

    local valid = ut.val_integer(tick, 0, M.MAX_TICK)

    if valid then
        local bar, beat, sub = M.tick_to_bt(tick)
        return string.format("%d.%d.%d", bar, beat, sub)
    else
        error('Invalid tick '..tick)
    end
end


-- Return the module.
return M
