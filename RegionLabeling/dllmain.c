// dllmain.cpp : Defines the entry point for the DLL application.
#include "pch.h"
#include "region.h"

extern int main_entry(char* path, int threshold);

__declspec(dllexport) int RegionLabeling(char* path, int threshold);
int RegionLabeling(char* path, int threshold)
{
    return main_entry(path, threshold);
}

extern rect_t regions[N_REGIONS_MAX];

__declspec(dllexport) int GetTopLeftX(int regionNo);
int GetTopLeftX(int regionNo)
{
    return regions[regionNo].top_left.x;
}

__declspec(dllexport) int GetTopLeftY(int regionNo);
int GetTopLeftY(int regionNo)
{
    return regions[regionNo].top_left.y;
}

__declspec(dllexport) int GetBottomRightX(int regionNo);
int GetBottomRightX(int regionNo)
{
    return (regions[regionNo].bottom_right.x + 1);
}

__declspec(dllexport) int GetBottomRightY(int regionNo);
int GetBottomRightY(int regionNo)
{
    return (regions[regionNo].bottom_right.y + 1);
}

BOOL APIENTRY DllMain( HMODULE hModule,
                       DWORD  ul_reason_for_call,
                       LPVOID lpReserved
                     )
{
    switch (ul_reason_for_call)
    {
    case DLL_PROCESS_ATTACH:
    case DLL_THREAD_ATTACH:
    case DLL_THREAD_DETACH:
    case DLL_PROCESS_DETACH:
        break;
    }
    return TRUE;
}

