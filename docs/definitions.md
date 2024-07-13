
# Definitions

## Glossary

- Since this is code, everything is 0-based, not 1-based like standard music notation.
- Nebulua doesn't care about measures, that's up to you.

Name       | Type   | Description                                     |
-------    | ------ | ---------------------------                     |
chan_num   | int    | midi channel number                             |
dev_index  | int    | index into windows midi device table            |
dev_name   | char*  | from windows midi device table                  |
chan_hnd   | int    | internal opaque handle for channel id           |
controller | int    | from midi_defs.lua                              |
value      | int    | controller payload                              |
note_num   | int    | 0 -> 127                                        |
volume     | double | 0.0 -> 1.0, 0 means note off                    |
velocity   | int    | 0 -> 127, 0 means note off                      |
bar        | int    | 0 -> N, absolute                                |
beat       | int    | 0 -> 3, in bar, quarter note                    |
sub        | int    | 0 -> 7, in beat, "musical"                      |
tick       | int    | absolute time, see ##Timing, same length as sub |


## Time

- Midi DeltaTicksPerQuarterNote aka subs per beat is fixed at 8. This provides 32nd note resolution which
  should be more than adequate.
- The fast timer resolution is fixed at 1 msec giving a usable range of bpm of 40 (188 msec period)
  to 240 (31 msec period).
- Each sequence is typically 8 beats. Each section is typically 4 sequences -> 32 beats. A 4 minute song at
  80bpm is 320 beats -> 10 sections -> 40 sequences. If each sequence has an average of 8 notes for a total
  of 320 notes per instrument. A "typical" song with 6 instruments would then have about 4000 on/off events.
- To make the script translation between bar-beat-sub and ticks, see the [BarTime](#bartime) class below.

## Standard Note Syntax

- Scales and chords are specified by strings like `"1 4 6 b13"`.
- There are many builtin scales and chords defined [here](music_defs.lua).
- Users can add their own by using `function create_definition("FOO", "1 4 6 b13")`.

Notes, chords, and scales can be specified in several ways:

Form              | Description                                              |
-------           | ---------------------------                              |
"F4"              | Named note with octave                                   |
"F4.m7"           | Named chord in the key of middle F                       |
"F4.Aeolian"      | Named scale in the key of middle F                       |
"F4.FOO"          | Custom chord or scale created with `create_definition()` |
inst.SideStick    | Drum name from the definitions                           |
57                | Simple midi note number                                  |

