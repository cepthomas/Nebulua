

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

-- Specify midi channels.
local hout = neb.create_output_channel(midi_out, 1, mid.instruments.Pad2Warm)


------------------------- Variables -----------------------------------

-- Misc vars.
local volume = 0.8
local valid = true

-- All the loops.
local loops = {}


------------------------- Local Functions -----------------------------

--- Add a new loop note.
--   snote: see README.md.Standard Note Syntax
--   duration: how long to play in BarTime
--   delay: wait before start in BarTime
local function add_loop(snote, duration, delay)
    local notes, err = mus.get_notes_from_string(snote)

    -- Check args.
    if notes == nil then
        neb.log_error("Invalid note name: "..snote)
        valid = false
    end

    if duration == nil then
        neb.log_error("Invalid duration")
        valid = false
    end

    if delay == nil then
        neb.log_error("Invalid delay")
        valid = false
    end

    if valid then
        table.insert(loops, { snote=snote, duration=duration, delay=delay, notes=notes, next_start=delay })
    end
end


--- Convert beat/sub to tick.
--   beat: which beat
--   sub: which subbeat
--   return: tick
local function tot(beat, sub)
    local tick = beat * com.SUBS_PER_BEAT + sub
    return tick
end


-----------------------------------------------------------------------------
-- Called once to initialize your script stuff. This is a required function!
function setup()
    -- Set up all the loop notes. Key is Ab.
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
    neb.set_tempo(61)

    -- neb.log_info(string.format('setup %s', tostring(valid )))

    return 0
end


-----------------------------------------------------------------------------
-- Main work loop called every subbeat/tick. This is a required function!
function step(tick)

    neb.process_step(tick)

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

        -- Do something every new bar.
        local t = BarTime(tick)
        if t.get_beat() == 0 and t.get_sub() == 0 then
            xxx = 0
            -- neb.send_controller(hnd_synth, ctrl.Pan, 90)
        end
    end
    return 0
end
