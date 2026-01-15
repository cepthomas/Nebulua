-- local ut = require('lbot_utils')
local sx = require("stringex")
local mid = require("midi_defs")
local mus = require("music_defs")

-- music_defs_api.lua

-- Create the namespace/module.
local M = {}

local dbg = require("debugex")
dbg.init()
-- dbg()


--- Music definitions. Some borrowed from midi for convenience.
M.NOTES_PER_OCTAVE = 12
M.MIDDLE_C = 60 -- C4
M.DEFAULT_OCTAVE = 4 -- middle 


--- Runtime stuff - The chord and scale note definitions. Key is chord/scale name, value is list of constituent intervals.
M.definitions = {}


-----------------------------------------------------------------------------
--- Add a named chord or scale definition.
-- Like "MY_SCALE", "1 +3 4 -b7"
-- @param name string which
-- @param intervals string space separated interval names
-- @return intervals or nil,string if invalid.
function M.create_definition(name, intervals)
    local sints = sx.strsplit(intervals, " ", true)
    local iints = {}
    for _, sint in ipairs(sints) do
        local iint = M.interval_name_to_number(sint)
        if iint ~= nil then
            table.insert(iints, iint)
        else
            return nil, "Oops bad interval ".. sint .. " in " .. name
        end
    end
    if #iints > 0 then
        M.definitions[name] = iints
    else
        return nil, "Oops bad def: ".. name
    end

    return iints
end

-----------------------------------------------------------------------------
--- Parse note or notes from input value. Could look like:
--   F4 - named note
--   Bb2.dim7 - named key.chord
--   E#5.major - named key.scale
--   A3.MY_SCALE - user defined key.chord-or-scale
-- @param nstr string Standard string to parse.
-- @return List of note numbers or nil, error if invalid nstr.
function M.get_notes_from_string(nstr)
    local notes = nil
    local serr = ""

    -- Break it up.
    local parts = sx.strsplit(nstr, ".", true)
    local snote = parts[1]
    local c_or_s = parts[2] -- chord-name or scale-name or nil
    if snote ~= nil then
        -- Capture root (0-based) and octave (1-based).
        local soct = snote:sub(#snote, -1)
        local octave = tonumber(soct)
        if not octave then -- not specified
            octave = M.DEFAULT_OCTAVE
        else -- trim original note
            snote = snote:sub(1, #snote - 1)
        end

        local note_num = M.note_name_to_number(snote)

        if note_num ~= nil then
            notes = {}

            -- Transpose octave.
            -- note_num = note_num + (octave - 1) * M.NOTES_PER_OCTAVE
            local abs_note_num = note_num + M.MIDDLE_C - (M.DEFAULT_OCTAVE - octave) * M.NOTES_PER_OCTAVE

            if c_or_s ~= nil then
                -- It's a chord or scale.
                local intervals = M.definitions[c_or_s]
                if intervals ~= nil then
                    for _, cint in ipairs(intervals) do
                        table.insert(notes, cint + abs_note_num)
                    end
                else
                    serr = 'intervals error'
                    notes = nil
                end
            else
                -- Just the root.
                table.insert(notes, abs_note_num)
            end
        end
    end

    return notes, serr
end

-----------------------------------------------------------------------------
--- Convert note name into note number offset from middle C.
--  Could be F4 Bb2+ E#5-
-- @param snote string The root of the note with optional +- octave shift. TODO multiple octaves?
-- @return The number or nil if invalid.
function M.note_name_to_number(snote)
    local inote = nil

    if snote ~= nil then
        local ch1 = snote:sub(1, 1)
        local up = false
        local dn = false
        if ch1 == '+' then
            up = true
            snote = snote:sub(2)
        elseif ch1 == '-' then
            dn = true
            snote = snote:sub(2)
        end
        inote = mus.notes[snote]
        -- Adjust for octave shift.
        if inote and up then inote = inote + M.NOTES_PER_OCTAVE end
        if inote and dn then inote = inote - M.NOTES_PER_OCTAVE end
    end

    return inote
end

-----------------------------------------------------------------------------
--- Convert interval name into number.
-- @param sinterval string The interval name with optional +- octave shift. TODO multiple octaves?
-- @return The number or nil if invalid.
function M.interval_name_to_number(sinterval)
    local iinterval = nil

    if sinterval ~= nil then
        local ch1 = sinterval:sub(1, 1)
        local up = false
        local dn = false
        if ch1 == '+' then
            up = true
            sinterval = sinterval:sub(2)
        elseif ch1 == '-' then
            dn = true
            sinterval = sinterval:sub(2)
        end
        iinterval = mus.intervals[sinterval]
        -- Adjust for octave shift.
        if iinterval and up then iinterval = iinterval + M.NOTES_PER_OCTAVE end
        if iinterval and dn then iinterval = iinterval - M.NOTES_PER_OCTAVE end
    end

    return iinterval
end


-----------------------------------------------------------------------------
--- Split a midi note number into root note and octave.
-- @param note_num Absolute note number.
-- @return ints of root, octave or nil if invalid
function M.split_note_number(note_num)
    local root = nil
    local octave = nil
    if note_num ~= nil then
        root = note_num % M.NOTES_PER_OCTAVE
        octave = (note_num // M.NOTES_PER_OCTAVE) + 1
    end
    return root, octave
end


------ Init stuff ---------------------------------------------------------------------

for _, coll in ipairs({ mus.scales, mus.chords }) do
    for k, v in pairs(coll) do
        M.create_definition(k, v)
    end
    -- for _, sc in ipairs(coll) do
    --     local parts = sx.strsplit(sc, "|", true)
    --     M.create_definition(parts[1], parts[2])
    -- end
end

-- Return the module.
return M
