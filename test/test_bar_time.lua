
-- Unit tests for bar_time.lua.

local v  = require('validators')
local ut = require("utils")
local bt = require("bar_time")
require('neb_common')
throw_error = false

-- ut.config_debug(true)


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
function M.suite_bar_time_create(pn)

    -- Basic construction.
    bt = BarTime(12345)
    pn.UT_NOT_NIL(bt)

    pn.UT_EQUAL(bt.get_tick(), 12345)
    pn.UT_EQUAL(bt.get_bar(), 385)
    pn.UT_EQUAL(bt.get_beat(), 3)
    pn.UT_EQUAL(bt.get_sub(), 1)
    pn.UT_STR_EQUAL(tostring(bt), "385:3:1")

    -- Create from music terminology.
    bt = BarTime(129, 1, 6)
    pn.UT_NOT_NIL(bt)
    pn.UT_EQUAL(bt.get_tick(), 4142)
    pn.UT_EQUAL(bt.get_bar(), 129)
    pn.UT_EQUAL(bt.get_beat(), 1)
    pn.UT_EQUAL(bt.get_sub(), 6)
    pn.UT_STR_EQUAL(tostring(bt), "129:1:6")

    -- Parse three part form - time usually.
    bt = BarTime("108:0:7")
    pn.UT_NOT_NIL(bt)
    pn.UT_EQUAL(bt.get_tick(), 3463)
    pn.UT_STR_EQUAL(tostring(bt), "108:0:7")

    bt = BarTime("711:3:0")
    pn.UT_NOT_NIL(bt)
    pn.UT_EQUAL(bt.get_tick(), 22776)
    pn.UT_STR_EQUAL(tostring(bt), "711:3:0")

    -- Parse two part form - duration usually.
    bt = BarTime("1:4")
    pn.UT_NOT_NIL(bt)
    pn.UT_EQUAL(bt.get_tick(), 12)
    pn.UT_STR_EQUAL(tostring(bt), "0:1:4")

    -- Bad input in many ways. Need to use pcall().
    ok, bt = pcall(BarTime, 5.78)
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

    ok, bt = pcall(BarTime, "1.2.3")
    pn.UT_FALSE(ok)
    pn.UT_STR_CONTAINS(bt, "Invalid time: 1.2.3")

    ok, bt = pcall(BarTime, "78")
    pn.UT_FALSE(ok)
    pn.UT_STR_CONTAINS(bt, "attempt to compare string with number")

    ok, bt = pcall(BarTime, "3.45")
    pn.UT_FALSE(ok)
    pn.UT_STR_CONTAINS(bt, "Invalid time: 3.45")

    ok, bt = pcall(BarTime, "1:2:3:4")
    pn.UT_FALSE(ok)
    pn.UT_STR_CONTAINS(bt, "Invalid time: 1:2:3:4")

    ok, bt = pcall(BarTime, "1:alpha:5")
    pn.UT_FALSE(ok)
    pn.UT_STR_CONTAINS(bt, "Invalid time: 1:alpha:5")
end


-----------------------------------------------------------------------------
function M.suite_bar_time_meta(pn)
    -- Test metamethods: + - == < <="

    -- Test objects.
    bt1 = BarTime(1109)
    pn.UT_NOT_NIL(bt1)

    bt2 = BarTime(472)
    pn.UT_NOT_NIL(bt2)

    -- add
    bt3 = bt1 + bt2
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
    ok, bt = pcall(function() return bt2 + 'p' end)
    pn.UT_FALSE(ok)
    pn.UT_STR_EQUAL(bt, "Invalid datatype for operator add")

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


-----------------------------------------------------------------------------
-- Return the test module.
return M
