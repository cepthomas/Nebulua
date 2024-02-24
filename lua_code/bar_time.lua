-- Musical time handling.

local ut = require("utils")
local sx = require("stringex")
local v = require('validators')
require('neb_common')

-- Forward refs.
local mt


-----------------------------------------------------------------------------
-- Construction. Can call error().
-- @param overloads
--  - (tick)
--  - (bar, beat, sub)
--  - ("1:2:3")
-- @param sections table user section specs
-- @return object or nil if invalid
function BT(arg1, arg2, arg3)

    local d = {}
    d.tick = 0 -- default
    -- Meta.
    setmetatable(d, mt)
    local err_level = 4

    -- Determine flavor.
    if ut.is_integer(arg1) and arg2 == nil and arg3 == nil then
        -- From ticks.
        err = v.val_integer(arg1, 0, MAX_TICK, 'tick')
        if err == nil then
            d.tick = arg1
        else
            d = nil
            syntax_error(err_level, "Bad constructor", err)
        end
    elseif ut.is_integer(arg1) and ut.is_integer(arg2) and ut.is_integer(arg3) then
        -- From bar/beat/sub.
        err = v.val_integer(arg1, 0, MAX_BAR, 'bar')
        if err == nil then err = v.val_integer(arg2, 0, BEATS_PER_BAR, 'beat') end
        if err == nil then err = v.val_integer(arg3, 0, SUBS_PER_BEAT, 'sub') end
        if err == nil then
            d.tick = (arg1 * SUBS_PER_BAR) + (arg2 * SUBS_PER_BEAT) + (arg3)
        else
            d = nil
            syntax_error(err_level, "Bad constructor", err)
        end
    elseif ut.is_string(arg1) and arg2 == nil and arg3 == nil then
        -- Parse from string.
        local valid = true
        local parts = sx.strsplit(arg1, ':', false)
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
-- print(">>>", valid, d.tick, bar, beat, sub)

        if valid then
            d.tick = (bar * SUBS_PER_BAR) + (beat * SUBS_PER_BEAT) + (sub)
        else
            d = nil
            syntax_error(err_level, "Invalid time", arg1)
        end
-- print(">>>", valid, d.tick, bar, beat, sub)
    else
        s = string.format("%s, %s, %s", tostring(arg1), tostring(arg2), tostring(arg3))
        syntax_error(err_level, "Invalid constructor", s)
        d = nil
    end

    ----------------------------------------
    -- Get the tick.
    d.get_tick = function()
        return d.tick
    end

    ----------------------------------------
    -- Get the bar number.
    d.get_bar = function()
        return math.floor(d.tick / SUBS_PER_BAR)
    end

    ----------------------------------------
    -- Get the beat number in the bar.
    d.get_beat = function()
        return math.floor(d.tick / SUBS_PER_BEAT % BEATS_PER_BAR)
    end

    ----------------------------------------
    -- Get the sub in the beat.
    d.get_sub = function()
        return math.floor(d.tick % SUBS_PER_BEAT)
    end

    return d
end


-----------------------------------------------------------------------------
-- Sanity check the metamethod args and return two ints.
local normalize_operands = function(a, b, op)
    asan = nil
    bsan = nil
    local err_level = 5

    if a == nil then
        syntax_error(err_level, "Operand 1 is nil", op)
    elseif b == nil then
        syntax_error(err_level, "Operand 2 is nil", op)
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
        syntax_error(err_level, "Invalid datatype for operator", op)
    end

    return asan, bsan
end


-- Static metatable. Only table and integer supported.
-- Lua guarantees that at least one of the args is the table but order is not determined.
-- Except eq works only for two identical types.
mt =
{
    __tostring = function(self) return string.format("%d:%d:%d", self.get_bar(), self.get_beat(), self.get_sub()) end,
    __eq = function(a, b) return a.tick == b.tick end,
    __add = function(a, b)
        sana, sanb = normalize_operands(a, b, 'add')
            ret = nil
            if sana ~= nil and sanb ~= nil then ret = BT(sana + sanb) end
            return ret
        end,
    __sub = function(a, b)
            sana, sanb = normalize_operands(a, b, 'sub')
            ret = nil
            if sana ~= nil and sanb ~= nil and sana >= sanb then ret = BT(sana - sanb) end
            return ret
        end,
    __lt = function(a, b)
            sana, sanb = normalize_operands(a, b, 'lt')
            ret = nil
            if sana ~= nil and sanb ~= nil
                then ret = sana < sanb
            else
                error("attempt to compare incompatble operands")
            end
            return ret
        end,
    __le = function(a, b)
            sana, sanb = normalize_operands(a, b, 'le')
            ret = nil
            if sana ~= nil and sanb ~= nil
                then ret = sana <= sanb
            else
                error("attempt to compare incompatble operands")
            end
            return ret
        end,
}
