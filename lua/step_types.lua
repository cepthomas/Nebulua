-- Family of midi events.

local ut = require('lbot_utils')
local mid = require('midi_defs')
local bt = require('bar_time')


local M = {}

-- Forward refs.
local format_chan_hnd
local format_tick


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
        string.format('%s %s NOTE:%d VOL:%.1f DUR:%d',
            format_tick(d.tick), format_chan_hnd(d.chan_hnd), d.note_num, d.volume, d.duration)
    end

    setmetatable(d, { __tostring = function(self) return self._format() end })

    -- setmetatable(d,
    --     {
    --         __tostring = function(self)
    --             return self._format()
    --         end
    --     }
    -- )

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
        string.format('%s %s CTRL:%d VAL:%d',
            format_tick(d.tick), format_chan_hnd(d.chan_hnd), d.controller, d.value)
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
        string.format('%s %s FUNC:? VOL:%.1f',
            format_tick(d.tick), format_chan_hnd(d.chan_hnd), d.volume)
    end

    setmetatable(d, { __tostring = function(self) return self._format() end })

    return d
end


-----------------------------------------------------------------------------
format_chan_hnd = function(chan_hnd)
    return string.format("DEV:%02d CH:%02d", (chan_hnd >> 8) & 0xFF, chan_hnd & 0xFF)
end

-----------------------------------------------------------------------------
format_tick = function(tick)
    return string.format('%05d %s', tick, bt.tick_to_str(tick))
end


-- Return the module.
return M
