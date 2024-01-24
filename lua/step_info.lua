
-- Class family for midi things to send.

local ut = require("utils")
require('class')

STEP_TYPE = { NONE = 0, NOTE = 1, CONTROLLER = 2, FUNCTION = 3 }

-- base
StepInfo = class(
    function(a, subbeats, chan_hnd)
        a.type = STEP_TYPE.NONE
        a.subbeats = subbeats
        a.chan_hnd = chan_hnd
    end)

function StepInfo:__tostring()
    -- ex: interp( [[Hello {name}, welcome to {company}.]], { name = name, company = get_company_name() } )
    return self.subbeats..' '..self.chan_hnd..': '..self:format()
    -- return self.name..': '..self:speak()
end


-- derived
StepNote = class(StepInfo,
    function(c, subbeats, chan_hnd, note_num, velocity)
        StepInfo.init(c, subbeats, chan_hnd) -- init base
        c.type = STEP_TYPE.NOTE
        c.note_num = note_num
        c.velocity = velocity
    end)

function StepNote:format()
    return 'NOTE'
end


-- derived
StepController = class(StepInfo,
    function(c, subbeats, chan_hnd, controller, value)
        StepInfo.init(c, subbeats, chan_hnd) -- init base
        c.type = STEP_TYPE.CONTROLLER
        c.controller = controller
        c.value = value
    end)

function StepFunction:format()
    return 'CONTROLLER'
end


-- derived
StepFunction = class(StepInfo,
    function(c, subbeats, chan_hnd, func)
        StepInfo.init(c, subbeats, chan_hnd) -- init base
        c.type = STEP_TYPE.FUNCTION
        c.func = func
    end)

function StepFunction:format()
    return 'FUNCTION'
end
