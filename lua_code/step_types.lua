-- Family of midi events.

local ut = require("utils")
local v = require('validators')
local md = require('midi_defs')
require('neb_common')


-----------------------------------------------------------------------------
function StepNote(tick, chan_hnd, note_num, velocity)
    local d = {}
    d.err = nil
    d.tick = tick
    d.chan_hnd = chan_hnd
    d.note_num = note_num
    d.velocity = velocity

    -- Validate.
    d.err = d.err or v.val_integer(d.tick, 0, MAX_TICK, 'tick')
    d.err = d.err or v.val_integer(d.chan_hnd, 0, MAX_MIDI, 'chan_hnd')
    d.err = d.err or v.val_integer(d.note_num, 0, MAX_MIDI, 'note_num')
    d.err = d.err or v.val_integer(d.velocity, 0, MAX_MIDI, 'velocity')

    setmetatable(d,
    {
        __tostring = function(self)
            return self.err or string.format('%05d %d NOTE %d %d', self.tick, self.chan_hnd, self.note_num, self.velocity)
        end
    } )

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
        __tostring = function(self)
            return self.err or string.format('%05d %d CONTROLLER %d %d', self.tick, self.chan_hnd, self.controller, self.value)
        end
    } )

    return d
end


-----------------------------------------------------------------------------
function StepFunction(tick, chan_hnd, func)
    local d = {}
    d.err = nil
    d.tick = tick
    d.chan_hnd = chan_hnd
    d.func = func

    -- Validate.
    d.err = d.err or v.val_integer(d.tick, 0, MAX_TICK, 'tick')
    d.err = d.err or v.val_integer(d.chan_hnd, 0, MAX_MIDI, 'chan_hnd')

    setmetatable(d,
    {
        __tostring = function(self)
            return self.err or string.format('%05d %d FUNCTION', self.tick, self.chan_hnd)
        end
    } )

    return d
end
