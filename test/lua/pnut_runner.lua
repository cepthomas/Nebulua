--[[
Performs test run discovery, management, report generation.
Can be run from the cmd line or require pnut_runner from another script/module.
Future opts maybe: write to file, junit/xml format.
--]]


local pn = require("pnut")
local ut = require("utils")

-- Create the namespace/module.
local M = {}


-- Optional - get from user.
local report_fn = nil

-----------------------------------------------------------------------------
-- Report writer.
local function report_line(line)
    if report_fn == nil then
        io.write(line, '\n')
    else
        local fout = io.open(report_fn, "w")
        if fout == nil then
            error("Invalid filename: "..report_fn)
        else
            fout:write(line, '\n')
            fout:close()
        end
    end
end

-----------------------------------------------------------------------------
function M.do_tests(...)
    local func_arg = {...}
    pn.reset()

    local start_time = os.clock()
    local start_date = os.date()
    -- Indicates an app failure.
    local app_fail = false
    -- Indicates an error in the user script.
    local script_fail = false


    for i = 1, #func_arg do
        local scrfn = func_arg[i]

        -- load script by converting to module.
        local mod = scrfn:gsub('%.lua', '')

        -- Load file in protected mode.
        local ok, test_mod = pcall(require, mod)
        -- local ok, test_mod = xpcall(require, debug.traceback, mod)

        if not ok then -- or type(test_mod) ~= "table" then
            app_fail = true
            error(string.format("Failed to load file %s test_mod:%s ", scrfn, test_mod))
            -- goto done
        end

        -- Dig out the test cases.
        for k, v in pairs(test_mod) do
            if type(v) == "function" and k:match("suite_") then
                -- Found something to do. Run it in between optional test boilerplate.
                pn.start_suite(k.." in "..scrfn)

                -- Optional setup().
                local ok, result = xpcall(test_mod.setup, debug.traceback, pn)

                -- Run the suite.
                ok, result = xpcall(v, debug.traceback, pn)
                if not ok then
                    pn.UT_ERROR(result)
                    script_fail = true
                    -- goto done
                end

                -- Optional teardown().
                ok, result = xpcall(test_mod.teardown, debug.traceback, pn)
            end
        end
    end

    -- Finished tests.
    local end_time = os.clock()
    local dur = (end_time - start_time)

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
    for _, v in ipairs(pn.result_text) do
        report_line(v)
    end

    pn.result_text = ''
end

--------------------- start here ----------------------------------------
-- Get script args.
scrarg = {...}

if #scrarg >= 1 then
    if scrarg[1] == 'pnut_runner' then
        -- From require. Return the module and let the client handle test scripts.
        return M
    else
        -- From command line. Process all.
        M.do_tests(...)
    end
else
    -- From cmd line with No arg.
    error('Missing required argument')
end

-- Return the module.
return M
