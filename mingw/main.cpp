#include <cstdio>
#include "pnut.h"

extern "C"
{
#include "logger.h"
}

// Main for unit testing.

int main()
{
    TestManager& tm = TestManager::Instance();

    // Run the requested tests.
    std::vector<std::string> whichSuites;

    // tests for exec, interopwork??   

    whichSuites.emplace_back("NEBCOM");
    whichSuites.emplace_back("DEVMGR");

    // Init system before running tests.
    // FILE* fp = fopen("out\\log.txt", "w");
    // logger_Init(fp);
    logger_Init(stdout);
    tm.RunSuites(whichSuites, 'r'); // 'r' for readable, 'x' for xml
    // close(fp);

    return 0;
}
