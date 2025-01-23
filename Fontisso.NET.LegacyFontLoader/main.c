#include <windows.h>

__declspec(dllexport) BOOLEAN WINAPI DllMain(HMODULE hModule, DWORD  ul_reason_for_call, LPVOID lpReserved) {
    switch (ul_reason_for_call) {
    case DLL_PROCESS_ATTACH:
        AddFontResourceExW(L"Fonts\\RPG2000.fon", 0x10, 0);
        AddFontResourceExW(L"Fonts\\RPG2000G.fon", 0x10, 0);
        break;

    case DLL_PROCESS_DETACH:
        RemoveFontResourceExW(L"Fonts\\RPG2000.fon", 0x10, 0);
        RemoveFontResourceExW(L"Fonts\\RPG2000G.fon", 0x10, 0);
        break;
    }
    return TRUE;
}

__declspec(dllexport) void Dummy() {}