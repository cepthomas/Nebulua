
-- Create the namespace/module.
local M = {}

NOTES_PER_OCTAVE = 12

-- Init stuff
-- The chord and scale note definitions. Key is chord/scale name, value is list of constituent notes.
M.chords_and_scales = {}
for sc in scale_defs do
    parts = ut.split("|", sc)
    M.chords_and_scales[parts[1]] = ut.split(" ", parts[2])
end
for sc in chord_defs do
    parts = ut.split("|", sc)
    M.chords_and_scales[parts[1]] = ut.split(" ", parts[2])
end


-- All the builtin chord defs.
local chord_defs =
[
--  Chord    | Notes             | Description
    "M       | 1 3 5             | Named after the major 3rd interval between root and 3.",
    "m       | 1 b3 5            | Named after the minor 3rd interval between root and b3.",
    "7       | 1 3 5 b7          | Also called dominant 7th.",
    "M7      | 1 3 5 7           | Named after the major 7th interval between root and 7th major scale note.",
    "m7      | 1 b3 5 b7         |",
    "6       | 1 3 5 6           | Major chord with 6th major scale note added.",
    "m6      | 1 b3 5 6          | Minor chord with 6th major scale note added.",
    "o       | 1 b3 b5           | Diminished.",
    "o7      | 1 b3 b5 bb7       | Diminished added 7.",
    "m7b5    | 1 b3 b5 b7        | Also called minor 7b5.",
    "+       | 1 3 #5            | Augmented.",
    "7#5     | 1 3 #5 b7         |",
    "9       | 1 3 5 b7 9        |",
    "7#9     | 1 3 5 b7 #9       | The 'Hendrix' chord.",
    "M9      | 1 3 5 7 9         |",
    "Madd9   | 1 3 5 9           | Chords extended beyond the octave are called added when the 7th is not present.",
    "m9      | 1 b3 5 b7 9       |",
    "madd9   | 1 b3 5 9          |",
    "11      | 1 3 5 b7 9 11     | The 3rd is often omitted to avoid a clash with the 11th.",
    "m11     | 1 b3 5 b7 9 11    |",
    "7#11    | 1 3 5 b7 #11      | Often used in preference to 11th chords to avoid the dissonant clash between 11 and 3 .",
    "M7#11   | 1 3 5 7 9 #11     |",
    "13      | 1 3 5 b7 9 11 13  | The 11th is often omitted to avoid a clash with the 3rd.",
    "M13     | 1 3 5 7 9 11 13   | The 11th is often omitted to avoid a clash with the 3rd.",
    "m13     | 1 b3 5 b7 9 11 13 |",
    "sus4    | 1 4 5             |",
    "sus2    | 1 2 5             | Sometimes considered as an inverted sus4 (GCD).",
    "5       | 1 5               | Power chord."
]

