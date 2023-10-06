--[[
Simple logger. TODO2 will get more capabilities: user supplied streams, filtering,...
--]]

local api = require("neb_api")

-- Create the namespace/module.
local M = {}

-- Defs from the C# logger side.
M.LOG_TRACE = 0
M.LOG_DEBUG = 1
M.LOG_INFO  = 2
M.LOG_WARN  = 3
M.LOG_ERROR = 4

-- Main function.
-----------------------------------------------------------------------------
-- Description
-- Description
-- @param name type desc
-- @return type desc
function M.log(level, msg)
    local marker = ""
    if level == M.LOG_WARN then marker = "? "
    elseif level == M.LOG_ERROR then marker = "! "
    end

    api.log(marker)
    api.log(msg)
    -- api.log(marker .. msg)
end

-- Convenience functions.
function M.error(msg) api.log(M.LOG_ERROR, msg) end
function M.warn(msg) api.log(M.LOG_WARN, msg) end
function M.info(msg) api.log(M.LOG_INFO, msg) end
function M.debug(msg) api.log(M.LOG_DEBUG, msg) end


-----------------------------------------------------------------------------
-- Return the module.
return M
