
-- Example Nebulator composition file with some UI demo. This is not actual music.

local log = require("logger")
local sx = require("stringex")
local ut = require("utils")

local neb = require("nebulua") -- lua api
local api = require("host_api") -- C# api

local md = require("midi_defs")
local inst = md.instruments
local drum = md.drums
local kit = md.drum_kits
local ctrl = md.controllers


log.info("=============== go go go =======================")


------------------------- Config ----------------------------------------

-- channels =
-- {
--     -- outputs
--     keys  = { device_id = "midi_out",  channel_num = 1,  patch = inst.AcousticGrandPiano },
--     bass  = { device_id = "midi_out",  channel_num = 2,  patch = inst.AcousticBass },
--     synth = { device_id = "midi_out",  channel_num = 3,  patch = inst.Lead1Square },
--     drums = { device_id = "midi_out",  channel_num = 10, patch = kit.Jazz }, -- for drums
--     -- inputs
--     tune  = { device_id = "midi_in",   channel_num = 1   },
--     trig  = { device_id = "virt_key",  channel_num = 2,  }, -- optional: show_note_names
--     whiz  = { device_id = "bing_bong", channel_num = 10, }, -- optional: draw_note_grid, min_note, max_note, min_control, max_control
-- }

-- Devices
local midi_in = "xxx zzz"
local midi_out = "abc 123"

-- Channels
local hkeys  = create_channel(midi_out, 1, inst.AcousticGrandPiano)
local hbass  = create_channel(midi_out, 2, inst.AcousticBass)
local hsynth = create_channel(midi_out, 3, inst.Lead1Square)
local hdrums = create_channel(midi_out, 10, kit.Jazz)
local hinp1  = create_channel(midi_in, 2)
-- etc

------------------------- Vars ----------------------------------------

-- local vars - Volumes. 
local keys_vol = 0.8
local drum_vol = 0.8

-- Get some stock chords and scales.
local alg_scale = md.get_notes("G3.Algerian")
local chord_notes = md.get_notes("C4.o7")

-- Create custom scale.
md.create_notes("MY_SCALE", "1 +3 4 -b7")
local my_scale_notes = md.get_notes("B4.MY_SCALE")


------------------------- Called from C# core ----------------------------------------

-----------------------------------------------------------------------------
-- Init stuff.
function setup()
    log.info("example initialization")
    math.randomseed(os.time())

    -- Load her up.
    -- local steps = {}
    -- steps = neb.process_all(sequences, sections)
    neb.process_all(sequences, sections)

    api.set_tempo(100)

    return 0

-----------------------------------------------------------------------------
-- Main loop - called every mmtimer increment.
function step(bar, beat, subbeat)
    -- boing(60)

    -- Main work.
    neb.do_step(steps, bar, beat, subbeat)

    -- Selective work.
    if beat == 0 and subbeat == 0 then
        api.send_controller(hsynth, ctrl.Pan, 90)
        -- or...
        api.send_controller(hkeys,  ctrl.Pan, 30)
    end

    -- -- Plays well with others.
    -- coroutine.yield()

end

-----------------------------------------------------------------------------
-- Handlers for input note events.
function input_note(channel, note, vel) -> hndchan
    log.info("input_note") -- string.format("%s", variable_name), channel, note, vel)

    if channel == hbing_bong then
        -- whiz = ...
    end

    api.send_note("synth", note, vel, 0.5)
end

-----------------------------------------------------------------------------
-- Handlers for input controller events.
function input_controller(channel, ctlid, value)
    log.info("input_controller") --, channel, ctlid, value)
end

----------------------- User lua functions -------------------------

