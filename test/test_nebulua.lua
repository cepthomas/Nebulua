
-- Unit tests for nebulua.lua.

-- local v  = require('validators')
local ut = require("utils")
local st = require("step_types")
local bt = require("bar_time")
local api = require("host_api") -- C api mock
local neb = require("nebulua") -- lua api
require('neb_common')


ut.config_debug(false)


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
    local steps, seq_length = neb.parse_chunk( { "|1       |2    9 9|3       |4    9 9|5       |6    9 9|7       |8    9 9|", 89 } )
    -- print('+++', ut.dump_table_string(steps, 'steps1'))
    pn.UT_EQUAL(#steps, 16)
    pn.UT_EQUAL(seq_length, 64)
    step = steps[6]
    pn.UT_EQUAL(step.step_type, STEP_NOTE)
    pn.UT_EQUAL(step.tick, 24)
    pn.UT_EQUAL(step.chan_hnd, 0)
    pn.UT_EQUAL(step.note_num, 89)
    pn.UT_EQUAL(step.volume, 0.4)
    pn.UT_EQUAL(step.duration, 1)

    -- Note name.
    steps, seq_length = neb.parse_chunk( { "|7   7   |        |        |        |    4---|---     |        |        |", "C2" } )
    -- print('+++', ut.dump_table_string(steps, 'steps2'))
    pn.UT_EQUAL(#steps, 3)
    pn.UT_EQUAL(seq_length, 64)

    -- Chord.
    steps, seq_length = neb.parse_chunk( { "|        |    6---|----    |        |        |        |3 2 1   |        |", "B4.m7" } )
    -- print('+++', ut.dump_table_string(steps, 'steps3'))
    pn.UT_EQUAL(#steps, 16) -- 4 x 4 notes in chord
    pn.UT_EQUAL(seq_length, 64)

    -- Function.
    local dummy = function() end
    steps, seq_length = neb.parse_chunk( { "|        |    6-  |        |        |        | 9999   |  111   |        |", dummy } )
    -- print('+++', ut.dump_table_string(steps, 'steps4'))
    pn.UT_EQUAL(#steps, 8)
    pn.UT_EQUAL(seq_length, 64)

    -- Bad syntax.
    -- ok, steps = pcall(neb.parse_chunk, { "|   ---  |     8 8|        |     8 8|        |     8 8|        |     8 8|", 67 } )
    -- print('+++', ut.dump_table_string(steps, 'steps5'))
end


-----------------------------------------------------------------------------
function M.suite_process_script(pn)
    -- Test loading script file. TODO1 examine all.

    -- Load test file in protected mode.
    local scrfn = 'script1'
    local ok, scr = pcall(require, scrfn)
    pn.UT_TRUE(ok, string.format("Failed to load script: %s\n%s ", scrfn, scr))

    -- Look at script raw data -- in global space.
    pn.UT_NOT_NIL(sequences)
    pn.UT_NOT_NIL(sections)

    -- Process the data.
-- dbg()
    local length = neb.init(sequences, sections)
--    pn.UT_EQUAL(length, 999)

    _seq_steps, _sections, _transients = _mole()


    -- Execute the script steps.
    for i = 0, 100 do
        stat = neb.process_step(i)
        pn.UT_EQUAL(stat, 0)
    end


    -- s = ut.dump_table_string(api.activity, 'api.activity')
    -- print(s)



-- >>>>>>> function M.send_note(chan_hnd, note_num, volume, dur)

    -- ok, ret = pcall(input_note, 10, 11, 0.3)
    -- pn.UT_TRUE(ok, string.format("Script function input_note() failed:\n%s ", ret))

    -- -- Examine collected data.
    -- for _, d in ipairs(api.activity) do
end


-----------------------------------------------------------------------------
function M.suite_step_types(pn)
    -- Test all functions in step_types.lua

    local n = StepNote(1234, 99, 101, 0.4, 10)
    pn.UT_NIL(n.err)
    pn.UT_STR_EQUAL(n.format(), "01234 99 NOTE 101 0.4 10")

    n = StepNote(100001, 88, 111, 0.3, 22)
    pn.UT_NOT_NIL(n.err)
    pn.UT_STR_EQUAL(n.format(), "Invalid integer tick: 100001")

    local c = StepController(344, 37, 88, 55)
    pn.UT_NIL(c.err)
    pn.UT_STR_EQUAL(c.format(), "00344 37 CONTROLLER 88 55")

    local c = StepController(344, 55, 260, 23)
    pn.UT_NOT_NIL(c.err)
    pn.UT_STR_EQUAL(c.format(), "Invalid integer controller: 260")

    local function stub() end

    local f = StepFunction(122, stub)
    pn.UT_NIL(f.err)
    pn.UT_STR_EQUAL(f.format(), "00122 FUNCTION")
end


-----------------------------------------------------------------------------
-- Return the module.
return M
