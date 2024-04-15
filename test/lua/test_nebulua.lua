
-- Unit tests for nebulua.lua.

-- local v  = require('validators')
local ut  = require("utils")
local st  = require("step_types")
local bt  = require("bar_time")
local api = require("host_api") -- host api mock
local neb = require("nebulua") -- lua api
local com = require('neb_common')


ut.config_debug(false) -- TODO an easy way to toggle this? and/or insert/delete breakpoints from ST.


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
    -- Note number. This also checks the list of steps in more detail
    local chunk = { "|5       |2    9 9|3       |4    9 9|5       |6    9 9|7       |8    9 9|", 89 }
    local seq_length, steps = neb.parse_chunk(chunk, 0x030E, 1000 )
    -- print('+++', ut.dump_table_string(steps, true, 'steps1'))
    pn.UT_EQUAL(#steps, 16)
    pn.UT_EQUAL(seq_length, 64)
    step = steps[6] -- pick one
    pn.UT_STR_EQUAL(step.step_type, "note")
    pn.UT_EQUAL(step.tick, 1024)
    pn.UT_EQUAL(step.chan_hnd, 0x030E)
    pn.UT_EQUAL(step.note_num, 89)
--TODO1 fix/test volumes    pn.UT_EQUAL(step.volume, 0.4)
    pn.UT_EQUAL(step.duration, 1)


    -- Note name.
    chunk = { "|7   7   |        |        |        |    4---|---     |        |        |",  "C2" }
    seq_length, steps = neb.parse_chunk(chunk, 0x0A04, 234 )
    -- print('+++', ut.dump_table_string(steps, true, 'steps2'))
    pn.UT_EQUAL(#steps, 3)
    pn.UT_EQUAL(seq_length, 64)

    -- Chord.
    chunk = { "|        |    6---|----    |        |        |        |3 2 1   |        |", "B4.m7" }
    seq_length, steps = neb.parse_chunk(chunk, 0x0A05, 1111 )
    -- print('+++', ut.dump_table_string(steps, true, 'steps3'))
    pn.UT_EQUAL(#steps, 16) -- 4 x 4 notes in chord
    pn.UT_EQUAL(seq_length, 64)

    -- Function.
    local dummy = function() end
    chunk = { "|        |    6-  |        |        |        | 9999   |  111   |        |", dummy }
    seq_length, steps = neb.parse_chunk(chunk, 0x0A06, 1555 )
    -- print('+++', ut.dump_table_string(steps, true, 'steps4'))
    pn.UT_EQUAL(#steps, 8)
    pn.UT_EQUAL(seq_length, 64)

    -- Bad syntax.
    -- dbg()
    chunk = { "|   ---  |     8 8|        |     8 8|        |     8 8|        |     8 8|", 99 }
    seq_length, steps = neb.parse_chunk(chunk, 0x0A07, 678 )
    -- print('+++', ut.dump_table_string(steps, true, 'steps5'))
    pn.UT_EQUAL(seq_length, 0)
    pn.UT_STR_CONTAINS(steps, "Invalid '-' in pattern string")

end


-----------------------------------------------------------------------------
function M.suite_process_script(pn)
    -- Test loading script file.

    -- Load test file in protected mode.
    local scrfn = 'script_happy'
    local ok, scr = pcall(require, scrfn)
    pn.UT_TRUE(ok, string.format("Failed to load script: %s\n  => %s ", scrfn, scr))

    -- Process the data.
    neb.process_comp(sections)


    dumpfn = 'C:\\Dev\\repos\\Lua\\Nebulua\\_dump.txt', 'w+'
    neb.dump_steps(dumpfn) -- diagnostic  INDEX-CHANNEL


    pn.UT_EQUAL(_length, 201) --[5911] is not equal to [201].
    pn.UT_EQUAL(ut.table_count(_section_names), 3)
    print(ut.dump_table_string(_section_names, false, '_section_names'))





    -- Look inside.
    local steps, transients = _mole()





    -- s = ut.dump_table_string(steps, true, "steps")
    -- print(s)

    -- Execute some script steps. Times and counts are based on script_happy.lua observed.
    -- valid ticks: 0000, 0004, 0032, 0088, 0092, 0120, 0122, 0128, 0160, 0188, 0192, 0196,
    --              0216, 0248, 0250, 0256, 0284, 0288, 0344, 0376, 0378, 0380, 0384, 0388, 0476
    for i = 0, 200 do
        api.current_tick = i
        stat = neb.process_step(i)
        pn.UT_EQUAL(stat, 0)
        -- print(">>>", ut.table_count(transients))

        if i == 4 then
            pn.UT_EQUAL(#api.activity, 11)
            pn.UT_EQUAL(ut.table_count(transients), 1)
        end

        if i == 40 then
            pn.UT_EQUAL(#api.activity, 23)
            pn.UT_EQUAL(ut.table_count(transients), 1)
        end
    end

    pn.UT_EQUAL(#api.activity, 99)
    pn.UT_EQUAL(ut.table_count(transients), 0)

    -- Examine collected data.
    --for _, d in ipairs(api.activity) do

    -- s = ut.dump_table_string(transients, true, "transients")
    -- print(s)


    ok, ret = pcall(rcv_note, 10, 11, 0.3)
    pn.UT_TRUE(ok, string.format("Script function rcv_note() failed:\n%s ", ret))
end


-----------------------------------------------------------------------------
function M.suite_step_types(pn)
    -- Test all functions in step_types.lua

    local n = StepNote(1234, 99, 101, 0.4, 10)
    pn.UT_NIL(n.err)
    pn.UT_STR_EQUAL(n.format(), "01234 00-63 NOTE 101 0.4 10")

    n = StepNote(100001, 88, 111, 0.3, 22)
    pn.UT_NOT_NIL(n.err)
    pn.UT_STR_EQUAL(n.format(), "Invalid integer tick: 100001")

    local c = StepController(344, 37, 88, 55)
    pn.UT_NIL(c.err)
    pn.UT_STR_EQUAL(c.format(), "00344 00-25 CTRL 88 55")

    local c = StepController(455, 55, 260, 23)
    pn.UT_NOT_NIL(c.err)
    pn.UT_STR_EQUAL(c.format(), "Invalid integer controller: 260")

    local function stub() end

    local f = StepFunction(508, 122, stub, 0.44)
    pn.UT_NIL(f.err)
    pn.UT_STR_EQUAL(f.format(), "00508 00-7A FUNC 0.4")
end

-----------------------------------------------------------------------------
-- Return the module.
return M
