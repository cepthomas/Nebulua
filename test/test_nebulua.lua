
-- Unit tests for nebulua.lua.

-- local v  = require('validators')
local ut = require("utils")
local st = require("step_types")
local bt = require("bar_time")
local neb = require("nebulua") -- lua api
-- local api = require("host_api") -- C api (or sim)
require('neb_common')
throw_error = false


ut.config_debug(false, true)


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
function M.suite_parse_chunk(pn)

    -- Note number.
    steps = neb.parse_chunk( { "|1       |2    9 9|3       |4    9 9|5       |6    9 9|7       |8    9 9|", 89 } )
    -- print('+++', ut.dump_table_string(steps, 'steps1'))

    -- Note name.
    steps = neb.parse_chunk( { "|7   7   |        |        |        |    4---|---     |        |        |", "C2" } )
    -- print('+++', ut.dump_table_string(steps, 'steps2'))

    -- Chord.
    steps = neb.parse_chunk( { "|        |    6---|----    |        |        |        |3 2 1   |        |", "B4.m7" } )
    -- print('+++', ut.dump_table_string(steps, 'steps3'))

    -- Function.
    local func = function() end
    ok, steps = parse_chunk( { "|        |    6-  |        |        |        | 9999   |  111   |        |", func } )
    -- print('+++', ut.dump_table_string(steps, 'steps4'))

    -- Bad syntax.
    ok, steps = pcall(neb.parse_chunk, { "|   ---  |     8 8|        |     8 8|        |     8 8|        |     8 8|", 67 } )
    -- print('+++', ut.dump_table_string(steps, 'steps5'))
end


-----------------------------------------------------------------------------
function M.suite_process_script(pn)
    -- Test loading script file. TODO1 examine all.

    -- Load test file in protected mode.
    scrfn = 'script1'
    local ok, scr = pcall(require, scrfn)
    pn.UT_TRUE(ok, string.format("Failed to load script: %s\n%s ", scrfn, scr))

    -- Look at global script raw data.
    pn.UT_NOT_NIL(sequences)
    pn.UT_NOT_NIL(sections)

    neb.init(sequences, sections)

    pn.UT_NOT_NIL(tempdbg.steps)
    pn.UT_NOT_NIL(tempdbg.sections)

    s = ut.dump_table_string(tempdbg.steps, 'tempdbg.steps')
    -- print('+++', s)

    -- -- Run setup.
    -- ok, length = pcall(setup)
    -- pn.UT_TRUE(ok, string.format("Script function setup() failed:\n%s ", length))

    -- -- Execute the script steps.
    -- for i = 0, 100 do
    --     api.current_tick = 1
    --     ok, ret = pcall(step, i)
    --     pn.UT_TRUE(ok, string.format("Script function step() failed:\n%s ", ret))
    -- end

    -- ok, ret = pcall(input_note, 10, 11, 0.3)
    -- pn.UT_TRUE(ok, string.format("Script function input_note() failed:\n%s ", ret))

    -- -- Examine collected data.
    -- for _, d in ipairs(api.activity) do
end


-----------------------------------------------------------------------------
function M.suite_step_info(pn)
    -- Test all functions in step_types.lua

    n = StepNote(1234, 99, 101, 0.4, 10)
    pn.UT_NIL(n.err)
    pn.UT_STR_EQUAL(tostring(n), "01234 99 NOTE 101 0.4 10")

    n = StepNote(100001, 88, 111, 0.3, 22)
    pn.UT_NOT_NIL(n.err)
    pn.UT_STR_EQUAL(tostring(n), "Invalid integer tick: 100001")

    c = StepController(344, 37, 88, 55)
    pn.UT_NIL(c.err)
    pn.UT_STR_EQUAL(tostring(c), "00344 37 CONTROLLER 88 55")

    c = StepController(344, 55, 260, 23)
    pn.UT_NOT_NIL(c.err)
    pn.UT_STR_EQUAL(tostring(c), "Invalid integer controller: 260")

    function stub() end

    f = StepFunction(122, 66, 0.5, stub)
    pn.UT_NIL(f.err)
    pn.UT_STR_EQUAL(tostring(f), "00122 66 FUNCTION 0.5")

    f = StepFunction(122, 333, 0.6, stub)
    pn.UT_NOT_NIL(f.err)
    pn.UT_STR_EQUAL(tostring(f), "Invalid integer chan_hnd: 333")
end


-----------------------------------------------------------------------------
-- Return the module.
return M
