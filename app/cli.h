#ifndef CLI_H
#define CLI_H

#define MAX_LINE_LEN 200

/// Open the cli.
/// @return Status.
int cli_open();

/// Close the cli.
/// @return Status.
int cli_close();

/// Write to the cli.
/// @param[in] format Standard args.
/// @return Status.
int cli_printf(const char* format, ...);

/// Read from the cli.
/// @param[in] buff Where to write.
/// @param[in] len Size of buff.
/// @return buff on success, and NULL on error or when end of file occurs.
char* cli_gets(char* buff, int len);

#endif // CLI_H
