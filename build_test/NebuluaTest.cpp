#include <cstdio>
#include <fstream>
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

    // TODO2 tests for exec, interopwork??   

//    whichSuites.emplace_back("NEBCOM");
    whichSuites.emplace_back("DEVMGR");

    // Init system before running tests.
    FILE* fp_log = fopen("log_out.txt", "w");
    logger_Init(fp_log); // stdout

    std::ofstream s_ut("test_out.txt", std::ofstream::out);
    tm.RunSuites(whichSuites, 'r', &s_ut);

    fclose(fp_log);
    s_ut.close();

    return 0;
}
