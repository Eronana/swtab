// dllmain.cpp : 定义 DLL 应用程序的入口点。
#include "windows.h"
#include <cstdlib>
#define DETOURS_X86
#include "detours\detours.cpp"
#include "detours\disasm.cpp"
#include "detours\modules.cpp"
#include "detours\creatwth.cpp"
#include "detours\image.cpp"


decltype(ShowWindow) *OriginShowWindow = ShowWindow;
decltype(SetWindowPos)* OriginSetWindowPos = SetWindowPos;
// decltype(MoveWindow) *OriginMoveWindow = MoveWindow;

HMODULE thisModule;
HWND mainHwnd = NULL;
HWND parentHwnd;

struct GameInfo
{
    DWORD pid;
    HANDLE hProcess;
    HWND parentHwnd;
    HWND* mainHwnd;
    LPVOID idPtr;
} gameInfo;

BOOL WINAPI HookShowWindow(HWND hWnd, int nCmdShow)
{
    if (!mainHwnd)
    {
        mainHwnd = hWnd;
        SetParent(hWnd, parentHwnd);
        SetWindowLong(hWnd, GWL_STYLE, WS_VISIBLE);
        SetWindowPos(hWnd, NULL, 0, 0, 0, 0, SWP_NOSIZE | SWP_NOZORDER);
    }
    return OriginShowWindow(hWnd, nCmdShow);
}
/*
BOOL WINAPI HookMoveWindow(HWND hWnd, int X, int Y, int nWidth, int nHeight, BOOL bRepaint)
{
    if (hWnd == mainHwnd)
    {
        X = Y = 0;
    }
    return OriginMoveWindow(hWnd, X, Y, nWidth, nHeight, bRepaint);
}
*/

BOOL WINAPI HookSetWindowPos(HWND hWnd, HWND hWndInsertAfter, int X, int Y, int cx, int cy, UINT uFlags)
{
    if (hWnd == mainHwnd)
    {
        X = Y = 0;
    }
    return OriginSetWindowPos(hWnd, hWndInsertAfter, X, Y, cx, cy, uFlags);
}

void Hook()
{
    DetourRestoreAfterWith();
    DetourTransactionBegin();
    DetourUpdateThread(GetCurrentThread());
    DetourAttach(&(PVOID&)OriginShowWindow, HookShowWindow);
    //DetourAttach(&(PVOID&)OriginMoveWindow, HookMoveWindow);
    DetourAttach(&(PVOID&)OriginSetWindowPos, HookSetWindowPos);
    DetourTransactionCommit();
}

BOOL APIENTRY DllMain( HMODULE hModule,
                       DWORD  ul_reason_for_call,
                       LPVOID lpReserved
                     )
{
    thisModule = hModule;
    return TRUE;
}


LPVOID inject(HANDLE hProcess, HWND hWnd)
{
    BYTE shellcode[] = {
        0x68, 0x00, 0x00, 0x00, 0x00, // push "swhook.dll"
        0xE8, 0x00, 0x00, 0x00, 0x00, // call LoadLibraryA
        0x68, 0x35, 0x82, 0x00, 0x00, // push "setup"
        0x50,                         // push eax
        0xE8, 0x98, 0xAD, 0x00, 0x00, // call GetProcAddress
        0x68, 0x00, 0x00, 0x00, 0x00, // push hWnd
        0xFF, 0xD0,                   // call eax
        0xC3,                         // ret
    };

    CHAR dllPath[512];
    const CHAR funcName[] = "setup";
    int dllPathSize = GetModuleFileNameA(thisModule, dllPath, sizeof(dllPath));
    int totalSize = sizeof(shellcode) + dllPathSize + strlen(funcName) + 10;
    PBYTE remoteShllcode = (PBYTE)VirtualAllocEx(hProcess, NULL, totalSize, MEM_COMMIT | MEM_RESERVE, PAGE_EXECUTE_READWRITE);
    PBYTE remoteFuncName = remoteShllcode + sizeof(shellcode);
    WriteProcessMemory(hProcess, remoteFuncName, funcName, sizeof(funcName) + 1, NULL);
    PBYTE remoteDllPath = remoteFuncName + sizeof(funcName) + 1;
    WriteProcessMemory(hProcess, remoteDllPath, dllPath, dllPathSize + 1, NULL);


    auto kernel32 = LoadLibraryA("kernel32.dll");
    *(PBYTE*)(shellcode + 1) = remoteDllPath;
    *(PDWORD)(shellcode + 6) = (DWORD)GetProcAddress(kernel32, "LoadLibraryA") - (DWORD)remoteShllcode - 6 - 4;
    *(PBYTE*)(shellcode + 11) = remoteFuncName;
    *(PDWORD)(shellcode + 17) = (DWORD)GetProcAddress(kernel32, "GetProcAddress") - (DWORD)remoteShllcode - 17 - 4;
    *(HWND*)(shellcode + 22) = hWnd;
    WriteProcessMemory(hProcess, remoteShllcode, shellcode, sizeof(shellcode), NULL);
    
    HANDLE hRemoteThread = CreateRemoteThread(hProcess, NULL, NULL, (LPTHREAD_START_ROUTINE)remoteShllcode, NULL, NULL, NULL);
    WaitForSingleObject(hRemoteThread, INFINITE);
    DWORD exitCode;
    GetExitCodeThread(hRemoteThread, &exitCode);
    CloseHandle(hRemoteThread);
    return (LPVOID)exitCode;
}

