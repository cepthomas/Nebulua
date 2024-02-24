-- Musical time handling.

local ut = require("utils")
local sx = require("stringex")
local v = require('validators')
require('neb_common')

-- Forward refs.
local mt


-----------------------------------------------------------------------------
function BT(tick)
    ----------------------------------------
    -- Construct from tick.
    local d = {}
    -- Meta.
    setmetatable(d, mt)
    -- Validate. If the d.err field is not nil that implies it's invalid.
    d.err = v.val_integer(tick, 0, MAX_TICK, 'tick')
    if d.err ~= nil then d.tick = 0 else d.tick = tick end

    ----------------------------------------
    -- Init from explicit parts.
    d.from_bar = function(bar, beat, sub)
        d.tick = 0 -- default
        d.err = nil
        d.err = d.err or v.val_integer(bar, 0, MAX_BAR, 'bar')
        d.err = d.err or v.val_integer(beat, 0, BEATS_PER_BAR, 'beat')
        d.err = d.err or v.val_integer(sub, 0, SUBS_PER_BEAT, 'sub')
        if d.err == nil then
            d.tick = (bar * SUBS_PER_BAR) + (beat * SUBS_PER_BEAT) + (sub)
        end
    end

    ----------------------------------------
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
                d.from_bar(bar, beat, sub)
            else
                d.tick = 0
                d.err = string.format("Invalid time: %s", s)
            end
        end

        return valid
    end

    ----------------------------------------
    -- Get the error (maybe).
    d.get_err = function()
        return d.err
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
--- Report a user script syntax error. Calls error().
-- @param info
local function syntax_error(desc, info)
    s = string.format("Syntax error: %s %s", desc, info or "")
    error(s, 4)
end


-----------------------------------------------------------------------------
-- Sanity check the metamethod args and return two ints. Only table and integer supported.
-- Lua guarantees that at least one of the args is the table but order is not determined.
-- Except eq works only for two identical types.
local sanitize_operands = function(a, b, op)
    asan = nil
    bsan = nil
    err = nil

    if ut.is_integer(a) then
        if b.err == nil then
            asan = a
            bsan = b.tick
        else
            syntax_error(string.format("Bad operand for %s operator", op), b.err)
        end
    elseif ut.is_integer(b) then
        if a.err == nil then
            asan = a.tick
            bsan = b
        else
            syntax_error(string.format("Bad operand for %s operator", op), a.err)
        end
    elseif getmetatable(a) == getmetatable(b) then
        asan = a.tick
        bsan = b.tick
    else
        syntax_error(string.format("Invalid datatype for %s operator", op))
    end
    return asan, bsan, err
end

-- Static metatable.
mt =
{
    __tostring = function(self) return self.err or string.format("%d:%d:%d", self.get_bar(), self.get_beat(), self.get_sub()) end,
    __add = function(a, b) sana, sanb = sanitize_operands(a, b, 'add'); return BT(sana + sanb) end,
    __add = function(a, b) sana, sanb = sanitize_operands(a, b, 'add'); return BT(sana + sanb) end,
    __sub = function(a, b) sana, sanb = sanitize_operands(a, b, 'sub'); return BT(sana - sanb) end,
    __eq = function(a, b) return a.tick == b.tick end,
    __lt = function(a, b) sana, sanb = sanitize_operands(a, b, 'lt'); return sana < sanb end,
    __le = function(a, b) sana, sanb = sanitize_operands(a, b, 'le'); return sana <= sanb end,
}
