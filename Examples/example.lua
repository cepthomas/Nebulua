--[[
Example Nebulator composition file with some UI demo. This is not actual music.
--]]

local api = require("neb_api")
-- local ut = require("utils")
local scale = require("scale")

local md = require("midi_defs")
local inst = md.instruments
local drum = md.drums
local kit = md.drum_kits
local ctrl = md.controllers

local ad = require("app_defs")
local dt = ad.device_types

info("=============== go go go =======================")

math.randomseed(os.time())

channels =
{
    keys  = { device_id="midi_out",  channel=1,  patch=inst.AcousticGrandPiano },
    bass  = { device_id="midi_out",  channel=2,  patch=inst.AcousticBass },
    synth = { device_id="midi_out",  channel=3,  patch=inst.Lead1Square },
    drums = { device_id="midi_out",  channel=10, patch=kit.Jazz }, -- for drums = kit
    tune  = { device_id="midi_in",   channel=1   },
    trig  = { device_id="virt_key",  channel=2,  }, --show_note_names=true },  -- optional: show_note_names
    whiz  = { device_id="bing_bong", channel=10, }, --draw_note_grid=true } -- optional: draw_note_grid, min_note, max_note, min_control, max_control
}

-- local vars - Volumes. 
local keys_vol = 0.8
local drum_vol = 0.8


-- Get some stock chords and scales.
local alg_scale = api.get_notes("G3.Algerian")
local chord_notes = api.get_notes("C4.o7")
info(chord_notes)

-- Create custom scale.
api.create_notes("MY_SCALE", "1 3 4 b7")
local my_scale_notes = api.get_notes("B4.MY_SCALE")
info(my_scale_notes)


------------------------- Called from core ----------------------------------------

-- Init - called to initialize Nebulator stuff.
function setup()
    info("example initialization")
end

-- Main loop - called every mmtimer increment.
function step(bar, beat, subdiv)
    boing(60)

    -- Periodic work.
    if beat == 0 and subdiv == 0 then
        api.send_controller(devices.synth, ctrl.Pan, 90) -- table, int, int
        api.send_controller(devices.keys,  ctrl.Pan, 30)
    end
end

-- Handlers for input events.
function input_note(device, channel, note, vel) -- devices.key?, int, int, int

    local dev = devices[device]

    if device == devices.bing_bong then
        -- whiz  = { type=dt.bing_bong, channel=10, draw_note_grid=true } -- optional: minnote, maxnote, mincontrol, maxcontrol, drawnotegrid

    end

    info("input_note") -- string.format("%s", variable_name), device, channel, note, vel)
    api.send_note(devices.synth, note, vel, 0.5)
end

-- Handlers for input events.
function input_controller(device, channel, ctlid, value) -- ditto
    info("input_controller") --, device, channel, ctlid, value)
end

----------------------- User lua functions -------------------------

