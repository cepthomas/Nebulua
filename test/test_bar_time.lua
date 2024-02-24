
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
    bt = BT(12345)
    pn.UT_NOT_NIL(bt)

    pn.UT_EQUAL(bt.get_tick(), 12345)
    pn.UT_EQUAL(bt.get_bar(), 385)
    pn.UT_EQUAL(bt.get_beat(), 3)
    pn.UT_EQUAL(bt.get_sub(), 1)
    pn.UT_STR_EQUAL(tostring(bt), "385:3:1")

    -- Create from music terminology.
    bt = BT(129, 1, 6)
    pn.UT_NOT_NIL(bt)
    pn.UT_EQUAL(bt.get_tick(), 4142)
    pn.UT_EQUAL(bt.get_bar(), 129)
    pn.UT_EQUAL(bt.get_beat(), 1)
    pn.UT_EQUAL(bt.get_sub(), 6)
    pn.UT_STR_EQUAL(tostring(bt), "129:1:6")

    -- Parse three part form - time usually.
    bt = BT("108:0:7")
    pn.UT_NOT_NIL(bt)
    pn.UT_EQUAL(bt.get_tick(), 3463)
    pn.UT_STR_EQUAL(tostring(bt), "108:0:7")

    bt = BT("711:3:0")
    pn.UT_NOT_NIL(bt)
    pn.UT_EQUAL(bt.get_tick(), 22776)
    pn.UT_STR_EQUAL(tostring(bt), "711:3:0")

    -- Parse two part form - duration usually.
    bt = BT("1:4")
    pn.UT_NOT_NIL(bt)
    pn.UT_EQUAL(bt.get_tick(), 12)
    pn.UT_STR_EQUAL(tostring(bt), "0:1:4")


    -- Bad input in many ways.
    bt = BT(5.78)
    pn.UT_NIL(bt)
    pn.UT_STR_EQUAL(bt, "Invalid integer tick: 5.78")
    pn.UT_EQUAL(bt.get_tick(), 0)
    pn.UT_STR_EQUAL(tostring(bt), "Invalid integer tick: 5.78")

    bt = BT({ me="bad" })
    pn.UT_NIL(bt)
    pn.UT_STR_EQUAL(bt, "Invalid integer tick: table")
    pn.UT_EQUAL(bt.get_tick(), 0)
    pn.UT_STR_EQUAL(tostring(bt), "Invalid integer tick: table")

    bt = BT(25, 5, 2)
    pn.UT_NOT_NIL(bt)
    pn.UT_STR_EQUAL(bt, "Invalid integer beat: 5")
    pn.UT_EQUAL(bt.get_tick(), 0)
    pn.UT_STR_EQUAL(tostring(bt), "Invalid integer beat: 5")

    bt = BT(25, 1, 9)
    pn.UT_NOT_NIL(bt)
    pn.UT_STR_EQUAL(bt, "Invalid integer sub: 9")
    pn.UT_EQUAL(bt.get_tick(), 0)
    pn.UT_STR_EQUAL(tostring(bt), "Invalid integer sub: 9")

    bt = BT("1.2.3")
    pn.UT_NIL(bt)
    pn.UT_STR_EQUAL(bt, "Invalid time: 1.2.3")

    bt = BT("78")
    pn.UT_NIL(bt)
    pn.UT_STR_EQUAL(bt, "Invalid time: 78")

    bt = BT("3.45")
    pn.UT_NIL(bt)
    pn.UT_STR_EQUAL(bt, "Invalid time: 3.45")

    bt = BT("1:2:3:4")
    pn.UT_NIL(bt)
    pn.UT_STR_EQUAL(bt, "Invalid time: 1:2:3:4")

    bt = BT("1:alpha:5")
    pn.UT_NIL(bt)
    pn.UT_STR_EQUAL(bt, "Invalid time: 1:alpha:5")

    bt = BT({ 1, 2 })
    pn.UT_NIL(bt)
    pn.UT_STR_EQUAL(bt, "Not a string")
end



-- -----------------------------------------------------------------------------
-- function M.suite_bar_time_create(pn)

--     -- Basic construction.
--     bt = BT(12345)
--     pn.UT_NIL(bt.get_err())

--     pn.UT_EQUAL(bt.get_tick(), 12345)
--     pn.UT_EQUAL(bt.get_bar(), 385)
--     pn.UT_EQUAL(bt.get_beat(), 3)
--     pn.UT_EQUAL(bt.get_sub(), 1)
--     pn.UT_STR_EQUAL(tostring(bt), "385:3:1")

