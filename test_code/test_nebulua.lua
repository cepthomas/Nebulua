
-- Unit tests for nebulua.lua.

local v = require('validators')
local ut = require("utils")
local st = require("step_types")

-- ut.config_error_handling(true, true)


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

-----------------------------------------------------------------------------
function M.suite_step_info(pn)
    pn.UT_INFO("Test all functions in step_types.lua")

    n = StepNote(1234, 99, 101, 202)
    pn.UT_NIL(n.err)
    pn.UT_STR_EQUAL(n, "1234 99 NOTE 101 202")

    n = StepNote(10000, 99, 101, 202)
    pn.UT_NOT_NIL(n.err)
    pn.UT_STR_EQUAL(n, "Invalid integer subbeats: 10000")

    c = StepController(344, 37, 143, 99)
    pn.UT_NIL(c.err)
    pn.UT_STR_EQUAL(c, "344 37 CONTROLLER 143 99")

    c = StepController(344, 37, 260, 99)
    pn.UT_NOT_NIL(c.err)
    pn.UT_STR_EQUAL(c, "Invalid integer controller: 260")

    function stub() end

    f = StepFunction(122, 66, stub)
    pn.UT_NIL(f.err)
    pn.UT_STR_EQUAL(f, "122 66 FUNCTION")

    f = StepFunction(122, 333, stub)
    pn.UT_NOT_NIL(f.err)
    pn.UT_STR_EQUAL(f, "Invalid integer chan_hnd: 333")

end


-----------------------------------------------------------------------------
function M.suite_process(pn) --  TODO1
    pn.UT_INFO("Test xxx")

--[[
Process all sequences into discrete steps. Sections are stored as is.
@param sequences table user sequence specs
@param sections table user section specs
@return list of step_info ordered by subbeat
function M.process_all(sequences, sections)
]]

end


-----------------------------------------------------------------------------
function M.suite_do_step(pn) --  TODO1
    pn.UT_INFO("Test xxx")

--[[
Process notes at this time.
@param name type desc
@return type desc
function M.do_step(send_stuff, bar, beat, subbeat)
]]

end


--[[
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
