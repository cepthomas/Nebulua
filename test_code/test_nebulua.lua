
-- Unit tests for nebulua.lua.

local v  = require('validators')
local ut = require("utils")
local st = require("step_types")
local bt = require("bar_time")

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

    n = StepNote(100000, 99, 101, 202)
    pn.UT_NOT_NIL(n.err)
    pn.UT_STR_EQUAL(n, "Invalid integer tick: 100000")

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
function M.suite_bar_time(pn) --  TODO1
    pn.UT_INFO("suite_bar_time")

    bt = BT(12345)
    pn.UT_NIL(bt.err)
    pn.UT_EQUAL(bt.tick, 12345)
    pn.UT_EQUAL(bt.get_bar(), 0)
    pn.UT_EQUAL(bt.get_beat(), 0)
    pn.UT_EQUAL(bt.get_subbeat(), 0)
    pn.UT_STR_EQUAL(tostring(bt), "xxxx")

    bt.from_bar(129, 4, 2)
    pn.UT_NIL(bt.err)
    pn.UT_EQUAL(bt.tick, 0)
    pn.UT_EQUAL(bt.get_bar(), 0)
    pn.UT_EQUAL(bt.get_beat(), 0)
    pn.UT_EQUAL(bt.get_subbeat(), 0)
    pn.UT_STR_EQUAL(tostring(bt), "129.4.2")

    bt.from_bar(25, 5, 2)
    pn.UT_NOT_NIL(bt.err)
    pn.UT_EQUAL(bt.tick, 0)
    pn.UT_STR_EQUAL(tostring(bt), "poopoo")

    bt.from_bar(25, 1, 9)
    pn.UT_NOT_NIL(bt.err)
    pn.UT_EQUAL(bt.tick, 0)
    pn.UT_STR_EQUAL(tostring(bt), "poopoo")

end


-----------------------------------------------------------------------------
function M.suite_process(pn) --  TODO1
    pn.UT_INFO("suite_process")

    -- Load test file in protected mode.
    scrfn = 'script1'
    local ok, msg = pcall(require, scrfn)
    pn.UT_TRUE(ok, string.format("Failed to load file: %s\n%s ", scrfn, msg))

    -- TODO1 test these:
    ok, msg = pcall(setup)
    pn.UT_TRUE(ok, string.format("Function setup() failed:\n%s ", msg))

    ok, msg = pcall(process_all, sequences, sections)
--    pn.UT_TRUE(ok, string.format("Function process_all() failed:\n%s ", msg))
end


-----------------------------------------------------------------------------
function M.suite_input(pn) --  TODO1
    pn.UT_INFO("suite_input")

    -- Load test file in protected mode.
    scrfn = 'script1'
    local ok, msg = pcall(require, scrfn)
    pn.UT_TRUE(ok, string.format("Failed to load file: %s\n%s ", scrfn, msg))

    ok, msg = pcall(setup)
    pn.UT_TRUE(ok, string.format("Function setup() failed:\n%s ", msg))

    ok, msg = pcall(input_note, 10, 11, 0.3)
    pn.UT_TRUE(ok, string.format("Function input_note() failed:\n%s ", msg))

    ok, msg = pcall(input_controller, 21, 22, 23)
    pn.UT_TRUE(ok, string.format("Function input_controller() failed:\n%s ", msg))

    ok, msg = pcall(step, 31, 32, 33)
    pn.UT_TRUE(ok, string.format("Function step() failed:\n%s ", msg))
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
-- Return the test module.
return M
