
#ifndef CLI_H
#define CLI_H

#include <stdbool.h>

//---------------- Public API ----------------------//

// Max line.
#define CLI_BUFF_LEN 128


// ? C:\Dev\AL\harvester\xib-firmware\src\cli\cli_command_list.c
// ? C:\Dev\AL\caldwell\gen3procfirmware\src-application\cli\cli_command_list.c

// int cli_ProcessCommand(const char* sin);


/// Open a cli using stdio.
/// @param type Stdio or telnet or ...
/// @param cmds Cli commands.
/// @return status 0=ok
int cli_Open(char type);//, cli_command_t[] cmds);

/// Open a cli using telnet.
/// @param port Where to listen.
/// @return status 0=ok
// int cli_OpenTelnet(char* port);

/// Clean up component resources.
/// @return Status.
int cli_Destroy(void);

/// Read a line from a cli. This does not block. Buffers chars until EOL.
/// @param buff Data buffer. Will be a zero-terminated string.
/// @param num Max length of buff.
/// @return ready. True if buff has valid line.
bool cli_ReadLine(char* buff, int num);

/// Write a line to a cli.
/// @param buff Line to send to user.
/// @return Status.
int cli_WriteLine(const char* format, ...);

/// Write a car to a cli.
/// @param c Char to send to user.
/// @return Status.
int cli_WriteChar(char c);

#endif // CLI_H
