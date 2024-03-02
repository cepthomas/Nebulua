
-- Script for unit test.


local bt = require("bar_time")
local neb = require("nebulua") -- api


neb.log_info("=============== Is this thing on? ===============")


__script1 = 656565


------------------------- Config ----------------------------------------

-- Device names
local dev_in1 = "in1"
local dev_in2 = "in2"
local dev_out1 = "out1"
local dev_out2 = "out2"

-- Channels
local hnd_instrument1 = neb.create_output_channel(dev_out1, 1, 33)
local hnd_instrument2 = neb.create_output_channel(dev_out2, 4, 44)
local hnd_in1  = neb.create_input_channel(dev_in1, 3)
local hnd_in2  = neb.create_input_channel(dev_in2, 11)


------------------------- Vars ----------------------------------------

-- Local vars.
local master_vol = 0.8
local length = 0

--------------------- Called from C core -----------------------------------

-----------------------------------------------------------------------------
-- Init stuff. Required function.
function setup()
    neb.set_tempo(95)
    length = neb.init(sections) -- if using composition
    return length -- if using composition oherwise return 0
end

-----------------------------------------------------------------------------
-- Main loop - called every mmtimer increment. Required function.
function step(tick)

    neb.process_step(tick) -- required if using composition

    t = BT(tick)

    -- Selective work.
    if t.beat == 0 and t.sub == 0 then
        neb.send_controller(hinstrument1, 50, 51)
    end

    if t.beat == 1 and t.sub == 4 then
        neb.send_controller(hinstrument2,  60, 61)
    end
end

-----------------------------------------------------------------------------
-- Handler for input note events. Optional.
function input_note(chan_hnd, note_num, volume)
    local s = string.format("input_note: %d %d %f", chan_hnd, note_num, volume)
    neb.log_info(s)

    if chan_hnd == hnd_in1 then
        neb.send_note(hinstrument1, note_num + 1, volume * 0.5, 8)
    end
end

-----------------------------------------------------------------------------
-- Handler for input controller events. Optional.
function input_controller(chan_hnd, controller, value)
    local s = string.format("input_controller: %d %d %d", chan_hnd, controller, value)
    neb.log_info(s)
end

----------------------- Custom user functions -------------------------

-----------------------------------------------------------------------------
-- Called from sequence.
local function my_seq_func(tick)
    local note_num = math.random(0, #alg_scale)
    neb.send_note(hinstrument1, alg_scale[note_num], 0.7, 1)
end

-----------------------------------------------------------------------------
-- Called from section.
function my_section_func(tick)
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
        neb.send_note(hinstrument2, note_num, master_vol, 8)
    end
    return boinged
end

------------------------- Composition ---------------------------------------

--[[
Each section is 8 beats.
Each sequence is 4 sections => 32 beats.
A 4 minute song at 80bpm is 320 beats => 10 sequences => 40 sequences.

If each sequence has average 8 notes => total of 320 notes per instrument.
A 4 minute song at 80bpm is 320 beats => 10 sequences => 40 sequences => 320 notes.
A "typical" sone would have about 4000 on/off events.
]]


-- Sequences --

drums_verse =
{
    -- |........|........|........|........|........|........|........|........|
    { "|8       |        |8       |        |8       |        |8       |        |", 10 },
    { "|    8   |        |    8   |    8   |    8   |        |    8   |    8   |", 11 },
    { "|        |     8 8|        |     8 8|        |     8 8|        |     8 8|", 12 }
}

drums_chorus =
{
    -- |........|........|........|........|........|........|........|........|
    { "|6       |        |6       |        |6       |        |6       |        |", 10 },
    { "|        |7 7     |        |7 7     |        |7 7     |        |        |", 13 },
    { "|        |    4   |        |        |        |    4   |        |        |", 14 },
    { "|        |        |        |        |        |        |        |8       |", 15 },
}

keys_verse =
{
    -- |........|........|........|........|........|........|........|........|
    { "|7-------|--      |        |        |7-------|--      |        |        |", "G4.m7" },
    { "|        |        |        |5---    |        |        |        |5-8---  |", "G4.m6" }
}

keys_chorus =
{
    -- |........|........|........|........|........|........|........|........|
    { "|6-      |        |        |        |        |        |        |        |", "F4" },
    { "|    5-  |        |        |        |        |        |        |        |", my_seq_func },
    { "|        |6-      |        |        |        |        |        |        |", "C4" },
    { "|        |    6-  |        |        |        |        |        |        |", "B4.m7" },
}

bass_verse =
{
    -- |........|........|........|........|........|........|........|........|
    { "|7   7   |        |        |        |        |        |        |        |", "C2" },
    { "|        |        |        |    7   |        |        |        |        |", "E2" },
    { "|        |        |        |        |        |        |        |    7   |", "A#2" },
}

bass_chorus =
{
    -- |........|........|........|........|........|........|........|........|
    { "|5   5   |        |5   5   |        |5   5   |        |5   5   |        |", "C2" },
}

-- Fill space. Can't use nil.
empty = {}


-----------------------------------------------------------------------------

sections =
{
    {
        name = "beginning",
        { hnd_instrument1, empty,       keys_verse,    keys_verse,  keys_verse },
        { hnd_instrument2, bass_verse,  bass_verse,    empty,       bass_verse }
    },

    {
        name = "middle",
        { hnd_instrument1, empty,        keys_chorus,  keys_chorus,  keys_chorus },
        { hnd_instrument2, bass_chorus,  bass_chorus,  bass_chorus,  bass_chorus }
    },

    {
        name = "ending",
        { hnd_instrument1, keys_verse,    keys_verse,  keys_verse,   empty      },
        { hnd_instrument2, bass_verse,    bass_verse,  bass_verse,   bass_verse }
    }
}
