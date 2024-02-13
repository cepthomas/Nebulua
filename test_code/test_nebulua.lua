
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

-----------------------------------------------------------------------------
-- Return the test module.
return M
