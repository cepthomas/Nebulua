
-- Nebulua test script.

local neb = require("nebulua") -- lua api
-- local api = require("host_api") -- C api (or sim)


neb.info("=============== go go go =======================")


------------------------- Config ----------------------------------------

-- Device names
local dev_in1 = "in1"
local dev_in2 = "in2"
local dev_out1 = "out1"
local dev_out2 = "out2"

-- Channels
local hout1  = neb.create_output_channel(dev_out1, 1, 33)
local hout2  = neb.create_output_channel(dev_out2, 2, 44)
local hin1   = neb.create_input_channel(dev_in1, 3)
local hin2   = neb.create_input_channel(dev_in2, 4)

------------------------- Vars ----------------------------------------

-- -- Local vars.
local master_vol = 0.8


--------------------- Called from C core -----------------------------------

-----------------------------------------------------------------------------
-- Init stuff.
function setup()
    neb.info("initialization")
    -- Do work.
    neb.process_all(sequences, sections)
    neb.set_tempo(100)
    return 0
end

-----------------------------------------------------------------------------
-- Main loop - called every mmtimer increment.
function step(bar, beat, subbeat)
    -- Main work.
    neb.do_step(steps, bar, beat, subbeat)

    -- Selective work.
    if beat == 0 and subbeat == 0 then
        neb.send_controller(hout1, 50, 51)
    end
    if beat == 1 and subbeat == 4 then
        neb.send_controller(hout2,  60, 61)
    end
end

-----------------------------------------------------------------------------
-- Handler for input note events.
function input_note(chan_hnd, note_num, volume)
    local s = string.format("input_note: %d %d %f", chan_hnd, note_num, volume)
    neb.info(s)

    if chan_hnd == hin1 then
        neb.send_note(hout1, note_num + 1, volume + 1, 8)
    end
end

-----------------------------------------------------------------------------
-- Handler for input controller events.
function input_controller(chan_hnd, controller, value)
    local s = string.format("input_controller: %d %d %d", chan_hnd, controller, value)
    neb.info(s)
end

----------------------- User lua functions -------------------------

-----------------------------------------------------------------------------
-- Called from sequence.
local function seq_func(bar, beat, subbeat)
    local note_num = math.random(0, #alg_scale)
    neb.send_note(hout1, alg_scale[note_num], 0.7, 1)
end

-- Called from section.
function section_func(bar, beat, subbeat)
    -- do something
end

-----------------------------------------------------------------------------
-- Make a noise.
local function boing(note_num)
    local boinged = false;

    neb.info("boing")
    if note_num == 0 then
        note_num = math.random(30, 80)
        boinged = true
        neb.send_note(hout2, note_num, VEL, 8)
    end
    return boinged
end

------------------------- Composition ---------------------------------------

sequences =
{
    example_seq = -- neb.SUBBEATS_PER_BEAT = 8
    {
        -- | beat 1 | beat 2 |........|........|........|........|........|........|,  WHAT_TO_PLAY
        { "|M-------|--      |        |        |7-------|--      |        |        |", "G4.m7" },
    },

    drums_verse =
    {
        --|........|........|........|........|........|........|........|........|
        {"|8       |        |8       |        |8       |        |8       |        |", 10 },
        {"|    8   |        |    8   |    8   |    8   |        |    8   |    8   |", 11 },
        {"|        |     8 8|        |     8 8|        |     8 8|        |     8 8|", 12 }
    },

    drums_chorus =
    {
        -- |........|........|........|........|........|........|........|........|
        { "|6       |        |6       |        |6       |        |6       |        |", 10 },
        { "|        |7 7     |        |7 7     |        |7 7     |        |        |", 13 },
        { "|        |    4   |        |        |        |    4   |        |        |", 14 },
        { "|        |        |        |        |        |        |        |8       |", 15 },
    },

    keys_verse =
    {
        -- |........|........|........|........|........|........|........|........|
        { "|7-------|--      |        |        |7-------|--      |        |        |", "G4.m7" },
        { "|        |        |        |5---    |        |        |        |5-8---  |", "G4.m6" }
    },

    keys_chorus =
    {
        -- |........|........|........|........|........|........|........|........|
        { "|6-      |        |        |        |        |        |        |        |", "F4" },
        { "|    5-  |        |        |        |        |        |        |        |", "D#4" },
        { "|        |6-      |        |        |        |        |        |        |", "C4" },
        { "|        |    6-  |        |        |        |        |        |        |", "B4.m7" },
    },

    bass_verse =
    {
        -- |........|........|........|........|........|........|........|........|
        { "|7   7   |        |        |        |        |        |        |        |", "C2" },
        { "|        |        |        |    7   |        |        |        |        |", "E2" },
        { "|        |        |        |        |        |        |        |    7   |", "A#2" },
    },

    bass_chorus =
    {
        -- |........|........|........|........|........|........|........|........|
        { "|5   5   |        |5   5   |        |5   5   |        |5   5   |        |", "C2" },
    },
}


-----------------------------------------------------------------------------
sections =
{
    beginning =
    {
        { hout1,  keys_verse,  keys_verse,  keys_verse,  keys_verse },
        { hout2,  bass_verse,  bass_verse,  bass_verse,  bass_verse }
    },

    middle =
    {
        { hout1,  keys_chorus,  keys_chorus,  keys_chorus,  keys_chorus },
        { hout2,  bass_chorus,  bass_chorus,  bass_chorus,  bass_chorus },
    },

    ending =
    {
        { hout1,  keys_verse,  keys_verse,  keys_verse,  keys_verse },
        { hout2,  bass_verse,  bass_verse,  bass_verse,  bass_verse }
    }
}

