
-- An example Nebulua composition file. 

-- Import modules this needs.
local ut  = require("lbot_utils")
local neb = require("nebulua")
local mus = require("music_defs")
local mid = require("midi_defs")
local bt  = require("bar_time")

-- Setup for debug. Manually place dbg() statements for breakpoints.
-- ut.config_debug(true)
-- dbg()

-- Aliases for imports - easier typing.
local inst = mid.instruments
local drum = mid.drums
local kit  = mid.drum_kits
local ctrl = mid.controllers

-- Say hello.
neb.log_info('### loading example.lua ###')


------------------------- Configuration -------------------------------

-- Specify midi channels.
local midi_in = "ClickClack"
local hnd_ccin  = neb.create_input_channel(midi_in, 1)

-- Use DAW or VST host.
local midi_out = "loopMIDI Port"
local hnd_keys  = neb.create_output_channel(midi_out, 1, neb.NO_PATCH)
local hnd_bass  = neb.create_output_channel(midi_out, 2, neb.NO_PATCH)
local hnd_synth = neb.create_output_channel(midi_out, 3, neb.NO_PATCH)
local hnd_drums = neb.create_output_channel(midi_out, 10, neb.NO_PATCH)

-- Use builtin GM.
-- local midi_out = "VirtualMIDISynth #1" -- or "Microsoft GS Wavetable Synth"
-- local hnd_keys  = neb.create_output_channel(midi_out, 1, inst.AcousticGrandPiano)
-- local hnd_bass  = neb.create_output_channel(midi_out, 2, inst.AcousticBass)
-- local hnd_synth = neb.create_output_channel(midi_out, 3, inst.Lead1Square)
-- local hnd_drums = neb.create_output_channel(midi_out, 10, kit.Jazz)


------------------------- Variables -----------------------------------


-- Get some stock chords and scales.
-- local my_scale = mus.get_notes_from_string("G3.Algerian")
local my_scale = mus.get_notes_from_string("C4.o7")

-- Create custom note collection.
-- mus.create_definition("MY_SCALE", "1 +3 4 -b7")
-- local my_scale = mus.get_notes_from_string("B4.MY_SCALE")

-- Aliases for instruments - easier typing.
local snare = drum.AcousticSnare
local bdrum = drum.AcousticBassDrum
local hhcl = drum.ClosedHiHat
local ride = drum.RideCymbal1
local crash = drum.CrashCymbal2
local mtom = drum.HiMidTom


-- Forward refs.
local algo_func


------------------------- System Functions -----------------------------

-----------------------------------------------------------------------------
-- Called once to initialize your script stuff. Required.
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
    neb.process_comp()

    return 0
end

-----------------------------------------------------------------------------
-- Main work loop called every subbeat/tick. Required.
function step(tick)

    -- Overhead.
    neb.process_step(tick)

    -- Other work you may want to do.

    -- Do something every new bar.
    local t = BarTime(tick)
    if t.get_beat() == 2 and t.get_sub() == 0 then
        -- neb.send_controller(hnd_synth, ctrl.Pan, 90)
        algo_func(tick)
    end

    return 0
end

-----------------------------------------------------------------------------
-- Handler for input note events. Optional.
function rcv_note(chan_hnd, note_num, volume)
    -- neb.log_debug(string.format("RCV note:%d hnd:%d vol:%f", note_num, chan_hnd, volume))

    if chan_hnd == hnd_ccin then
        -- Play the note.
        neb.send_note(hnd_synth, note_num, volume)--, 0)
    end
    return 0
end

-----------------------------------------------------------------------------
-- Handlers for input controller events. Optional.
function rcv_controller(chan_hnd, controller, value)
    if chan_hnd == hnd_ccin then
        -- Do something.
        neb.log_debug(string.format("RCV controller:%d hnd:%d val:%d", controller, chan_hnd, value))
    end
    return 0
end