-- Calc something and play it.
function algo_func()
    note_num = math.random(0, #alg_scale)
    api.send_note(devices.synth.channel, alg_scale[note_num], 0.7, 0.5)
end

-- Make a noise.
function boing(note_num)
    boinged = false;

    info("boing")
    if note_num == 0 then
        note_num = Random(30, 80)
        boinged = true
        api.send_note(devices.synth.channel, note_num, VEL, 1.0)
    end
    return boinged
end

------------------------- Build composition ---------------------------------------

---- sequences ---- times are beat.subdiv: beat=0-N subdiv=0-7
sequences = {
    keys_verse = {
        { "|7-------|--      |        |        |7-------|--      |        |        |", "G4.m7", keys_vol },
        { "|        |        |        |5---    |        |        |        |5-8---  |", "G4.m6", keys_vol * 0.9 }
    },

    keys_chorus = {
        { 0.0, "F4",    0.7,      0.2 },
        { 0.4, "D#4",   keys_vol, 0.2 },
        { 1.0, "C4",    0.7,      0.2 },
        { 1.4, "B4.m7", 0.7,      0.2 },
        { 2.0, "F5",    0.7,      0.2 },
        { 2.4, "D#5",   keys_vol, 0.2 },
        { 3.0, "C5",    0.7,      0.2 },
        { 3.4, "B5.m7", 0.7,      0.2 },
        { 4.0, "F3",    0.7,      0.2 },
        { 4.4, "D#3",   keys_vol, 0.2 },
        { 5.0, "C3",    0.7,      0.2 },
        { 5.4, "B3.m7", 0.7,      0.2 },
        { 6.0, "F2",    0.7,      0.2 },
        { 6.4, "D#2",   keys_vol, 0.2 },
        { 7.0, "C2",    0.7,      0.2 },
        { 7.4, "B2.m7", 0.7,      0.2 },
    },

    bass_verse = {
        { 0.0, "C2",  0.7, 0.1 },
        { 0.4, "C2",  0.7, 0.1 },
        { 3.5, "E2",  0.7, 0.1 },
        { 4.0, "C2",  0.7, 1.0 },
        { 7.4, "A#2", 0.7, 0.1 },
    },

    bass_chorus = {
        { 0.0, "C2",  0.7, 0.1 },
        { 0.4, "C2",  0.7, 0.1 },
        { 2.0, "C2",  0.7, 0.1 },
        { 2.4, "C2",  0.7, 0.1 },
        { 4.0, "C2",  0.7, 0.1 },
        { 4.4, "C2",  0.7, 0.1 },
        { 6.0, "C2",  0.7, 0.1 },
        { 6.4, "C2",  0.7, 0.1 },
    },

    drums_verse = {
        --|........|........|........|........|........|........|........|........|
        {"|8       |        |8       |        |8       |        |8       |        |", AcousticBassDrum,  drum_vol},
        {"|    8   |        |    8   |    8   |    8   |        |    8   |    8   |", AcousticSnare,     drum_vol * 0.9},
        {"|        |     8 8|        |     8 8|        |     8 8|        |     8 8|", ClosedHiHat,       drum_vol * 1.1},
    },

    drums_chorus = {
        { 0.0, AcousticBassDrum,  drum_vol },
        { 0.0, AcousticBassDrum,  drum_vol },
        { 1.0, RideCymbal1,       drum_vol },
        { 1.2, RideCymbal1,       drum_vol },
        { 1.4, HiMidTom,          drum_vol },
        { 2.0, AcousticBassDrum,  drum_vol },
        { 3.0, RideCymbal1,       drum_vol },
        { 3.2, RideCymbal1,       drum_vol },
        { 4.0, AcousticBassDrum,  drum_vol },
        { 5.0, RideCymbal1,       drum_vol },
        { 5.2, RideCymbal1,       drum_vol },
        { 5.4, HiMidTom,          drum_vol },
        { 6.0, AcousticBassDrum,  drum_vol },
        { 7.0, CrashCymbal2,      drum_vol },
    },

    dynamic = {
        { 0.0, "G3",  0.7, 0.5 },
        { 1.0, "A3",  0.7, 0.5 },
        { 2.0, "Bb3", 0.7, 0.5 },
        { 6.0, "C4",  0.7, 0.5 },
    }
}



---- sections ----
sections = {
    beginning = {
        { keys,  keys_verse,  keys_verse,  keys_verse,  keys_verse },
        { drums, drums_verse, drums_verse, drums_verse, drums_verse },
        { bass,  bass_verse,  bass_verse,  bass_verse,  bass_verse }
    },

    middle = {
        { keys,    keys_chorus,  keys_chorus,  keys_chorus,  keys_chorus },
        { drums,   drums_chorus, drums_chorus, drums_chorus, drums_chorus },
        { bass,    bass_chorus,  bass_chorus,  bass_chorus,  bass_chorus },
        { synth,   algo_func,    nil,          algo_func,    dynamic },
    },

    ending = {
        { keys,  keys_verse,  keys_verse,  keys_verse,  keys_verse },
        { drums, drums_verse, drums_verse, drums_verse, drums_verse },
        { bass,  bass_verse,  bass_verse,  bass_verse,  bass_verse }
    }
}
