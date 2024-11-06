
-- Import modules this needs.
local api = require("script_api")
local mus = require("music_defs")
local mid = require("midi_defs")
local bt  = require("bar_time")
local ut  = require('lbot_utils')
local sx  = require("stringex")

-- TODO1 how to do this:
-- local oo  = require("other")


-- Setup for debug.
-- ut.config_debug(true)
-- dbg()


-- Say hello.
local fn, line, dir = ut.get_caller_info(2)
api.log_info('Loading piece1.lua in '..dir)


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


-- local fp = io.open('C:/Dev/repos/Apps/Nebulua/_glob.txt', 'w+')
-- fp:write(package.config..'\n')
-- fp:write(ut.dump_table_string(package, 0, 'package!!!')..'\n')
-- -- fp:write(ut.dump_table_string(package.loaded, 0, 'package.loaded')..'\n')
-- -- fp:write(ut.dump_table_string(_G, 0, '_G')..'\n')
-- fp:close()


------------------------- Configuration -------------------------------

-- Specify midi channels.
local midi_in = "ClickClack"
local hin = api.create_input_channel(midi_in, 1)

-- DAW or VST host.
local use_host = false

local midi_out = ut.tern(use_host, "loopMIDI Port", "VirtualMIDISynth #1")
local hnd_keys  = api.create_output_channel(midi_out, 1, ut.tern(use_host, mid.NO_PATCH, inst.AcousticGrandPiano))
local hnd_bass  = api.create_output_channel(midi_out, 2, ut.tern(use_host, mid.NO_PATCH, inst.AcousticBass))
local hnd_synth = api.create_output_channel(midi_out, 3, ut.tern(use_host, mid.NO_PATCH, inst.VoiceOohs))
local hnd_drums = api.create_output_channel(midi_out, 10, ut.tern(use_host, mid.NO_PATCH, kit.Jazz))


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

local drums_seq_steps = api.parse_sequence_steps(hnd_drums, drums_seq)
local keys_seq_steps = api.parse_sequence_steps(hnd_keys, keys_seq)
local bass_seq_steps = api.parse_sequence_steps(hnd_bass, bass_seq)

local my_scale = mus.get_notes_from_string("G3.Algerian")



------------------------- System Functions ----------------------------------

-----------------------------------------------------------------------------
-- Called once to initialize your script stuff. Required.
function setup()

    -- How fast you wanna go?
    api.set_tempo(80)

    return 0
end

-----------------------------------------------------------------------------
-- Main work loop called every subbeat/tick. Required.
function step(tick)
    if valid then
        -- Do something. TODO1 pattern matching like F#. Replace composition?

        local bar, beat, sub = bt.tick_to_bt(tick)

        if bar == 1 and beat == 0 and sub == 0 then
            api.send_sequence_steps(keys_seq_steps, tick)
        end

        if beat == 0 and sub == 0 then
            api.send_sequence_steps(drums_seq_steps, tick)
        end

        -- Every 2 bars
        if (bar == 0 or bar == 2) and beat == 0 and sub == 0 then
            api.send_sequence_steps(bass_seq_steps, tick)
        end

    end

    -- Overhead.
    api.process_step(tick)

    return 0
end

-----------------------------------------------------------------------------
-- Handler for input note events. Optional.
function rcv_note(chan_hnd, note_num, volume)
    -- api.log_debug(string.format("RCV note:%d hnd:%d vol:%f", note_num, chan_hnd, volume))

    if chan_hnd == hin then
        -- Play the note.
        api.send_note(hnd_synth, note_num, volume)
    end
    return 0
end

-----------------------------------------------------------------------------
-- Handlers for input controller events. Optional.
function rcv_controller(chan_hnd, controller, value)
    if chan_hnd == hin then
        -- Do something.
        api.log_debug(string.format("RCV controller:%d hnd:%d val:%d", controller, chan_hnd, value))
    end
    return 0
end


------------------------- Local Functions -----------------------------------

-----------------------------------------------------------------------------
-- Do something.
seq_func = function(tick)
    local note_num = math.random(1, #my_scale)
    api.send_note(hnd_synth, my_scale[note_num], 0.9, 8)
    api.send_sequence_steps(keys_seq_steps, tick)
end

