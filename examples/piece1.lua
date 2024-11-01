
-- Import modules this needs.
local neb = require("nebulua") -- lua api
local mus = require("music_defs")
local mid = require("midi_defs") -- GM midi instrument definitions
local bt  = require("bar_time") -- time utility
local ut  = require('lbot_utils')


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


-- local fp, err = io.open('C:\\Dev\\repos\\Apps\\Nebulua\\_glob.txt', 'w+')
-- fp:write(ut.dump_table_string(package.loaded, false, 'package.loaded')..'\n')
-- fp:write(ut.dump_table_string(_G, false, '_G')..'\n')
-- fp:close()



-- make lua more fp:
-- https://www.reddit.com/r/lua/comments/1al74ry/why_dont_more_people_suggest_closures_for_classes/


------------------------- Configuration -------------------------------

-- Specify midi channels.
local midi_in = "ClickClack"
local hin  = neb.create_input_channel(midi_in, 1)

-- Use DAW or VST host.
local use_host = false

local midi_out = ut.tern(use_host, "loopMIDI Port", "VirtualMIDISynth #1")
local hnd_keys  = neb.create_output_channel(midi_out, 1, ut.tern(use_host, mid.NO_PATCH, inst.AcousticGrandPiano))
local hnd_bass  = neb.create_output_channel(midi_out, 2, ut.tern(use_host, mid.NO_PATCH, inst.AcousticBass))
local hnd_synth = neb.create_output_channel(midi_out, 3, ut.tern(use_host, mid.NO_PATCH, inst.VoiceOohs))
local hnd_drums = neb.create_output_channel(midi_out, 10, ut.tern(use_host, mid.NO_PATCH, kit.Jazz))



------------------------- Variables -----------------------------------

-- Misc vars.
local valid = true

-- Forward refs.
local seq_func


------------------------- Canned Sequences ---------------------------------

local drums_seq =
{
    -- | beat 0 | beat 1 | beat 2 | beat 3 | beat 4 | beat 5 | beat 6 | beat 7 |,  WHAT_TO_PLAY
    -- | beat 0 | beat 1 | beat 2 | beat 3 | beat 0 | beat 1 | beat 2 | beat 3 |,  WHAT_TO_PLAY
    { "|8       |        |8       |        |8       |        |8       |        |", bdrum },
    { "|    8   |        |    8   |    8   |    8   |        |    8   |    8   |", snare },
    { "|        |     8 8|        |     8 8|        |     8 8|        |     8 8|", hhcl }
}

local keys_seq =
{
    -- | beat 0 | beat 1 |........|........|........|........|........|........|,  WHAT_TO_PLAY
    { "|6-------|--      |        |        |7-------|--      |        |        |", "G4.m7" },
    { "|7-------|--      |        |        |7-------|--      |        |        |",  84 },
    { "|        |        |        |5---    |        |        |        |5-8---  |", "D6" },
}

local bass_seq =
{
    -- | beat 0 | beat 1 |........|........|........|........|........|........|,  WHAT_TO_PLAY
    { "|8-------|        |        |        |8-------|        |        |        |", "D3" },
}

local drums_seq_steps = neb.parse_sequence_steps(hnd_drums, drums_seq)
local keys_seq_steps = neb.parse_sequence_steps(hnd_keys, keys_seq)
local bass_seq_steps = neb.parse_sequence_steps(hnd_bass, bass_seq)

local my_scale = mus.get_notes_from_string("G3.Algerian")



------------------------- System Functions ----------------------------------

-----------------------------------------------------------------------------
-- Called once to initialize your script stuff. Required.
function setup()

    -- How fast you wanna go?
    neb.set_tempo(61)

    return 0
end


local _vol_ind = 0
local _vol_map = { 0.0, 0.2, 0.3, 0.4, 0.5, 0.6, 0.7, 0.8, 0.9, 1.0 } -- mod-linear
-- local _vol_map = { 0.0, 0.11, 0.22, 0.33, 0.44, 0.55, 0.66, 0.77, 0.88, 1.0 } -- linear
-- local _vol_map = { 0.0, 0.3, 0.55, 0.75, 0.85, 0.92, 0.96, 0.98, 0.99, 1.0 } -- log-pot
-- local _vol_map = { 0.0, 0.4, 0.52, 0.6, 0.68, 0.76, 0.84, 0.90, 0.95, 1.0 } -- mod-log

-----------------------------------------------------------------------------
-- Main work loop called every subbeat/tick. Required.
function step(tick)
    if valid then
        -- Do something. TODO1 pattern matching like F#.

        local bar, beat, sub = bt.tick_to_bt(tick)


        if bar <= 10 and beat == 0 and sub == 0 then
            if _vol_ind < #_vol_map then
                neb.log_info('send '.._vol_ind)
                neb.send_note(hnd_synth, 60, 0)
                neb.send_note(hnd_synth, 60, _vol_map[_vol_ind + 1])
                _vol_ind = _vol_ind + 1
            end
        end


        -- if bar == 1 and beat == 0 and sub == 0 then
        --     neb.send_sequence_steps(keys_seq_steps, tick)
        -- end

        -- if beat == 0 and sub == 0 then
        --     neb.send_sequence_steps(drums_seq_steps, tick)
        -- end

        -- -- Every 2 bars
        -- if (bar == 0 or bar == 2) and beat == 0 and sub == 0 then
        --     neb.send_sequence_steps(bass_seq_steps, tick)
        -- end

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
end

