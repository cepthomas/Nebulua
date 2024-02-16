-- Family of midi events.

local ut = require("utils")
local v = require('validators')
local md = require('midi_defs')
require('neb_common')


-----------------------------------------------------------------------------
function StepNote(tick, chan_hnd, note_num, volume, duration)
    local d = {}
    d.err = nil
    d.tick = tick
    d.chan_hnd = chan_hnd
    d.note_num = note_num
    d.volume = volume
    d.duration = duration

    -- Validate.
    d.err = d.err or v.val_integer(d.tick, 0, MAX_TICK, 'tick')
    d.err = d.err or v.val_integer(d.chan_hnd, 0, MAX_MIDI, 'chan_hnd')
    d.err = d.err or v.val_integer(d.note_num, 0, MAX_MIDI, 'note_num')
    d.err = d.err or v.val_number(d.volume, 0.0, 1.0, 'volume')
    d.err = d.err or v.val_integer(d.duration, 0, MAX_TICK, 'duration')

    setmetatable(d,
    {
        __tostring = function(self) return self.err or string.format('%05d %d NOTE %d %.1f %d', 
            self.tick, self.chan_hnd, self.note_num, self.volume, self.duration) end
    })

    return d
end


-----------------------------------------------------------------------------
function StepController(tick, chan_hnd, controller, value)
    local d = {}
    d.err = nil
    d.tick = tick
    d.chan_hnd = chan_hnd
    d.controller = controller
    d.value = value
    
    -- Validate.
    d.err = d.err or v.val_integer(d.tick, 0, MAX_TICK, 'tick')
    d.err = d.err or v.val_integer(d.chan_hnd, 0, MAX_MIDI, 'chan_hnd')
    d.err = d.err or v.val_integer(d.controller, 0, MAX_MIDI, 'controller')
    d.err = d.err or v.val_integer(d.value, 0, MAX_MIDI, 'value')

    setmetatable(d,
    {
        __tostring = function(self) return self.err or string.format('%05d %d CONTROLLER %d %d', self.tick, self.chan_hnd, self.controller, self.value) end
    })

    return d
end


-----------------------------------------------------------------------------
function StepFunction(tick, chan_hnd, volume, func)
    local d = {}
    d.err = nil
    d.tick = tick
    d.chan_hnd = chan_hnd
    d.volume = volume
    d.func = func

    -- Validate.
    d.err = d.err or v.val_integer(d.tick, 0, MAX_TICK, 'tick')
    d.err = d.err or v.val_integer(d.chan_hnd, 0, MAX_MIDI, 'chan_hnd')
    d.err = d.err or v.val_number(d.volume, 0.0, 1.0, 'volume')
    d.err = d.err or v.val_function(d.func, 0.0, 1.0, 'func')

    setmetatable(d,
    {
        __tostring = function(self) return self.err or string.format('%05d %d FUNCTION %.1f', self.tick, self.chan_hnd, self.volume) end
    })

    return d
end