--     -- Feed it garbage.
--     bt = BT(5.78)
--     pn.UT_NOT_NIL(bt.get_err())
--     pn.UT_STR_EQUAL(bt.get_err(), "Invalid integer tick: 5.78")
--     pn.UT_EQUAL(bt.get_tick(), 0)
--     pn.UT_STR_EQUAL(tostring(bt), "Invalid integer tick: 5.78")

--     bt = BT({ me="bad" })
--     pn.UT_NOT_NIL(bt.get_err())
--     pn.UT_STR_EQUAL(bt.get_err(), "Invalid integer tick: table")
--     pn.UT_EQUAL(bt.get_tick(), 0)
--     pn.UT_STR_EQUAL(tostring(bt), "Invalid integer tick: table")

--     -- Create from music terminology.
--     bt.from_bar(129, 1, 6)
--     pn.UT_NIL(bt.get_err())
--     pn.UT_EQUAL(bt.get_tick(), 4142)
--     pn.UT_EQUAL(bt.get_bar(), 129)
--     pn.UT_EQUAL(bt.get_beat(), 1)
--     pn.UT_EQUAL(bt.get_sub(), 6)
--     pn.UT_STR_EQUAL(tostring(bt), "129:1:6")

--     bt.from_bar(25, 5, 2)
--     pn.UT_NOT_NIL(bt.get_err())
--     pn.UT_STR_EQUAL(bt.get_err(), "Invalid integer beat: 5")
--     pn.UT_EQUAL(bt.get_tick(), 0)
--     pn.UT_STR_EQUAL(tostring(bt), "Invalid integer beat: 5")

--     bt.from_bar(25, 1, 9)
--     pn.UT_NOT_NIL(bt.get_err())
--     pn.UT_STR_EQUAL(bt.get_err(), "Invalid integer sub: 9")
--     pn.UT_EQUAL(bt.get_tick(), 0)
--     pn.UT_STR_EQUAL(tostring(bt), "Invalid integer sub: 9")

--     -- Three part form - time usually.
--     bt.parse("108:0:7")
--     pn.UT_NIL(bt.get_err())
--     pn.UT_EQUAL(bt.get_tick(), 3463)
--     pn.UT_STR_EQUAL(tostring(bt), "108:0:7")

--     bt.parse("711:3:0")
--     pn.UT_NIL(bt.get_err())
--     pn.UT_EQUAL(bt.get_tick(), 22776)
--     pn.UT_STR_EQUAL(tostring(bt), "711:3:0")

--     -- Two part form - duration usually.
--     bt.parse("1:4")
--     pn.UT_NIL(bt.get_err())
--     pn.UT_EQUAL(bt.get_tick(), 12)
--     pn.UT_STR_EQUAL(tostring(bt), "0:1:4")

--     -- Bad input in many ways.
--     bt.parse("1.2.3")
--     pn.UT_NOT_NIL(bt.get_err())
--     pn.UT_STR_EQUAL(bt.get_err(), "Invalid time: 1.2.3")

--     bt.parse("78")
--     pn.UT_NOT_NIL(bt.get_err())
--     pn.UT_STR_EQUAL(bt.get_err(), "Invalid time: 78")

--     bt.parse("3.45")
--     pn.UT_NOT_NIL(bt.get_err())
--     pn.UT_STR_EQUAL(bt.get_err(), "Invalid time: 3.45")

--     bt.parse("1:2:3:4")
--     pn.UT_NOT_NIL(bt.get_err())
--     pn.UT_STR_EQUAL(bt.get_err(), "Invalid time: 1:2:3:4")

--     bt.parse("1:alpha:5")
--     pn.UT_NOT_NIL(bt.get_err())
--     pn.UT_STR_EQUAL(bt.get_err(), "Invalid time: 1:alpha:5")

--     bt.parse({ 1, 2 })
--     pn.UT_NOT_NIL(bt.get_err())
--     pn.UT_STR_EQUAL(bt.get_err(), "Not a string")
-- end



-----------------------------------------------------------------------------
function M.suite_bar_time_meta(pn)
    -- Metamethods: + - == < <="

    -- Test objects.
    bt1 = BT(1109)
    pn.UT_NOT_NIL(bt1)

    bt2 = BT(472)
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
    bt3 = bt2 + 'p'
    pn.UT_NIL(bt3)

    -- sub
    bt3 = bt1 - bt2
    pn.UT_NOT_NIL(bt3)
    pn.UT_EQUAL(bt3.get_tick(), 637)

    -- negative is invalid
    bt3 = bt2 - bt1
    pn.UT_NIL(bt3)
    -- pn.UT_EQUAL(bt3.get_tick(), 0)

    -- comparison
    bt3 = BT(1109)
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
