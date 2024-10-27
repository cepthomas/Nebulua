

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


------------------------- Configuration -------------------------------

-- Specify midi devices.
local midi_out = "VirtualMIDISynth #1"
-- local midi_out = "loopMIDI Port"
local midi_in = "ClickClack"

-- Specify midi channels.
local hout = neb.create_output_channel(midi_out, 1, mid.instruments.Pad2Warm)
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

--- Add a new loop note.
--   snote: see README.md.Standard Note Syntax
--   duration: how long to play in BarTime
--   delay: wait before start in BarTime
-- local function add_loop(snote, duration, delay)
--     local notes, err = mus.get_notes_from_string(snote)

--     -- Check args.
--     if notes == nil then
--         neb.log_error("Invalid note name: "..snote)
--         valid = false
--     end

--     if duration == nil then
--         neb.log_error("Invalid duration")
--         valid = false
--     end

--     if delay == nil then
--         neb.log_error("Invalid delay")
--         valid = false
--     end

--     if valid then
--         table.insert(loops, { snote=snote, duration=duration, delay=delay, notes=notes, next_start=delay })
--     end
-- end


--- Convert beat/sub to tick.
--   beat: which beat
--   sub: which subbeat
--   return: tick
-- local function tot(beat, sub)
--     local tick = beat * com.SUBS_PER_BEAT + sub
--     return tick
-- end


--------------------------------- stuff from example.lua -----------------------
local alg_scale = mus.get_notes_from_string("G3.Algerian")

-- Function called from sequence.
seq_func = function (tick)
    if alg_scale ~= nil then
        local note_num = math.random(0, #alg_scale)
        neb.send_note(hout, alg_scale[note_num], 0.9, 8)
    end
end


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
    { "|        |        |        |5---    |        |        |        |5-8---  |",  seq_func }
}

-- neb.sect_start("beginning")
--     -- _current_section = {}
--     -- _current_section.name = name
--     -- table.insert(_sections, _current_section)
-- neb.sect_chan(hout,  example_seq,  example_seq,  example_seq,  example_seq)
-- neb.sect_chan(hout,  drums_seq,  drums_seq,  drums_seq,  drums_seq)
--     -- elems = {}
--     -- if type(chan_hnd) ~= "number" then -- should check for valid/known handle
--     --     error("Invalid channel", 2)
--     -- end
--     -- table.insert(elems, chan_hnd)
--     -- num_args = select('#', ...)
--     -- for i = 1, num_args do
--     --     seq = select(i, ...)
--     --     if type(seq) ~= "table" then -- should check for valid/known
--     --         error("Invalid sequence "..i, 2)
--     --     end
--     --     table.insert(elems, seq)
--     -- end
--     -- table.insert(_current_section, elems)

local drums_seq_steps = neb.parse_sequence_steps(drums_seq)
local example_seq_steps = neb.parse_sequence_steps(example_seq)

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
            xxx = 0
            -- neb.send_controller(hout, ctrl.Pan, 90)
        end
    end

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
