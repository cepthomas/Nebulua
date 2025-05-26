
-- Unit tests for nebulua.

-- local ut  = require("lbot_utils")
local st  = require("step_types")
-- local bt  = require("bar_time")
local li  = require("luainterop") -- mock
local sx  = require("stringex")
local tx  = require("tableex")
local api = require("script_api")


-- Create the namespace/module.
local M = {}


-- -----------------------------------------------------------------------------
-- function M.setup(pn)
-- end

-- -----------------------------------------------------------------------------
-- function M.teardown(pn)
-- end


-----------------------------------------------------------------------------
function M.suite_parse_chunk(pn)
    -- Note number. This also checks the list of steps in more detail
    local chunk = { "|5       |2    9 9|3       |4    9 9|5       |6    9 9|7       |8    9 9|", 89 }
    local seq_length, steps = api.parse_chunk(chunk, 0x030E, 1000 )
    -- print('+++', tx.dump_table(steps, 'steps1', 1))
    pn.UT_EQUAL(#steps, 16)
    pn.UT_EQUAL(seq_length, 64)
    local step = steps[6] -- pick one
    pn.UT_STR_EQUAL(step.step_type, "note")
    pn.UT_EQUAL(step.tick, 1024)
    pn.UT_EQUAL(step.chan_hnd, 0x030E)
    pn.UT_EQUAL(step.note_num, 89)
    pn.UT_CLOSE(step.volume, 0.5, 0.001)
    pn.UT_EQUAL(step.duration, 1)

    -- Note name.
    chunk = { "|7   7   |        |        |        |    4---|---     |        |        |",  "C2" }
    seq_length, steps = api.parse_chunk(chunk, 0x0A04, 234 )
    -- print('+++', tx.dump_table(steps, 'steps2', 1))
    pn.UT_EQUAL(#steps, 3)
    pn.UT_EQUAL(seq_length, 64)

    -- Chord.
    chunk = { "|        |    6---|----    |        |        |        |3 2 1   |        |", "B4.m7" }
    seq_length, steps = api.parse_chunk(chunk, 0x0A05, 1111 )
    -- print('+++', tx.dump_table(steps, 'steps3', 1))
    pn.UT_EQUAL(#steps, 16) -- 4 x 4 notes in chord
    pn.UT_EQUAL(seq_length, 64)

    -- Function.
    local dummy = function() end
    chunk = { "|        |    6-  |        |        |        | 9999   |  111   |        |", dummy }
    seq_length, steps = api.parse_chunk(chunk, 0x0A06, 1555 )
    -- print('+++', tx.dump_table(steps, 'steps4', 1))
    pn.UT_EQUAL(#steps, 8)
    pn.UT_EQUAL(seq_length, 64)

    -- Bad syntax.
    -- dbg()
    chunk = { "|   ---  |     8 8|        |     8 8|        |     8 8|        |     8 8|", 99 }
    seq_length, steps = api.parse_chunk(chunk, 0x0A07, 678 )
    -- print('+++', tx.dump_table(steps, 'steps5', 1))
    pn.UT_EQUAL(seq_length, 0)
    pn.UT_STR_CONTAINS(tx.dump_table(steps, 'xxxx', 1), "Invalid '-' in pattern string")
end

-----------------------------------------------------------------------------
function M.suite_process_script(pn)
    -- Test loading script file.

    -- Load test file in protected mode.
    local scrfn = 'script_happy'
    local ok, scr = pcall(require, scrfn)
    pn.UT_TRUE(ok, string.format("Failed to load script: %s\n  => %s ", scrfn, scr))

    -- Process the data.
    local meta = api.process_comp()
    -- api.dump_steps('_steps.txt', 's') -- diagnostic
    pn.UT_TRUE(sx.contains(meta, '_LENGTH,768'))

    -- Look inside.
    -- local steps, transients = _mole()

    -- s = tx.dump_table(steps, "steps", 1)
    -- print(s)

    -- Execute some script steps.
    for i = 0, 200 do
        li.current_tick = i
        local stat = api.process_step(i)
        pn.UT_EQUAL(stat, 0)
        -- print(">>>", ut.table_count(transients))

        if i == 4 then
            pn.UT_EQUAL(#li.activity, 13)
            -- pn.UT_EQUAL(ut.table_count(transients), 2)
        end

        if i == 40 then
            pn.UT_EQUAL(#li.activity, 48)
            -- pn.UT_EQUAL(ut.table_count(transients), 1)
        end
    end

    pn.UT_EQUAL(#li.activity, 166)
    -- pn.UT_EQUAL(ut.table_count(transients), 1)

    -- Examine collected data.
    --for _, d in ipairs(li.activity) do

    -- s = tx.dump_table(transients, "transients", 1)
    -- print(s)
end

-----------------------------------------------------------------------------
function M.suite_step_types(pn)
    -- Test all functions in step_types.lua

    local n = st.note(1234, 99, 101, 0.4, 10)
    pn.UT_STR_EQUAL(tostring(n), "T:01234 BT:38.2.2 DEV:00 CH:99 NOTE:101 VOL:0.4 DUR:10")

    local res, msg = pcall(st.note, 100001, 88, 111, 0.3, 22)
    pn.UT_FALSE(res)
    pn.UT_STR_CONTAINS(msg, "Invalid integer:100001")

    local c = st.controller(344, 37, 88, 55)
    pn.UT_STR_EQUAL(tostring(c), "T:00344 BT:10.3.0 DEV:00 CH:37 CTRL:88 VAL:55")

    res, msg = pcall(st.controller, 455, 55, 260, 23)
    pn.UT_FALSE(res)
    pn.UT_STR_CONTAINS(msg, "Invalid integer:260")

    local function stub() end
    local f = st.func(508, 122, stub, 0.44)
    pn.UT_STR_EQUAL(tostring(f), "T:00508 BT:15.3.4 DEV:00 CH:122 FUNC:? VOL:0.4")
end


-----------------------------------------------------------------------------
-- Return the test module.
return M
