
-- Class family for midi events to send.

local ut = require("utils")
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
        Step.init(c, subbeats, chan_hnd) -- init base
        c.type = STEP_TYPE.NOTE
        c.note_num = note_num
        c.velocity = velocity
    end)

function StepNote:format()
    return string.format('NOTE %d %d', c.note_num, c.velocity)
end


-----------------------------------------------------------------------------
-- derived Controller class
StepController = class(Step,
    function(c, subbeats, chan_hnd, controller, value)
        Step.init(c, subbeats, chan_hnd) -- init base
        c.type = STEP_TYPE.CONTROLLER
        c.controller = controller
        c.value = value
    end)

function StepFunction:format()
    return string.format('CONTROLLER %d %d', c.controller, c.value)
end


-----------------------------------------------------------------------------
-- derived Function class
StepFunction = class(Step,
    function(c, subbeats, chan_hnd, func)
        Step.init(c, subbeats, chan_hnd) -- init base
        c.type = STEP_TYPE.FUNCTION
        c.func = func
    end)

function StepFunction:format()
    return string.format('FUNCTION %s', c.func)
end
