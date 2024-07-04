
-- This is an example of a dynamically generated algorithmic piece.
-- It's a take on Eno's Music for Airports - ported from github.com/teropa/musicforairports.js


-- Import modules we need.
local neb = require("nebulua") -- lua api
local mus = require("music_defs")
local mid = require("midi_defs") -- GM midi instrument definitions
local bt  = require("bar_time") -- time utility
local com = require('neb_common')


------------------------- Configuration -------------------------------

-- Specify midi devices.
local midi_out = "VirtualMIDISynth #1"
-- local midi_out = "Microsoft GS Wavetable Synth"
-- local midi_out = "loopMIDI Port"

-- Specify midi channels.
local hout = neb.create_output_channel(midi_out, 1, mid.instruments.Pad2Warm)


------------------------- Variables -----------------------------------

-- Misc vars.
local volume = 0.8
local valid = false

-- All the loops.
local loops = {}


------------------------- Local Functions -----------------------------

--- Add a new loop note.
--   snote: see README.md.Standard Note Syntax
--   duration: how long to play in BarTime
--   delay: wait before start in BarTime
local function add_loop(snote, duration, delay)
    notes, err = mus.get_notes_from_string(snote)
    dur = duration
    del = delay
    next_start = del

    -- Check values.
    if notes == nil or notes == nil or notes == nil then
        valid = false
    else
        table.insert(loops, { snote=snote, dur=dur, del=del, notes=notes, next_start=del })
        valid = valid and true
    end
end

--- Convert beat/sub to tick.
--   beat: which beat
--   sub: which subbeat
--   return: tick
local function tot(beat, sub)
    tick = beat * com.SUBS_PER_BEAT + sub
    return tick
    -- bt = BarTime(0, beat, sub)
    -- return bt.get_tick()
end


-----------------------------------------------------------------------------
-- Called once to initialize your script stuff. This is a required function!
function setup()
    -- Set up all the loop notes. Key is Ab.
    xxx = tot(1,2)

    add_loop("Ab4", tot(17,3),  tot(8,1))
    add_loop("Ab5", tot(17,2),  tot(3,1))
    -- 3rd
    add_loop("C5",  tot(21,1),  tot(5,3))
    -- 4th
    add_loop("Db5", tot(18,2),  tot(12,3))
    -- 5th
    add_loop("Eb5", tot(20,0),  tot(9,2))
    -- 6th
    add_loop("F4",  tot(19,3),  tot(4,2))
    add_loop("F5",  tot(20,0),  tot(14,1))

    -- How fast?
    neb.set_tempo(70)

    return 0
end


-----------------------------------------------------------------------------
-- Main work loop called every subbeat/tick. This is a required function!
function step(tick)
    if valid then
        -- Process each current loop.
        for _, loop in ipairs(loops) do
            if tick >= loop.next_start then
                for _, note_num in ipairs(loop.notes) do
                    -- Send any note starts now.
                    neb.send_note(chan_hnd, note_num, volume, loop.duration)
                end
                -- Calculate next time.
                loop.next_start = tick + loop.delay + loop.duration;
            end
        end
    end
    return 0
end
