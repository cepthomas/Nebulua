
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

    n = StepNote(1234, 99, 101, 44)
    pn.UT_NIL(n.err)
    pn.UT_STR_EQUAL(n, "1234 99 NOTE 101 44")

    n = StepNote(100001, 88, 111, 33)
    pn.UT_NOT_NIL(n.err)
    pn.UT_STR_EQUAL(n, "Invalid integer tick: 100001")

    c = StepController(344, 37, 88, 99)
    pn.UT_NIL(c.err)
    pn.UT_STR_EQUAL(c, "344 37 CONTROLLER 88 99")

    c = StepController(344, 55, 260, 66)
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
function M.suite_bar_time(pn)
    pn.UT_INFO("suite_bar_time")

    bt = BT(12345)
    ok, s = bt.is_valid()
    pn.UT_TRUE(ok)

    pn.UT_EQUAL(bt.get_tick(), 12345)
    pn.UT_EQUAL(bt.get_bar(), 385)
    pn.UT_EQUAL(bt.get_beat(), 3)
    pn.UT_EQUAL(bt.get_subbeat(), 1)
    pn.UT_STR_EQUAL(tostring(bt), "385:3:1")

    bt.from_bar(129, 1, 6)
    ok, s = bt.is_valid()
    pn.UT_TRUE(ok)
    pn.UT_EQUAL(bt.get_tick(), 4142)
    pn.UT_EQUAL(bt.get_bar(), 129)
    pn.UT_EQUAL(bt.get_beat(), 1)
    pn.UT_EQUAL(bt.get_subbeat(), 6)
    pn.UT_STR_EQUAL(tostring(bt), "129:1:6")

    bt.from_bar(25, 5, 2)
    ok, s = bt.is_valid()
    pn.UT_FALSE(ok)
    pn.UT_STR_EQUAL(s, "Invalid integer beat: 5")
    pn.UT_EQUAL(bt.get_tick(), 0)
    pn.UT_STR_EQUAL(tostring(bt), "Invalid integer beat: 5")

    bt.from_bar(25, 1, 9)
    ok, s = bt.is_valid()
    pn.UT_FALSE(ok)
    pn.UT_STR_EQUAL(s, "Invalid integer subbeat: 9")
    pn.UT_EQUAL(bt.get_tick(), 0)
    pn.UT_STR_EQUAL(tostring(bt), "Invalid integer subbeat: 9")

    -- time usually
    bt.parse("108:0:7")
    ok, s = bt.is_valid()
    pn.UT_TRUE(ok)
    pn.UT_EQUAL(bt.get_tick(), 3463)
    pn.UT_STR_EQUAL(tostring(bt), "108:0:7")

    bt.parse("711:3:0")
    ok, s = bt.is_valid()
    pn.UT_TRUE(ok)
    pn.UT_EQUAL(bt.get_tick(), 22776)
    pn.UT_STR_EQUAL(tostring(bt), "711:3:0")

    -- duration usually
    bt.parse("1:4")
    ok, s = bt.is_valid()
    pn.UT_TRUE(ok)
    pn.UT_EQUAL(bt.get_tick(), 12)
    pn.UT_STR_EQUAL(tostring(bt), "0:1:4")

    -- bad input
    bt.parse("1.2.3")
    ok, s = bt.is_valid()
    pn.UT_FALSE(ok)
    pn.UT_STR_EQUAL(s, "Invalid time: 1.2.3")

    bt.parse("78")
    ok, s = bt.is_valid()
    pn.UT_FALSE(ok)
    pn.UT_STR_EQUAL(s, "Invalid time: 78")

    bt.parse("1:2:3:4")
    ok, s = bt.is_valid()
    pn.UT_FALSE(ok)
    pn.UT_STR_EQUAL(s, "Invalid time: 1:2:3:4")

    bt.parse("1:alpha:5")
    ok, s = bt.is_valid()
    pn.UT_FALSE(ok)
    pn.UT_STR_EQUAL(s, "Invalid time: 1:alpha:5")

    bt.parse({ 1, 2 })
    ok, s = bt.is_valid()
    pn.UT_FALSE(ok)
    pn.UT_STR_EQUAL(s, "Not a string")
end


-----------------------------------------------------------------------------
function M.suite_process(pn)
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
