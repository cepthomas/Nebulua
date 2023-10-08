
-- Example Nebulator composition file with some UI demo. This is not actual music.

local log = require("logger")
local ut = require("utils")

local neb = require("nebulua") -- lua api
local api = require("neb_api") -- C# api

local md = require("midi_defs")
local inst = md.instruments
local drum = md.drums
local kit = md.drum_kits
local ctrl = md.controllers

local scale = require("scale")


log.info("=============== go go go =======================")


------------------------- Config ----------------------------------------

channels =
{
    -- outputs
    keys  = { device_id = "midi_out",  channel_num = 1,  patch = inst.AcousticGrandPiano },
    bass  = { device_id = "midi_out",  channel_num = 2,  patch = inst.AcousticBass },
    synth = { device_id = "midi_out",  channel_num = 3,  patch = inst.Lead1Square },
    drums = { device_id = "midi_out",  channel_num = 10, patch = kit.Jazz }, -- for drums TODO2 identify somehow?
    -- inputs
    tune  = { device_id = "midi_in",   channel_num = 1   },
    trig  = { device_id = "virt_key",  channel_num = 2,  }, -- optional: show_note_names
    whiz  = { device_id = "bing_bong", channel_num = 10, }, -- optional: draw_note_grid, min_note, max_note, min_control, max_control
}

-- local vars - Volumes. 
local keys_vol = 0.8
local drum_vol = 0.8

-- Get some stock chords and scales.
local alg_scale = md.get_notes("G3.Algerian")
local chord_notes = md.get_notes("C4.o7")
-- log.info(chord_notes)

-- Create custom scale.
md.create_notes("MY_SCALE", "1 3 4 b7")
local my_scale_notes = md.get_notes("B4.MY_SCALE")
-- log.info(my_scale_notes)

local steps = {}


------------------------- Called from C# core ----------------------------------------

-----------------------------------------------------------------------------
-- Init - called to initialize Nebulator stuff.
function setup()
    log.info("example initialization")
    math.randomseed(os.time())
    -- Load her up.
    steps = neb.process_all(sequences, sections)
    -- Manual patch.
    api.send_patch("synth", inst.Lead1Square)
end

-----------------------------------------------------------------------------
-- Main loop - called every mmtimer increment.
function step(bar, beat, subbeat)
    -- boing(60)

    -- Main work.
    neb.do_step(steps, bar, beat, subbeat)

    -- Selective work.
    if beat == 0 and subbeat == 0 then
        api.send_controller("synth", ctrl.Pan, 90) -- SII
        -- or...
        -- api.send_controller(channels.synth.channel_num, ctrl.Pan, 90) -- SII
        api.send_controller("keys",  ctrl.Pan, 30)
    end
end

-----------------------------------------------------------------------------
-- Handlers for input events.
function input_note(channel_name, note, vel) -- SII
    log.info("input_note") -- string.format("%s", variable_name), channel_name, note, vel)

    if channel_name == "bing_bong" then
        -- whiz = ...
    end

    api.send_note("synth", note, vel, 0.5) -- SIIT
end

-----------------------------------------------------------------------------
-- Handlers for input events.
function input_controller(channel_name, ctlid, value) -- SII
    log.info("input_controller") --, channel_name, ctlid, value)
end

----------------------- User lua functions -------------------------

-----------------------------------------------------------------------------
-- Calc something and play it.
function sequence_func()
    local note_num = math.random(0, #alg_scale)
    api.send_note("synth", alg_scale[note_num], 0.7, 0.5)
end

-----------------------------------------------------------------------------
-- Make a noise.
function boing(note_num)
    local boinged = false;

    log.info("boing")
    if note_num == 0 then
        note_num = Random(30, 80)
        boinged = true
        api.send_note("synth", note_num, VEL, 1.0)
    end
    return boinged
end

------------------------- Composition ---------------------------------------

-- TODO1 volumes could be a user map instead of linear range. optional?
drum_vol = { 0, 5.0, 5.5, 6.0, 6.5, 7.0, 7.5, 8.0, 8.5, 9.0 }
drum_vol_range = [5.0, 9.5]



-----------------------------------------------------------------------------
sequences = {
    graphical_seq = [
        [ "|M-------|--      |        |        |7-------|--      |        |        |", "G4.m7" ], --SS
        [ "|7-------|--      |        |        |7-------|--      |        |        |",  84 ], --SI
        [ "|7-------|--      |        |        |7-------|--      |        |        |",  drum.AcousticSnare ], --SI
        [ "|        |        |        |5---    |        |        |        |5-8---  |", "D6" ] --SS
        [ "|        |        |        |5---    |        |        |        |5-8---  |", sequence_func ] --SF
    ],

    list_seq = [
        [ 0.0, "C2",  7, 0.1 ], --XSM(X)
        [ 0.0, drum.AcousticBassDrum,  4, 0.1 ], --XIM(X)
        [ 0.4,  44,   5, 0.1 ], --XIM(X)
        [ 4.0, sequence_func,  7, 1.0 ], --XFM(X)
        [ 7.4, "A#2", 7, 0.1 ]  --XSM(X)
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


-----------------------------------------------------------------------------
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
        [ synth,   synth_chorus, nil,          synth_chorus, dynamic ]
    ],

    ending = [
        [ keys,  keys_verse,  keys_verse,  keys_verse,  keys_verse ],
        [ drums, drums_verse, drums_verse, drums_verse, drums_verse ],
        [ bass,  bass_verse,  bass_verse,  bass_verse,  bass_verse ]
    ]
}
