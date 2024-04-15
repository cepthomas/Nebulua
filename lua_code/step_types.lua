-- Family of midi events.

local ut = require("utils")
local v = require('validators')
local md = require('midi_defs')
local com = require('neb_common')


local function _FormatChanHnd(chan_hnd)
    local s = string.format("%02X-%02X", (chan_hnd >> 8) & 0xFF, chan_hnd & 0xFF)
    return s
end


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
    d.err = d.err or v.val_integer(d.tick, 0, com.MAX_TICK, 'tick')
    d.err = d.err or v.val_integer(d.chan_hnd, 0, 0xFFFF, 'chan_hnd')
    d.err = d.err or v.val_integer(d.note_num, 0, com.MAX_MIDI, 'note_num')
    d.err = d.err or v.val_number(d.volume, 0.0, 1.0, 'volume')
    d.err = d.err or v.val_integer(d.duration, 0, com.MAX_TICK, 'duration')

    -- if d.err ~= nil then
    --     d.err = string.format("Invalid note: %s", d.err)
    -- end

    d.format = function() return d.err or
        string.format('%05d %s NOTE %d %.1f %d', d.tick, _FormatChanHnd(d.chan_hnd), d.note_num, d.volume, d.duration)
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
    d.err = d.err or v.val_integer(d.tick, 0, com.MAX_TICK, 'tick')
    d.err = d.err or v.val_integer(d.chan_hnd, 0, 0xFFFF, 'chan_hnd')
    d.err = d.err or v.val_integer(d.controller, 0, com.MAX_MIDI, 'controller')
    d.err = d.err or v.val_integer(d.value, 0, com.MAX_MIDI, 'value')

    -- if d.err ~= nil then
    --     d.err = string.format("Invalid controller: %s", d.err)
    -- end

    d.format = function() return d.err or
        string.format('%05d %s CTRL %d %d', d.tick, _FormatChanHnd(d.chan_hnd), d.controller, d.value)
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
    d.err = d.err or v.val_integer(d.tick, 0, com.MAX_TICK, 'tick')
    d.err = d.err or v.val_integer(d.chan_hnd, 0, 0xFFFF, 'chan_hnd')
    d.err = d.err or v.val_function(d.func, 0.0, 1.0, 'func')
    d.err = d.err or v.val_number(d.volume, 0.0, 1.0, 'volume')

    -- if d.err ~= nil then
    --     d.err = string.format("Invalid function: %s", tostring(d.err))
    -- end

    d.format = function() return d.err or
        -- string.format('%05d %s FUNC', d.tick, _FormatChanHnd(d.chan_hnd))
        string.format('%05d %s FUNC %.1f', d.tick, _FormatChanHnd(d.chan_hnd), d.volume)
    end
    -- setmetatable(d, { __tostring = function(self) self.format() end })

    return d
end
