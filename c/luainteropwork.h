#ifndef LUAINTEROPWORK_H
#define LUAINTEROPWORK_H


// Declaration of work functions.


//--------------------------------------------------------//
// Script wants to log something.
// @param[in] level Log level
// @param[in] msg Log message
// @return int status
int luainteropwork_Log(int level, char* msg);

//--------------------------------------------------------//
// Script wants to change speed.
// @param[in] bpm New val
// @return int status
int luainteropwork_SetTempo(int bpm);

//--------------------------------------------------------//
// Create a channel - input or output depending on device.
// @param[in] device Name
// @param[in] channel Number
// @param[in] patch Optional for output
// @return int status
int luainteropwork_CreateChannel(const char* device, int channel, int patch);

//--------------------------------------------------------//
// If volume is 0 note_off else note_on. If dur is 0 send note_on with dur = 0.1 (for drum/hit).
// @param[in] channel Output channel handle
// @param[in] notenum Note number
// @param[in] volume Volume between 0.0 and 1.0
// @param[in] dur Duration as bar.beat
// @return int status
int luainteropwork_SendNote(int channel, int notenum, double volume, double dur);

//--------------------------------------------------------//
// Send a controller immediately.
// @param[in] channel Output channel handle
// @param[in] ctlr Specific controller
// @param[in] value Payload.
// @return int status
int luainteropwork_SendController(int channel, int ctlr, int value);


#endif // LUAINTEROPWORK_H
