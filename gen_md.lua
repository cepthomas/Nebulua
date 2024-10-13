
-- Makes markdown from definition files.

package.path = 'lua/?.lua;'..package.path

mus = require("music_defs")
mid = require("midi_defs")
sx  = require("stringex")

text = mus.gen_md()
content = sx.strjoin('\n', text)
f = io.open('docs/music_defs.md', "w")
f:write(content)
f:close()

text = mid.gen_md()
content = sx.strjoin('\n', text)
f = io.open('docs/midi_defs.md', "w")
f:write(content)
f:close()
