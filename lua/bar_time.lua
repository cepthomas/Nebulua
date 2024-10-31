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
--- Convert proper components to internal.
function M.bt_to_tick(bar, beat, sub)

    local e = ut.val_integer(bar, 0, M.MAX_BAR, 'bar')
    if e ~= nil then error("Invalid bar", 3) end

    e = ut.val_integer(beat, 0, M.BEATS_PER_BAR-1, 'beat')
    if e ~= nil then error("Invalid beat", 3) end

    e = ut.val_integer(sub, 0, M.SUBS_PER_BEAT-1, 'sub')
    if e ~= nil then error("Invalid sub", 3) end

    tick = bar * M.SUBS_PER_BAR + beat * M.SUBS_PER_BEAT + sub
    return tick
end

-----------------------------------------------------------------------------
--- Convert total beats and subs to internal.
function M.beats_to_tick(beat, sub)

    e = ut.val_integer(beat, 0, M.MAX_BEAT-1, 'beat')
    if e ~= nil then error("Invalid beat", 3) end

    e = ut.val_integer(sub, 0, M.SUBS_PER_BEAT-1, 'sub')
    if e ~= nil then error("Invalid sub", 3) end

    tick = beat * M.SUBS_PER_BEAT + sub
    return tick
end

-----------------------------------------------------------------------------
--- Convert string representation to internal.
function M.str_to_tick(str)

    local tick = -1

    if ut.is_string(str) then
        local parts = sx.strsplit(str, '.', false)

        if #parts == 2 then
            -- Duration form.
            local beat = ut.to_integer(parts[1])
            local sub = ut.to_integer(parts[2])
            tick = M.beats_to_tick(beat, sub)

        elseif #parts == 3 then
            -- Absolute form.
            local bar = ut.to_integer(parts[1])
            local beat = ut.to_integer(parts[2])
            local sub = ut.to_integer(parts[3])
            tick = M.bt_to_tick(bar, beat, sub)
        end
    end

    if tick < 0 then
        error("Invalid bar time: "..tostring(str), 3)
    end

    return tick
end

-----------------------------------------------------------------------------
--- Convert tick to bar, beat, sub.
function M.tick_to_bt(tick)
    e = ut.val_integer(tick, 0, M.MAX_TICK, 'tick')
    if e ~= nil then
        error("Invalid tick", 3)
    else
        bar = math.floor(tick / M.SUBS_PER_BAR)
        beat = math.floor(tick / M.SUBS_PER_BEAT % M.BEATS_PER_BAR)
        sub = math.floor(tick % M.SUBS_PER_BEAT)
        return bar, beat, sub
    end
end

-----------------------------------------------------------------------------
--- Convert tick to string representation
function M.tick_to_str(tick)
    -- return like '1.2.3'
    e = ut.val_integer(tick, 0, M.MAX_TICK, 'tick')
    if e ~= nil then
        error("Invalid tick", 3)
    else
        bar, beat, sub = M.tick_to_bt(tick)
        return string.format("%d.%d.%d", bar, beat, sub)
    end
end


-- Return the module.
return M
