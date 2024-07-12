
-- Unload everything so that the script can be reloaded.
print('>>> reset()')

local M = {}

function M.unload_all()
    package.loaded.bar_time = nil
    package.loaded.midi_defs = nil
    package.loaded.music_defs = nil
    package.loaded.nebulua = nil
    package.loaded.neb_common = nil
    package.loaded.step_types = nil
    package.loaded.debugger = nil
    package.loaded.stringex = nil
    package.loaded.utils = nil
    package.loaded.validators = nil
end

return M
