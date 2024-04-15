
-- Script for unit test - the happy path.

-- print('!!!', package.path)

local neb = require("nebulua")
local mus = require("music_defs")
local bt  = require("bar_time")


neb.log_info("=============== Is this thing on? ===============")


------------------------- Config ----------------------------------------

-- Device names
local dev_out1 = "Microsoft GS Wavetable Synth"
local dev_out2 = "loopMIDI Port"
local dev_in1 = "loopMIDI Port"

-- Channels
local hnd_piano = neb.create_output_channel(dev_out1, 1, 2)
local hnd_synth = neb.create_output_channel(dev_out1, 2, 90)
local hnd_drums = neb.create_output_channel(dev_out2, 10, 16)
local hnd_input = neb.create_input_channel(dev_in1, 3)


------------------------- Vars ----------------------------------------

-- Get some stock chords and scales.
local alg_scale = mus.get_notes_from_string("G3.Algerian")
local chord_notes = mus.get_notes_from_string("C4.o7")

--------------------- Called from app -----------------------------------

-----------------------------------------------------------------------------
-- Init stuff. Required function.
function setup()

    neb.process_comp(sections) -- required if using composition

    -- Set master volumes.
    neb.set_volume(hnd_piano, 0.6)
    neb.set_volume(hnd_drums, 0.9)

    neb.set_tempo(95)

    return 0
end

-----------------------------------------------------------------------------
-- Main loop - called every mmtimer increment. Required function.
function step(tick)
    neb.process_step(tick) -- required if using composition

    -- Selective work.
    t = BarTime(tick)
    if t.beat == 0 and t.sub == 0 then
        neb.send_controller(hnd_synth, 50, 51)
    end

    if t.beat == 1 and t.sub == 4 then
        -- neb.send_controller(hinstrument2,  60, 61)
    end

    return 0
end

-----------------------------------------------------------------------------
-- Handler for input note events. Optional.
function rcv_note(chan_hnd, note_num, volume)
    local s = string.format("rcv_note: %d %d %f", chan_hnd, note_num, volume)
    neb.log_info(s)

    if chan_hnd == hnd_input then
        neb.send_note(hnd_synth, note_num + 1, volume * 0.5, 8)
    end
    return 0
end

-----------------------------------------------------------------------------
-- Handler for input controller events. Optional.
function rcv_controller(chan_hnd, controller, value)
    local s = string.format("rcv_controller: %d %d %d", chan_hnd, controller, value)
    neb.log_info(s)
    return 0
end

----------------------- Custom user functions -------------------------

-----------------------------------------------------------------------------
-- Called from sequence.
local function my_seq_func(tick)
    local note_num = math.random(0, #alg_scale)
    neb.send_note(hnd_synth, alg_scale[note_num], 0.7, 1)
end

-----------------------------------------------------------------------------
-- Called from section.
local function my_section_func(tick)
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
        neb.send_note(hnd_synth, note_num, 0.7, 8)
    end
    return boinged
end


------------------------- Composition ---------------------------------------

-- Sequences --

local piano_verse =
{
    -- |........|........|........|........|........|........|........|........|
    { "|7-------|--      |        |        |7-------|--      |        |        |", "G4.m7" },
    { "|        |        |        |5---    |        |        |        |5-8---  |", "G4.m6" }
}

local piano_chorus =
{
    -- |........|........|........|........|........|........|........|........|
    { "|6-      |        |        |        |        |        |        |        |", "F4" },
    { "|    5   |        |        |        |        |        |      7 |        |", my_seq_func },
    { "|        |6-      |        |        |        |        |        |        |", "C4" },
    { "|        |    6-  |        |        |        |        |        |        |", "B4.m7" },
}

local drums_verse =
{
    -- |........|........|........|........|........|........|........|........|
    { "|8       |        |8       |        |8       |        |8       |        |", 51 },
    { "|    8   |        |    8   |    8   |    8   |        |    8   |    8   |", 11 },
    { "|        |     8 8|        |     8 8|        |     8 8|        |     8 8|", 38 }
}

local drums_chorus =
{
    -- |........|........|........|........|........|........|........|........|
    { "|6       |        |6       |        |6       |        |6       |        |", 38 },
    { "|        |7 7     |        |7 7     |        |7 7     |        |        |", 60 },
    { "|        |    4   |        |        |        |    4   |        |        |", 35 },
    { "|        |        |        |        |        |        |        |8       |", 49 },
}



-- Sections --

-- Identify space. Can't use nil!
quiet = {}

neb.sect_start("beginning")
neb.sect_chan(hnd_piano, piano_verse,  quiet,         piano_verse,  piano_verse  ) -- 6ch
neb.sect_chan(hnd_drums, drums_verse,  drums_verse,   quiet,        drums_verse  ) -- 9ch

neb.sect_start("middle")
neb.sect_chan(hnd_piano, quiet,         piano_chorus, piano_chorus, piano_chorus ) -- 12ch
neb.sect_chan(hnd_drums, drums_chorus,  drums_chorus, drums_chorus, drums_chorus ) -- 16ch

neb.sect_start("ending")
neb.sect_chan(hnd_piano, piano_verse,   piano_verse,  piano_verse,  quiet        ) -- 6ch
neb.sect_chan(hnd_drums, drums_verse,   drums_verse,  drums_verse,  drums_chorus ) -- 13ch

-- total 62ch

--[[
Each seq is 64t.
total is 12seq => 768t

drums_verse is 18evt X 5 => 90evt
piano_chorus is 4evt X 3 => 12evt
piano_verse is 5evt X 5 => 25evt
total is 127evt

]]
