
-- Import modules this needs.
local api = require("script_api")
local mus = require("music_defs")
local mid = require("midi_defs")
local bt  = require("bar_time")
local ut  = require('lbot_utils')
local sx  = require("stringex")

local M = {}

-- Setup for debug.
-- ut.config_debug(true)
-- dbg()

-- Say hello.
api.log_info('Loading other.lua...')


-----------------------------------------------------------------------------
-- Do something.
function M.do_something()
    local note_num = math.random(1, 10)
    api.log_info('>>>'..note_num)
end


-- Return the module.
return M
