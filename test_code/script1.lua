
-- Example Nebulator composition file. This is not actual music.

local log = require("logger")

local neb = require("nebulua") -- lua api
local api = require("host_api") -- C api


log.info("=============== go go go =======================")


------------------------- Config ----------------------------------------

-- Devices
local dev_in1 = "in1"
local dev_in2 = "in2"
local dev_out1 = "out1"
local dev_out2 = "out2"

-- Channels
local hout1  = create_output_channel(dev_out1, 1, 33)
local hout2  = create_output_channel(dev_out2, 2, 44)
local hin1  = create_input_channel(dev_in1, 3)
local hin2  = create_input_channel(dev_in2, 4)

------------------------- Vars ----------------------------------------

-- Local vars.
local keys_vol = 0.8
local drum_vol = 0.8

-- Aliases.
snare = drum.AcousticSnare
bdrum = drum.AcousticBassDrum
hhcl = drum.ClosedHiHat
ride = drum.RideCymbal1
crash = drum.CrashCymbal2
mtom = drum.HiMidTom


--------------------- Called from C core -----------------------------------

-----------------------------------------------------------------------------
-- Init stuff.
function setup()
    log.info("initialization")

    -- Load her up.
    -- local steps = {}
    -- steps = neb.process_all(sequences, sections)
    neb.process_all(sequences, sections)

    api.set_tempo(100)

    return 0

-----------------------------------------------------------------------------
-- Main loop - called every mmtimer increment.
function step(bar, beat, subbeat)

    -- Main work.
    neb.do_step(steps, bar, beat, subbeat)

    -- Selective work.
    if beat == 0 and subbeat == 0 then
        api.send_controller(hsynth, ctrl.Pan, 90)
        -- or...
        api.send_controller(hout1,  ctrl.Pan, 30)
    end
end

-----------------------------------------------------------------------------
-- Handlers for input note events.
function input_note(chan_hnd, note_num, velocity)
    log.info("input_note") -- string.format("%s", variable_name), chan_hnd, note, vel)

    if chan_hnd == hbing_bong then
        -- whiz = ...
    end

    api.send_note(hout1, note_num, velocity, 0.5)
end

-----------------------------------------------------------------------------
-- Handlers for input controller events.
function input_controller(chan_hnd, controller, value)
    log.info("input_controller") --, chan_hnd, ctlid, value)
end

----------------------- User lua functions -------------------------

-----------------------------------------------------------------------------
-- Called from sequence.
local function seq_func(bar, beat, subbeat)
    local note_num = math.random(0, #alg_scale)
    api.send_note(hout1, alg_scale[note_num], 0.7, 0.5)
end

-- Called from section.
function section_func(bar, beat, subbeat)
    -- do something
end

-----------------------------------------------------------------------------
-- Make a noise.
local function boing(note_num)
    local boinged = false;

    log.info("boing")
    if note_num == 0 then
        note_num = Random(30, 80)
        boinged = true
        api.send_note(hout2, note_num, VEL, 1.0)
    end
    return boinged
end

------------------------- Composition ---------------------------------------

sequences =
{
    drums_verse =
    {
        --|........|........|........|........|........|........|........|........|
        {"|8       |        |8       |        |8       |        |8       |        |", bdrum },
        {"|    8   |        |    8   |    8   |    8   |        |    8   |    8   |", snare },
        {"|        |     8 8|        |     8 8|        |     8 8|        |     8 8|", hhcl }
    },

    drums_chorus =
    {
        -- |........|........|........|........|........|........|........|........|
        { "|6       |        |6       |        |6       |        |6       |        |", bdrum },
        { "|        |7 7     |        |7 7     |        |7 7     |        |        |", ride },
        { "|        |    4   |        |        |        |    4   |        |        |", mtom },
        { "|        |        |        |        |        |        |        |8       |", crash },
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
        { hout1,    keys_chorus,  keys_chorus,  keys_chorus,  keys_chorus },
        { hout2,    bass_chorus,  bass_chorus,  bass_chorus,  bass_chorus },
    },

    ending =
    {
        { hout1,  keys_verse,  keys_verse,  keys_verse,  keys_verse },
        { hout2,  bass_verse,  bass_verse,  bass_verse,  bass_verse }
    }
}

-- -- Return the module.
-- return M