----------------------- Local Functions ----------------------------------

-- Function called from sequence.
algo_func = function(tick)
    if my_scale ~= nil then
        local note_num = math.random(1, #my_scale)
        neb.send_note(hnd_synth, my_scale[note_num], 0.8, 3)
    end
end


------------------------- Composition ---------------------------------------

-- Sequences --

local quiet =
{
    { "|        |        |        |        |        |        |        |        |", 0 }
}

local example_seq =
{
    -- | beat 1 | beat 2 |........|........|........|........|........|........|,  WHAT_TO_PLAY
    { "|6-------|--      |        |        |7-------|--      |        |        |", "G4.m7" },
    { "|7-------|--      |        |        |7-------|--      |        |        |",  84 },
    { "|        |        |        |5---    |        |        |        |5-8---  |", "D6" },
}

local drums_verse =
{
    --|........|........|........|........|........|........|........|........|
    {"|8       |        |8       |        |8       |        |8       |        |", bdrum },
    {"|    8   |        |    8   |    8   |    8   |        |    8   |    8   |", snare },
    {"|        |     8 8|        |     8 8|        |     8 8|        |     8 8|", hhcl }
}

local drums_chorus =
{
    -- |........|........|........|........|........|........|........|........|
    { "|6       |        |6       |        |6       |        |6       |        |", bdrum },
    { "|        |7 7     |        |7 7     |        |7 7     |        |        |", ride },
    { "|        |    4   |        |        |        |    4   |        |        |", mtom },
    { "|        |        |        |        |        |        |        |8       |", crash },
}

local keys_verse =
{
    -- |........|........|........|........|........|........|........|........|
    { "|7-------|--      |        |        |7-------|--      |        |        |", "G4.m7" },
    { "|        |        |        |5---    |        |        |        |5-8---  |", "G4.m6" },
}

local keys_chorus =
{
    -- |........|........|........|........|........|........|........|........|
    { "|6-      |        |        |        |        |        |        |        |", "F4" },
    { "|    5-  |        |        |        |        |        |        |        |", "D#4" },
    { "|        |6-      |        |        |        |        |        |        |", "C4" },
    { "|        |    6-  |        |        |        |        |        |        |", "B4.m7" },
}

local bass_verse =
{
    -- |........|........|........|........|........|........|........|........|
    { "|7   7   |        |        |        |        |        |        |        |", "C2" },
    { "|        |        |        |    7   |        |        |        |        |", "E2" },
    { "|        |        |        |        |        |        |        |    7   |", "A#2" },
}

local bass_chorus =
{
    -- |........|........|........|........|........|........|........|........|
    { "|5   5   |        |5   5   |        |5   5   |        |5   5   |        |", "C2" },
}


-----------------------------------------------------------------------------
neb.sect_start("beginning")
neb.sect_chan(hnd_keys,  keys_verse,   keys_verse,   keys_verse,   keys_verse)
neb.sect_chan(hnd_drums, drums_verse,  drums_verse,  drums_verse,  drums_verse)
neb.sect_chan(hnd_bass,  bass_verse,   bass_verse,   bass_verse,   bass_verse)

neb.sect_start("middle")
neb.sect_chan(hnd_keys,  keys_chorus,  keys_chorus,  keys_chorus,  keys_chorus)
neb.sect_chan(hnd_drums, drums_chorus, drums_chorus, drums_chorus, drums_chorus)
neb.sect_chan(hnd_bass,  bass_chorus,  bass_chorus,  bass_chorus,  bass_chorus)

neb.sect_start("ending")
neb.sect_chan(hnd_keys,  keys_verse,   keys_verse,   keys_verse,   keys_verse)
neb.sect_chan(hnd_drums, drums_verse,  drums_verse,  drums_verse,  drums_verse)
neb.sect_chan(hnd_bass,  bass_verse,   bass_verse,   bass_verse,   bass_verse)
