-- Used to build and test all parts of the nebulua universe.

-- Fix up the lua path first.
package.path = './lua/?.lua;./test/lua/?.lua;'..package.path

local ut = require('lbot_utils')
local sx = require('stringex')

local current_dir = io.popen("cd"):read()
local opt = arg[1]
local ret_code = 0


-- Make pretty. TODO1 put in lbot?
local function output_text(text)
    -- Split into lines and colorize.

    local GRAY = string.char(27).."[95m" --"[90m"
    local RED = string.char(27).."[41m" --"[91m"
    local BLUE = string.char(27).."[94m"
    local YELLOW = string.char(27).."[33m"
    local GREEN = string.char(27).."[92m"
    local RESET = string.char(27).."[0m"

    lines = sx.strsplit(text, '\n', false)
    for _, s in ipairs(lines) do
        if sx.startswith(s, 'Build: ') then print(GREEN..s..RESET)
        elseif sx.startswith(s, '! ') then  print(RED..s..RESET)
        elseif sx.contains(s, '): error ') then print(RED..s..RESET)
        elseif sx.contains(s, '): warning ') then print(YELLOW..s..RESET)
        else  print(s)
        end
    end
end
-- spec = { ['Build: ']='green', ['! ']='red', ['): error ']='red', ['): warning ']='yellow' }


-------------------------------------------------------------------------

if opt == 'build_app' then

    local bld_exe = '"C:/Program Files/Microsoft Visual Studio/2022/Community/Msbuild/Current/Bin/MSBuild.exe"'
    -- -r = restore first
    -- -t:rebuild
    -- Verbosity levels: q[uiet], m[inimal], n[ormal] (default), d[etailed], and diag[nostic].
    vrb = '-v:m'

    output_text('Build: Building app...')
    cmd = sx.strjoin(' ', { bld_exe, vrb, 'Nebulua.sln' } )
    res = ut.execute_and_capture(cmd)
    output_text(res)

    output_text('Build: Building app tests...')
    cmd = sx.strjoin(' ', { bld_exe, vrb, 'test/NebuluaTest.sln' } )
    res = ut.execute_and_capture(cmd)
    output_text(res)

elseif opt == 'test_app' then

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

elseif opt == 'test_lua' then

    output_text('Build: Running lua tests...')
    local pr = require('pnut_runner')

    -- rep = pr.do_tests('test_nebulua')
    rep = pr.do_tests('test_defs', 'test_bar_time', 'test_nebulua')
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

elseif opt == 'gen_interop' then

    -- Convert spec into interop files.
    output_text('Build: Generating interop...')
    cmd = 'pushd "../../Libs/LuaBagOfTricks" & lua gen_interop.lua -ch '..current_dir..'/interop/interop_spec.lua '..current_dir..'/interop & popd'
    print(cmd)
    res = ut.execute_and_capture(cmd)
    output_text(res)

elseif opt == 'dev' then

    local function go(name, func)
        v = {}
        table.insert(v, name)
        for i = 0, 9 do table.insert(v, string.format("%.2f", func(i)))  end
        print(sx.strjoin(', ', v))
    end

    go('linear', function(i) return i / 9 end)
    go('exp', function(i) return math.exp(i)/8104 end)
    go('log', function(i) return math.log(i)/2.2 end)
    go('log 10', function(i) return math.log(i, 10)/0.95 end)
    go('pow 2', function(i) return i^2/81 end)
    go('pow 3', function(i) return i^3 end)
    -- go('pow 10', function(i) return i^10/81 end)
    go('pow 0.67', function(i) return i^0.67/4.36 end)
    -- go('pow -2', function(i) return i^-2 end)
    -- go('pow -10', function(i) return i^-10 end)
    -- go('10^x/20', function(i) return 10^i/20 end)

else

    if opt == nil then
        output_text('Build: Error: Missing option - Select one of:')
    else
        output_text('Build: Error: Invalid option '..opt..' - Select one of:')
    end

    output_text('build_app  test_app  test_lua  gen_md')
    -- goto done
    ret_code = 1

end

return ret_code