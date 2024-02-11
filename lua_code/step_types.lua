-- Class family for midi events to send.

local ut = require("utils")
local v = require('validators')
local md = require('midi_defs')
require('class')


STEP_TYPE = { NONE = 0, NOTE = 1, CONTROLLER = 2, FUNCTION = 3 }

-----------------------------------------------------------------------------
-- base class
Step = class(
    function(c, tick, chan_hnd)
        c.type = STEP_TYPE.NONE
        c.tick = tick
        c.chan_hnd = chan_hnd
        c.err = nil -- if this is not nil it contains info about failed construction.
    end)

function Step:__tostring()
    return self.err or string.format("%d %d %s", self.tick, self.chan_hnd, self:format())
end


-----------------------------------------------------------------------------
-- derived Note class
StepNote = class(Step,
    function(c, tick, chan_hnd, note_num, velocity)
        c.err = nil
        c.err = c.err or v.val_integer(tick, 0, 99999, 'tick')
        c.err = c.err or v.val_integer(chan_hnd, 0, md.MIDI_MAX, 'chan_hnd')
        c.err = c.err or v.val_integer(note_num, 0, md.MIDI_MAX, 'note_num')
        c.err = c.err or v.val_integer(velocity, 0, md.MIDI_MAX, 'velocity')
        Step.__init(c, tick, chan_hnd) -- init base
        c.type = STEP_TYPE.NOTE
        c.note_num = note_num
        c.velocity = velocity
    end)

function StepNote:format()
    return self.err or string.format('NOTE %d %d', self.note_num, self.velocity)
end


-----------------------------------------------------------------------------
-- derived Controller class
StepController = class(Step,
    function(c, tick, chan_hnd, controller, value)
        c.err = nil
        c.err = c.err or v.val_integer(tick, 0, 9999, 'tick')
        c.err = c.err or v.val_integer(chan_hnd, 0, md.MIDI_MAX, 'chan_hnd')
        c.err = c.err or v.val_integer(controller, 0, md.MIDI_MAX, 'controller')
        c.err = c.err or v.val_integer(value, 0, md.MIDI_MAX, 'value')
        Step.__init(c, tick, chan_hnd) -- init base
        c.type = STEP_TYPE.CONTROLLER
        c.controller = controller
        c.value = value
    end)

function StepController:format()
    return self.err or string.format('CONTROLLER %d %d', self.controller, self.value)
end


-----------------------------------------------------------------------------
-- derived Function class
StepFunction = class(Step,
    function(c, tick, chan_hnd, func)
        c.err = nil
        c.err = c.err or v.val_integer(tick, 0, 9999, 'tick')
        c.err = c.err or v.val_integer(chan_hnd, 0, md.MIDI_MAX, 'chan_hnd')
        c.err = c.err or v.val_function(func, 'func')
        Step.__init(c, tick, chan_hnd) -- init base
        c.type = STEP_TYPE.FUNCTION
        c.func = func
    end)

function StepFunction:format()
    -- return 'FUNCTION'
    return self.err or string.format('FUNCTION')
end
