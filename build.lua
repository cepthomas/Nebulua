--[[
Used to build and test all parts of this system.
--]]

-- print(res) TODO2 in all of these parse for 'Error' or something. Maybe colorize like debugger.py.

-- Fix up the lua path first.
package.path = './lua/?.lua;./test/lua/?.lua;'..package.path

local ut = require('utils')
local sx = require('stringex')

local opt = arg[1]
local ret_code = 0
local res = {}

if opt == nil then

    print('Error: No operation supplied')
    ret_code = 1

elseif opt == 'build_app' then

    bld = '"C:/Program Files/Microsoft Visual Studio/2022/Community/Msbuild/Current/Bin/MSBuild.exe"'
    -- -restore -r Runs the Restore target prior to building the actual targets.
    -- Verbosity levels: q[uiet], m[inimal], n[ormal] (default), d[etailed], and diag[nostic].
    -- -t:rebuild
    vrb = '-v:m'

    print('Building app...')
    cmd = sx.strjoin(' ', { bld, vrb, 'Nebulua.sln' } )
    res = ut.execute_and_capture(cmd)
    print(res)

    print('Building app tests...')
    cmd = sx.strjoin(' ', { bld, vrb, 'test/Test.sln' } )
    res = ut.execute_and_capture(cmd)
    print(res)

elseif opt == 'app_tests' then

    print('Running app tests...')
    cmd = 'pushd "test/Cli/bin/x64/Debug/net8.0-windows" & TestCli.exe & popd'
    res = ut.execute_and_capture(cmd)
    print(res)

    cmd = 'pushd "test/Core/bin/x64/Debug/net8.0-windows" & TestCore.exe & popd'
    res = ut.execute_and_capture(cmd)
    print(res)

    cmd = 'pushd "test/Interop/bin/x64/Debug/net8.0-windows" & TestInterop.exe & popd'
    res = ut.execute_and_capture(cmd)
    print(res)

    cmd = 'pushd "test/Misc/bin/x64/Debug/net8.0-windows" & TestMisc.exe & popd'
    res = ut.execute_and_capture(cmd)
    print(res)

elseif opt == 'lua_tests' then

    print('Running lua tests...')
    local pr = require('pnut_runner')

    -- pr.do_tests('test_defs')
    -- pr.do_tests('test_bar_time')
    -- pr.do_tests('test_nebulua')
    pr.do_tests('test_defs.lua', 'test_bar_time.lua', 'test_nebulua.lua')

elseif opt == 'gen_md' then

    print('Gen markdown from definition files...')

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

    print('Gen interop files...')
    -- Not generally useful or interesting to other than me.

    -- gen_interop.cmd:
    -- Needs lua path set up first!
    -- set spec_fn=%~dp0%interop_spec.lua
    -- set out_path=%~dp0%interop
    -- pushd "..\..\Libs\LuaBagOfTricks"
    -- lua gen_interop.lua -ch %spec_fn% %out_path%
    -- popd

else

    print('Error: Invalid option '..opt)
    -- goto done
    ret_code = 1

end

return ret_code
