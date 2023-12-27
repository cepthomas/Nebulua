
#ifndef CLI_H
#define CLI_H

#include <stdbool.h>



//---------------- Public API ----------------------//

/// Max line.
#define CLI_BUFF_LEN 128


//---------------- Main Functions -----------------//

// Initialize the component.
// @return Status.
int cli_Init(void);

// Open a cli (polled).
// @param channel Specific channel.
// @return Status.
int cli_Open(int channel);

// Clean up component resources.
// @return Status.
int cli_Destroy(void);

// Read a line from a cli. This does not block. Buffers chars until EOL.
// @param buff Data buffer. Will be a zero-terminated string.
// @param num Max length of buff.
// @return Status. True if buff is valid line.
bool cli_ReadLine(char* buff, int num);

// Write a line to a cli.
// @param buff Line to send to user.
// @return Status.
int cli_WriteLine(const char* format, ...);


#endif // CLI_H
