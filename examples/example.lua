
-- An example Nebulua composition file. Warning: this is not actual music.


-- Import modules we need.
local neb = require("nebulua")
local mus = require("music_defs")
local mid = require("midi_defs")
local bt  = require("bar_time")
local ut  = require("utils")

-- Aliases for imports - easier typing.
local inst = mid.instruments
local drum = mid.drums
local kit  = mid.drum_kits
local ctrl = mid.controllers

-- Log something to the application log.
neb.log_info("=============== go go go =======================")


------------------------- Configuration -------------------------------

-- Specify midi devices.
local midi_in = "loopMIDI Port"
local midi_out = "Microsoft GS Wavetable Synth"

-- Specify midi output channels.
local hnd_keys  = neb.create_output_channel(midi_out, 1, inst.AcousticGrandPiano)
local hnd_bass  = neb.create_output_channel(midi_out, 2, inst.AcousticBass)
local hnd_synth = neb.create_output_channel(midi_out, 3, inst.Lead1Square)
local hnd_drums = neb.create_output_channel(midi_out, 10, kit.Jazz)

-- Specify midi input channels.
local hnd_inp1  = neb.create_input_channel(midi_in, 2)


------------------------- Variables -----------------------------------


-- Get some stock chords and scales.
local alg_scale = mus.get_notes_from_string("G3.Algerian")
local chord_notes = mus.get_notes_from_string("C4.o7")

-- Create custom note collection.
mus.create_definition("MY_SCALE", "1 +3 4 -b7")
-- Now it can be used like stock:
local my_scale_notes = mus.get_notes_from_string("B4.MY_SCALE")

-- Aliases for instruments - easier typing.
local snare = drum.AcousticSnare
local bdrum = drum.AcousticBassDrum
local hhcl = drum.ClosedHiHat
local ride = drum.RideCymbal1
local crash = drum.CrashCymbal2
local mtom = drum.HiMidTom

-- Quiet sequence in section. Can't use nil!
local quiet = {}


--------------------- Called from main applicatio ---------------------------

-----------------------------------------------------------------------------
-- Called once to initialize your script stuff. This is a required function!
function setup()
    neb.log_info("example initialization")
    math.randomseed(os.time())
    
    -- How fast?
    neb.set_tempo(88)

    -- Set master volumes.
    neb.set_volume(hnd_keys, 0.7)
    neb.set_volume(hnd_bass, 0.9)
    neb.set_volume(hnd_synth, 0.6)
    neb.set_volume(hnd_drums, 0.9)

    -- This file uses static composition so you must call this!
    neb.process_comp(sections)
    
    return 0

-----------------------------------------------------------------------------
-- Main work loop called every subbeat/tick. This is a required function!
function step(tick)
    -- This file uses static composition so you must call this!
    neb.process_step(tick)

    -- Other work you may want to do.
    boing(60) -- a local function that makes a noise.

    -- Do something every new bar.
    t = BarTime(tick)
    if t.get_beat() == 0 and t.get_sub() == 0 then
        neb.send_controller(hnd_synth, ctrl.Pan, 90)
    end

    return 0
end

-----------------------------------------------------------------------------
-- Handler for input note events. Optional.
function rcv_note(chan_hnd, note_num, velocity)
    neb.log_info("rcv_note %d %d %d", chan_hnd, note_num, velocity)

    if chan_hnd == hnd_inp1 then
        boing(note_num + 10)
    else
        -- Echo the note.
        neb.send_note(hnd_synth, note_num - 10, velocity, 8)
    end
    return 0
end

-----------------------------------------------------------------------------
-- Handlers for input controller events. Optional.
function rcv_controller(chan_hnd, controller, value)
    if chan_hnd == hnd_inp1 then
        -- Do something.
        neb.log_debug("rcv_controller") --, chan_hnd, ctlid, value)
    end
    return 0
end


----------------------- User lua functions ----------------------------------

-- Function called from sequence.
local function seq_func(tick)
    local note_num = math.random(0, #alg_scale)
    neb.send_note(hnd_synth, alg_scale[note_num], 0.9, 8) --0.5)
end

-- Make a noise.
local function boing(note_num)
    local boinged = false;

    neb.log_info("boing")
    if note_num == 0 then
        note_num = math.random(30, 80)
        boinged = true
        neb.send_note(hnd_synth, note_num, 0.7, 8) --0.5)
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

local quiet = { {"|        |        |        |        |        |        |        |        |", 0 } }

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
    },

    ending =
    {
        { hnd_keys,  keys_verse,  keys_verse,  keys_verse,  keys_verse },
        { hnd_drums, drums_verse, drums_verse, drums_verse, drums_verse },
        { hnd_bass,  bass_verse,  bass_verse,  bass_verse,  bass_verse }
    }
}

