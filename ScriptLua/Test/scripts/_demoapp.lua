--[[
Lua script for a simplistic coroutine application.
--]]


local li = require "luainterop" -- C module
local ut = require "utils"
local math = require "math"

-- print("*** script 1")

-- This is the same as the C type.
state_type = {
  [1] = 'ST_READY',       -- Ready to be scheduled
  [2] = 'ST_IN_PROCESS',  -- Scheduled or running
  [3] = 'ST_DONE'         -- All done
}

-- Print something.
function tell(s)
  li.cliwr('S:'..s)
end

------------------------- Main loop ----------------------------------------------------

function do_it()
  tell("module initialization")

  -- for n in pairs(_G) do print(n) end

  -- Process the data passed from C. my_static_data contains the equivalent of my_static_data_t.
  slog = string.format ("script_string:%s script_int:%s", script_string, script_int)
  tell(slog)

  -- Start working.
  tell("do some pretend script work then yield")

  for i = 1, 5 do
    tell("doing loop number " .. i)

    -- Do pretend work.
    counter = 0
    while counter < 1000 do
      counter = counter + 1
    end
    -- ut.sleep(200)

    -- Plays well with others.
    coroutine.yield()
  end
  tell("done loop")
end


-------------- Handlers for commands from C --------------------------

-- Pin input has arrived from board via C.
function hinput (pin, value)
  tell(string.format("demoapp: got hinput pin:%d value:%s ", pin, tostring(value)))
end

-- Dumb calculator, only does addition.
function calc (x, y)
  return (x + y)
end

-- Just a test for struct IO.
function structinator(data)
  state_name = state_type[data.state]
  slog = string.format ("demoapp: structinator got val:%d state:%s text:%s", data.val, state_name, data.text)
  tell(slog)

  -- Package return data.
  data.val = data.val + 1
  data.state = 3
  data.text = "Back atcha"

  return data
end