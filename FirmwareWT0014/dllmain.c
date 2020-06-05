// dllmain.cpp : Defines the entry point for the DLL application.
#include "pch.h"

#include <stdio.h>
#include "conf_auto.h"
#define PARAMETER_START_ADDRESS     0x00038010
extern cfg_t cfg;
__declspec(dllexport) void LoadParameterFromFirmwareBinary(char* path);
void LoadParameterFromFirmwareBinary(char* path)
{
    FILE* stream;
    if (fopen_s(&stream, path, "rb") == 0)
    {
        fseek(stream, PARAMETER_START_ADDRESS, SEEK_SET);
        fread(&cfg, sizeof(cfg), 1, stream);
    }
}

GetNextFrameFunctionPointer GetNextFrame;
GetPositiveImageFunctionPointer GetPositiveImage;
GetNegativeImageFunctionPointer GetNegativeImage;
GetRegionCountFunctionPointer GetRegionCount;
GetRegionLeftFunctionPointer GetRegionLeft;
GetRegionTopFunctionPointer GetRegionTop;
GetRegionRightFunctionPointer GetRegionRight;
GetRegionBottomFunctionPointer GetRegionBottom;
UpdateReferenceImageFunctionPointer UpdateReferenceImage;
UpdatePositiveImageFunctionPointer UpdatePositiveImage;
UpdateNegativeImageFunctionPointer UpdateNegativeImage;
UpdatePositiveRegionFunctionPointer UpdatePositiveRegion;
UpdateNegativeRegionFunctionPointer UpdateNegativeRegion;
SaveTouchOutputImageFunctionPointer SaveTouchOutputImage;

__declspec(dllexport) void SetupCallbackFunctions(GetNextFrameFunctionPointer GetNextFramePtr,
                                                  GetPositiveImageFunctionPointer GetPositiveImagePtr,
                                                  GetNegativeImageFunctionPointer GetNegativeImagePtr,
                                                  GetRegionCountFunctionPointer GetRegionCountPtr,
                                                  GetRegionLeftFunctionPointer GetRegionLeftPtr,
                                                  GetRegionTopFunctionPointer GetRegionTopPtr,
                                                  GetRegionRightFunctionPointer GetRegionRightPtr,
                                                  GetRegionBottomFunctionPointer GetRegionBottomPtr,
                                                  UpdateReferenceImageFunctionPointer UpdateReferenceImage,
                                                  UpdatePositiveImageFunctionPointer UpdatePositiveImage,
                                                  UpdateNegativeImageFunctionPointer UpdateNegativeImage,
                                                  UpdatePositiveRegionFunctionPointer UpdatePositiveRegion,
                                                  UpdateNegativeRegionFunctionPointer UpdateNegativeRegion,
                                                  SaveTouchOutputImageFunctionPointer SaveTouchOuputImage);
void SetupCallbackFunctions(GetNextFrameFunctionPointer GetNextFramePtr,
                            GetPositiveImageFunctionPointer GetPositiveImagePtr,
                            GetNegativeImageFunctionPointer GetNegativeImagePtr,
                            GetRegionCountFunctionPointer GetRegionCountPtr,
                            GetRegionLeftFunctionPointer GetRegionLeftPtr,
                            GetRegionTopFunctionPointer GetRegionTopPtr,
                            GetRegionRightFunctionPointer GetRegionRightPtr,
                            GetRegionBottomFunctionPointer GetRegionBottomPtr,
                            UpdateReferenceImageFunctionPointer UpdateReferenceImagePtr,
                            UpdatePositiveImageFunctionPointer UpdatePositiveImagePtr,
                            UpdateNegativeImageFunctionPointer UpdateNegativeImagePtr,
                            UpdatePositiveRegionFunctionPointer UpdatePositiveRegionPtr,
                            UpdateNegativeRegionFunctionPointer UpdateNegativeRegionPtr,
                            SaveTouchOutputImageFunctionPointer SaveTouchOutputImagePtr)
{
    GetNextFrame = GetNextFramePtr;
    GetPositiveImage = GetPositiveImagePtr;
    GetNegativeImage = GetNegativeImagePtr;
    GetRegionCount = GetRegionCountPtr;
    GetRegionLeft = GetRegionLeftPtr;
    GetRegionTop = GetRegionTopPtr;
    GetRegionRight = GetRegionRightPtr;
    GetRegionBottom = GetRegionBottomPtr;
    UpdateReferenceImage = UpdateReferenceImagePtr;
    UpdatePositiveImage = UpdatePositiveImagePtr;
    UpdateNegativeImage = UpdateNegativeImagePtr;
    UpdatePositiveRegion = UpdatePositiveRegionPtr;
    UpdateNegativeRegion = UpdateNegativeRegionPtr;
    SaveTouchOutputImage = SaveTouchOutputImagePtr;
}

FILE* logStream;

extern void ctrl_init_sys(void);
extern void adjust_fixed_point(void);
extern void sequencer_step(void);

__declspec(dllexport) void StartProcessTouchSignal();
void StartProcessTouchSignal()
{
    fopen_s(&logStream, ".\\Log\\FirmwareWT0014.log", "wt");

    ctrl_init_sys();
    adjust_fixed_point();
    sequencer_step();

    fclose(logStream);
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

