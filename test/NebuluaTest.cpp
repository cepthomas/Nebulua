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
    //whichSuites.emplace_back("NEBCOM");
    //whichSuites.emplace_back("DEVMGR");
    //whichSuites.emplace_back("CLI");
    whichSuites.emplace_back("EXEC");

    // Init system before running tests.
    FILE* fp_log = fopen("_log.txt", "w");
    logger_Init(fp_log);

    std::ofstream s_ut("_test.txt", std::ofstream::out);
    tm.RunSuites(whichSuites, 'r', true, &s_ut); //false

    fclose(fp_log);
    s_ut.close();

    return 0;
}