__declspec(dllexport) GameInfo* WINAPI setup(HWND hWnd)
{
    parentHwnd = hWnd;
    Hook();

    auto audioLib = LoadLibraryA("AudioLib.dll");
    gameInfo.mainHwnd = &mainHwnd;
    gameInfo.idPtr = (LPVOID)((DWORD)audioLib + 0xDD5F4);
    return &gameInfo;
}

VOID CALLBACK ReleaseGameCallback(PVOID lpParameter, BOOLEAN TimerOrWaitFired)
{
    auto gi = (GameInfo*)lpParameter;
    PostMessage(gi->parentHwnd, WM_USER + 1, NULL, NULL);
    delete (GameInfo*)lpParameter;
}

__declspec(dllexport) GameInfo* WINAPI newGame(char* fileName, char *workdDirectory, HWND hWnd)
{
    STARTUPINFOA si = { sizeof(si) };
    PROCESS_INFORMATION pi = {};
    si.dwFlags = STARTF_USESTDHANDLES;
    if (CreateProcessA(NULL, fileName, NULL, NULL, FALSE, CREATE_SUSPENDED, NULL, workdDirectory, &si, &pi))
    {
        auto addr = inject(pi.hProcess, hWnd);
        ResumeThread(pi.hThread);
        auto gi = new GameInfo;
        ReadProcessMemory(pi.hProcess, addr, gi, sizeof(GameInfo), NULL);
        gi->pid = pi.dwProcessId;
        gi->hProcess = pi.hProcess;
        gi->parentHwnd = hWnd;
        HANDLE hNewHandle;
        RegisterWaitForSingleObject(&hNewHandle, pi.hProcess, ReleaseGameCallback, gi, INFINITE, WT_EXECUTEONLYONCE);
        CloseHandle(pi.hThread);
        return gi;
    }
    return NULL;
}

__declspec(dllexport) BOOL WINAPI readId(GameInfo* pGameInfo, PCHAR buffer)
{
    PCHAR p=NULL;
    ReadProcessMemory(pGameInfo->hProcess, pGameInfo->idPtr, &p, sizeof(p), NULL);
    if ((DWORD)p < 0x400000) {
        return FALSE;
    }
    ReadProcessMemory(pGameInfo -> hProcess, p + 0x37, buffer, 20, NULL);
    return TRUE;
}


__declspec(dllexport) HWND WINAPI readHwnd(GameInfo* pGameInfo)
{
    HWND hWnd;
    ReadProcessMemory(pGameInfo->hProcess, pGameInfo->mainHwnd, &hWnd, sizeof(hWnd), NULL);
    return hWnd;
}

__declspec(dllexport) void WINAPI killGame(GameInfo* pGameInfo)
{
    TerminateProcess(pGameInfo->hProcess, 0);
}

__declspec(dllexport) HANDLE WINAPI getProcess(GameInfo* pGameInfo)
{
    return pGameInfo->hProcess;
}
