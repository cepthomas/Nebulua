
-- A take on Eno's "algorithmic" Music for Airports - ported from github.com/teropa/musicforairports.js

local neb = require("nebulua") -- lua api
local md  = require("midi_defs")
local bt  = require("bar_time")
-- local inst = md.instruments



------------------------- Vars ----------------------------------------

-- local vars - Volumes. TODO1 stitch into playing sequences.
local keys_vol = 0.8
local drum_vol = 0.8

local volume = 0.8

local valid = false

-- Possible loops.
-- List<TapeLoop> _loops = new List<TapeLoop>();
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
    dur = dur_to_subbeats(duration)
    del = dur_to_subbeats(delay)
    next_start = del

    -- Check values.
    if notes == nil or notes == nil or notes == nil then
        valid = false
    else
        table.insert(loops, { snote=snote, dur=dur, del=del, notes=notes, next_start=del })
        valid = valid and true
    end
end


-----------------------------------------------------------------------------
-- Init stuff.
function setup()
    -- neb.info("example initialization")
    math.randomseed(os.time())

    -- Set up the _loops.
    -- Key is Ab.
    add_loop("Ab4", BT(17,3),  BT(8,1)) nope...
    add_loop("Ab5", BT(17,2),  BT(3,1))
    -- 3rd
    add_loop("C5",  BT(21,1),  BT(5,3))
    -- 4th
    add_loop("Db5", BT(18,2),  BT(12,3))
    -- 5th
    add_loop("Eb5", BT(20,0),  BT(9,2))
    -- 6th
    add_loop("F4",  BT(19,3),  BT(4,2))
    add_loop("F5",  BT(20,0),  BT(14,1))

    neb.set_tempo(70)

    return 0

-----------------------------------------------------------------------------
-- Main loop - called every mmtimer increment.
function step(tm) --bar, beat, subbeat)
    if not valid then
        subbeats = to_subbeats(bar, beat, subbeat)
        for _, loop in ipairs(loops) do
            if subbeats >= loop.next_start then
                print("Starting note", loop.snote)
                for _, note in ipairs(loop.notes) do
                    neb.send_note(chan_hnd, note, volume, loop.dur)
                -- Calc next time.
                loop.next_start = subbeats + loop.delay + loop.duration;
        end
    end
end
