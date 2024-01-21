
-- Unit tests for music_defs.lua and midi_defs.lua. TODO1-NEB

local ut = require("utils")

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
function M.suite_music defs(pn)
    pn.UT_INFO("Test all functions in music_defs.lua")

    pn.UT_INFO("Verify that this info line appears in the report file.")
    pn.UT_ERROR("Verify that this error line appears in the report file.")

    pn.UT_TRUE(2 + 2 == 4) -- pass
    pn.UT_TRUE(2 + 2 == 5) -- fail

    pn.UT_FALSE(2 + 2 == 4) -- fail
    pn.UT_FALSE(2 + 2 == 5) -- pass

    pn.UT_NIL(nil) -- pass
    pn.UT_NIL(2) -- fail

end

-----------------------------------------------------------------------------
-- Return the module.
return M
