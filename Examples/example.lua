
-- Example Nebulator composition file with some UI demo. This is not actual music.

-- https://github.com/hishamhm/f-strings

local api = require("neb_api")
local scale = require("scale")

local md = require("midi_defs")
local inst = md.instruments
local drum = md.drums
local kit = md.drum_kits
local ctrl = md.controllers

local ad = require("app_defs")
local dt = ad.device_types

local ut = require("utils")

-- Logging. Defs from the C# logger side.
LOG_TRACE = 0
LOG_DEBUG = 1
LOG_INFO  = 2
LOG_WARN  = 3
LOG_ERROR = 4

api.log(LOG_INFO, "=============== go go go =======================")

math.randomseed(os.time())

channels =
{
    -- outputs
    keys  = { device_id = "midi_out",  channel = 1,  patch = inst.AcousticGrandPiano },
    bass  = { device_id = "midi_out",  channel = 2,  patch = inst.AcousticBass },
    synth = { device_id = "midi_out",  channel = 3,  patch = inst.Lead1Square },
    drums = { device_id = "midi_out",  channel = 10, patch = kit.Jazz }, -- for drums TODO identify somehow?
    -- inputs
    tune  = { device_id = "midi_in",   channel = 1   },
    trig  = { device_id = "virt_key",  channel = 2,  }, -- optional: show_note_names
    whiz  = { device_id = "bing_bong", channel = 10, }, -- optional: draw_note_grid, min_note, max_note, min_control, max_control
}

-- local vars - Volumes. 
local keys_vol = 0.8
local drum_vol = 0.8

-- Get some stock chords and scales.
local alg_scale = api.get_notes("G3.Algerian")
local chord_notes = api.get_notes("C4.o7")
-- api.log(LOG_INFO, chord_notes)

-- Create custom scale.
api.create_notes("MY_SCALE", "1 3 4 b7")
local my_scale_notes = api.get_notes("B4.MY_SCALE")
-- api.log(LOG_INFO, my_scale_notes)


------------------------- Called from core ----------------------------------------

-- Init - called to initialize Nebulator stuff.
function setup()
    api.log(LOG_INFO, "example initialization")
end

-- Main loop - called every mmtimer increment.
function step(bar, beat, subbeat)
    -- boing(60)

    -- Periodic work.
    if beat == 0 and subbeat == 0 then
        api.send_controller("synth", ctrl.Pan, 90) -- string, int, int
        api.send_controller("keys",  ctrl.Pan, 30)
    end
end

-- Handlers for input events.
function input_note(channel, note, vel) -- string?, int, int
    api.log(LOG_INFO, "input_note") -- string.format("%s", variable_name), channel, note, vel)

    if channel == "bing_bong" then
        -- whiz = ...
    end

    api.send_note("synth", note, vel, 0.5) -- table, int, int, dbl
end

-- Handlers for input events.
function input_controller(channel, ctlid, value) -- ditto
    api.log(LOG_INFO, "input_controller") --, channel, ctlid, value)
end

----------------------- User lua functions -------------------------

