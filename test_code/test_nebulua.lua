
-- Unit tests for nebulua.lua.

-- local v  = require('validators')
local ut = require("utils")
local st = require("step_types")
local bt = require("bar_time")
local neb = require("nebulua") -- lua api
local api = require("host_api") -- C api (or sim)


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
    -- pr-int('+++', ut.dump_table_string(steps, 'steps1'))

    -- Note name.
    steps = neb.parse_chunk( { "|7   7   |        |        |        |    4---|---     |        |        |", "C2" } )
    -- pr-int('+++', ut.dump_table_string(steps, 'steps2'))

    -- Chord.
    steps = neb.parse_chunk( { "|        |    6---|----    |        |        |        |3 2 1   |        |", "B4.m7" } )
    -- pr-int('+++', ut.dump_table_string(steps, 'steps3'))

    -- Function.
    local func = function() end
    steps = neb.parse_chunk( { "|        |    6-  |        |        |        | 9999   |  111   |        |", func } )
    -- pr-int('+++', ut.dump_table_string(steps, 'steps4'))

    -- Bad syntax.
    steps = neb.parse_chunk( { "|   ---  |     8 8|        |     8 8|        |     8 8|        |     8 8|", 67 } )
    -- pr-int('+++', ut.dump_table_string(steps, 'steps5'))
end


-----------------------------------------------------------------------------
function M.suite_process_script(pn)
    -- Test loading script file.

    -- Load test file in protected mode.
    scrfn = 'script1'
    local ok, msg = pcall(require, scrfn)
    pn.UT_TRUE(ok, string.format("Failed to load script: %s\n%s ", scrfn, msg))

    -- Look at global script raw data.
    pn.UT_NOT_NIL(sequences)
    pn.UT_NOT_NIL(sections)

    neb.process_all(sequences, sections)

    pn.UT_NOT_NIL(tempdbg.steps)
    pn.UT_NOT_NIL(tempdbg.sections)

    s = ut.dump_table_string(tempdbg.steps, 'tempdbg.steps')
    -- pr-int('+++', s)

    -- TODO1 examine contents

end


-----------------------------------------------------------------------------
-- function M.suite_exec_script(pn)
--     -- Test loading and executing script file.

--     -- Load test file in protected mode.
--     scrfn = 'script1'
--     local ok, msg = pcall(require, scrfn)
--     pn.UT_TRUE(ok, string.format("Failed to load script: %s\n%s ", scrfn, msg))

--     -- Run setup.
--     ok, msg = pcall(setup)
--     pn.UT_TRUE(ok, string.format("Script function setup() failed:\n%s ", msg))

--     -- Execute the script steps.
--     for i = 0, 100 do
--         api.current_tick = 1
--         ok, msg = pcall(step, i)
--         pn.UT_TRUE(ok, string.format("Script function step() failed:\n%s ", msg))
--     end

--     -- Examine collected data. TODO1
--     for _, d in ipairs(api.activity) do


--     end
-- end


-----------------------------------------------------------------------------
-- function M.suite_script_input(pn) --  TODO1

--     -- Load test file in protected mode.
--     scrfn = 'script1'
--     local ok, msg = pcall(require, scrfn)
--     pn.UT_TRUE(ok, string.format("Failed to load script: %s\n%s ", scrfn, msg))

--     ok, msg = pcall(setup)
--     pn.UT_TRUE(ok, string.format("Script function setup() failed:\n%s ", msg))

--     ok, msg = pcall(input_note, 10, 11, 0.3)
--     pn.UT_TRUE(ok, string.format("Script function input_note() failed:\n%s ", msg))

--     ok, msg = pcall(input_controller, 21, 22, 23)
--     pn.UT_TRUE(ok, string.format("Script function input_controller() failed:\n%s ", msg))

--     for i = 0, 100 do
--         ok, msg = pcall(step, i)
--         pn.UT_TRUE(ok, string.format("Script function step() failed:\n%s ", msg))
--         sleep(10)
--     end

-- end


-----------------------------------------------------------------------------
function M.suite_step_info(pn)
    -- Test all functions in step_types.lua

    n = StepNote(1234, 99, 101, 0.4, 10)
    pn.UT_NIL(n.err)
    pn.UT_STR_EQUAL(n, "01234 99 NOTE 101 0.4 10")

    n = StepNote(100001, 88, 111, 0.3, 22)
    pn.UT_NOT_NIL(n.err)
    pn.UT_STR_EQUAL(n, "Invalid integer tick: 100001")

    c = StepController(344, 37, 88, 55)
    pn.UT_NIL(c.err)
    pn.UT_STR_EQUAL(c, "00344 37 CONTROLLER 88 99")

    c = StepController(344, 55, 260, 23)
    pn.UT_NOT_NIL(c.err)
    pn.UT_STR_EQUAL(c, "Invalid integer controller: 260")

    function stub() end

    f = StepFunction(122, 66, 0.5, stub)
    pn.UT_NIL(f.err)
    pn.UT_STR_EQUAL(f, "00122 66 FUNCTION 0.5")

    f = StepFunction(122, 333, 0.6, stub)
    pn.UT_NOT_NIL(f.err)
    pn.UT_STR_EQUAL(f, "Invalid integer chan_hnd: 333")
end


-----------------------------------------------------------------------------
-- Return the module.
return M
