
-- Minimal script for interop unit test - was script_happy.lua.

local li = require("luainterop")
local mus = require("music_defs")
local bt = require("music_time")
local ut = require("lbot_utils")


-- info = 2
li.log(2, "======== script_happy.lua is a beautiful thing ==========")


------------------------- Config ----------------------------------------

-- Specify midi devices.
local dev_out1 = "Microsoft GS Wavetable Synth"
local dev_out2 = "loopMIDI Port"
local dev_in1  = "loopMIDI Port"

-- Channels
local hnd_piano = li.open_midi_output(dev_out1, 1, 2)
local hnd_synth = li.open_midi_output(dev_out1, 2, 90)
local hnd_drums = li.open_midi_output(dev_out1, 10, 8)
local hnd_input = li.open_midi_input(dev_in1, 3)


------------------------- Vars ----------------------------------------

-- Get some stock chords and scales.
local alg_scale = mus.get_notes_from_string("G3.Algerian")
local chord_notes = mus.get_notes_from_string("C4.o7")

--------------------- Called from app -----------------------------------

-----------------------------------------------------------------------------
-- Init stuff. Required function.
function setup()
    li.set_tempo(95)
    return "ok"
end

-----------------------------------------------------------------------------
-- Main loop - called every mmtimer increment. Required function.
function step(tick)

    local bar, beat, sub = bt.tick_to_bt(tick)
    if beat == 0 and sub == 0 then
        li.send_midi_controller(hnd_synth, 50, 51)
    end

    if beat == 1 and sub == 4 then
        li.send_midi_controller(hnd_synth, 60, 61)
    end

    return 0
end

-----------------------------------------------------------------------------
-- Handler for input note events. Optional.
function receive_midi_note(chan_hnd, note_num, volume)
    local s = string.format("Script rcv note:%d hnd:%d vol:%f", note_num, chan_hnd, volume)
    li.log(2, s)

    if chan_hnd == hnd_input then
        li.send_midi_note(hnd_synth, note_num + 1, volume * 0.5, 8)
    end
    return 0
end

-----------------------------------------------------------------------------
-- Handler for input controller events. Optional.
function receive_midi_controller(chan_hnd, controller, value)
    local s = string.format("Script rcv controller:%d hnd:%d val:%f", controller, chan_hnd, value)
    li.log(2, s)
    return 0
end
