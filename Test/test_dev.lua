local ut  = require("lbot_utils")
local st  = require("step_types")
local bt  = require("music_time")
local li  = require("luainterop") -- mock
local api = require("script_api")


local dbg = require("debugex")
dbg.init()
-- dbg()


-- Create the namespace/module.
local M = {}


-- function M.fix_lua_path(s)
--     local _, _, dir = M.get_caller_info(3)
--     if not sx.contains(package.path, dir) then -- already there?
--         package.path = dir..s..';'..package.path
--         -- package.path = './lua/?.lua;./test/lua/?.lua;'..package.path
--     end
-- end

-- ut.fix_lua_path('/mypath')
-- -- print(package.path)
-- pn.UT_STR_CONTAINS(package.path, 'mypath')



-----------------------------------------------------------------------------
function M.suite_xxx(pn)
    -- Test loading script file.

    -- Load test file in protected mode.
    local scrfn = 'script_happy'
    local ok, scr = pcall(require, scrfn)
    pn.UT_TRUE(ok, string.format("Failed to load script: %s\n  => %s ", scrfn, scr))

    -- Process the data.
    local meta = api.process_comp()

    local dumpfn = '_dump.txt'
    api.dump_steps(dumpfn, 's') -- diagnostic


    -- pn.UT_EQUAL(ut.table_count(_section_info), 4)
    -- pn.UT_EQUAL(_section_info['_LENGTH'], 768)
    -- print(ut.dump_table_string(_section_info, 0, '_section_info'))

    -- Look inside.
    -- local steps, transients = _mole()

    -- -- Execute some script steps. Times and counts are based on script_happy.lua observed.
    -- for i = 0, 200 do
    --     api.current_tick = i
    --     stat = api.process_step(i)
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

    -- -- s = ut.dump_table_string(transients, 1, "transients")
    -- -- print(s)


    local ok, ret = pcall(receive_midi_note, 10, 11, 0.3)
    pn.UT_TRUE(ok, string.format("Script function receive_midi_note() failed:\n%s ", ret))

end

-----------------------------------------------------------------------------
-- Return the module.
return M
