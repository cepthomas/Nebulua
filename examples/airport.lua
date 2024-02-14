
-- A take on Eno's "algorithmic" Music for Airports - ported from github.com/teropa/musicforairports.js

local neb = require("nebulua") -- lua api
local md  = require("midi_defs")
local bt  = require("bar_time")
-- local inst = md.instruments


------------------------- Vars ----------------------------------------

-- local vars
local volume = 0.8
local valid = false

-- Possible loops.
local loops = {}

------------------------- Config ----------------------------------------

-- Devices
local midi_out = "Microsoft GS Wavetable Synth"

-- Channels
local hout = create_output_channel(midi_out, 1, md.instruments.Pad2Warm)

-- note string, BT dur, BT delay
local function add_loop(snote, duration, delay)
    -- List of note numbers or nil, error if invalid nstr.
    notes, err = md.get_notes_from_string(snote)
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

local function tot(beat, sub)
    bt = BT(beat, sub)
    return bt.get_tick()
end


-----------------------------------------------------------------------------
-- Init stuff.
function setup()
    -- neb.info("example initialization")
    -- math.randomseed(os.time())

    -- Set up the _loops.
    -- Key is Ab.
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

    neb.set_tempo(70)

    return 0

-----------------------------------------------------------------------------
-- Main loop - called every mmtimer increment.
function step(tick)
    if valid then
        for _, loop in ipairs(loops) do
            if tick >= loop.next_start then
                print("Starting note", loop.snote)
                for _, note_num in ipairs(loop.notes) do
                    neb.send_note(chan_hnd, note_num, volume, loop.dur)
                -- Calc next time.
                loop.next_start = tick + loop.delay + loop.duration;
        end
    end
end
