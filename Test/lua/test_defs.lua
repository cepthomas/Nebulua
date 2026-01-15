
-- Unit tests for music_defs.lua.

local tx = require("tableex")
local mid = require('midi_defs')
local mus = require('music_defs')
local def = require('defs_api')


some_arg = 'abc123'
local dbg = require("debugex")
dbg.init()

-- dbg.print('test_defs: '..some_arg)


-- Create the namespace/module.
local M = {}


-----------------------------------------------------------------------------
function M.suite_music_defs(pn)
    -- Test all functions in music_defs.lua

    ----- note_name_to_number()
    local res = def.note_name_to_number("G#")
    pn.UT_EQUAL(res, 8)
    res = def.note_name_to_number("+D")
    pn.UT_EQUAL(res, 14)
    res = def.note_name_to_number("+Cb")
    pn.UT_EQUAL(res, 23)
    res = def.note_name_to_number("6")
    pn.UT_EQUAL(res, 5) --17
    res = def.note_name_to_number("-Db")
    pn.UT_EQUAL(res, -11) --1
    res = def.note_name_to_number("XXX")
    pn.UT_NIL(res)
    res = def.note_name_to_number(nil)
    pn.UT_NIL(res)

    ----- split_note_number().
    local root, octave = def.split_note_number(5)
    pn.UT_EQUAL(root, 5)
    pn.UT_EQUAL(octave, 1)
    root, octave = def.split_note_number(34)
    pn.UT_EQUAL(root, 10)
    pn.UT_EQUAL(octave, 3)
    root, octave = def.split_note_number(68)
    pn.UT_EQUAL(root, 8)
    pn.UT_EQUAL(octave, 6)
    root, octave = def.split_note_number(nil)
    pn.UT_EQUAL(root, nil)
    pn.UT_EQUAL(octave, nil)

    ----- Create custom scales and chords.
    local tres = def.create_definition("MY_SCALE", "1 +3 4 -b7")
    pn.UT_NOT_NIL(tres)
    tres = def.create_definition("KRAZY_CHORD", "1 2 +#6 7 8 -9 b3 4 5 +2")
    pn.UT_NOT_NIL(tres)
    tres = def.create_definition("INVALID_CHORD", "1 +3 ABC -b7")
    pn.UT_NIL(tres)

    ----- Get scales using get_notes_from_string().
    -- "Lydian                  | 1 2 3 #4 5 6 7               | Lydian mode                              | whole tone        | major",
    tres = def.get_notes_from_string("D#2.Lydian") -- stock
    pn.UT_NOT_NIL(tres)

    -- if not pn.UT_NOT_NIL(res) then error("Fatal") end
    pn.UT_EQUAL(#tres, 7)
    pn.UT_EQUAL(tres[4], 45) -- should be  D# = 3 + 2*12 = 27
    pn.UT_EQUAL(tres[7], 50)

    tres = def.get_notes_from_string("Bb5.MY_SCALE") -- custom
    -- if not pn.UT_NOT_NIL(res) then error("Fatal") end
    pn.UT_EQUAL(#tres, 4)
    pn.UT_EQUAL(tres[2], 98)
    pn.UT_EQUAL(tres[4], 80)

    ----- Get chords using get_notes_from_string();
    tres = def.get_notes_from_string("C3.M7#11") -- stock
        -- "   | 1 3 5 7 9 #11     |",
    -- if not pn.UT_NOT_NIL(res) then error("Fatal") end
    pn.UT_EQUAL(#tres, 6)
    pn.UT_EQUAL(tres[2], 52)
    pn.UT_EQUAL(tres[6], 66)

    tres = def.get_notes_from_string("F#5.KRAZY_CHORD") -- custom
    -- if not pn.UT_NOT_NIL(res) then error("Fatal") end
    pn.UT_EQUAL(#tres, 10)
    pn.UT_EQUAL(tres[4], 89)
    pn.UT_EQUAL(tres[9], 85)

    tres, err = def.get_notes_from_string("C.INVALID_CHORD") -- invalid
    pn.UT_NIL(res, err)

    tres, err = def.get_notes_from_string("C.NONEXISTENT_CHORD") -- invalid
    pn.UT_NIL(tres, err)


    ----------------------- debugging --------------------

    tres = def.get_notes_from_string('G4.m7')
    pn.UT_NOT_NIL(tres)
    -- dbg.print(tx.dump_table(tres))

end

-----------------------------------------------------------------------------
-- Return the module.
return M
