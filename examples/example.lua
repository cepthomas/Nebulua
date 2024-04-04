
-- Example Nebulua composition file. This is not actual music.

local neb = require("nebulua") -- lua api
local md  = require("midi_defs")
local bt  = require("bar_time")
local ut  = require("utils")

local inst = md.instruments
local drum = md.drums
local kit = md.drum_kits
local ctrl = md.controllers


neb.log_info("=============== go go go =======================")


------------------------- Config ----------------------------------------

-- Devices
local midi_in = "loopMIDI Port"
local midi_out = "Microsoft GS Wavetable Synth"

-- Channels
local hnd_keys  = neb.create_output_channel(midi_out, 1, inst.AcousticGrandPiano)
local hnd_bass  = neb.create_output_channel(midi_out, 2, inst.AcousticBass)
local hnd_synth = neb.create_output_channel(midi_out, 3, inst.Lead1Square)
local hnd_drums = neb.create_output_channel(midi_out, 10, kit.Jazz)
local hnd_inp1  = neb.create_input_channel(midi_in, 2)
-- etc

------------------------- Vars ----------------------------------------

-- local vars - Volumes.
local synth_vol = 0.8
local drum_vol = 0.8

-- Get some stock chords and scales.
local alg_scale = md.get_notes("G3.Algerian")
local chord_notes = md.get_notes("C4.o7")

-- Create custom scale.
md.create_definition("MY_SCALE", "1 +3 4 -b7")
local my_scale_notes = md.get_notes("B4.MY_SCALE")

-- Aliases.
local snare = drum.AcousticSnare
local bdrum = drum.AcousticBassDrum
local hhcl = drum.ClosedHiHat
local ride = drum.RideCymbal1
local crash = drum.CrashCymbal2
local mtom = drum.HiMidTom



--------------------- Called from app -----------------------------------

-----------------------------------------------------------------------------
-- Init stuff. Required function.
function setup()
    neb.log_info("example initialization")
    math.randomseed(os.time())
    neb.set_tempo(88)
    neb.init(sections)
    return 0

-----------------------------------------------------------------------------
-- Main loop - called every mmtimer increment. Required function.
function step(tick)
    -- Main work.
    neb.process_step(tick) -- required if using composition

    -- Other work.
    boing(60)
    t = BarTime(tick)
    if t.get_beat() == 0 and t.get_sub() == 0 then
        neb.send_controller(hnd_synth, ctrl.Pan, 90)
        -- or...
        neb.send_controller(hnd_keys,  ctrl.Pan, 30)
    end

    return 0
end

-----------------------------------------------------------------------------
-- Handlers for input note events.
function rcv_note(chan_hnd, note_num, velocity)
    neb.log_info("rcv_note %d %d %d", chan_hnd, note_num, velocity)

    if chan_hnd == hbing_bong then
        -- whiz = ...
    end

    neb.send_note(hnd_synth, note_num, velocity, 8)

    return 0
end

-----------------------------------------------------------------------------
-- Handlers for input controller events.
function rcv_controller(chan_hnd, controller, value)
    neb.log_info("rcv_controller") --, chan_hnd, ctlid, value)
    return 0
end


----------------------- User lua functions -------------------------

-----------------------------------------------------------------------------
-- Called from sequence.
local function seq_func(tick)
    local note_num = math.random(0, #alg_scale)
    neb.send_note(hnd_synth, alg_scale[note_num], drum_vol, 8) --0.5)
end

-- Called from section.
function section_func(bar, beat, sub)
    -- do something
end

-----------------------------------------------------------------------------
-- Make a noise.
local function boing(note_num)
    local boinged = false;

    neb.log_info("boing")
    if note_num == 0 then
        note_num = math.random(30, 80)
        boinged = true
        neb.send_note(hnd_synth, note_num, synth_vol, 8) --0.5)
    end
    return boinged
end


------------------------- Composition ---------------------------------------

