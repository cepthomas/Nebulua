#ifndef CLI_H
#define CLI_H

#define MAX_LINE_LEN 200

int cli_open();

int cli_close();

int cli_printf(const char* format, ...);

char* cli_gets(char* buff, int len);
// This function returns str on success, and NULL on error or when end of file occurs,

#endif // CLI_H
