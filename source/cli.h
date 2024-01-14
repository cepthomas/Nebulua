
#ifndef CLI_H
#define CLI_H

#include <stdbool.h>

//---------------- Public API ----------------------//

/// Open a cli using stdio.
/// @param type Stdio or telnet or ...
/// @param cmds Cli commands.
/// @return status 0=ok
int cli_Open(char type);

/// Clean up component resources.
/// @return Status.
int cli_Destroy(void);

// /// Read a line from a cli. This does not block. Buffers chars until EOL.
// /// @param buff Data buffer. Will be a zero-terminated string.
// /// @param num Max length of buff.
// /// @return ready. True if buff has valid line.
// bool cli_ReadLine(char* buff, int num);

/// Read an EOL-terminated line from a cli. Does not block.
/// @return line ptr if available otherwise NULL. ptr is transient and client must copy line now.
const char* cli_ReadLine(void);

/// Write a line to a cli.
/// @param buff Line to send to user.
/// @return Status.
int cli_WriteLine(const char* format, ...);

/// Write a car to a cli.
/// @param c Char to send to user.
/// @return Status.
int cli_WriteChar(char c);

#endif // CLI_H
