
-- Unit tests for bar_time.lua.

local ut = require("lbot_utils")
local btut = require("bar_time")


-- ut.config_debug(true)
-- dbg()

-- Create the namespace/module.
local M = {}


-----------------------------------------------------------------------------
function M.setup(pn)
    -- pn.UT_INFO("setup()")
end

-----------------------------------------------------------------------------
function M.teardown(pn)
    -- pn.UT_INFO("teardown()")
end

--[[
-----------------------------------------------------------------------------
function M.suite_bar_time_create(pn)

    -- Basic construction.
    bt = BarTime(12345)
    pn.UT_NOT_NIL(bt)

    pn.UT_EQUAL(bt.get_tick(), 12345)
    pn.UT_EQUAL(bt.get_bar(), 385)
    pn.UT_EQUAL(bt.get_beat(), 3)
    pn.UT_EQUAL(bt.get_sub(), 1)
    pn.UT_STR_EQUAL(tostring(bt), "385.3.1")

    -- Create from music terminology.
    bt = BarTime(129, 1, 6)
    pn.UT_NOT_NIL(bt)
    pn.UT_EQUAL(bt.get_tick(), 4142)
    pn.UT_EQUAL(bt.get_bar(), 129)
    pn.UT_EQUAL(bt.get_beat(), 1)
    pn.UT_EQUAL(bt.get_sub(), 6)
    pn.UT_STR_EQUAL(tostring(bt), "129.1.6")

    -- Parse three part form - time usually.
    bt = BarTime("108.0.7")
    pn.UT_NOT_NIL(bt)
    pn.UT_EQUAL(bt.get_tick(), 3463)
    pn.UT_STR_EQUAL(tostring(bt), "108.0.7")

    bt = BarTime("711.3.0")
    pn.UT_NOT_NIL(bt)
    pn.UT_EQUAL(bt.get_tick(), 22776)
    pn.UT_STR_EQUAL(tostring(bt), "711.3.0")

    -- Parse two part form - duration usually.
    bt = BarTime("1.4")
    pn.UT_NOT_NIL(bt)
    pn.UT_EQUAL(bt.get_tick(), 12)
    pn.UT_STR_EQUAL(tostring(bt), "0.1.4")

    -- Bad input in many ways. Need to use pcall().
    local ok, bt = pcall(BarTime, 5.78)
    pn.UT_FALSE(ok)
    pn.UT_STR_CONTAINS(bt, "Bad constructor: 5.78, nil, nil")

    ok, bt = pcall(BarTime, { me="bad" })
    pn.UT_FALSE(ok)
    pn.UT_STR_CONTAINS(bt, "Bad constructor: table:")

    ok, bt = pcall(BarTime, 25, 5, 2)
    pn.UT_FALSE(ok)
    pn.UT_STR_CONTAINS(bt, "Bad constructor: Invalid integer beat: 5")

    ok, bt = pcall(BarTime, 25, 1, 9)
    pn.UT_FALSE(ok)
    pn.UT_STR_CONTAINS(bt, "Bad constructor: Invalid integer sub: 9")

    ok, bt = pcall(BarTime, "1:2:3")
    pn.UT_FALSE(ok)
    pn.UT_STR_CONTAINS(bt, "Invalid bar time: 1:2:3")

    ok, bt = pcall(BarTime, "78")
    pn.UT_FALSE(ok)
    pn.UT_STR_CONTAINS(bt, "attempt to compare string with number")

    ok, bt = pcall(BarTime, "3:45")
    pn.UT_FALSE(ok)
    pn.UT_STR_CONTAINS(bt, "Invalid bar time: 3:45")

    ok, bt = pcall(BarTime, "1:2:3:4")
    pn.UT_FALSE(ok)
    pn.UT_STR_CONTAINS(bt, "Invalid bar time: 1:2:3:4")

    ok, bt = pcall(BarTime, "1:alpha:5")
    pn.UT_FALSE(ok)
    pn.UT_STR_CONTAINS(bt, "Invalid bar time: 1:alpha:5")
end


-----------------------------------------------------------------------------
function M.suite_bar_time_meta(pn)
    -- Test metamethods: + - == < <="

    -- Test objects.
    local bt1 = BarTime(1109)
    pn.UT_NOT_NIL(bt1)

    local bt2 = BarTime(472)
    pn.UT_NOT_NIL(bt2)

    -- add
    local bt3 = bt1 + bt2
    pn.UT_NOT_NIL(bt3)
    pn.UT_EQUAL(bt3.get_tick(), 1581)
    pn.UT_EQUAL(bt1.get_tick(), 1109)
    pn.UT_EQUAL(bt2.get_tick(), 472)

    bt3 = bt1 + 60
    pn.UT_NOT_NIL(bt3)
    pn.UT_EQUAL(bt3.get_tick(), 1169)

    bt3 = 50 + bt2
    pn.UT_NOT_NIL(bt3)
    pn.UT_EQUAL(bt3.get_tick(), 522)

    -- Add incompatible types.
    local ok, bt = pcall(function() return bt2 + 'p' end)
    pn.UT_FALSE(ok)
    pn.UT_STR_EQUAL(bt, "Invalid data type for operator add")

    -- sub ok
    bt3 = bt1 - bt2
    pn.UT_NOT_NIL(bt3)
    pn.UT_EQUAL(bt3.get_tick(), 637)

    -- sub negative is invalid
    ok, bt = pcall(function() return bt2 - bt1 end)
    pn.UT_FALSE(ok)
    pn.UT_STR_EQUAL(bt, "result is negative")

    -- comparison
    bt3 = BarTime(1109)
    pn.UT_TRUE(bt1 == bt3)
    pn.UT_FALSE(bt1 ~= bt3)

    pn.UT_TRUE(bt2 < bt1)
    pn.UT_FALSE(bt1 < bt2)
    pn.UT_TRUE(bt2 < 473)
    pn.UT_FALSE(472 < bt2)

    pn.UT_TRUE(bt3 <= bt1)
    pn.UT_FALSE(bt1 <= bt2)
    pn.UT_TRUE(bt2 <= 472)
    pn.UT_FALSE(473 <= bt2)

    pn.UT_FALSE(bt2 > bt1)
    pn.UT_TRUE(bt1 > bt2)
    pn.UT_FALSE(bt2 > 473)
    pn.UT_TRUE(473 > bt2)

    pn.UT_TRUE(bt3 >= bt1)
    pn.UT_TRUE(bt1 >= bt2)
    pn.UT_TRUE(bt2 >= 472)
    pn.UT_FALSE(bt2 >= 473)
    pn.UT_TRUE(472 >= bt2)
end

]]

