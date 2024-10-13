local ut = require("utils")

-- Flotsam and jetsam.

local M = {}

-- Standard midi.
M.MAX_MIDI = 127

---------------- These must match C code! ----------
-- Only 4/4 time supported.
M.BEATS_PER_BAR = 4

-- Our resolution = 32nd note. aka midi DeltaTicksPerQuarterNote.
M.SUBS_PER_BEAT = 8
M.SUBS_PER_BAR = M.SUBS_PER_BEAT * M.BEATS_PER_BAR

M.MAX_BAR = 1000
M.MAX_TICK = M.MAX_BAR * M.SUBS_PER_BAR

-- Return module.
return M
