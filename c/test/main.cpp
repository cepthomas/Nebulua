#include <cstdio>
#include "pnut.h"

extern "C"
{
#include "logger.h"
}

int main()
{
    TestManager& tm = TestManager::Instance();

    // Run the requested tests.
    std::vector<std::string> whichSuites;

    // TODO1 Add tests: devmgr  interop/work  nebcommon
    // main??

    // add test files dir?

    whichSuites.emplace_back("CLI");

    // Init system before running tests.
    // FILE* fp = fopen("log_test.txt", "w");
    // logger_Init(fp);

    tm.RunSuites(whichSuites, 'r'); // 'r' for readable, 'x' for xml

    return 0;
}