-- All the builtin scale defs.
local scale_defs =
[
--  Scale                    | Notes                        | Description                              | Lower tetrachord  | Upper tetrachord
    "Acoustic                | 1 2 3 #4 5 6 b7              | Acoustic scale                           | whole tone        | minor",
    "Aeolian                 | 1 2 b3 4 5 b6 b7             | Aeolian mode or natural minor scale      | minor             | Phrygian",
    "NaturalMinor            | 1 2 b3 4 5 b6 b7             | Aeolian mode or natural minor scale      | minor             | Phrygian",
    "Algerian                | 1 2 b3 #4 5 b6 7             | Algerian scale                           |                   |",
    "Altered                 | 1 b2 b3 b4 b5 b6 b7          | Altered scale                            | diminished        | whole tone",
    "Augmented               | 1 b3 3 5 #5 7                | Augmented scale                          |                   |",
    "Bebop                   | 1 2 3 4 5 6 b7 7             | Bebop dominant scale                     |                   |",
    "Blues                   | 1 b3 4 b5 5 b7               | Blues scale                              |                   |",
    "Chromatic               | 1 #1 2 #2 3 4 #4 5 #5 6 #6 7 | Chromatic scale                          |                   |",
    "Dorian                  | 1 2 b3 4 5 6 b7              | Dorian mode                              | minor             | minor",
    "DoubleHarmonic          | 1 b2 3 4 5 b6 7              | Double harmonic scale                    | harmonic          | harmonic",
    "Enigmatic               | 1 b2 3 #4 #5 #6 7            | Enigmatic scale                          |                   |",
    "Flamenco                | 1 b2 3 4 5 b6 7              | Flamenco mode                            | Phrygian          | Phrygian",
    "Gypsy                   | 1 2 b3 #4 5 b6 b7            | Gypsy scale                              | Gypsy             | Phrygian",
    "HalfDiminished          | 1 2 b3 4 b5 b6 b7            | Half diminished scale                    | minor             | whole tone",
    "HarmonicMajor           | 1 2 3 4 5 b6 7               | Harmonic major scale                     | major             | harmonic",
    "HarmonicMinor           | 1 2 b3 4 5 b6 7              | Harmonic minor scale                     | minor             | harmonic",
    "Hirajoshi               | 1 3 #4 5 7                   | Hirajoshi scale                          |                   |",
    "HungarianGypsy          | 1 2 b3 #4 5 b6 7             | Hungarian Gypsy scale                    | Gypsy             | harmonic",
    "HungarianMinor          | 1 2 b3 #4 5 b6 7             | Hungarian minor scale                    | Gypsy             | harmonic",
    "In                      | 1 b2 4 5 b6                  | In scale                                 |                   |",
    "Insen                   | 1 b2 4 5 b7                  | Insen scale                              |                   |",
    "Ionian                  | 1 2 3 4 5 6 7                | Ionian mode or major scale               | major             | major",
    "Istrian                 | 1 b2 b3 b4 b5 5              | Istrian scale                            |                   |",
    "Iwato                   | 1 b2 4 b5 b7                 | Iwato scale                              |                   |",
    "Locrian                 | 1 b2 b3 4 b5 b6 b7           | Locrian mode                             | Phrygian          | whole tone",
    "LydianAugmented         | 1 2 3 #4 #5 6 7              | Lydian augmented scale                   | whole tone        | diminished",
    "Lydian                  | 1 2 3 #4 5 6 7               | Lydian mode                              | whole tone        | major",
    "Major                   | 1 2 3 4 5 6 7                | Ionian mode or major scale               | major             | major",
    "MajorBebop              | 1 2 3 4 5 #5 6 7             | Major bebop scale                        |                   |",
    "MajorLocrian            | 1 2 3 4 b5 b6 b7             | Major Locrian scale                      | major             | whole tone",
    "MajorPentatonic         | 1 2 3 5 6                    | Major pentatonic scale                   |                   |",
    "MelodicMinorAscending   | 1 2 b3 4 5 6 7               | Melodic minor scale (ascending)          | minor             | varies",
    "MelodicMinorDescending  | 1 2 b3 4 5 b6 b7 8           | Melodic minor scale (descending)         | minor             | major",
    "MinorPentatonic         | 1 b3 4 5 b7                  | Minor pentatonic scale                   |                   |",
    "Mixolydian              | 1 2 3 4 5 6 b7               | Mixolydian mode or Adonai malakh mode    | major             | minor",
    "NeapolitanMajor         | 1 b2 b3 4 5 6 7              | Neapolitan major scale                   | Phrygian          | major",
    "NeapolitanMinor         | 1 b2 b3 4 5 b6 7             | Neapolitan minor scale                   | Phrygian          | harmonic",
    "Octatonic               | 1 2 b3 4 b5 b6 6 7           | Octatonic scale (or 1 b2 b3 3 #4 5 6 b7) |                   |",
    "Persian                 | 1 b2 3 4 b5 b6 7             | Persian scale                            | harmonic          | unusual",
    "PhrygianDominant        | 1 b2 3 4 5 b6 b7             | Phrygian dominant scale                  | harmonic          | Phrygian",
    "Phrygian                | 1 b2 b3 4 5 b6 b7            | Phrygian mode                            | Phrygian          | Phrygian",
    "Prometheus              | 1 2 3 #4 6 b7                | Prometheus scale                         |                   |",
    "Tritone                 | 1 b2 3 b5 5 b7               | Tritone scale                            |                   |",
    "UkrainianDorian         | 1 2 b3 #4 5 6 b7             | Ukrainian Dorian scale                   | Gypsy             | minor",
    "WholeTone               | 1 2 3 #4 #5 #6               | Whole tone scale                         |                   |",
    "Yo                      | 1 b3 4 5 b7                  | Yo scale                                 |                   |"
]


