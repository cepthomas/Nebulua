
json = require "json"

printex("Loading luaex.lua!")

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
g_number = 7.654
g_int = 80808
g_bool = false
g_table = { table_type="bing_bong", channel=10, abool=true }
g_list = { 2, 56, 98, 2 }

things_json = json.encode(things)

-- for n in pairs(_G) do api.log(n) end

local index = 1

function g_func(s)
  printex(#s .. " " .. index)
  index = index + 1
  return #s
end

