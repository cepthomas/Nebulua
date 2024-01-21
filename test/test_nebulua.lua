
-- Unit tests for nebulua.lua.  TODO1-NEB

local ut = require("utils")
local si = require("step_info")

-- Create the namespace/module.
local M = {}


-----------------------------------------------------------------------------
function M.setup(pn)
    -- pn.UT_INFO("setup()!!!")
end

-----------------------------------------------------------------------------
function M.teardown(pn)
    -- pn.UT_INFO("teardown()!!!")
end


-- if use_dbgr then
--     have_dbgr, dbg = pcall(require, "debugger")
--     if not have_dbgr then
--         print(dbg)
--     end
-- end

-----------------------------------------------------------------------------
function M.suite_step_info(pn)
    pn.UT_INFO("Test all functions in step_info.lua")

    note1 = StepNote(1234, 99)
    pn.UT_EQUAL(note1, "Ut")

end

-----------------------------------------------------------------------------
-- Return the module.
return M
