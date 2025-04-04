
-- Script for interop and lua unit test - the happy path.

local li = require("luainterop")
local api = require("script_api")
local md = require("music_defs")
local bt = require("bar_time")
local ut = require("lbot_utils")


-- info = 2
li.log(2, "======== script_happy.lua is a beautiful thing ==========")


------------------------- Config ----------------------------------------

-- Specify midi devices.
local dev_out1 = "Microsoft GS Wavetable Synth"
local dev_out2 = "loopMIDI Port"
local dev_in1  = "loopMIDI Port"

-- Channels
local hnd_piano = li.open_midi_output(dev_out1, 1, 2)
local hnd_synth = li.open_midi_output(dev_out1, 2, 90)
local hnd_drums = li.open_midi_output(dev_out1, 10, 8)
local hnd_input = li.open_midi_input(dev_in1, 3)


------------------------- Vars ----------------------------------------

-- Get some stock chords and scales.
local alg_scale = md.get_notes_from_string("G3.Algerian")
local chord_notes = md.get_notes_from_string("C4.o7")

--------------------- Called from app -----------------------------------

-----------------------------------------------------------------------------
-- Init stuff. Required function.
function setup()
    -- Set master volumes.
    api.set_volume(hnd_piano, 0.6)
    api.set_volume(hnd_drums, 0.9)

    api.set_tempo(95)

    return api.process_comp() -- required if using composition
end

-----------------------------------------------------------------------------
-- Main loop - called every mmtimer increment. Required function.
function step(tick)

    api.process_step(tick)

    -- Selective work.

    local bar, beat, sub = bt.tick_to_bt(tick)
    if beat == 0 and sub == 0 then
        api.send_midi_controller(hnd_synth, 50, 51)
    end

    if beat == 1 and sub == 4 then
        api.send_midi_controller(hnd_synth, 60, 61)
    end

    return 0
end

-----------------------------------------------------------------------------
-- Handler for input note events. Optional.
function receive_midi_note(chan_hnd, note_num, volume)
    local s = string.format("Script rcv note:%d hnd:%d vol:%f", note_num, chan_hnd, volume)
    li.log(2, s)

    if chan_hnd == hnd_input then
        li.send_midi_note(hnd_synth, note_num + 1, volume * 0.5, 8)
    end
    return 0
end

-----------------------------------------------------------------------------
-- Handler for input controller events. Optional.
function receive_midi_controller(chan_hnd, controller, value)
    local s = string.format("Script rcv controller:%d hnd:%d val:%f", controller, chan_hnd, value)
    li.log(2, s)
    return 0
end

----------------------- Custom user functions -------------------------

-----------------------------------------------------------------------------
-- Called from sequence.
local function my_seq_func(tick)
    local note_num = math.random(0, #alg_scale)
    api.send_note(hnd_synth, alg_scale[note_num], 0.7, 1)
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

    api.log_info("boing")
    if note_num == 0 then
        note_num = math.random(30, 80)
        boinged = true
        api.send_note(hnd_synth, note_num, 0.7, 8)
    end
    return boinged
end


------------------------- Composition ---------------------------------------

-- Sequences --

local piano_verse =
{
    -- |........|........|........|........|........|........|........|........|
    { "|7-------|--      |        |        |7-------|-       |        |        |", "G4.m7" },
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
    { "|8       |        |8       |        |8       |        |8       |        |", 35 },
    { "|    8   |        |    8   |    8   |    8   |        |    8   |    8   |", 38 },
    { "|        |     8 8|        |     8 8|        |     8 8|        |     8 8|", 42 }
}

local drums_chorus =
{
    -- |........|........|........|........|........|........|........|........|
    { "|6       |        |6       |        |6       |        |6       |        |", 35 },
    { "|        |7 7     |        |7 7     |        |7 7     |        |        |", 38 },
    { "|        |    4   |        |        |        |    4   |        |        |", 46 },
    { "|        |        |        |        |        |        |        |8       |", 52 },
    { "|    8   |        |    8   |        |    8   |        |    8   |        |", 56 },
}



-- Sections --

-- Empty space.
local quiet = { {"|        |        |        |        |        |        |        |        |", 0 } }


api.sect_start("beginning")
api.sect_chan(hnd_piano, piano_verse,  quiet,         piano_verse,  piano_verse  )
api.sect_chan(hnd_drums, drums_verse,  drums_verse,   quiet,        drums_verse  )

api.sect_start("middle")
api.sect_chan(hnd_piano, quiet,         piano_chorus, piano_chorus, piano_chorus )
api.sect_chan(hnd_drums, drums_chorus,  drums_chorus, drums_chorus, drums_chorus )

api.sect_start("ending")
api.sect_chan(hnd_piano, piano_verse,   piano_verse,  piano_verse,  quiet        )
api.sect_chan(hnd_drums, drums_verse,   drums_verse,  drums_verse,  drums_chorus )
