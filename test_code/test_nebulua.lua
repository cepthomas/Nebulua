
-- Unit tests for nebulua.lua.  TODO1

local v = require('validators')
local ut = require("utils")
local st = require("step_types")

-- ut.config_error_handling(true, true)


-- Create the namespace/module.
local M = {}


-----------------------------------------------------------------------------
function M.setup(pn)
    -- pn.UT_INFO("setup()!!!")
    v.set_mode(false, 4)
end

-----------------------------------------------------------------------------
function M.teardown(pn)
    -- pn.UT_INFO("teardown()!!!")
end

-----------------------------------------------------------------------------
function M.suite_step_info(pn)
    pn.UT_INFO("Test all functions in step_types.lua")

    note1 = StepNote(1234, 99, 101, 202)
    pn.UT_STR_EQUAL(note1, "1234 99 NOTE 101 202")

    note1 = StepNote(10000, 99, 101, 202)
    pn.UT_STR_EQUAL(note1, "Invalid integer:10000 arg 1")


end



--[[
--- Process all sequences into discrete steps. Sections are stored as is.
-- @param sequences table user sequence specs
-- @param sections table user section specs
-- @return list of step_info ordered by subbeat
function M.process_all(sequences, sections)

--- Process notes at this time.
-- @param name type desc
-- @return type desc
function M.do_step(send_stuff, bar, beat, subbeat)


M.UT_CLOSE(val1, val2, tol)
M.UT_EQUAL(val1, val2)
M.UT_ERROR(info)
M.UT_FALSE(expr)
M.UT_GREATER(val1, val2)
M.UT_GREATER_OR_EQUAL(val1, val2)
M.UT_INFO(info)
M.UT_LESS(val1, val2)
M.UT_LESS_OR_EQUAL(val1, val2)
M.UT_NIL(expr)
M.UT_NOT_EQUAL(val1, val2)
M.UT_NOT_NIL(expr)
M.UT_TRUE(expr)

]]

-----------------------------------------------------------------------------
-- Return the module.
-- print("return test_nebula module:")
-- print(ut.dump_table_string(M, 'test_nebula module'))
return M
