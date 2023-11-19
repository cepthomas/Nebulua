
local ut = require("utils")
require('class')

STEP_TYPE = { NONE = 0, NOTE = 1, CONTROLLER = 2, PATCH = 3, FUNCTION = 4 }

-- base
StepInfo = class(
    function(a, subbeat, channel_num)
        a.type = STEP_TYPE.NONE
        a.subbeat = subbeat
        a.channel_num = channel_num
    end)

function StepInfo:__tostring()

-- ex: interp( [[Hello {name}, welcome to {company}.]], { name = name, company = get_company_name() } )

    return self.subbeat..' '..self.channel_num..': '..self:format()
    -- return self.name..': '..self:speak()
end

-- derived
StepNote = class(StepInfo,
    function(c, subbeat, channel_num)
        StepInfo.init(c, subbeat, channel_num) -- must init base!
        c.type = STEP_TYPE.NOTE
        -- notenum(I), volume(N), duration(I subbeats)
    end)

function StepNote:format()
    return 'NOTE'
end


-- StepController   STEP_TYPE.CONTROLLER: ctlid(I), value(I)
-- StepPatch        STEP_TYPE.PATCH: patch_num(I)
-- StepFunction     STEP_TYPE.FUNCTION: function(F)

