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

    // TODO1 Add tests:
    // cli
    // devmgr
    // interop/work?
    // nebcommon
    // main??
    // test for timeanalyzer (in cbot test)
    // any lbot stuff?

    // files fir.

    whichSuites.emplace_back("CLI");

    // Init system before running tests.
    // FILE* fp = fopen("log_test.txt", "w");
    // logger_Init(fp);

    tm.RunSuites(whichSuites, 'r'); // 'r' for readable, 'x' for xml

    return 0;
}
