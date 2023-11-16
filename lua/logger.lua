-- Simple logger using host api.

local api = require("neb_api")

-- Create the namespace/module.
local M = {}

-- Host defs: enum LogLevel { Trace = 0, Debug = 1, Info = 2, Warn = 3, Error = 4 }
M.LOG_LEVEL = { TRC=0, DBG=1, INF=2, WRN=3, ERR=4 }


-----------------------------------------------------------------------------
-- Log something.
-- @param level int LOG_LEVEL.
-- @param msg string Content.
function M.log(level, msg)
    api.log(level, msg)

    -- local marker = ""
    -- if level == M.LOG_LEVEL.WRN then marker = "? "
    -- elseif level == M.LOG_LEVEL.ERR then marker = "! "
    -- end
    -- api.log(marker .. msg)
end

-- Convenience functions.
function M.error(msg) api.log(M.LOG_LEVEL.ERR, msg) end
function M.warn(msg)  api.log(M.LOG_LEVEL.WRN, msg) end
function M.info(msg)  api.log(M.LOG_LEVEL.INF, msg) end
function M.debug(msg) api.log(M.LOG_LEVEL.DBG, msg) end
function M.trace(msg) api.log(M.LOG_LEVEL.TRC, msg) end


-----------------------------------------------------------------------------
-- Return the module.
return M
