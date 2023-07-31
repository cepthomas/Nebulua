json = require "json"

math.randomseed(os.time())

-- locals.
local TUNE <const> = "tune" 
local TRIG <const> = "trig" 
local WHIZ <const> = "whiz" 


-- table of tables
things =
{
  TUNE = { dev_type="midi_in", channel=1 },
  TRIG = { dev_type="virt_key", channel=2, adouble=1.234 },
  WHIZ = { dev_type="bing_bong", channel=10, abool=true }
}
print(things)

things_json = json.encode(things)
print(things_json)
-- {"TUNE":{"channel":1,"dev_type":"midi_in"},"WHIZ":{"channel":10,"abool":true,"dev_type":"bing_bong"},"TRIG":{"channel":2,"adouble":1.234,"dev_type":"virt_key"}}

sf = string.format('%q', 'a string with "quotes" and \n new line')


-- misc globals.
g_string = "booga booga"
g_number = 7.654
g_int = 80808
g_bool = false
g_table = { dev_type="bing_bong", channel=10, abool=true }
g_list_int = { 2, 56, 98, 2 }
g_list_number = { 2.2, 56.3, 98.77, 2.303 }
g_list_string = { "a", "string", "with" }



-- arrays
a = {}
  for i=1, 10 do
    a[i] = 0
  end

-- We can use constructors to create and initialize arrays in a single expression:
squares = {1, 25, 36, 4, 81, 9, 16, 49, 64}


-- Print something.
print(squares)
print("----- pairs")
for k,v in pairs(squares) do print(k, v) end

print("----- ipairs")
for i,v in ipairs(squares) do print(i, v) end

