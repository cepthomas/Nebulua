
json = require "json"

print("Load interop.lua")

math.randomseed(os.time())

-- locals.
local TUNE  <const> = "tune" 
local TRIG  <const> = "trig" 
local WHIZ  <const> = "whiz" 


-- table of tables
things =
{
  TUNE = { type="midi_in", channel=1,  },
  TRIG = { type="virt_key", channel=2, adouble=1.234 },
  WHIZ = { type="bing_bong", channel=10, abool=true }
}

-- misc globals.
gdouble = 7.654
gbool = false
gtable = { type="bing_bong", channel=10, abool=true }
glist = { 2, 56, 98, 2 }

things_json = json.encode(things)


-- Create sets of notes.
local scaleNotes = api.create_notes("MY_SCALE", "1 3 4 b7")
local myScaleNotes = api.get_notes("B4.MY_SCALE")
-- Get some stock chords and scales.
local scaleNotes = api.get_notes("G3.Algerian")
local chordNotes = api.get_notes("C4.o7")


-- for n in pairs(_G) do api.log(n) end

local index = 1

function gfunc(msg)
  print(index, _G[index])
  index += 1
end

