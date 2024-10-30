-- Musical time handling.

local ut  = require("lbot_utils")
local sx  = require("stringex")


local M = {}

-- TODO1 new style:

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



-----------------------------------------------------------------------------
-------------------------- OLD STYLE ----------------------------------------
-----------------------------------------------------------------------------

-- Forward refs.
local mt


-----------------------------------------------------------------------------
-- Construction. Can call error().
-- @param overloads
--  - (tick)
--  - (bar, beat, sub)
--  - ("1.2.3")
-- @param sections table user section specs
-- @return object or nil, err if invalid
function BarTime(arg1, arg2, arg3)
    local d = { tick = 0 } -- default
    local err = nil
    -- Meta.
    setmetatable(d, mt)

    -- Determine flavor.
    if ut.is_integer(arg1) and arg2 == nil and arg3 == nil then
        -- From ticks.
        local e = ut.val_integer(arg1, 0, M.MAX_TICK, 'tick')
        if e == nil then
            d.tick = arg1
        else
            err = string.format("Bad constructor: %s", e)
        end
    elseif ut.is_integer(arg1) and ut.is_integer(arg2) and ut.is_integer(arg3) then
        -- From bar/beat/sub.
        local e = ut.val_integer(arg1, 0, M.MAX_BAR, 'bar')
        e = e or ut.val_integer(arg2, 0, M.BEATS_PER_BAR, 'beat')
        e = e or ut.val_integer(arg3, 0, M.SUBS_PER_BEAT, 'sub')
        if e == nil then
            d.tick = (arg1 * M.SUBS_PER_BAR) + (arg2 * M.SUBS_PER_BEAT) + (arg3)
        else
            err = string.format("Bad constructor: %s", e)
        end
    elseif ut.is_string(arg1) and arg2 == nil and arg3 == nil then
        -- Parse from string.
        local valid = true
        local parts = sx.strsplit(arg1, '.', false)
        local bar
        local beat
        local sub

        if #parts == 2 then
            bar = 0
            beat = ut.to_integer(parts[1])
            sub = ut.to_integer(parts[2])
        elseif #parts == 3 then
            bar = ut.to_integer(parts[1])
            beat = ut.to_integer(parts[2])
            sub = ut.to_integer(parts[3])
        else
            valid = false
        end

        valid = valid and bar ~= nil and beat ~= nil and sub ~= nil
        if valid then
            d.tick = (bar * M.SUBS_PER_BAR) + (beat * M.SUBS_PER_BEAT) + (sub)
        else
            err = string.format("Invalid bar time: %s", tostring(arg1))
        end
    else
        err = string.format("Bad constructor: %s, %s, %s", tostring(arg1), tostring(arg2), tostring(arg3))
    end

    ----------------------------------------
    -- Get the tick.
    d.get_tick = function()
        return d.tick
    end

    ----------------------------------------
    -- Get the bar number.
    d.get_bar = function()
        return math.floor(d.tick / M.SUBS_PER_BAR)
    end

    ----------------------------------------
    -- Get the beat number in the bar.
    d.get_beat = function()
        return math.floor(d.tick / M.SUBS_PER_BEAT % M.BEATS_PER_BAR)
    end

    ----------------------------------------
    -- Get the sub in the beat.
    d.get_sub = function()
        return math.floor(d.tick % M.SUBS_PER_BEAT)
    end

    -- Return success/fail.
    if err ~= nil then
        d = nil
        error(err, 3)
    end

    return d, err
end


-----------------------------------------------------------------------------
-- Sanity check the metamethod args and return two ints.
local normalize_operands = function(a, b, op)
    local asan = nil
    local bsan = nil
    -- local ERR_LEVEL = 4

    if a == nil then
        error("Operand 1 is nil", 4)
    elseif b == nil then
        error("Operand 2 is nil", 4)
    elseif ut.is_integer(a) then
        asan = a
        bsan = b.tick
    elseif ut.is_integer(b) then
        asan = a.tick
        bsan = b
    elseif getmetatable(a) == getmetatable(b) then
        asan = a.tick
        bsan = b.tick
    else
        error(string.format("Invalid data type for operator %s", op), 4)
    end

    return asan, bsan
end


-----------------------------------------------------------------------------
-- Static metatable. Only table and integer supported.
-- Lua guarantees that at least one of the args is the table but order is not determined.
-- Except eq works only for two identical types.
mt =
{
    __tostring = function(self)
            return string.format("%d.%d.%d", self.get_bar(), self.get_beat(), self.get_sub())
        end,

    __eq = function(a, b)
            return a.tick == b.tick
        end,

    __add = function(a, b)
        local sana, sanb = normalize_operands(a, b, 'add')
            local ret = nil
            if sana ~= nil and sanb ~= nil then ret = BarTime(sana + sanb) end
            return ret
        end,

    __sub = function(a, b)
            local ret = nil
            local sana, sanb = normalize_operands(a, b, 'sub')
            if sana ~= nil and sanb ~= nil and sana >= sanb then
                ret = BarTime(sana - sanb)
            else
                error("result is negative", 3)
            end
            return ret
        end,

    __lt = function(a, b)
            local ret = nil
            local sana, sanb = normalize_operands(a, b, 'lt')
            if sana ~= nil and sanb ~= nil
                then ret = sana < sanb
            else
                error("attempt to compare incompatible operands", 3)
            end
            return ret
        end,

    __le = function(a, b)
            local ret = nil
            local sana, sanb = normalize_operands(a, b, 'le')
            if sana ~= nil and sanb ~= nil
                then ret = sana <= sanb
            else
                error("attempt to compare incompatible operands", 3)
            end
            return ret
        end,
}


-- Return the module.
return M
