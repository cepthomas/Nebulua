-- Family of midi events.

local ut = require('lbot_utils')
local bt = require('bar_time')
local mid = require('midi_defs')


local M = {}


-- Forward refs.
local _format_chan_hnd
local _format_tick



-----------------------------------------------------------------------------
function M.note(tick, chan_hnd, note_num, volume, duration)
    local d = {}
    d.valid = true
    d.step_type = "note"
    d.tick = tick
    d.chan_hnd = chan_hnd
    d.note_num = note_num
    d.volume = volume
    d.duration = duration

    -- Validate.
    d.valid = d.valid and ut.val_integer(d.tick, 0, bt.MAX_TICK)
    d.valid = d.valid and ut.val_integer(d.chan_hnd, 0, 0xFFFF)
    d.valid = d.valid and ut.val_integer(d.note_num, 0, mid.MAX_MIDI)
    d.valid = d.valid and ut.val_number(d.volume, 0.0, 1.0)
    d.valid = d.valid and ut.val_integer(d.duration, 0, bt.MAX_TICK)

    setmetatable(d,
    {
        __tostring = function(self)
            return string.format('%s %s NOTE:%d VOL:%.1f DUR:%d',
                _format_tick(d.tick), _format_chan_hnd(d.chan_hnd), d.note_num, d.volume, d.duration)
        end
    })

    if not d.valid then error('Invalid note '..tostring(d)) end

    return d
end


-----------------------------------------------------------------------------
function M.controller(tick, chan_hnd, controller, value)

    local d = {}
    d.valid = true
    d.step_type = "controller"
    d.tick = tick
    d.chan_hnd = chan_hnd
    d.controller = controller
    d.value = value

    -- Validate.
    d.valid = d.valid and ut.val_integer(d.tick, 0, bt.MAX_TICK)
    d.valid = d.valid and ut.val_integer(d.chan_hnd, 0, 0xFFFF)
    d.valid = d.valid and ut.val_integer(d.controller, 0, mid.MAX_MIDI)
    d.valid = d.valid and ut.val_integer(d.value, 0, mid.MAX_MIDI)

    setmetatable(d,
    {
        __tostring = function(self)
            return string.format('%s %s CTRL:%d VAL:%d',
                _format_tick(d.tick), _format_chan_hnd(d.chan_hnd), d.controller, d.value)
        end
    })

    if not d.valid then error('Invalid controller '..tostring(d)) end

    return d
end

-----------------------------------------------------------------------------
function M.func(tick, chan_hnd, func, volume)

    local d = {}
    d.valid = true
    d.step_type = "function"
    d.tick = tick
    d.chan_hnd = chan_hnd
    d.func = func
    d.volume = volume

    -- Validate.
    d.valid = d.valid and ut.val_integer(d.tick, 0, bt.MAX_TICK)
    d.valid = d.valid and ut.val_integer(d.chan_hnd, 0, 0xFFFF)
    d.valid = d.valid and ut.val_number(d.volume, 0.0, 1.0)
    d.valid = d.valid and func ~= nil and type(func) == 'function'

    setmetatable(d,
    {
        __tostring = function(self)
            return string.format('%s %s FUNC:? VOL:%.1f',
                _format_tick(d.tick), _format_chan_hnd(d.chan_hnd), d.volume)
        end
    })

    if not d.valid then error('Invalid function '..d._format()) end

    return d
end


-----------------------------------------------------------------------------
_format_chan_hnd = function(chan_hnd)
    return string.format("DEV:%02d CH:%02d", (chan_hnd >> 8) & 0xFF, chan_hnd & 0xFF)
end

-----------------------------------------------------------------------------
_format_tick = function(tick)
    return string.format('T:%05d BT:%s', tick, bt.tick_to_str(tick))
end


-- Return the module.
return M