-----------------------------------------------------------------------------
-- Add a named chord or scale definition.
-- Description
-- @param name string which
-- @param notes string space separated note names
function M.create_notes(name, notes) --"MY_SCALE", "1 3 4 b7"
    _chordsScales[name] = ut.split(" ", notes)

-----------------------------------------------------------------------------
-- Get a defined chord or scale definition.
-- Description
-- @param name string which
-- @return The list of notes or nil if invalid
function M.get_named_notes(name)
    ret = _chordsScales[name]
    --throw new ArgumentException("Invalid chord or scale: {name}");
    return ret;

-----------------------------------------------------------------------------
-- Parse note or notes from input value.
-- Could be:
--   F4 - named note
--   F4.dim7 - named key/chord
--   F4.major - named key/scale
--   F4.MY_SCALE - user defined key/chord or scale
-- @param str string Standard string to parse.
-- @return partially filled-in step_info[]
function M.get_notes_from_string(str) --TODO1
    step_infos = []
-- List<int> notes = MusicDefinitions.GetNotesFromString(noteString);
-- /// <param name="noteString"></param>
-- /// <returns>List of note numbers - empty if invalid.</returns>
-- function GetNotesFromString(noteString)
--     List<int> notes = new();
--     // Parse the input value.
--     // Note: Need exception handling here to protect from user script errors.
--     try
--     {
--         // Break it up.
--         var parts = noteString.SplitByToken(".");
--         string snote = parts[0];
--         // Start with octave.
--         int octave = 4; // default is middle C
--         string soct = parts[0].Last().ToString();
--         if (soct.IsInteger())
--         {
--             octave = int.Parse(soct);
--             snote = snote.Remove(snote.Length - 1);
--         }
--         // Figure out the root note.
--         int? noteNum = NoteNameToNumber(snote);
--         if (noteNum is not null)
--         {
--             // Transpose octave.
--             noteNum += (octave + 1) * NOTES_PER_OCTAVE;
--             if (parts.Count > 1)
--             {
--                 // It's a chord. M, M7, m, m7, etc. Determine the constituents.
--                 var chordNotes = _chordsScales[parts[1] ];
--                 //var chordNotes = chordParts[0].SplitByToken(" ");
--                 for (int p = 0; p < chordNotes.Count; p++)
--                 {
--                     string interval = chordNotes[p];
--                     bool down = false;
--                     if (interval.StartsWith("-"))
--                     {
--                         down = true;
--                         interval = interval.Replace("-", "");
--                     }
--                     int? iint = GetInterval(interval);
--                     if (iint is not null)
--                     {
--                         iint = down ? iint - NOTES_PER_OCTAVE : iint;
--                         notes.Add(noteNum.Value + iint.Value);
--                     }
--                 }
--             }
--             else
--             {
--                 // Just the root.
--                 notes.Add(noteNum.Value);
--             }
--         }
--         else
--         {
--             notes.Clear();
--         }
--     }
--     catch (Exception)
--     {
--         notes.Clear();
--         //throw new InvalidOperationException("Invalid note or chord: " + noteString);
--     }
--     return notes;
-- }

    return step_infos
end