-- Sequences --
-- template =
-- {
--     -- |........|........|........|........|........|........|........|........|
--     { "|        |        |        |        |        |        |        |        |", "??" },
--     { "|        |        |        |        |        |        |        |        |", "??" },
-- },

local example_seq =
{
    -- | beat 1 | beat 2 |........|........|........|........|........|........|,  WHAT_TO_PLAY
    { "|M-------|--      |        |        |7-------|--      |        |        |", "G4.m7" },
    { "|7-------|--      |        |        |7-------|--      |        |        |",  84 },
    { "|        |        |        |5---    |        |        |        |5-8---  |", "D6" },
    { "|        |        |        |5---    |        |        |        |5-8---  |",  seq_func }
},

local drums_verse =
{
    --|........|........|........|........|........|........|........|........|
    {"|8       |        |8       |        |8       |        |8       |        |", bdrum },
    {"|    8   |        |    8   |    8   |    8   |        |    8   |    8   |", snare },
    {"|        |     8 8|        |     8 8|        |     8 8|        |     8 8|", hhcl }
},

local drums_chorus =
{
    -- |........|........|........|........|........|........|........|........|
    { "|6       |        |6       |        |6       |        |6       |        |", bdrum },
    { "|        |7 7     |        |7 7     |        |7 7     |        |        |", ride },
    { "|        |    4   |        |        |        |    4   |        |        |", mtom },
    { "|        |        |        |        |        |        |        |8       |", crash },
},

local keys_verse =
{
    -- |........|........|........|........|........|........|........|........|
    { "|7-------|--      |        |        |7-------|--      |        |        |", "G4.m7" },
    { "|        |        |        |5---    |        |        |        |5-8---  |", "G4.m6" }
},

local keys_chorus =
{
    -- |........|........|........|........|........|........|........|........|
    { "|6-      |        |        |        |        |        |        |        |", "F4" },
    { "|    5-  |        |        |        |        |        |        |        |", "D#4" },
    { "|        |6-      |        |        |        |        |        |        |", "C4" },
    { "|        |    6-  |        |        |        |        |        |        |", "B4.m7" },
    -- ... 2.0: "F5", 2.4: "D#5", 3.0: "C5", 3.4: "B5.m7", 4.0: "F3", 4.4: "D#3", 5.0: "C3", 5.4: "B3.m7", 6.0: "F2", 6.4: "D#2", 7.0: "C2", 7.4: "B2.m7",
},

local bass_verse =
{
    -- |........|........|........|........|........|........|........|........|
    { "|7   7   |        |        |        |        |        |        |        |", "C2" },
    { "|        |        |        |    7   |        |        |        |        |", "E2" },
    { "|        |        |        |        |        |        |        |    7   |", "A#2" },
},

local bass_chorus =
{
    -- |........|........|........|........|........|........|........|........|
    { "|5   5   |        |5   5   |        |5   5   |        |5   5   |        |", "C2" },
}


-----------------------------------------------------------------------------
sections =
{
    beginning =
    {
        { hnd_keys,  keys_verse,  keys_verse,  keys_verse,  keys_verse },
        { hnd_drums, drums_verse, drums_verse, drums_verse, drums_verse },
        { hnd_bass,  bass_verse,  bass_verse,  bass_verse,  bass_verse }
    },

    middle =
    {
        { hnd_keys,    keys_chorus,  keys_chorus,  keys_chorus,  keys_chorus },
        { hnd_drums,   drums_chorus, drums_chorus, drums_chorus, drums_chorus },
        { hnd_bass,    bass_chorus,  bass_chorus,  bass_chorus,  bass_chorus },
        { hnd_synth,   synth_chorus, nil,          synth_chorus, section_func }
    },

    ending =
    {
        { hnd_keys,  keys_verse,  keys_verse,  keys_verse,  keys_verse },
        { hnd_drums, drums_verse, drums_verse, drums_verse, drums_verse },
        { hnd_bass,  bass_verse,  bass_verse,  bass_verse,  bass_verse }
    }
}

