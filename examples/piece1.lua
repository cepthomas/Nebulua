

-- Import modules we need.
local neb = require("nebulua") -- lua api
local mus = require("music_defs")
local mid = require("midi_defs") -- GM midi instrument definitions
local bt  = require("bar_time") -- time utility
local com = require('neb_common')
local ut  = require('utils')


-- Setup for debug.
-- ut.config_debug(true)
-- dbg()

-- Say hello.
neb.log_info('### loading piece1.lua ###')


------------------------- Configuration -------------------------------

-- Specify midi devices.
-- local midi_out = "VirtualMIDISynth #1"
local midi_out = "loopMIDI Port"
local midi_in = "ClickClack"

-- Specify midi channels.
local hnd_keys  = neb.create_output_channel(midi_out, 1, neb.NO_PATCH)--inst.AcousticGrandPiano)
local hnd_bass  = neb.create_output_channel(midi_out, 2, neb.NO_PATCH)--inst.AcousticBass)
local hnd_synth = neb.create_output_channel(midi_out, 3, neb.NO_PATCH)--inst.Lead1Square)
local hnd_drums = neb.create_output_channel(midi_out, 10, neb.NO_PATCH)--kit.Jazz)
local hin  = neb.create_input_channel(midi_in, 1)


------------------------- Variables -----------------------------------

-- Misc vars.
local master_volume = 0.8
local valid = true

-- Aliases
local inst = mid.instruments
local drum = mid.drums
local kit  = mid.drum_kits
local ctrl = mid.controllers

local snare = drum.AcousticSnare
local bdrum = drum.AcousticBassDrum
local hhcl = drum.ClosedHiHat
local ride = drum.RideCymbal1
local crash = drum.CrashCymbal2
local mtom = drum.HiMidTom


------------------------- Local Functions -----------------------------


--------------------------------- stuff from example.lua -----------------------


local drums_seq =
{
    --|........|........|........|........|........|........|........|........|
    {"|8       |        |8       |        |8       |        |8       |        |", bdrum },
    {"|    8   |        |    8   |    8   |    8   |        |    8   |    8   |", snare },
    {"|        |     8 8|        |     8 8|        |     8 8|        |     8 8|", hhcl }
}

local example_seq =
{
    -- | beat 1 | beat 2 |........|........|........|........|........|........|,  WHAT_TO_PLAY
    { "|M-------|--      |        |        |7-------|--      |        |        |", "G4.m7" },
    { "|7-------|--      |        |        |7-------|--      |        |        |",  84 },
    { "|        |        |        |5---    |        |        |        |5-8---  |", "D6" },
}

local drums_seq_steps = neb.parse_sequence_steps(hnd_drums, drums_seq)
local example_seq_steps = neb.parse_sequence_steps(hnd_keys, example_seq)

local alg_scale = mus.get_notes_from_string("G3.Algerian")


local function seq_func()-- = function ()
    if alg_scale ~= nil then

        local note_num = math.random(1, #alg_scale)
        neb.send_note(hnd_synth, alg_scale[note_num], 0.9, 8)

        local s = ut.dump_table_string(example_seq_steps, true, 'name')
        print(s)
        
    -- 1(table):
    --     chan_hnd:33025(number)
    --     note_num:86(number)
    --     duration:4(number)
    --     format:function: 000001e38245d0c0(function)
    --     tick:24(number)
    --     volume:0.31(number)
    --     step_type:note(string)
    -- 2(table):
    --     chan_hnd:33025(number)
    --     note_num:86(number)
    --     duration:2(number)
    --     format:function: 000001e38245c7c0(function)
    --     tick:56(number)
    --     volume:0.31(number)
    --     step_type:note(string)
    -- 3(table):
    --     chan_hnd:33025(number)
    --     note_num:86(number)
    --     duration:4(number)
    --     format:function: 000001e38245d540(function)
    --     tick:58(number)
    --     volume:0.79(number)
    --     step_type:note(string)

        neb.send_sequence_steps(example_seq_steps)



    end
end

-----------------------------------------------------------------------------
-- Called once to initialize your script stuff. This is a required function!
function setup()

    -- How fast?
    neb.set_tempo(61)

    -- neb.log_info(string.format('setup %s', tostring(valid )))

    return 0
end


-----------------------------------------------------------------------------
-- Main work loop called every subbeat/tick. This is a required function!
function step(tick)
    if valid then
        -- Do something.
        local t = BarTime(tick)
        if t.get_bar() == 1 and t.get_beat() == 0 and t.get_sub() == 0 then
            seq_func()
            -- neb.send_controller(hout, ctrl.Pan, 90)
        end
    end

    -- Overhead.
    neb.process_step(tick)

    return 0
end

-----------------------------------------------------------------------------
-- Handler for input note events. Optional.
function rcv_note(chan_hnd, note_num, volume)
    -- neb.log_debug(string.format("RCV note:%d hnd:%d vol:%f", note_num, chan_hnd, volume))

    if chan_hnd == hin then
        -- Play the note.
        neb.send_note(hout, note_num, volume)
    end
    return 0
end

-----------------------------------------------------------------------------
-- Handlers for input controller events. Optional.
function rcv_controller(chan_hnd, controller, value)
    if chan_hnd == hin then
        -- Do something.
        neb.log_debug(string.format("RCV controller:%d hnd:%d val:%d", controller, chan_hnd, value))
    end
    return 0
end
