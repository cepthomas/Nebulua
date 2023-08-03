
json = require "json"

printex("Loading luaex.lua!")

math.randomseed(os.time())

-- locals.
-- local tune <const> = "tune" 
-- local trig <const> = "trig" 
-- local whiz <const> = "whiz" 


-- table of tables
things =
{
  tune = { dev_type="midi_in", channel=1, long_list={ 44, 77, 101 } },
  trig = { dev_type="virt_key", channel=2, adouble=1.234 },
  whiz = { dev_type="bing_bong", double_table={ 145.89, 71.23, 909.555 }, channel=99 },
  per = { dev_type="abra", string_table={ 1, 23, 88, 22 }, channel=111, abool=false },
  --invalid_table = { atable={ 1, 2, 3, "ppp", 88.22 }, channel=10, abool=true }
}

invalid_table = { 1, 2, 3, "ppp", 88.22 }

start_timer()
things_json = json.encode(things)
-- printex(things_json)
msec = stop_timer()
printex('things_json op took ' .. msec .. ' msec')
-- 0.05 to 0.85(first only?)

-- misc globals.
g_string = "booga booga"
g_number = 7.654
g_int = 80808
g_bool = false
g_table = { dev_type="bing_bong", channel=10, abool=true }
g_list_int = { 2, 56, 98, 2 }
g_list_number = { 2.2, 56.3, 98.77, 2.303 }
g_list_string = { "a", "string", "with" }

start_timer()
things_list = json.encode(g_list_int)
-- printex(g_list_int)
msec = stop_timer()
printex('things_list op took ' .. msec .. ' msec')
--lot faster


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



local index = 1

function g_func(s)
  printex(#s .. "," .. index)
  index = index + 1
  return #s
end

