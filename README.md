# Nebulua
An experimental version of Nebulator using Lua as the script flavor.



field types:
S = string
N = number
I = integer
F = function
M = map index(0-9)
T = TableEx
V = void
B = boolean
X = bar time(Number?)
E = expression?



---- sequences


-- Graphical format:
-- "|7-------|" is one beat with 8 subbeats
-- note velocity is 1-9 (map) or - which means sustained
-- note/chord, velocity/volume
-- List format:
-- times are beat.subbeat where beat is 0-N subbeat is 0-7
-- note/chord, velocity/volume is 0.0 to 1.0, duration is 0.1 to N.7

