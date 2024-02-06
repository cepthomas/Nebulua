
-- Unit tests for nebulua.lua.  TODO1

local ut = require("utils")
local st = require("step_types")

-- Create the namespace/module.
local M = {}


--[[
--- Process all sequences into discrete steps. Sections are stored as is.
-- @param sequences table user sequence specs
-- @param sections table user section specs
-- @return list of step_info ordered by subbeat
function M.process_all(sequences, sections)

--- Parse a pattern.
-- @param notes_src like: { "|M-------|--      |        |        |7-------|--      |        |        |", "G4.m7" }
-- @return partially filled-in step_info list
function parse_graphic_notes(notes_src)

--- Description
-- @param notes_src like: { 0.4, 44, 5, 0.4 }
-- @return partially filled-in type_info list
function parse_explicit_notes(notes_src)

--- Process notes at this time.
-- @param name type desc
-- @return type desc
function M.do_step(send_stuff, bar, beat, subbeat)

--- Construct a subbeat from beat.subbeat representation as a double.
-- @param d number value to convert
-- @return type desc
function M.to_subbeats(dbeat)


M.UT_CLOSE(val1, val2, tol)
M.UT_EQUAL(val1, val2)
M.UT_ERROR(info)
M.UT_FALSE(expr)
M.UT_GREATER(val1, val2)
M.UT_GREATER_OR_EQUAL(val1, val2)
M.UT_INFO(info)
M.UT_LESS(val1, val2)
M.UT_LESS_OR_EQUAL(val1, val2)
M.UT_NIL(expr)
M.UT_NOT_EQUAL(val1, val2)
M.UT_NOT_NIL(expr)
M.UT_TRUE(expr)

]]


-----------------------------------------------------------------------------
function M.setup(pn)
    -- pn.UT_INFO("setup()!!!")
end

-----------------------------------------------------------------------------
function M.teardown(pn)
    -- pn.UT_INFO("teardown()!!!")
end


-- if use_dbgr then
--     have_dbgr, dbg = pcall(require, "debugger")
--     if not have_dbgr then
--         print(dbg)
--     end
-- end

-----------------------------------------------------------------------------
function M.suite_step_info(pn)
    pn.UT_INFO("Test all functions in step_types.lua")

    note1 = StepNote(1234, 99)
    pn.UT_EQUAL(note1, "Ut")

end

-----------------------------------------------------------------------------
-- Return the module.
return M
