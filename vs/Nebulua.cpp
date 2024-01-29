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

    // tests for exec, interopwork??   

    whichSuites.emplace_back("NEBCOM");
    whichSuites.emplace_back("DEVMGR");

    // Init system before running tests.
    // FILE* fp = fopen("log_test_out.txt", "w");
    // logger_Init(fp);
    logger_Init(stdout);
    tm.RunSuites(whichSuites, 'r'); // 'r' for readable, 'x' for xml
    // close(fp);

    return 0;
}




/* original


// Nebulua.cpp : This file contains the 'main' function. Program execution begins and ends there.
//

#include <iostream>

int main()
{
    std::cout << "Hello World!\n";
}

// Run program: Ctrl + F5 or Debug > Start Without Debugging menu
// Debug program: F5 or Debug > Start Debugging menu

// Tips for Getting Started: 
//   1. Use the Solution Explorer window to add/manage files
//   2. Use the Team Explorer window to connect to source control
//   3. Use the Output window to see build output and other messages
//   4. Use the Error List window to view errors
//   5. Go to Project > Add New Item to create new code files, or Project > Add Existing Item to add existing code files to the project
//   6. In the future, to open this project again, go to File > Open > Project and select the .sln file

*/