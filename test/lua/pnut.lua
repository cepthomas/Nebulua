--[[
Core module for executing the test suites themselves.
Has all the assert functions - UT_XXX(info).
    - Each has an optional info arg for reporting.
    - Each returns a bool pass status.
]]

local ut = require('lbot_utils')

local M = {}

-- Create an execution context.
M.num_suites_run = 0
M.num_suites_failed = 0
M.num_cases_run = 0
M.num_cases_failed = 0
M.result_text = {}

-- Current states.
local curr_suite_pass = true


-----------------------------------------------------------------------------
-- General msg output to file and log.
-- @param msg The info to write.
local function write_line(msg)
    table.insert(M.result_text, msg)
end

-----------------------------------------------------------------------------
-- Error msg output to file and log.
-- @param msg The info to write.
local function write_error(msg)
    write_line('! '..msg)
end

-----------------------------------------------------------------------------
-- A case has failed so update all states and counts.
-- @param msg message.
-- @param info optional additional info.
local function case_failed(msg, info)
    -- Update the states and counts.
    if curr_suite_pass then
        curr_suite_pass = false
        M.num_suites_failed = M.num_suites_failed + 1
    end

    M.num_cases_failed = M.num_cases_failed + 1

    -- Print failure information.
    local filename, linenumber, _ = ut.get_caller_info(4)
    info = info or ''
    write_error(filename..':'..linenumber..' '..msg..'. '..info)
end

-----------------------------------------------------------------------------
-- Start a new suite.
-- @param desc Free text.
function M.start_suite(desc)
    write_line('\nRunning Suite: '..desc)
    write_line('-----------------------------------------------------------')

    -- Reset the current p/f states.
    curr_suite_pass = true

    M.num_suites_run = M.num_suites_run + 1
end

-----------------------------------------------------------------------------
-- Reset everything.
function M.reset()
    M.num_suites_run = 0
    M.num_suites_failed = 0
    M.num_cases_run = 0
    M.num_cases_failed = 0
    M.result_text = {}
    curr_suite_pass = true
end

-----------------------------------------------------------------------------
-- Add a general comment line to the report.
-- @param info free text.
function M.UT_INFO(info)
    write_line(info)
    return true
end

-----------------------------------------------------------------------------
-- Add an error comment line to the report.
-- @param info free text.
function M.UT_ERROR(info)
    write_error(info)
    return false
end

-----------------------------------------------------------------------------
-- Tests expression and registers a failure if not true.
-- @param expr Boolean expression.
-- @param info optional additional info.
function M.UT_TRUE(expr, info)
    local pass = true
    M.num_cases_run = M.num_cases_run + 1
    if expr == nil or expr == false then
        case_failed('Expression is not true', info)
        pass = false
    end
    return pass
end

-----------------------------------------------------------------------------
-- Tests expression and registers a failure if not true.
-- @param expr Boolean expression.
-- @param info optional additional info.
function M.UT_FALSE(expr, info)
    local pass = true
    M.num_cases_run = M.num_cases_run + 1
    if expr == nil or expr == true then
        case_failed('Expression is not false', info)
        pass = false
    end
    return pass
end

-----------------------------------------------------------------------------
-- Tests expression and registers a failure if not true.
-- @param expr Boolean expression.
-- @param info optional additional info.
function M.UT_NOT_NIL(expr, info)
    local pass = true
    M.num_cases_run = M.num_cases_run + 1
    if expr == nil then
        case_failed('Expression is nil', info)
        pass = false
    end
    return pass
end

-----------------------------------------------------------------------------
-- Tests expression and registers a failure if not true.
-- @param expr Boolean expression.
-- @param info optional additional info.
function M.UT_NIL(expr, info)
    local pass = true
    M.num_cases_run = M.num_cases_run + 1
    if expr ~= nil then
        case_failed('Expression is not nil', info)
        pass = false
    end
    return pass
end

-----------------------------------------------------------------------------
-- Tests expression and registers a failure if not equal.
-- @param val1 First value.
-- @param val2 Second value.
-- @param info optional additional info.
function M.UT_STR_EQUAL(val1, val2, info)
    local pass = true
    M.num_cases_run = M.num_cases_run + 1

    if type(val1) ~= 'string' then
        local msg = string.format('[%s] is not a string', tostring(val1))
        case_failed(msg, info)
        pass = false
    elseif type(val2) ~= 'string' then
        local msg = string.format('[%s] is not a string', tostring(val2))
        case_failed(msg, info)
        pass = false
    elseif val1 ~= val2 then
        local msg = string.format('[%s] is not equal to [%s]', tostring(val1), tostring(val2))
        case_failed(msg, info)
        pass = false
    end

    return pass
end

