// pch.h: This is a precompiled header file.
// Files listed below are compiled only once, improving build performance for future builds.
// This also affects IntelliSense performance, including code completion and many code browsing features.
// However, files listed here are ALL re-compiled if any one of them is updated between builds.
// Do not add files here that you will be updating frequently as this negates the performance advantage.

#ifndef PCH_H
#define PCH_H
#include <Windows.h>

// add headers that you want to pre-compile here
void LogItW(WCHAR* msg);
void LogItA(char* msg);
void LogIt(System::String^ msg);
System::Collections::ArrayList^ listAllDevices();
int removeDeviceByInstanceId(System::String^ deviceInstanceId);
int removeDeviceByInstanceId2(System::String^ deviceInstanceId);
bool doesUSBDeviceExist(System::String^ calFilename);
#endif //PCH_H
