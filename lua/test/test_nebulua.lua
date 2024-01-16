
-- Unit tests for nebulua.lua.  TODO2

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

-- fido = StepNote('Fido')
-- felix = StepController('Felix', 'Tabby')



    -- pn.UT_INFO("Verify that this info line appears in the report file.")
    -- pn.UT_ERROR("Verify that this error line appears in the report file.")

    -- pn.UT_TRUE(2 + 2 == 4) -- pass
    -- pn.UT_TRUE(2 + 2 == 5) -- fail

    -- pn.UT_FALSE(2 + 2 == 4) -- fail
    -- pn.UT_FALSE(2 + 2 == 5) -- pass

    -- pn.UT_NIL(nil) -- pass
    -- pn.UT_NIL(2) -- fail

end

-----------------------------------------------------------------------------
-- Return the module.
return M
