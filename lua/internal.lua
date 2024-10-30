
-- Internal housekeeping etc.

-- Unload everything so that the script can be reloaded.
local function unload_all()
    package.loaded.bar_time = nil
    package.loaded.midi_defs = nil
    package.loaded.music_defs = nil
    package.loaded.nebulua = nil
    package.loaded.step_types = nil
    package.loaded.debugger = nil
    package.loaded.stringex = nil
    package.loaded.lbot_utils = nil
end

-- Api function
function neb_command(cmd, arg)
    if cmd == 'unload_all' then unload_all() end
    return 123
end