-- Calc something and play it.
function sequence_func()
    local note_num = math.random(0, #alg_scale)
    api.send_note("synth", alg_scale[note_num], 0.7, 0.5)
end

-- Calc something and play it.
function section_func()
    local note_num = math.random(0, #alg_scale)
    api.send_note("synth", alg_scale[note_num], 0.7, 0.5)
end

-- Make a noise.
function boing(note_num)
    local boinged = false;

    api.log(LOG_INFO, "boing")
    if note_num == 0 then
        note_num = Random(30, 80)
        boinged = true
        api.send_note("synth", note_num, VEL, 1.0)
    end
    return boinged
end

------------------------- Composition ---------------------------------------

---- sequences

-- field types: String Number Integer Function barTime(Number?) Expression? Mapindex(0-9)
-- TODO eliminate strings for notes?
-- TODO type for BarTime? or just use number.
-- TODO volumes could be a user map instead of linear range. optional?
drum_vol = [0, 5.0, 5.5, 6.0, 6.5, 7.0, 7.5, 8.0, 8.5, 9.0 ]
drum_vol_range = [5.0, 9.5]



sequences = {
    graphical_seq = [
        [ "|M-------|--      |        |        |7-------|--      |        |        |", "G4.m7" ], --SS
        [ "|7-------|--      |        |        |7-------|--      |        |        |",  84 ], --SI
        [ "|7-------|--      |        |        |7-------|--      |        |        |",  drum.AcousticSnare ], --SI
        [ "|        |        |        |5---    |        |        |        |5-8---  |", "D6" ] --SS
        [ "|        |        |        |5---    |        |        |        |5-8---  |", sequence_func ] --SF
    ],

    list_seq = [
        [ 0.0, "C2",  7, 0.1 ], --TSM(T)
        [ 0.0, drum.AcousticBassDrum,  4, 0.1 ], --TIM(T)
        [ 0.4,  44,   5, 0.1 ], --TIM(T)
        [ 4.0, sequence_func,  7, 1.0 ], --TFM(T)
        [ 7.4, "A#2", 7, 0.1 ]  --TSM(T)
    ],

    keys_verse = [
        [ "|7-------|--      |        |        |7-------|--      |        |        |", "G4.m7" ],
        [ "|        |        |        |5---    |        |        |        |5-8---  |", "G4.m6" ]
    ],

    keys_chorus = [
        [ 0.0, "F4",    6,      0.2 ],
        [ 0.4, "D#4",   5,      0.2 ],
        [ 1.0, "C4",    6,      0.2 ],
        [ 1.4, "B4.m7", 6,      0.2 ],
        [ 2.0, "F5",    6,      0.2 ],
        [ 2.4, "D#5",   5,      0.2 ],
        [ 3.0, "C5",    6,      0.2 ],
        [ 3.4, "B5.m7", 6,      0.2 ],
        [ 4.0, "F3",    6,      0.2 ],
        [ 4.4, "D#3",   5,      0.2 ],
        [ 5.0, "C3",    6,      0.2 ],
        [ 5.4, "B3.m7", 6,      0.2 ],
        [ 6.0, "F2",    6,      0.2 ],
        [ 6.4, "D#2",   5,      0.2 ],
        [ 7.0, "C2",    6,      0.2 ],
        [ 7.4, "B2.m7", 6,      0.2 ]
    ],

    bass_verse = [
        [ 0.0, "C2",    7,   0.1 ],
        [ 0.4, "C2",    7,   0.1 ],
        [ 3.5, "E2",    7,   0.1 ],
        [ 4.0, "C2",    7,   1.0 ],
        [ 7.4, "A#2",   7,   0.1 ]
    ],

    bass_chorus = [
        [ 0.0, "C2",  5,     0.1 ],
        [ 0.4, "C2",  5,     0.1 ],
        [ 2.0, "C2",  5,     0.1 ],
        [ 2.4, "C2",  5,     0.1 ],
        [ 4.0, "C2",  5,     0.1 ],
        [ 4.4, "C2",  5,     0.1 ],
        [ 6.0, "C2",  5,     0.1 ],
        [ 6.4, "C2",  5,     0.1 ]
    ],

    drums_verse = [
        --|........|........|........|........|........|........|........|........|
        ["|8       |        |8       |        |8       |        |8       |        |", drum.AcousticBassDrum ],
        ["|    8   |        |    8   |    8   |    8   |        |    8   |    8   |", drum.AcousticSnare ],
        ["|        |     8 8|        |     8 8|        |     8 8|        |     8 8|", drum.ClosedHiHat ]
    ],

    drums_chorus = [
        [ 0.0, drum.AcousticBassDrum, 6 ],
        [ 0.0, drum.AcousticBassDrum, 6 ],
        [ 1.0, drum.RideCymbal1,      7 ],
        [ 1.2, drum.RideCymbal1,      7 ],
        [ 1.4, drum.HiMidTom,         4 ],
        [ 2.0, drum.AcousticBassDrum, 6 ],
        [ 3.0, drum.RideCymbal1,      7 ],
        [ 3.2, drum.RideCymbal1,      7 ],
        [ 4.0, drum.AcousticBassDrum, 6 ],
        [ 5.0, drum.RideCymbal1,      7 ],
        [ 5.2, drum.RideCymbal1,      7 ],
        [ 5.4, drum.HiMidTom,         4 ],
        [ 6.0, drum.AcousticBassDrum, 6 ],
        [ 7.0, drum.CrashCymbal2,     8 ]
    ],

    dynamic = [
        [ 0.0, "G3",  5, 0.5 ],
        [ 1.0, "A3",  5, 0.5 ],
        [ 2.0, "Bb3", 5, 0.5 ],
        [ 6.0, "C4",  5, 0.5 ]
    ],
}


---- sections ----
sections = {
    beginning = [
        [ keys,  keys_verse,  keys_verse,  keys_verse,  keys_verse ],
        [ drums, drums_verse, drums_verse, drums_verse, drums_verse ],
        [ bass,  bass_verse,  bass_verse,  bass_verse,  bass_verse ]
    ]
    
    middle = [
        [ keys,    keys_chorus,  keys_chorus,  keys_chorus,  keys_chorus ],
        [ drums,   drums_chorus, drums_chorus, drums_chorus, drums_chorus ],
        [ bass,    bass_chorus,  bass_chorus,  bass_chorus,  bass_chorus ],
        [ synth,   section_func,    nil,          section_func,    dynamic ]
    ],

    ending = [
        [ keys,  keys_verse,  keys_verse,  keys_verse,  keys_verse ],
        [ drums, drums_verse, drums_verse, drums_verse, drums_verse ],
        [ bass,  bass_verse,  bass_verse,  bass_verse,  bass_verse ]
    ]
}
