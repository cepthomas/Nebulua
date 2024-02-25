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