-----------------------------------------------------------------------------
-- Called from sequence.
local function seq_func(bar, beat, subbeat)
    local notenum = math.random(0, #alg_scale)
    api.send_note("synth", alg_scale[notenum], 0.7, 0.5)
end

-- Called from section.
function section_func(bar, beat, subbeat)
    -- do something
end

-----------------------------------------------------------------------------
-- Make a noise.
local function boing(notenum)
    local boinged = false;

    log.info("boing")
    if notenum == 0 then
        notenum = Random(30, 80)
        boinged = true
        api.send_note("synth", notenum, VEL, 1.0)
    end
    return boinged
end

------------------------- Composition ---------------------------------------

-- -- TODO3 volumes could be an optional user map instead of linear range.
-- drum_vol = { 0, 5.0, 5.5, 6.0, 6.5, 7.0, 7.5, 8.0, 8.5, 9.0 }
-- drum_vol_range = { 5.0, 9.5 }



-----------------------------------------------------------------------------
-- aliases
snare = drum.AcousticSnare
bdrum = drum.AcousticBassDrum
hhcl = drum.ClosedHiHat
ride = drum.RideCymbal1
crash = drum.CrashCymbal2
mtom = drum.HiMidTom

-- WHAT_TO_PLAY is a string or integer or function.

sequences =
{
    graphical_seq = -- these are 8 beats long - end with WHAT_TO_PLAY.
    {
        -- |........|........|........|........|........|........|........|........|
        { "|M-------|--      |        |        |7-------|--      |        |        |", "G4.m7" },
        { "|7-------|--      |        |        |7-------|--      |        |        |",  84 },
        { "|7-------|--      |        |        |7-------|--      |        |        |",  snare },
        { "|        |        |        |5---    |        |        |        |5-8---  |", "D6" },
        { "|        |        |        |5---    |        |        |        |5-8---  |",  seq_func }
    },

    list_seq = -- these are terminator beats long - seq[2] is WHAT_TO_PLAY.
    {
        { 0.0, "C2",        7, 0.1 },
        { 0.0,  bdrum,      4, 0.1 },
        { 0.4,  44,         5, 0.1 },
        { 4.0,  seq_func,   7, 1.0 },
        { 7.4, "A#2",       7, 0.1 },
        { 8.0, "",          0, 0.0 }   -- ?? terminator -> length
    },

    keys_verse =
    {
        -- |........|........|........|........|........|........|........|........|
        { "|7-------|--      |        |        |7-------|--      |        |        |", "G4.m7" },
        { "|        |        |        |5---    |        |        |        |5-8---  |", "G4.m6" }
    },

    keys_chorus =
    {
        { 0.0, "F4",    6,   0.2 },
        { 0.4, "D#4",   5,   0.2 },
        { 1.0, "C4",    6,   0.2 },
        { 1.4, "B4.m7", 6,   0.2 },
        { 2.0, "F5",    6,   0.2 },
        { 2.4, "D#5",   5,   0.2 },
        { 3.0, "C5",    6,   0.2 },
        { 3.4, "B5.m7", 6,   0.2 },
        { 4.0, "F3",    6,   0.2 },
        { 4.4, "D#3",   5,   0.2 },
        { 5.0, "C3",    6,   0.2 },
        { 5.4, "B3.m7", 6,   0.2 },
        { 6.0, "F2",    6,   0.2 },
        { 6.4, "D#2",   5,   0.2 },
        { 7.0, "C2",    6,   0.2 },
        { 7.4, "B2.m7", 6,   0.2 }
    },

    bass_verse =
    {
        { 0.0, "C2",    7,   0.1 },
        { 0.4, "C2",    7,   0.1 },
        { 3.5, "E2",    7,   0.1 },
        { 4.0, "C2",    7,   1.0 },
        { 7.4, "A#2",   7,   0.1 }
    },

    bass_chorus =
    {
        { 0.0, "C2",    5,   0.1 },
        { 0.4, "C2",    5,   0.1 },
        { 2.0, "C2",    5,   0.1 },
        { 2.4, "C2",    5,   0.1 },
        { 4.0, "C2",    5,   0.1 },
        { 4.4, "C2",    5,   0.1 },
        { 6.0, "C2",    5,   0.1 },
        { 6.4, "C2",    5,   0.1 }
    },


    drums_verse =
    {
        --|........|........|........|........|........|........|........|........|
        {"|8       |        |8       |        |8       |        |8       |        |", bdrum },
        {"|    8   |        |    8   |    8   |    8   |        |    8   |    8   |", snare },
        {"|        |     8 8|        |     8 8|        |     8 8|        |     8 8|", hhcl }
    },

    drums_chorus =
    {
        { 0.0, bdrum, 6 },
        { 0.0, bdrum, 6 },
        { 1.0, ride,  7 },
        { 1.2, ride,  7 },
        { 1.4, mtom,  4 },
        { 2.0, bdrum, 6 },
        { 3.0, ride,  7 },
        { 3.2, ride,  7 },
        { 4.0, bdrum, 6 },
        { 5.0, ride,  7 },
        { 5.2, ride,  7 },
        { 5.4, mtom,  4 },
        { 6.0, bdrum, 6 },
        { 7.0, crash, 8 }
    },

    something_else =
    {
        { 0.0, "G3",  5, 0.5 },
        { 1.0, "A3",  5, 0.5 },
        { 2.0, "Bb3", 5, 0.5 },
        { 6.0, "C4",  5, 0.5 }
    },
}


-----------------------------------------------------------------------------
sections =
{
    beginning =
    {
        { hkeys,  keys_verse,  keys_verse,  keys_verse,  keys_verse },
        { hdrums, drums_verse, drums_verse, drums_verse, drums_verse },
        { hbass,  bass_verse,  bass_verse,  bass_verse,  bass_verse }
    }
    
    middle =
    {
        { hkeys,    keys_chorus,  keys_chorus,  keys_chorus,  keys_chorus },
        { hdrums,   drums_chorus, drums_chorus, drums_chorus, drums_chorus },
        { hbass,    bass_chorus,  bass_chorus,  bass_chorus,  bass_chorus },
        { hsynth,   synth_chorus, nil,          synth_chorus, section_func }
    },

    ending =
    {
        { hkeys,  keys_verse,  keys_verse,  keys_verse,  keys_verse },
        { hdrums, drums_verse, drums_verse, drums_verse, drums_verse },
        { hbass,  bass_verse,  bass_verse,  bass_verse,  bass_verse }
    }
}

-- -- Return the module.
-- return M
