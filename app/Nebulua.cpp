#include <stdlib.h>
#include <stdio.h>

extern "C"
{
    // Stub for entry. Mainly to make testing easier.
    int exec_Main(const char* script_fn);
}


int main(int argc, char* argv[])
{
    // Check args.
    if(argc != 2)
    {
        printf("Bad cmd line. Use nebulua <file.lua>.");
        exit(100);
    }

    FILE* fp = fopen(argv[1], "r");
    if (fp == NULL)
    {
        printf("Bad lua file name.");
        exit(101);
    }
    fclose (fp);

    int ret = exec_Main(argv[1]);

    exit(ret);
}
