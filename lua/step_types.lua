-- Family of midi events.

-- local ut = require('lbot_utils')
local lt = require('lbot_types')
local bt = require('music_time')
local mid = require('midi_defs')


local M = {}


-- Forward refs.
local _format_nid
local _format_tick



-----------------------------------------------------------------------------
function M.note(tick, nid, note_num, volume, duration)
    local d = {}
    d.step_type = "note"
    d.tick = tick
    d.nid = nid
    d.note_num = note_num
    d.volume = volume
    d.duration = duration

    -- Validate.
    lt.val_integer(d.tick, 0, bt.MAX_TICK)
    lt.val_integer(d.nid, 0, 0xFFFF)
    lt.val_integer(d.note_num, 0, mid.MAX_MIDI)
    lt.val_number(d.volume, 0.0, 1.0)
    lt.val_integer(d.duration, 0, bt.MAX_TICK)

    setmetatable(d,
    {
        __tostring = function(self)
            return string.format('%s %s NOTE:%d VOL:%.1f DUR:%d',
                _format_tick(self.tick), _format_nid(self.nid), self.note_num, self.volume, self.duration)
        end
    })

    return d
end


-----------------------------------------------------------------------------
function M.controller(tick, nid, controller, value)

    local d = {}
    d.step_type = "controller"
    d.tick = tick
    d.nid = nid
    d.controller = controller
    d.value = value

    -- Validate.
    lt.val_integer(d.tick, 0, bt.MAX_TICK)
    lt.val_integer(d.nid, 0, 0xFFFF)
    lt.val_integer(d.controller, 0, mid.MAX_MIDI)
    lt.val_integer(d.value, 0, mid.MAX_MIDI)

    setmetatable(d,
    {
        __tostring = function(self)
            return string.format('%s %s CTRL:%d VAL:%d',
                _format_tick(self.tick), _format_nid(self.nid), self.controller, self.value)
        end
    })

    return d
end

-----------------------------------------------------------------------------
function M.func(tick, nid, func, volume)

    local d = {}
    d.step_type = "function"
    d.tick = tick
    d.nid = nid
    d.func = func
    d.volume = volume

    -- Validate.
    lt.val_integer(d.tick, 0, bt.MAX_TICK)
    lt.val_integer(d.nid, 0, 0xFFFF)
    lt.val_number(d.volume, 0.0, 1.0)
    lt.val_function(func)

    setmetatable(d,
    {
        __tostring = function(self)
            return string.format('%s %s FUNC:? VOL:%.1f',
                _format_tick(self.tick), _format_nid(self.nid), self.volume)
        end
    })

    return d
end


-----------------------------------------------------------------------------
_format_nid = function(nid)
    return string.format("DEV:%02d CH:%02d", (nid >> 8) & 0xFF, nid & 0xFF)
end

-----------------------------------------------------------------------------
_format_tick = function(tick)
    local s = bt.tick_to_str(tick)
    return string.format('T:%05d BT:%s', tick, s)
end


-- Return the module.
return M