--[[  TODO2 some of these?

-- All possible note names and aliases.
local _noteNames =
[
    "C",   "Db",  "D",  "Eb",  "E",   "F",   "Gb",  "G",  "Ab",  "A",   "Bb",  "B", 
    "B#",  "C#",  "",   "D#",  "Fb",  "E#",  "F#",   "",  "G#",  "",    "A#",  "Cb", 
    "1",   "2",   "3",  "4",   "5",   "6",   "7",   "8",  "9",   "10",  "11",  "12"
]

-- White keys.
local _naturals =
[
    0, 2, 4, 5, 7, 9, 11
]

-- Names of note intervals two ways.
local _intervals =
[
    "1",  "b2",  "2",  "b3",  "3",  "4",  "b5",  "5",  "#5",  "6",  "b7",  "7", 
    "",   "",    "9",  "#9",  "",  "11",  "#11",  "",  "",    "13",  "",   ""
]


Convert note number into name.
 <param name="inote"></param>
/// <returns></returns>
public static string NoteNumberToName(int inote)
{
    var split = SplitNoteNumber(inote);
    string s = $"{_noteNames[split.root]}{split.octave}";
    return s;
}

Convert note name into number.
 <param name="snote">The root of the note without octave.</param>
/// <returns>The number or -1 if invalid.</returns>
public static int NoteNameToNumber(string snote)
{
    int inote = _noteNames.IndexOf(snote) % NOTES_PER_OCTAVE;
    return inote;
}

Is it a white key?
 <param name="notenum">Which note</param>
/// <returns>True/false</returns>
public static bool IsNatural(int notenum)
{
    return _naturals.Contains(SplitNoteNumber(notenum).root % NOTES_PER_OCTAVE);
}

Split a midi note number into root note and octave.
 <param name="notenum">Absolute note number</param>
/// <returns>tuple of root and octave</returns>
public static (int root, int octave) SplitNoteNumber(int notenum)
{
    int root = notenum % NOTES_PER_OCTAVE;
    int octave = (notenum / NOTES_PER_OCTAVE) - 1;
    return (root, octave);
}

Get interval offset from name.
 <param name="sinterval"></param>
/// <returns>Offset or -1 if invalid.</returns>
public static int GetInterval(string sinterval)
{
    int flats = sinterval.Count(c => c == 'b');
    int sharps = sinterval.Count(c => c == '#');
    sinterval = sinterval.Replace(" ", "").Replace("b", "").Replace("#", "");

    int iinterval = _intervals.IndexOf(sinterval);
    return iinterval == -1 ? -1 : iinterval + sharps - flats;
}

Get interval name from note number offset.
 <param name="iint">The name or empty if invalid.</param>
/// <returns></returns>
public static string GetInterval(int iint)
{
    return iint >= _intervals.Count ? "" : _intervals[iint % _intervals.Count];
}

Try to make a note and/or chord string from the param. If it can't find a chord return the individual notes.
 <param name="notes"></param>
/// <returns></returns>
public static List<string> FormatNotes(List<int> notes)
{
    List<string> snotes = new();

    // Dissect root note.
    foreach (int n in notes)
    {
        int octave = SplitNoteNumber(n).octave;
        int root = SplitNoteNumber(n).root;
        snotes.Add($"\"{NoteNumberToName(root)}{octave}\"");
    }

    return snotes;
}

Make markdown content from the definitions.
 <returns></returns>
public static List<string> FormatDoc()
{
    List<string> docs = new();

    docs.Add("# Chords");
    docs.Add("These are the built-in chords.");
    docs.Add("Chord   | Notes             | Description");
    docs.Add("------- | ----------------- | -----------");
    docs.AddRange(chord_defs);
    docs.Add("# Scales");
    docs.Add("These are the built-in scales.");
    docs.Add("Scale   | Notes             | Description       | Lower tetrachord  | Upper tetrachord");
    docs.Add("------- | ----------------- | ----------------- | ----------------  | ----------------");
    docs.AddRange(scale_defs);

    return docs;
}

]]

-- Return the module.
return M
