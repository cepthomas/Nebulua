--[[
Example Nebulator composition file with some UI demo. This is not actual music.
--]]

local api = require("neb_api_sim") -- TODO do better
local ut = require("utils")
local scale = require("scale")
local md = require("mididefs")
local inst = md.instruments

print("=============== go go go =======================")
-- print(api)



math.randomseed(os.time())

-- Identifiers.
local KEYS  <const> = "keys" 
local BASS  <const> = "bass" 
local SYNTH <const> = "synth"
local DRUMS <const> = "drums"
local TUNE  <const> = "tune" 
local TRIG  <const> = "trig" 
local WHIZ  <const> = "whiz" 


-- All the devices. Also oscout (midi over osc).
devices =
{
    keys  = { dev_type="midi_out", channel=1, patch=inst.AcousticGrandPiano },
    bass  = { dev_type="midi_out", channel=2, patch=inst.AcousticBass },
    synth = { dev_type="midi_out", channel=3, patch=inst.Lead1Square },
    drums = { dev_type="midi_out", channel=10, patch="Jazz" }, -- for drums = kit
    tune  = { dev_type="midi_in", channel=1 },
    trig  = { dev_type="virt_key", channel=2 },  -- opt props: shownotenames, keysize
    whiz  = { dev_type="bing_bong", channel=10 } -- opt props: minnote, maxnote, mincontrol, maxcontrol, drawnotegrid
}


-- local vars - Volumes. 
local KEYS_VOL = 0.8
local DRUM_VOL = 0.8

-- Create custom scale.
api.create_notes("MY_SCALE", "1 3 4 b7")
local my_scale_notes = api.get_notes("B4.MY_SCALE")
-- print(my_scale_notes)
-- Get some stock chords and scales.
local alg_scale = api.get_notes("G3.Algerian")
local chord_notes = api.get_notes("C4.o7")


------------------------- Init ----------------------------------------------------
-- Called to initialize Nebulator stuff.
function setup()
    info("example initialization")
end


------------------------- Main loop ----------------------------------------------------

-- Called every mmtimer increment.
function step(bar, beat, subdiv)
    ut.boing(60)

    -- Periodic work.
    if beat == 0 and subdiv == 0 then
        api.send_controller(SYNTH, "Pan", 90)
        api.send_controller(KEYS,  "Pan", 30)
    end
end


-------------- Handlers for input events --------------------------

-- Override handler.
function input_note(dev, channel, note, vel)
    info("input_note") -- string.format("%s", variable_name), dev, channel, note, vel)
    api.send_note("synth", note, vel, 0.5)
end

-- Override handler.
function input_controller(dev, channel, ctlid, value)
    info("input_controller") --, dev, channel, ctlid, value)
end

----------------------- Lua functions -------------------------

function algo_func()
    notenum = math.random(0, #alg_scale)
    api.send_note(SYNTH, alg_scale[notenum], 0.7, 0.5)
end

-- User functions.

function boing(notenum)
    boinged = false;

    info("boing")
    if notenum == 0 then
        notenum = Random(30, 80)
        boinged = true
        api.send_note("synth", notenum, VEL, 1.0)
    end
    return boinged
end
------------------------- Build composition ----------------------------------------------------

---- sequences ----
seq_KeysChorusX = [[
0.0     F4      0.7         0.2
2.0     F5      0.7         0.2
5.4     B3.m7   KEYS_VOL    0.2
-- etc...
7.4     B2.m7   0.7         0.2
-- end
8.0 ]]

seq_KeysChorus = {
{0.0,     "F4",      0.7,       0.2},
{2.0,     "F5",      0.7,       0.2},
{5.4,     "B3.m7",   KEYS_VOL,  0.2},
-- etc...
{7.4,     "B2.m7",   0.7,       0.2},
{8.0 }} -- end marker, specifies total length


-- This is probably the better way to do drums and rhythmic instruments. But could be notes too.
-- |........|........|........|........|........|........|........|........|
seq_DrumsVerseX = [[
|8       |        |8       |        |8       |        |8       |        |   AcousticBassDrum  DRUM_VOL
|    8   |        |    8   |    8   |    8   |        |    8   |    8   |   AcousticSnare     DRUM_VOL*0.9
|        |     8 8|        |     8 8|        |     8 8|        |     8 8|   ClosedHiHat       DRUM_VOL*1.1
]]

seq_DrumsVerse = {
--|........|........|........|........|........|........|........|........|
{"|8       |        |8       |        |8       |        |8       |        |", AcousticBassDrum,  DRUM_VOL},
{"|    8   |        |    8   |    8   |    8   |        |    8   |    8   |", AcousticSnare,     DRUM_VOL*0.9},
{"|        |     8 8|        |     8 8|        |     8 8|        |     8 8|", ClosedHiHat,       DRUM_VOL*1.1},
}

---- sections ----
sect_MiddleX = [[
keys    seq_KeysChorus  seq_KeysChorus  seq_KeysChorus  seq_KeysChorus
drums   seq_DrumsChorus seq_DrumsChorus seq_DrumsChorus seq_DrumsChorus
bass    seq_BassChorus  seq_BassChorus  seq_BassChorus  seq_BassChorus
synth   algo_func       nil             algo_func       seq_Dynamic
]]

sect_Middle = {
{keys,    seq_KeysChorus,  seq_KeysChorus,  seq_KeysChorus,  seq_KeysChorus },
{drums,   seq_DrumsChorus, seq_DrumsChorus, seq_DrumsChorus, seq_DrumsChorus },
{bass,    seq_BassChorus,  seq_BassChorus,  seq_BassChorus,  seq_BassChorus },
{synth,   algo_func,       nil,             algo_func,       seq_Dynamic },
}
