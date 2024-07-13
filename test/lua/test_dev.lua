
-- Unit tests for nebulua.lua.

-- local v  = require('validators')
local ut  = require("utils")
local st  = require("step_types")
local bt  = require("bar_time")
local api = require("host_api") -- host api mock
local neb = require("nebulua") -- lua api
local com = require('neb_common')


ut.config_debug(false)


-- Create the namespace/module.
local M = {}



-----------------------------------------------------------------------------
function M.suite_1(pn)
    -- Test loading script file.

    -- Load test file in protected mode.
    local scrfn = 'script_happy'
    local ok, scr = pcall(require, scrfn)
    pn.UT_TRUE(ok, string.format("Failed to load script: %s\n  => %s ", scrfn, scr))

    -- Process the data.
    neb.process_comp(sections)


    dumpfn = 'C:\\Dev\\repos\\Lua\\Nebulua\\_dump.txt', 'w+'
    neb.dump_steps(dumpfn) -- diagnostic


    pn.UT_EQUAL(ut.table_count(section_info), 4)
    pn.UT_EQUAL(section_info['_LENGTH'], 768)
    print(ut.dump_table_string(section_info, false, 'section_info'))

    -- Look inside.
    -- local steps, transients = _mole()

    -- -- Execute some script steps. Times and counts are based on script_happy.lua observed.
    -- for i = 0, 200 do
    --     api.current_tick = i
    --     stat = neb.process_step(i)
    --     pn.UT_EQUAL(stat, 0)
    --     -- print(">>>", ut.table_count(transients))

    --     if i == 4 then
    --         pn.UT_EQUAL(#api.activity, 11)
    --         pn.UT_EQUAL(ut.table_count(transients), 1)
    --     end

    --     if i == 40 then
    --         pn.UT_EQUAL(#api.activity, 23)
    --         pn.UT_EQUAL(ut.table_count(transients), 1)
    --     end
    -- end

    -- pn.UT_EQUAL(#api.activity, 99)
    -- pn.UT_EQUAL(ut.table_count(transients), 0)

    -- -- Examine collected data.
    -- --for _, d in ipairs(api.activity) do

    -- -- s = ut.dump_table_string(transients, true, "transients")
    -- -- print(s)


    ok, ret = pcall(rcv_note, 10, 11, 0.3)
    pn.UT_TRUE(ok, string.format("Script function rcv_note() failed:\n%s ", ret))

end

-----------------------------------------------------------------------------
-- Return the module.
return M
