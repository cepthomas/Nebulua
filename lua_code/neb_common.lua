
-- Flotsam and jetsam.

-- Standard midi.
MAX_MIDI = 127

---------------- These must match C code! ----------
-- Only 4/4 time supported.
BEATS_PER_BAR = 4

-- Our resolution = 32nd note. aka midi DeltaTicksPerQuarterNote.
SUBS_PER_BEAT = 8
SUBS_PER_BAR = SUBS_PER_BEAT * BEATS_PER_BAR

MAX_BAR = 1000
MAX_TICK = MAX_BAR * SUBS_PER_BAR


-----------------------------------------------------------------------------
--- Report a user script syntax error. This is the only place that user script calls error().
-- @param info
function syntax_error(desc, info)
    s = string.format("Syntax error: %s %s", desc, info or "")
    error(s, 3) --TODO1 need to locate the script line.

    -- Maybe use:
--- Gets the file and line of the caller.
-- @param level How deep to look:
--    0 is the getinfo() itself
--    1 is the function that called getinfo() - get_caller_info()
--    2 is the function that called get_caller_info() - usually the one of interest
-- @return { filename, linenumber } or nil if invalid
-- function M.get_caller_info(level)
    -- -- Print failure information.
    -- local caller = ut.get_caller_info(4)
    -- info = info or ""
    -- write_error(caller[1]..":"..caller[2].." "..msg..". "..info)



-- ! lua-L error(message [, level])  Raises an error (see §2.3) with message as the error object. This function never returns.
-- Usually, error adds some information about the error position at the beginning of the message, if the message is a string.
-- The level argument specifies how to get the error position. With level 1 (the default), the error position is where the
-- error function was called. Level 2 points the error to where the function that called error was called; and so on.
-- Passing a level 0 avoids the addition of error position information to the message.
--   ... these trickle up to the caller via luaex_docall/lua_pcall return

end

