-- Family of midi events.

local ut = require('lbot_utils')
local mid = require('midi_defs')
local bt = require('bar_time')


-- TODO1 new style:

local M = {}

-- Forward ref.
local format_chan_hnd


-----------------------------------------------------------------------------
function M.note(tick, chan_hnd, note_num, volume, duration)
    local d = {}
    d.step_type = "note"
    d.err = nil
    d.tick = tick
    d.chan_hnd = chan_hnd
    d.note_num = note_num
    d.volume = volume
    d.duration = duration

    -- Validate.
    d.err = d.err or ut.val_integer(d.tick, 0, bt.MAX_TICK, 'tick')
    d.err = d.err or ut.val_integer(d.chan_hnd, 0, 0xFFFF, 'chan_hnd')
    d.err = d.err or ut.val_integer(d.note_num, 0, mid.MAX_MIDI, 'note_num')
    d.err = d.err or ut.val_number(d.volume, 0.0, 1.0, 'volume')
    d.err = d.err or ut.val_integer(d.duration, 0, bt.MAX_TICK, 'duration')

    d._format = function() return d.err or
        string.format('%05d %s %s NOTE:%d VOL:%.1f DUR:%d',
            d.tick, bt.tick_to_str(d.tick), format_chan_hnd(d.chan_hnd), d.note_num, d.volume, d.duration)
    end

    setmetatable(d, { __tostring = function(self) return self._format() end })

    return d
end

-----------------------------------------------------------------------------
function M.controller(tick, chan_hnd, controller, value)

    local d = {}
    d.step_type = "controller"
    d.err = nil
    d.tick = tick
    d.chan_hnd = chan_hnd
    d.controller = controller
    d.value = value

    -- Validate.
    d.err = d.err or ut.val_integer(d.tick, 0, bt.MAX_TICK, 'tick')
    d.err = d.err or ut.val_integer(d.chan_hnd, 0, 0xFFFF, 'chan_hnd')
    d.err = d.err or ut.val_integer(d.controller, 0, mid.MAX_MIDI, 'controller')
    d.err = d.err or ut.val_integer(d.value, 0, mid.MAX_MIDI, 'value')

    d._format = function() return d.err or
        string.format('%05d %s %s CTRL:%d VAL:%d',
            d.tick, bt.tick_to_str(d.tick), format_chan_hnd(d.chan_hnd), d.controller, d.value)
    end

    setmetatable(d, { __tostring = function(self) return self._format() end })

    return d
end

-----------------------------------------------------------------------------
function M.func(tick, chan_hnd, func, volume)

    local d = {}
    d.step_type = "function"
    d.err = nil
    d.tick = tick
    d.chan_hnd = chan_hnd
    d.func = func
    d.volume = volume

    -- Validate.
    d.err = d.err or ut.val_integer(d.tick, 0, bt.MAX_TICK, 'tick')
    d.err = d.err or ut.val_integer(d.chan_hnd, 0, 0xFFFF, 'chan_hnd')
    d.err = d.err or ut.val_function(d.func, 'func')
    d.err = d.err or ut.val_number(d.volume, 0.0, 1.0, 'volume')

    d._format = function() return d.err or
        string.format('%05d %s %s FUNC:? VOL:%.1f',
            d.tick, bt.tick_to_str(d.tick), format_chan_hnd(d.chan_hnd), d.volume)
    end

    setmetatable(d, { __tostring = function(self) return self._format() end })

    return d
end


-----------------------------------------------------------------------------
format_chan_hnd = function(chan_hnd)
    local s = string.format("DEV:%02d CH:%02d", (chan_hnd >> 8) & 0xFF, chan_hnd & 0xFF)
    return s
end


-----------------------------------------------------------------------------
-------------------------- OLD STYLE ----------------------------------------
-----------------------------------------------------------------------------


-----------------------------------------------------------------------------
function StepNote(tick, chan_hnd, note_num, volume, duration)
    local d = {}
    d.step_type = "note"
    d.err = nil
    d.tick = tick
    d.chan_hnd = chan_hnd
    d.note_num = note_num
    d.volume = volume
    d.duration = duration

    -- Validate.
    d.err = d.err or ut.val_integer(d.tick, 0, bt.MAX_TICK, 'tick')
    d.err = d.err or ut.val_integer(d.chan_hnd, 0, 0xFFFF, 'chan_hnd')
    d.err = d.err or ut.val_integer(d.note_num, 0, mid.MAX_MIDI, 'note_num')
    d.err = d.err or ut.val_number(d.volume, 0.0, 1.0, 'volume')
    d.err = d.err or ut.val_integer(d.duration, 0, bt.MAX_TICK, 'duration')

    -- if d.err ~= nil then
    --     d.err = string.format("Invalid note: %s", d.err)
    -- end

    d.format = function() return d.err or
        string.format('%05d %s %s NOTE:%d VOL:%.1f DUR:%d',
            d.tick, tostring(BarTime(d.tick)), format_chan_hnd(d.chan_hnd), d.note_num, d.volume, d.duration)
    end
     -- setmetatable(d, { __tostring = function(self) self.format() end })

    return d
end


-----------------------------------------------------------------------------
function StepController(tick, chan_hnd, controller, value)
    local d = {}
    d.step_type = "controller"
    d.err = nil
    d.tick = tick
    d.chan_hnd = chan_hnd
    d.controller = controller
    d.value = value

    -- Validate.
    d.err = d.err or ut.val_integer(d.tick, 0, bt.MAX_TICK, 'tick')
    d.err = d.err or ut.val_integer(d.chan_hnd, 0, 0xFFFF, 'chan_hnd')
    d.err = d.err or ut.val_integer(d.controller, 0, mid.MAX_MIDI, 'controller')
    d.err = d.err or ut.val_integer(d.value, 0, mid.MAX_MIDI, 'value')

    -- if d.err ~= nil then
    --     d.err = string.format("Invalid controller: %s", d.err)
    -- end

    d.format = function() return d.err or
        string.format('%05d %s %s CTRL:%d VAL:%d',
            d.tick, tostring(BarTime(d.tick)), format_chan_hnd(d.chan_hnd), d.controller, d.value)
    end
    -- setmetatable(d, { __tostring = function(self) self.format() end })

    return d
end


-----------------------------------------------------------------------------
function StepFunction(tick, chan_hnd, func, volume)
    local d = {}
    d.step_type = "function"
    d.err = nil
    d.tick = tick
    d.chan_hnd = chan_hnd
    d.func = func
    d.volume = volume

    -- Validate.
    d.err = d.err or ut.val_integer(d.tick, 0, bt.MAX_TICK, 'tick')
    d.err = d.err or ut.val_integer(d.chan_hnd, 0, 0xFFFF, 'chan_hnd')
    d.err = d.err or ut.val_function(d.func, 'func')
    d.err = d.err or ut.val_number(d.volume, 0.0, 1.0, 'volume')

    -- if d.err ~= nil then
    --     d.err = string.format("Invalid function: %s", tostring(d.err))
    -- end

    d.format = function() return d.err or
        string.format('%05d %s %s FUNC:? VOL:%.1f',
            d.tick, tostring(BarTime(d.tick)), format_chan_hnd(d.chan_hnd), d.volume)
    end
    -- setmetatable(d, { __tostring = function(self) self.format() end })

    return d
end


-- Return the module.
return M