-----------------------------------------------------------------------------
-- Tests expression and registers a failure if equal.
-- @param val1 First value.
-- @param val2 Second value.
-- @param info optional additional info.
function M.UT_STR_NOT_EQUAL(val1, val2, info)
    local pass = true
    M.num_cases_run = M.num_cases_run + 1

    if type(val1) ~= 'string' then
        local msg = string.format('[%s] is not a string', tostring(val1))
        case_failed(msg, info)
        pass = false
    elseif type(val2) ~= 'string' then
        local msg = string.format('[%s] is not a string', tostring(val2))
        case_failed(msg, info)
        pass = false
    elseif val1 == val2 then
        local msg = string.format('[%s] is equal to [%s]', tostring(val1), tostring(val2))
        case_failed(msg, info)
        pass = false
    end

    return pass
end

-----------------------------------------------------------------------------
-- Tests expression and registers a failure if phrase not in val.
-- @param val Look in this...
-- @param phrase For this.
-- @param info optional additional info.
function M.UT_STR_CONTAINS(val, phrase, info)
    local pass = true
    M.num_cases_run = M.num_cases_run + 1

    if type(val) ~= 'string' then
        local msg = string.format('[%s] is not a string', tostring(val))
        case_failed(msg, info)
        pass = false
    elseif type(phrase) ~= 'string' then
        local msg = string.format('[%s] is not a string', tostring(phrase))
        case_failed(msg, info)
        pass = false
    elseif val:find(phrase, 1, true) == nil then
        local msg = string.format('[%s] does not contain [%s]', tostring(val), tostring(phrase))
        case_failed(msg, info)
        pass = false
    end

    return pass
end

-----------------------------------------------------------------------------
-- Tests expression and registers a failure if not equal.
-- @param val1 First value.
-- @param val2 Second value.
-- @param info optional additional info.
function M.UT_EQUAL(val1, val2, info)
    local pass = true
    M.num_cases_run = M.num_cases_run + 1
    if val1 ~= val2 then
        local msg = string.format('[%s] is not equal to [%s]', tostring(val1), tostring(val2))
        case_failed(msg, info)
        pass = false
    end
    return pass
end

-----------------------------------------------------------------------------
-- Tests expression and registers a failure if equal.
-- @param val1 First value.
-- @param val2 Second value.
-- @param info optional additional info.
function M.UT_NOT_EQUAL(val1, val2, info)
    local pass = true
    M.num_cases_run = M.num_cases_run + 1
    if val1 == val2 then
        local msg = string.format('[%s] is equal to [%s]', tostring(val1), tostring(val2))
        case_failed(msg, info)
        pass = false
    end
    return pass
end

-----------------------------------------------------------------------------
-- Tests expression and registers a failure if not less than.
-- @param val1 First value.
-- @param val2 Second value.
-- @param info optional additional info.
function M.UT_LESS(val1, val2, info)
    local pass = true
    M.num_cases_run = M.num_cases_run + 1
    if not(val1 < val2) then
        local msg = string.format('[%s] is not less than [%s]', tostring(val1), tostring(val2))
        case_failed(msg, info)
        pass = false
    end
    return pass
end

-----------------------------------------------------------------------------
-- Tests expression and registers a failure if not less than or equal.
-- @param val1 First value.
-- @param val2 Second value.
-- @param info optional additional info.
function M.UT_LESS_OR_EQUAL(val1, val2, info)
    local pass = true
    M.num_cases_run = M.num_cases_run + 1
    if not(val1 <= val2) then
        local msg = string.format('[%s] is not less than or equal to [%s]', tostring(val1), tostring(val2))
        case_failed(msg, info)
        pass = false
    end
    return pass
end

-----------------------------------------------------------------------------
-- Tests expression and registers a failure if not greater than.
-- @param val1 First value.
-- @param val2 Second value.
-- @param info optional additional info.
function M.UT_GREATER(val1, val2, info)
    local pass = true
    M.num_cases_run = M.num_cases_run + 1
    if not(val1 > val2) then
        local msg = string.format('[%s] is not greater than [%s]', tostring(val1), tostring(val2))
        case_failed(msg, info)
        pass = false
    end
    return pass
end

-----------------------------------------------------------------------------
-- Tests expression and registers a failure if not greater than or equal.
-- @param val1 First value.
-- @param val2 Second value.
-- @param info optional additional info.
function M.UT_GREATER_OR_EQUAL(val1, val2, info)
    local pass = true
    M.num_cases_run = M.num_cases_run + 1
    if not(val1 >= val2) then
        local msg = string.format('[%s] is not greater than or equal to [%s]', tostring(val1), tostring(val2))
        case_failed(msg, info)
        pass = false
    end
    return pass
end

-----------------------------------------------------------------------------
-- Tests expression and registers a failure if not close to each other.
-- @param val1 First value.
-- @param val2 Second value.
-- @param tol Within tolerance.
-- @param info optional additional info.
function M.UT_CLOSE(val1, val2, tol, info)
    local pass = true
    M.num_cases_run = M.num_cases_run + 1
    if math.abs(val1 - val2) > tol then
        local msg = string.format('[%s] is not close to [%s]', tostring(val1), tostring(val2))
        case_failed(msg, info)
        pass = false
    end
    return pass
end

-----------------------------------------------------------------------------
-- Return the module.
return M
