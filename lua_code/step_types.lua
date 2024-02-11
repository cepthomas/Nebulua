
-- Class family for midi events to send.

local ut = require("utils")
local v = require('validators')
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
        err = nil
        err = err or v.val_integer(tick, 0, 9999, 'tick')
        err = err or v.val_integer(chan_hnd, 0, 255, 'chan_hnd')
        err = err or v.val_integer(note_num, 0, 255, 'note_num')
        err = err or v.val_integer(velocity, 0, 255, 'velocity')
        Step.__init(c, tick, chan_hnd) -- init base
        c.type = STEP_TYPE.NOTE
        c.note_num = note_num
        c.velocity = velocity
        c.err = err
    end)

function StepNote:format()
    return self.err or string.format('NOTE %d %d', self.note_num, self.velocity)
end


-----------------------------------------------------------------------------
-- derived Controller class
StepController = class(Step,
    function(c, tick, chan_hnd, controller, value)
        err = nil
        err = err or v.val_integer(tick, 0, 9999, 'tick')
        err = err or v.val_integer(chan_hnd, 0, 255, 'chan_hnd')
        err = err or v.val_integer(controller, 0, 255, 'controller')
        err = err or v.val_integer(value, 0, 255, 'value')
        Step.__init(c, tick, chan_hnd) -- init base
        c.type = STEP_TYPE.CONTROLLER
        c.controller = controller
        c.value = value
        c.err = err
    end)

function StepController:format()
    return self.err or string.format('CONTROLLER %d %d', self.controller, self.value)
end


-----------------------------------------------------------------------------
-- derived Function class
StepFunction = class(Step,
    function(c, tick, chan_hnd, func)
        err = nil
        err = err or v.val_integer(tick, 0, 9999, 'tick')
        err = err or v.val_integer(chan_hnd, 0, 255, 'chan_hnd')
        err = err or v.val_function(func, 'func')
        Step.__init(c, tick, chan_hnd) -- init base
        c.type = STEP_TYPE.FUNCTION
        c.func = func
        c.err = err
    end)

function StepFunction:format()
    -- return 'FUNCTION'
    return self.err or string.format('FUNCTION')
end
