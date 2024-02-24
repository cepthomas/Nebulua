local ut = require("utils")

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


-- -- -----------------------------------------------------------------------------
-- -- --- Report a user script syntax error. Calls error().
-- -- -- @param info
-- -- local function syntax_error(desc, info)
-- --     s = string.format("Syntax error: %s %s", desc, info or "")
-- --     error(s, 4) --TODOX
-- -- end

-- -- TODO2 polish this up:
-- -- During normal operation call error() and allow framework to propogate to the user.
-- -- Unit testing would prefer to continue operating.
-- throw_error = true
-- -----------------------------------------------------------------------------
-- --- Report a user script syntax error. This is the only place that user script calls error().
-- -- @param desc
-- -- @param info
-- -- @return formatted string if not throw_error
-- function syntax_error(level, desc, info)
--     -- Optional: call error() or handle by script.
--     local caller = ut.get_caller_info(level)

--     if throw_error then
--         print(">>>", "throwwwwwwwwwww")
--         s = string.format("%s %s", desc or "", info or "")
--         -- s = string.format("%s(%d): %s %s", caller.filename, caller.linenumber, desc or "", info or "")
--         error(s, level)
--     else
--         print(">>>", "not throw")
--         -- s = string.format("%s %s", desc or "", info or "")
--         s = string.format("%s(%d): %s %s", caller.filename, caller.linenumber, desc or "", info or "")
--     end
--     return s
-- end
