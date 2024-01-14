
#ifndef CLI_H
#define CLI_H

#include <stdbool.h>

//---------------- Public API ----------------------//

/// Convenience container for pre-digested args.
#define MAX_CLI_ARGS 10
#define MAX_CLI_ARG_LEN  32 // incl terminator
typedef struct
{
    // how many arg_values. 0 indicates error string in arg_values[0].
    int arg_count;
    char arg_values[MAX_CLI_ARGS][MAX_CLI_ARG_LEN];
} cli_args_t;


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
// const char* cli_ReadLine(void);

/// Read a line from a cli. This does not block. Buffers chars until EOL.
/// @param args User supplied collection.
/// @return True if line complete and new args filled in.
const bool cli_ReadLine(cli_args_t* args);

/// Write a line to a cli.
/// @param buff Line to send to user.
/// @return status 0=ok
int cli_WriteLine(const char* format, ...);

/// Write a car to a cli.
/// @param c Char to send to user.
/// @return status 0=ok
int cli_WriteChar(char c);

#endif // CLI_H
