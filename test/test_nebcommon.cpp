#include <cstdio>
#include <cstring>

#include "pnut.h"

extern "C"
{
#include "nebcommon.h"
#include "logger.h"
}

/* TODO1-TEST test these:

/// Calculate period for tempo.
/// @param[in] tempo
/// @return msec per subbeat
double nebcommon_InternalPeriod(int tempo);

/// Calculate integer period >= 1 for tempo.
/// @param[in] tempo
/// @return rounded msec per subbeat
int nebcommon_RoundedInternalPeriod(int tempo);

/// Convert subbeat to time.
/// @param[in] tempo
/// @param[in] subbeat
/// @return msec
double nebcommon_InternalToMsec(int tempo, int subbeat);


//----------------------- Utilities -----------------------------//

/// Convert a status to string.
/// @param[in] mstat Midi status to examine
/// @return String or NULL if not valid
const char* nebcommon_FormatMidiStatus(int mstat);

/// Convert a string bar time to absolute position.
/// @param[in] sbt time string can be "1.2.3" or "1.2" or "1".
/// @return String or -1 if not valid.
int nebcommon_ParseBarTime(const char* sbt);

/// Convert a position to string bar time.
/// @param[in] position
/// @return string
const char* nebcommon_FormatBarTime(int position);

/// Safe convert a string to double with bounds checking.
/// @param[in] str to parse
/// @param[out] val answer
/// @param[in] min limit inclusive
/// @param[in] max limit inclusive
/// @return success
bool nebcommon_ParseDouble(const char* str, double* val, double min, double max);

/// Safe convert a string to int with bounds checking.
/// @param[in] str to parse
/// @param[out] val answer
/// @param[in] min limit inclusive
/// @param[in] max limit inclusive
/// @return success
bool nebcommon_ParseInt(const char* str, int* val, int min, int max);
*/

/////////////////////////////////////////////////////////////////////////////
UT_SUITE(NEBCOM_SILLY, "Test stuff.")
{
    int x = 1;
    int y = 2;

    UT_EQUAL(x, y);

    LOG_INFO(CAT_INIT, "Hello!");

    return 0;
}    
