--[[
Performs test run discovery, management, report generation.
Future opts maybe: write to file, junit/xml format.
--]]

local pn = require("pnut")
local ut = require("utils")


local start_time = os.clock()
local start_date = os.date()
-- Indicates an app failure.
local app_fail = false
-- Indicates an error in the user script.
local script_fail = false


-----------------------------------------------------------------------------
-- -- Errors not associated with test cases.
-- local function internal_error(msg)
--     pn.UT_ERROR(debug.traceback(msg, 1))
-- end

-----------------------------------------------------------------------------
-- Report writer.
local function report_line(line)
    io.write(line, '\n')
    -- local rf = nil
    -- local report_fn = nil -- or from user
    -- local function report_line(line)
    --     if rf ~= nil then
    --         rf:write(line, "\n")
    --     end
end

-----------------------------------------------------------------------------
-- Get the cmd line args.
if #arg < 1 then
    -- log a message and exit.
    error("No files supplied")
    app_fail = true
    goto done
end

-- Process each script filename.
for i = 1, #arg do
    -- load script
    local scrfn = arg[i]
    local mod = scrfn:gsub('%.lua', '')

    -- Load file in protected mode.
    ok, mut = pcall(require, mod)

-- local ok, res = xpcall(f, debug.traceback, args...)

    if not ok or type(mut) ~= "table" then
        error(string.format("Failed to load file %s: %s ", scrfn, mut))
        app_fail = true
        goto done
    end

    -- Dig out the test cases.
    for k, v in pairs(mut) do
        if type(v) == "function" and k:match("suite_") then
            -- Found something to do. Run it in between optional test boilerplate.
            pn.start_suite(k.." in "..scrfn)

            -- Optional setup().
            local ok, result = xpcall(mut.setup, debug.traceback, pn)

            -- Run the suite.
            ok, result = xpcall(v, debug.traceback, pn)
            if not ok then
                pn.UT_ERROR(result)
                script_fail = true
                goto done
            end

            -- Optional teardown().
            ok, result = xpcall(mut.teardown, debug.traceback, pn)
        end
    end
end


-----------------------------------------------------------------------------
::done::

-- Metrics.
local end_time = os.clock()
local dur = (end_time - start_time)

-- Open the report file.
if report_fn ~= nil then
    rf = io.open (report_fn, "w+")
end

-- Overall status.
if app_fail then pf_run = "Runner Fail"
elseif script_fail then pf_run = "Script Fail"
elseif pn.num_suites_failed == 0 then pf_run = "Test Pass"
else pf_run = "Test Fail"
end

-- Report.
report_line("#------------------------------------------------------------------")
report_line("# Unit Test Report")
report_line("# Start Time: "..start_date)
report_line("# Duration: "..dur)
report_line("# Suites Run: "..pn.num_suites_run)
report_line("# Suites Failed: "..pn.num_suites_failed)
report_line("# Cases Run: "..pn.num_cases_run)
report_line("# Cases Failed: "..pn.num_cases_failed)
report_line("# Run Result: "..pf_run)
report_line("#------------------------------------------------------------------")
report_line("")

-- Add the accumulated text.
for i, v in ipairs(pn.result_text) do
    report_line(v)
end

-- Close the report file
if rf ~= nil then
    rf:close()
end
