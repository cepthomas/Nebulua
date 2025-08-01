-- Makes some markdown from definitions files.

package.path = '/Dev/Apps/Nebulua/lua/?.lua;/Dev/Apps/Nebulua/LBOT/?.lua;'..package.path

local mid = require("midi_defs")
local mus = require("music_defs")

for _,v in ipairs(mid.gen_md()) do
   print(v)
end

local mid = require("midi_defs")
for _,v in ipairs(mus.gen_md()) do
   print(v)
end
