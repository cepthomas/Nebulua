--[[
-- Example Nebulator composition file with some UI demo. This is not actual music.
--]]

api = require "neb_api"
ut = require "utils"
scale = require "scale"


-- -- These should come from a sys/util file
-- function error(msg) api.log(4, msg) end
-- function info(msg) api.log(2, msg) end
-- function debug(msg) api.log(1, msg) end

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
  keys  = { dev_type="midi_out", channel=1, patch="AcousticGrandPiano" },
  bass  = { dev_type="midi_out", channel=2, patch="AcousticBass" },
  synth = { dev_type="midi_out", channel=3, patch="Lead1Square" },
  drums = { dev_type="midi_out", channel=10, patch="Jazz" }, -- for drums = kit
  tune  = { dev_type="midi_in", channel=1 },
  trig  = { dev_type="virt_key", channel=2 },  -- opt props: shownotenames, keysize
  whiz  = { dev_type="bing_bong", channel=10 } -- opt props: minnote, maxnote, mincontrol, maxcontrol, drawnotegrid
}


-- local vars - Volumes. 
local KEYS_VOL = 0.8
local DRUM_VOL = 0.8

-- Create sets of notes.
local scaleNotes = api.create_notes("MY_SCALE", "1 3 4 b7")
local myScaleNotes = api.get_notes("B4.MY_SCALE")
-- Get some stock chords and scales.
local scaleNotes = api.get_notes("G3.Algerian")
local chordNotes = api.get_notes("C4.o7")




-- Print something.
for n in pairs(_G) do api.log(n) end


------------------------- Init ----------------------------------------------------
-- Called to initialize Nebulator stuff.
function setup()
  api.log("module initialization")
end


------------------------- Main loop ----------------------------------------------------

-- Called every mmtimer increment.
function step(bar, beat, subdiv)

  u.Boing(60)

  if beat == 0 and subdiv == 0 then
    api.send_controller(SYNTH, "Pan", 90)
    api.send_controller(KEYS,  "Pan", 30)
  end


  -- dump
  for n in pairs(_G) do api.log(n) end

  -- Process the data passed from C. my_static_data contains the equivalent of my_static_data_t.
  slog = string.format ("script_string:%s script_int:%s", script_string, script_int)
  api.log(slog)

  -- Start working.
  api.log("do some pretend script work then yield")

  for i = 1, 5 do
    api.log("doing loop number " .. i)

    -- Do pretend work.
    counter = 0
    while counter < 1000 do
      counter = counter + 1
    end
    -- ut.sleep(200)

    -- Plays well with others.
    coroutine.yield()
  end
  api.log("done loop")
end


-------------- Handlers for input events --------------------------

-- Override handler.
function input_note(dev, channel, note, vel)
    api.log("Input note", dev, channel, note, vel)
    api.send_note("synth", note, vel, 0.5)
end

-- Override handler.
function input_controller(dev, channel, ctlid, value)
    api.log("Input controller", dev, channel, ctlid, value)
end

----------------------- Lua functions -------------------------

function algo_func()
  notenum = math.random(0, #scaleNotes)
  api.send_note(SYNTH, scaleNotes[notenum], 0.7, 0.5)
end


------------------------- Build composition ----------------------------------------------------



-- --------------------------------------------

---- sequences ----
-- when, named chords, volume, duration(def=0.1)
seq_KeysChorus = [[
0.0     F4      0.7     0.2
2.0     F5      0.7      0.2
5.4     B3.m7   KEYS_VOL     0.2
-- etc...
7.4     B2.m7   0.7     0.2
8.0 end?]]


-- This is probably the better way to do drums and rhythmic instruments. But could be notes too.
-- |........|........|........|........|........|........|........|........|
seq_DrumsVerse = [[
|8       |        |8       |        |8       |        |8       |        | AcousticBassDrum  DRUM_VOL
|    8   |        |    8   |    8   |    8   |        |    8   |    8   | AcousticSnare     DRUM_VOL*0.9
|        |     8 8|        |     8 8|        |     8 8|        |     8 8| ClosedHiHat       DRUM_VOL*1.1
]]

---- sections ----
sect_Middle = [[
keys    seq_KeysChorus  seq_KeysChorus  seq_KeysChorus  seq_KeysChorus
drums   seq_DrumsChorus seq_DrumsChorus seq_DrumsChorus seq_DrumsChorus
bass    seq_BassChorus  seq_BassChorus  seq_BassChorus  seq_BassChorus
synth   algo_func       nil             algo_func       seq_Dynamic
end
]]



--[[

---- sequences ----


-- This is probably the better way to do chordal instruments.
-- when, named chords, volume, duration(def=0.1)
-- seq_name total-beats
seq_KeysChorus 8
0.0     F4      0.7     0.2
-- more steps...
7.4     B2.m7   0.7     0.2


-- This is probably the better way to do drums and rhythmic instruments. But could be notes too.
--|........|........|........|........|........|........|........|........|
seq_DrumsVerse 8
|8       |        |8       |        |8       |        |8       |        | AcousticBassDrum  DRUM_VOL
|    8   |        |    8   |    8   |    8   |        |    8   |    8   | AcousticSnare     DRUM_VOL*0.9
|        |     8 8|        |     8 8|        |     8 8|        |     8 8| ClosedHiHat       DRUM_VOL*1.1




seq_Algo = algo_func


---- sections ----

-- sect_name  length???

sect_Beginning
keys    seq_KeysVerse   seq_KeysVerse   seq_KeysVerse   seq_KeysVerse
drums   seq_DrumsVerse  seq_DrumsVerse  seq_DrumsVerse  seq_DrumsVerse
bass    seq_BassVerse   seq_BassVerse   seq_BassVerse   seq_BassVerse

sect_Middle
keys    seq_KeysChorus  seq_KeysChorus  seq_KeysChorus  seq_KeysChorus
drums   seq_DrumsChorus seq_DrumsChorus seq_DrumsChorus seq_DrumsChorus
bass    seq_BassChorus  seq_BassChorus  seq_BassChorus  seq_BassChorus
synth   seq_Algo        nil             seq_Algo        seq_Dynamic     nil

--]]
