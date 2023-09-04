--[[
Example Nebulator composition file with some UI demo. This is not actual music.
--]]

local api = require("neb_api")
-- local ut = require("utils")
local scale = require("scale")
local md = require("mididefs")
local inst = md.instruments
local drum = md.drums
local kit = md.drumkits
local ctrl = md.controllers


print("=============== go go go =======================")
-- print(api)



math.randomseed(os.time())

-- Identifiers.
local KEYS  <const> = "keys" 
local BASS  <const> = "bass" 
local SYNTH <const> = "synth"
local DRUMS <const> = "drums"
local TUNE  <const> = "tune" 
local TRIG  <const> = "trig" 
local WHIZ  <const> = "whiz" 


-- All the devices. Also oscout (midi over osc).
devices =
{
    keys  = { dev_type="midi_out", channel=1, patch=inst.AcousticGrandPiano },
    bass  = { dev_type="midi_out", channel=2, patch=inst.AcousticBass },
    synth = { dev_type="midi_out", channel=3, patch=inst.Lead1Square },
    drums = { dev_type="midi_out", channel=10, patch=kit.Jazz }, -- for drums = kit
    tune  = { dev_type="midi_in", channel=1 },
    trig  = { dev_type="virt_key", channel=2, show_note_names=true },  -- optional: shownotenames, keysize
    whiz  = { dev_type="bing_bong", channel=10, draw_note_grid=true } -- optional: minnote, maxnote, mincontrol, maxcontrol, drawnotegrid
}


-- local vars - Volumes. 
local KEYS_VOL = 0.8
local DRUM_VOL = 0.8

-- Create custom scale.
api.create_notes("MY_SCALE", "1 3 4 b7")
local my_scale_notes = api.get_notes("B4.MY_SCALE")
-- print(my_scale_notes)
-- Get some stock chords and scales.
local alg_scale = api.get_notes("G3.Algerian")
local chord_notes = api.get_notes("C4.o7")


------------------------- Init ----------------------------------------------------
-- Called to initialize Nebulator stuff.
function setup()
    info("example initialization")
end


------------------------- Main loop ----------------------------------------------------
-- Called every mmtimer increment.
function step(bar, beat, subdiv)
    boing(60)

    -- Periodic work.
    if beat == 0 and subdiv == 0 then
        api.send_controller(SYNTH, "Pan", 90)
        api.send_controller(KEYS,  "Pan", 30)
    end
end


-------------- Handlers for input events --------------------------
-- Override handler.
function input_note(dev, channel, note, vel)
    info("input_note") -- string.format("%s", variable_name), dev, channel, note, vel)
    api.send_note("synth", note, vel, 0.5)
end

-- Override handler.
function input_controller(dev, channel, ctlid, value)
    info("input_controller") --, dev, channel, ctlid, value)
end

----------------------- Lua functions -------------------------
function algo_func()
    notenum = math.random(0, #alg_scale)
    api.send_note(SYNTH, alg_scale[notenum], 0.7, 0.5)
end

-- User functions.

function boing(notenum)
    boinged = false;

    info("boing")
    if notenum == 0 then
        notenum = Random(30, 80)
        boinged = true
        api.send_note("synth", notenum, VEL, 1.0)
    end
    return boinged
end

------------------------- Build composition ----------------------------------------------------

---- sequences ---- times are beat.subdiv: beat=0-N subdiv=0-7
sequences = {
    keys_verse = {
        { "|7-------|--      |        |        |7-------|--      |        |        |", "G4.m7", KEYS_VOL },
        { "|        |        |        |5---    |        |        |        |5-8---  |", "G4.m6", KEYS_VOL * 0.9 }
    },

    keys_chorus = {
        { 0.0, "F4",    0.7,      0.2 },
        { 0.4, "D#4",   KEYS_VOL, 0.2 },
        { 1.0, "C4",    0.7,      0.2 },
        { 1.4, "B4.m7", 0.7,      0.2 },
        { 2.0, "F5",    0.7,      0.2 },
        { 2.4, "D#5",   KEYS_VOL, 0.2 },
        { 3.0, "C5",    0.7,      0.2 },
        { 3.4, "B5.m7", 0.7,      0.2 },
        { 4.0, "F3",    0.7,      0.2 },
        { 4.4, "D#3",   KEYS_VOL, 0.2 },
        { 5.0, "C3",    0.7,      0.2 },
        { 5.4, "B3.m7", 0.7,      0.2 },
        { 6.0, "F2",    0.7,      0.2 },
        { 6.4, "D#2",   KEYS_VOL, 0.2 },
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
        {"|8       |        |8       |        |8       |        |8       |        |", AcousticBassDrum,  DRUM_VOL},
        {"|    8   |        |    8   |    8   |    8   |        |    8   |    8   |", AcousticSnare,     DRUM_VOL * 0.9},
        {"|        |     8 8|        |     8 8|        |     8 8|        |     8 8|", ClosedHiHat,       DRUM_VOL * 1.1},
    },

    drums_chorus = {
        { 0.0, AcousticBassDrum,  DRUM_VOL },
        { 0.0, AcousticBassDrum,  DRUM_VOL },
        { 1.0, RideCymbal1,       DRUM_VOL },
        { 1.2, RideCymbal1,       DRUM_VOL },
        { 1.4, HiMidTom,          DRUM_VOL },
        { 2.0, AcousticBassDrum,  DRUM_VOL },
        { 3.0, RideCymbal1,       DRUM_VOL },
        { 3.2, RideCymbal1,       DRUM_VOL },
        { 4.0, AcousticBassDrum,  DRUM_VOL },
        { 5.0, RideCymbal1,       DRUM_VOL },
        { 5.2, RideCymbal1,       DRUM_VOL },
        { 5.4, HiMidTom,          DRUM_VOL },
        { 6.0, AcousticBassDrum,  DRUM_VOL },
        { 7.0, CrashCymbal2,      DRUM_VOL },
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
        { synth,   algo_func,   nil,         algo_func,   dynamic },
    },

    ending = {
        { keys,  keys_verse,  keys_verse,  keys_verse,  keys_verse },
        { drums, drums_verse, drums_verse, drums_verse, drums_verse },
        { bass,  bass_verse,  bass_verse,  bass_verse,  bass_verse }
    }
}
