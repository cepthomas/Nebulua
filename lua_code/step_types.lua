
-- Class family for midi events to send.

local ut = require("utils")
local v = require('validators')
require('class')


STEP_TYPE = { NONE = 0, NOTE = 1, CONTROLLER = 2, FUNCTION = 3 }

-----------------------------------------------------------------------------
-- base class
Step = class(
    function(c, subbeats, chan_hnd)
        c.type = STEP_TYPE.NONE
        c.subbeats = subbeats
        c.chan_hnd = chan_hnd
    end)

function Step:__tostring()
    return string.format("%d %d %s", self.subbeats, self.chan_hnd, self:format())
end


-----------------------------------------------------------------------------
-- derived Note class
StepNote = class(Step,
    function(c, subbeats, chan_hnd, note_num, velocity)
        v.val_integer(subbeats, 0, 9999, 'arg 1')
        v.val_integer(chan_hnd, 0, 255, 'arg 2')
        v.val_integer(note_num, 0, 255, 'arg 3')
        v.val_integer(velocity, 0, 255, 'arg 4')
        Step.__init(c, subbeats, chan_hnd) -- init base
        c.type = STEP_TYPE.NOTE
        c.note_num = note_num
        c.velocity = velocity
    end)

function StepNote:format()
    return string.format('NOTE %d %d', self.note_num, self.velocity)
end


-----------------------------------------------------------------------------
-- derived Controller class
StepController = class(Step,
    function(c, subbeats, chan_hnd, controller, value)
        v.val_integer(subbeats, 0, 9999, 'arg 1')
        v.val_integer(chan_hnd, 0, 255, 'arg 2')
        v.val_integer(controller, 0, 255, 'arg 3')
        v.val_integer(value, 0, 255, 'arg 4')
        Step.__init(c, subbeats, chan_hnd) -- init base
        c.type = STEP_TYPE.CONTROLLER
        c.controller = controller
        c.value = value
    end)

function StepController:format()
    return string.format('CONTROLLER %d %d', self.controller, self.value)
end


-----------------------------------------------------------------------------
-- derived Function class
StepFunction = class(Step,
    function(c, subbeats, chan_hnd, func)
        v.val_integer(subbeats, 0, 9999, 'arg 1')
        v.val_integer(chan_hnd, 0, 255, 'arg 2')
        v.val_function(func)
        Step.__init(c, subbeats, chan_hnd) -- init base
        c.type = STEP_TYPE.FUNCTION
        c.func = func
    end)

function StepFunction:format()
    -- return 'FUNCTION'
    return string.format('FUNCTION %s', self.func)
end
