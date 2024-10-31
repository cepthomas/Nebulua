
-- Internal housekeeping etc. TODO1 elsewhere? not global?

-- Unload everything so that the script can be reloaded.
function neb_command(cmd, arg)
    if cmd == 'unload_all' then
        package.loaded.bar_time = nil
        package.loaded.debugger = nil
        package.loaded.lbot_utils = nil
        package.loaded.midi_defs = nil
        package.loaded.music_defs = nil
        package.loaded.nebulua = nil
        package.loaded.step_types = nil
        package.loaded.stringex = nil
    end
    return 0
end