-----------------------------------------------------------------------------
function M.suite_bar_time(pn)
    -- Basic ops.

    local bar, beat, sub = btut.tick_to_bt(12345)
    pn.UT_EQUAL(bar, 385)
    pn.UT_EQUAL(beat, 3)
    pn.UT_EQUAL(sub, 1)

    local s = btut.tick_to_str(12345)
    pn.UT_STR_EQUAL(s, "385.3.1")

    -- Create from music terminology.

    local t = btut.bt_to_tick(129, 1, 6)
    pn.UT_EQUAL(t, 4142)
    s = btut.tick_to_str(t)
    pn.UT_STR_EQUAL(s, "129.1.6")

    -- Two part, duration.
    t = btut.beats_to_tick(179, 5)
    pn.UT_EQUAL(t, 1437)

    -- Parse three part string form - time usually.
    t = btut.str_to_tick("108.0.7")
    pn.UT_EQUAL(t, 3463)

    t = btut.str_to_tick("711.3.0")
    pn.UT_EQUAL(t, 22776)

    -- Parse two part string form - duration usually.
    t = btut.str_to_tick("241.4")
    pn.UT_EQUAL(t, 1932)
    s = btut.tick_to_str(t)
    pn.UT_STR_EQUAL(s, "60.1.4")

    -- Bad input in many ways.

    -- Sub error handler to intercept errors.
    local last_error = ""
    get_error = function()
        e = last_error
        last_error = ""
        return e
    end
    local save_error = error
    error = function(err, level) last_error = err end

    t = btut.bt_to_tick(1001, 1, 5)
    pn.UT_STR_CONTAINS(get_error(), "Invalid bar")

    t = btut.bt_to_tick(10, 4, 3)
    pn.UT_STR_CONTAINS(get_error(), "Invalid beat")

    t = btut.bt_to_tick(10, 2, 11)
    pn.UT_STR_CONTAINS(get_error(), "Invalid sub")

    t = btut.beats_to_tick(4001, 2)
    pn.UT_STR_CONTAINS(get_error(), "Invalid beat")

    t = btut.beats_to_tick(1122, 12)
    pn.UT_STR_CONTAINS(get_error(), "Invalid sub")

    s = btut.str_to_tick("1:2:3")
    pn.UT_STR_CONTAINS(get_error(), "Invalid bar time")

    s = btut.str_to_tick("1.2.3.4")
    pn.UT_STR_CONTAINS(get_error(), "Invalid bar time")

    s = btut.str_to_tick("junk")
    pn.UT_STR_CONTAINS(get_error(), "Invalid bar time")

    s = btut.str_to_tick({"i'm", "a", "table" })
    pn.UT_STR_CONTAINS(get_error(), "Invalid bar time")

    bar, beat, sub = btut.tick_to_bt(93.81)
    pn.UT_STR_CONTAINS(get_error(), "Invalid tick")

    bar, beat, sub = btut.tick_to_bt({1,2,3})
    pn.UT_STR_CONTAINS(get_error(), "Invalid tick")

    s = btut.tick_to_str("tick")
    pn.UT_STR_CONTAINS(get_error(), "Invalid tick")

    -- Restore.
    error = save_error

end

-----------------------------------------------------------------------------
-- Return the test module.
return M
