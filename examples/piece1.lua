
-- Import modules this needs.
local neb = require("nebulua") -- lua api
local mus = require("music_defs")
local mid = require("midi_defs") -- GM midi instrument definitions
local bt  = require("bar_time") -- time utility
local com = require('neb_common')
local ut  = require('utils')


-- Setup for debug.
-- ut.config_debug(true)
-- dbg()


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


-- Say hello.
neb.log_info('### loading piece1.lua ###')

local function ternary(cond, tval, fval)
    if cond then return tval else return fval end
end

-- print(ut.dump_table_string(package.loaded, false, 'package.loaded'))


------------------------- Configuration -------------------------------

-- Specify midi channels.
local midi_in = "ClickClack"
local hin  = neb.create_input_channel(midi_in, 1)

-- local midi_out = "loopMIDI Port"
-- local hnd_keys  = neb.create_output_channel(midi_out, 1, neb.NO_PATCH)
-- local hnd_bass  = neb.create_output_channel(midi_out, 2, neb.NO_PATCH)
-- local hnd_synth = neb.create_output_channel(midi_out, 3, neb.NO_PATCH)
-- local hnd_drums = neb.create_output_channel(midi_out, 10, neb.NO_PATCH)

local midi_out = "VirtualMIDISynth #1" -- or "Microsoft GS Wavetable Synth"
local hnd_keys  = neb.create_output_channel(midi_out, 1, inst.AcousticGrandPiano)
local hnd_bass  = neb.create_output_channel(midi_out, 2, inst.AcousticBass)
local hnd_synth = neb.create_output_channel(midi_out, 3, inst.Lead1Square)
local hnd_drums = neb.create_output_channel(midi_out, 10, kit.Jazz)


------------------------- Variables -----------------------------------

-- Misc vars.
local master_volume = 0.8
local valid = true

-- Forward refs.
local seq_func


------------------------- Canned Sequences ---------------------------------

local drums_seq =
{
    --|........|........|........|........|........|........|........|........|
    {"|8       |        |8       |        |8       |        |8       |        |", bdrum },
    {"|    8   |        |    8   |    8   |    8   |        |    8   |    8   |", snare },
    {"|        |     8 8|        |     8 8|        |     8 8|        |     8 8|", hhcl }
}

local keys_seq =
{
    -- | beat 1 | beat 2 |........|........|........|........|........|........|,  WHAT_TO_PLAY
    { "|6-------|--      |        |        |7-------|--      |        |        |", "G4.m7" },
    { "|7-------|--      |        |        |7-------|--      |        |        |",  84 },
    { "|        |        |        |5---    |        |        |        |5-8---  |", "D6" },
}

local drums_seq_steps = neb.parse_sequence_steps(hnd_drums, drums_seq)
local keys_seq_steps = neb.parse_sequence_steps(hnd_keys, keys_seq)

local my_scale = mus.get_notes_from_string("G3.Algerian")



------------------------- System Functions ----------------------------------

-----------------------------------------------------------------------------
-- Called once to initialize your script stuff. Required.
function setup()

    -- How fast you wanna go?
    neb.set_tempo(61)

    return 0
end

-----------------------------------------------------------------------------
-- Main work loop called every subbeat/tick. Required.
function step(tick)
    if valid then
        -- Do something.
        local t = BarTime(tick)
        if t.get_bar() == 1 and t.get_beat() == 0 and t.get_sub() == 0 then
            neb.log_info('call seq_func() '..tick)

            seq_func(tick)
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
        neb.send_note(hnd_synth, note_num, volume)
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


------------------------- Local Functions -----------------------------------

-----------------------------------------------------------------------------
-- Do something.
seq_func = function(tick)
    local note_num = math.random(1, #my_scale)
    neb.send_note(hnd_synth, my_scale[note_num], 0.9, 8)

    neb.send_sequence_steps(keys_seq_steps, tick)
    -- local dumpfn = 'C:\\Dev\\repos\\Apps\\Nebulua\\_dump.txt'
    -- neb.dump_steps(dumpfn, 't') -- diagnostic

end

