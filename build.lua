--[[
Used to build and test all parts of this system.
--]]


-- Fix up the lua path first.
package.path = './lua/?.lua;./test/lua/?.lua;'..package.path

local ut = require('lbot_utils')
local sx = require('stringex')

local opt = arg[1]
local ret_code = 0
local res = {}

-- Make pretty.
local GRAY = string.char(27).."[95m" --"[90m"
local RED = string.char(27).."[41m" --"[91m"
local BLUE = string.char(27).."[94m"
local YELLOW = string.char(27).."[33m"
local GREEN = string.char(27).."[92m"
local RESET = string.char(27).."[0m"

local function output_text(text)
    -- Split into lines and colorize.
    lines = sx.strsplit(text, '\n', false)
    for _, s in ipairs(lines) do
        if sx.startswith(s, 'Build: ') then
            print(GREEN..s..RESET)
        elseif sx.startswith(s, '! ') then
            print(RED..s..RESET)
        elseif sx.contains(s, '): error ') then
            print(RED..s..RESET)
        elseif sx.contains(s, '): warning ') then
            print(YELLOW..s..RESET)
        else
            print(s)
        end
    end
end


-------------------------------------------------------------------------

if opt == 'build_app' then

    bld = '"C:/Program Files/Microsoft Visual Studio/2022/Community/Msbuild/Current/Bin/MSBuild.exe"'
    -- -r restore first
    -- -t:rebuild
    -- Verbosity levels: q[uiet], m[inimal], n[ormal] (default), d[etailed], and diag[nostic].
    vrb = '-v:m'

    output_text('Build: Building app...')
    cmd = sx.strjoin(' ', { bld, vrb, 'Nebulua.sln' } )
    res = ut.execute_and_capture(cmd)
    output_text(res)

    output_text('Build: Building app tests...')
    cmd = sx.strjoin(' ', { bld, vrb, 'test/NebuluaTest.sln' } )
    res = ut.execute_and_capture(cmd)
    output_text(res)

elseif opt == 'app_tests' then

    output_text('Build: Running app tests...')
    cmd = 'pushd "test/Cli/bin/x64/Debug/net8.0-windows" & TestCli.exe & popd'
    res = ut.execute_and_capture(cmd)
    output_text(res)

    cmd = 'pushd "test/Core/bin/x64/Debug/net8.0-windows" & TestCore.exe & popd'
    res = ut.execute_and_capture(cmd)
    output_text(res)

    cmd = 'pushd "test/Interop/bin/x64/Debug/net8.0-windows" & TestInterop.exe & popd'
    res = ut.execute_and_capture(cmd)
    output_text(res)

    cmd = 'pushd "test/Misc/bin/x64/Debug/net8.0-windows" & TestMisc.exe & popd'
    res = ut.execute_and_capture(cmd)
    output_text(res)

elseif opt == 'lua_tests' then

    output_text('Build: Running lua tests...')
    local pr = require('pnut_runner')

    rep = pr.do_tests('test_bar_time')
    -- rep = pr.do_tests('test_defs', 'test_bar_time', 'test_nebulua')
    for _, s in ipairs(rep) do
        output_text(s)
    end

elseif opt == 'gen_md' then

    output_text('Build: Gen markdown from definition files...')

    mus = require('music_defs')
    mid = require('midi_defs')
    sx  = require('stringex')

    text = mus.gen_md()
    content = sx.strjoin('\n', text)
    f = io.open('docs/music_defs.md', 'w')
    f:write(content)
    f:close()

    text = mid.gen_md()
    content = sx.strjoin('\n', text)
    f = io.open('docs/midi_defs.md', 'w')
    f:write(content)
    f:close()

else

    if opt == nil then
        output_text('Build: Error: Missing option - Select one of:')
    else
        output_text('Build: Error: Invalid option '..opt..' - Select one of:')
    end

    output_text('build_app  app_tests  lua_tests  gen_md')
    -- goto done
    ret_code = 1

end

return ret_code
