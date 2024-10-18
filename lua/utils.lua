--- GP utilities: tables, math, validation, errors, ...

local sx = require("stringex")

local M = {}


---------------------------------------------------------------
--- Execute a file and return the output.
-- @param cmd Command to run.
-- @return Output text or nil if invalid file.
function M.execute_and_capture(cmd)
    local f = io.popen(cmd, 'r')
    if f ~= nil then
        local s = f:read('*a')
        f:close()
        return s
    else
        return nil
    end    
end

---------------------------------------------------------------
--- If using debugger, bind lua error() function to it.
-- @param use_dbgr Use debugger.
function M.config_debug(use_dbgr)
    local have_dbgr = false
    local orig_error = error -- save original error function
    local use_term = true -- Use terminal for debugger.

    if use_dbgr then
        have_dbgr, dbg = pcall(require, "debugger")
    end

    if dbg then 
        -- sub debug handler
        error = dbg.error
        if use_term then
            dbg.enable_color()
            dbg.auto_where = 3
        end
    else
        -- Not using debugger so make global stubs to keep breakpoints from yelling.
        dbg =
        {
            error = function(error, level) end,
            assert = function(error, message) end,
            call = function(f, ...) end,
        }
        setmetatable(dbg, { __call = function(self) end })
    end
end

-----------------------------------------------------------------------------
--- Diagnostic.
-- @param tbl What to dump.
-- @param recursive Avoid death loops.
-- @param name Of the tbl.
-- @param indent Nesting.
-- @return list table of strings
function M.dump_table(tbl, recursive, name, indent)
    local res = {}
    indent = indent or 0
    name = name or "no_name"

    if type(tbl) == "table" then
        local sindent = string.rep("    ", indent)
        table.insert(res, sindent..name.."(table):")

        -- Do contents.
        indent = indent + 1
        sindent = sindent.."    "
        for k, v in pairs(tbl) do
            if type(v) == "table" and recursive then
                trec = M.dump_table(v, recursive, k, indent) -- recursion!
                for _,v in ipairs(trec) do
                    table.insert(res, v)
                end
            else
                table.insert(res, sindent..k..":"..tostring(v).."("..type(v)..")")
            end
        end
    else
        table.insert(res, "Not a table")
    end

    return res
end

-----------------------------------------------------------------------------
--- Diagnostic.
-- @param tbl What to dump.
-- @param recursive Avoid death loops.
-- @param name Of tbl.
-- @return string
function M.dump_table_string(tbl, recursive, name)
    local res = M.dump_table(tbl, recursive, name, 0)
    return sx.strjoin('\n', res)
end

-----------------------------------------------------------------------------
--- Lua has no builtin way to count number of values in an associative table so this does.
-- @param tbl the table
-- @return number of values
function M.table_count(tbl)
    num = 0
    for k, _ in pairs(tbl) do
        num = num + 1
    end
    return num
end

-----------------------------------------------------------------------------
-- Boilerplate for adding a new kv to a table.
-- @param tbl the table
-- @param key new entry key
-- @param val new entry value
function M.table_add(tbl, key, val)
   if tbl[key] == nil then tbl[key] = {} end
   table.insert(tbl[key], val)
end

-----------------------------------------------------------------------------
--- Gets the file and line of the caller.
-- @param level How deep to look:
--    0 is the getinfo() itself
--    1 is the function that called getinfo() - get_caller_info()
--    2 is the function that called get_caller_info() - usually the one of interest
-- @return filename, linenumber or nil if invalid
function M.get_caller_info(level)
    local ret = nil
    local s = debug.getinfo(level, 'S')
    local l = debug.getinfo(level, 'l')
    fn = nil
    ln = nil
    if s ~= nil and l ~= nil then
        fn = s.short_src
        ln = l.currentline
    end
    return fn, ln
end

----------------------------------------------------------------------------
-- function M.is_integer(v) return type(v) == "number" and math.ceil(v) == v end
function M.is_integer(v) return M.to_integer(v) ~= nil end

----------------------------------------------------------------------------
function M.is_number(v) return v ~= nil and type(v) == 'number' end

----------------------------------------------------------------------------
function M.is_string(v) return v ~= nil and type(v) == 'string' end

----------------------------------------------------------------------------
function M.is_boolean(v) return v ~= nil and type(v) == 'boolean' end

----------------------------------------------------------------------------
function M.is_function(v) return v ~= nil and type(v) == 'function' end

----------------------------------------------------------------------------
function M.is_table(v) return v ~= nil and type(v) == 'table' end

-----------------------------------------------------------------------------
--- Convert value to integer.
-- @param v value to convert
-- @return integer or nil if not convertible
function M.to_integer(v)
    if type(v) == "number" and math.ceil(v) == v then return v
    elseif type(v) == "string" then return tonumber(v, 10)
    else return nil
    end
end

-----------------------------------------------------------------------------
--- Like tostring() without address info. Mainly for unit testing.
-- @param v value to convert
-- @return string
function M.tostring_cln(v)
    ret = "???"
    vtp = type(v)
    if vtp == "table" then ret = "table"
    elseif vtp == "function" then ret = "function"
    elseif vtp == "thread" then ret = "thread"
    elseif vtp == "userdata" then ret = "userdata"
    else ret = tostring(v)
    end
    return ret
end

-----------------------------------------------------------------------------
--- Remap a value to new coordinates.
-- @param val
-- @param start1
-- @param stop1
-- @param start2
-- @param stop2
-- @return
function M.map(val, start1, stop1, start2, stop2)
    return start2 + (stop2 - start2) * (val - start1) / (stop1 - start1)
end

-----------------------------------------------------------------------------
--- Bounds limits a value.
-- @param val
-- @param min
-- @param max
-- @return
function M.constrain(val, min, max)
    val = math.max(val, min)
    val = math.min(val, max)
    return val
end

-----------------------------------------------------------------------------
--- Snap to closest neighbor.
-- @param val what to snap
-- @param granularity The neighbors property line.
-- @param round Round or truncate.
-- @return snapped value
function M.clamp(val, granularity, round)
    res = (val / granularity) * granularity
    if round and (val % granularity > granularity / 2) then res = res + granularity end
    return res
end


-- Return the module.
return M
