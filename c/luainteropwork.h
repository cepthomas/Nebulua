#ifndef LUAINTEROPWORK_H
#define LUAINTEROPWORK_H


// Declaration of work functions.


//--------------------------------------------------------//
// Host export function: Script wants to log something.
// @param[in] level Log level
// @param[in] msg Log message
// Lua return: int status
int luainteropwork_Log(int level, char* msg);


//--------------------------------------------------------//
// Host export function: If volume is 0 note_off else note_on. If dur is 0 dur = note_on with dur = 0.1 (for drum/hit).
// @param[in] channel Output channel handle
// @param[in] notenum Note number
// @param[in] volume Volume between 0.0 and 1.0
// @param[in] dur Duration as bar.beat
// Lua return: int status
int luainteropwork_SendNote(int channel, int notenum, double volume, double dur);


//--------------------------------------------------------//
// Host export function: Send a controller immediately.
// @param[in] channel Output channel handle
// @param[in] ctlr Specific controller
// @param[in] value Payload.
// Lua return: int status
int luainteropwork_SendController(int channel, int ctlr, int value);


#endif // LUAINTEROPWORK_H
