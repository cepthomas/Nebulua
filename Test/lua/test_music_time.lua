
-- Unit tests for music_time.lua.

-- local ut = require("lbot_utils")
local mt = require("music_time")


-- Create the namespace/module.
local M = {}


-- -----------------------------------------------------------------------------
-- function M.setup(pn)
-- end

-- -----------------------------------------------------------------------------
-- function M.teardown(pn)
-- end

-----------------------------------------------------------------------------
function M.suite_music_time(pn)
    -- Basic ops.

    local bar, beat, sub = mt.tick_to_mt(12345)
    pn.UT_EQUAL(bar, 385)
    pn.UT_EQUAL(beat, 3)
    pn.UT_EQUAL(sub, 1)

    local s = mt.tick_to_str(12345)
    pn.UT_STR_EQUAL(s, "385.3.1")

    -- Create from music terminology.

    local t = mt.mt_to_tick(129, 1, 6)
    pn.UT_EQUAL(t, 4142)
    s = mt.tick_to_str(t)
    pn.UT_STR_EQUAL(s, "129.1.6")

    -- Two part, duration.
    t = mt.beats_to_tick(179, 5)
    pn.UT_EQUAL(t, 1437)

    -- Parse three part string form - time usually.
    t = mt.str_to_tick("108.0.7")
    pn.UT_EQUAL(t, 3463)

    t = mt.str_to_tick("711.3.0")
    pn.UT_EQUAL(t, 22776)

    -- Parse two part string form - duration usually.
    t = mt.str_to_tick("241.4")
    pn.UT_EQUAL(t, 1932)
    s = mt.tick_to_str(t)
    pn.UT_STR_EQUAL(s, "60.1.4")

    -- Bad input in many ways.
    local res = pcall(mt.mt_to_tick, 1001, 1, 5)
    pn.UT_FALSE(res)

    res = pcall(mt.mt_to_tick, 10, 4, 3)
    pn.UT_FALSE(res)

    res = pcall(mt.mt_to_tick, 10, 2, 11)
    pn.UT_FALSE(res)

    res = pcall(mt.beats_to_tick, 4001, 2)
    pn.UT_FALSE(res)

    res = pcall(mt.beats_to_tick, 1122, 12)
    pn.UT_FALSE(res)

    res = pcall(mt.str_to_tick, "1:2:3")
    pn.UT_FALSE(res)

    res = pcall(mt.str_to_tick, "1.2.3.4")
    pn.UT_FALSE(res)

    res = pcall(mt.str_to_tick, "junk")
    pn.UT_FALSE(res)

    res = pcall(mt.str_to_tick, {"i'm", "a", "table" })
    pn.UT_FALSE(res)

    res = pcall(mt.tick_to_mt, 93.81)
    pn.UT_FALSE(res)

    res = pcall(mt.tick_to_mt, {1,2,3})
    pn.UT_FALSE(res)

    res = pcall(mt.tick_to_str, "tick")
    pn.UT_FALSE(res)

end

-----------------------------------------------------------------------------
-- Return the test module.
return M
