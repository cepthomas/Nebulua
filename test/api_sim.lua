

-- Create the namespace/module.
local M = {}


-- Log(int? level, string? msg)
-- SendNote(string? inst, int? note_num, double? volume, double? dur)
-- SendNoteOn(string? inst, int? note_num, double? volume)
-- SendNoteOff(string? inst, int? note_num)
-- SendController(string? inst, int? ctlr, int? value)
-- SendPatch(string? inst, int? patch)



function M.log(level, msg)

end

function M.send_controller(name, id, value)

end

function M.send_note(name, note, vel, dur)

end

function M.get_notes(which)
    return { 0, 1, 2, 3 }
end

-- Return the module.
return M
