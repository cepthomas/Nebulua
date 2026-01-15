
-- This is an example of a dynamically generated algorithmic piece.
-- It's a take on Eno's Music for Airports - ported from github.com/teropa/musicforairports.js


-- Import modules we need.
local api = require("script_api")
local mus = require("music_defs")
local mid = require("midi_defs")
local bt  = require("music_time")
local ut  = require('lbot_utils')


-- Say hello.
api.log_info('Loading airport.lua...')


------------------------- Configuration -------------------------------

-- Specify midi channels.
local midi_device_out = "VirtualMIDISynth #1"
local chan_hnd = api.open_midi_output(midi_device_out, 1, "voices", "Pad2Warm")


------------------------- Variables -----------------------------------

-- Misc vars.
local volume = 0.8
local valid = true

-- All the loops.
local loops = {}

-- Forward refs.
local add_loop


------------------------- System Functions -----------------------------

-----------------------------------------------------------------------------
-- Called once to initialize your script stuff. Required.
function setup()
    -- Set up all the loop notes. Key is Ab.
    add_loop("Ab4", bt.beats_to_tick(17,3),  bt.beats_to_tick(8,1))
    add_loop("Ab5", bt.beats_to_tick(17,2),  bt.beats_to_tick(3,1))
    -- 3rd
    add_loop("C5",  bt.beats_to_tick(21,1),  bt.beats_to_tick(5,3))
    -- 4th
    add_loop("Db5", bt.beats_to_tick(18,2),  bt.beats_to_tick(12,3))
    -- 5th
    add_loop("Eb5", bt.beats_to_tick(20,0),  bt.beats_to_tick(9,2))
    -- 6th
    add_loop("F4",  bt.beats_to_tick(19,3),  bt.beats_to_tick(4,2))
    add_loop("F5",  bt.beats_to_tick(20,0),  bt.beats_to_tick(14,1))

    -- How fast?
    api.set_tempo(61)

    -- api.log_info(string.format('setup %s', tostring(valid )))

    return ''
end

-----------------------------------------------------------------------------
-- Main work loop called every tick/subbeat. Required.
function step(tick)

    -- Overhead.
    api.process_step(tick)

    if valid then
        -- Process each current loop.
        for _, loop in ipairs(loops) do
            if tick >= loop.next_start then
                for _, note_num in ipairs(loop.notes) do
                    -- Send any note starts now.
                    -- print('on:'..step.note_num)
                    api.send_midi_note(chan_hnd, note_num, volume, loop.duration)
                end
                -- Calculate next time.
                loop.next_start = tick + loop.delay + loop.duration;
            end
        end
    end
    return 0
end


------------------------- Local Functions -----------------------------

-----------------------------------------------------------------------------
--- Add a new loop note.
--   snote: see README.md.Standard Note Syntax
--   duration: how long to play in ticks
--   delay: wait before start in ticks
add_loop = function(snote, duration, delay)
    local notes, err = mus.get_notes_from_string(snote)

    -- Check args.
    if notes == nil then
        api.log_error("Invalid note name: "..snote)
        valid = false
    end

    -- if duration == nil then
    --     api.log_error("Invalid duration")
    --     valid = false
    -- end

    -- if delay == nil then
    --     api.log_error("Invalid delay")
    --     valid = false
    -- end

    if valid then
        table.insert(loops, { snote=snote, duration=duration, delay=delay, notes=notes, next_start=delay })
    end
end

