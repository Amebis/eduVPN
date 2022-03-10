/*
    eduMSICA - MSI Custom Actions

    Copyright: 2021-2022 The Commons Conservancy
    SPDX-License-Identifier: GPL-3.0+
*/

// Prevent inclusion of winsock.h in windows.h.
#define _WINSOCKAPI_

#include <Windows.h>
#include <Msi.h>
#include <MsiQuery.h>
#include <Shlwapi.h>
#include <stdarg.h>
#include <TlHelp32.h>
#include <wchar.h>
#include <WinStd/COM.h>
#include <WinStd/MSI.h>
#include <WinStd/Win.h>
#include <wintun.h>

#include <vector>

using namespace std;
using namespace winstd;

#define WINTUN_COMPONENT  TEXT("wintun.dll")
#define WINTUN_FILE_NAME  TEXT("wintun.dll")
#define WINTUN_DIRECTORY  TEXT("OPENVPNDIR")
#define ERROR_EDUMSICA_ERRNO 2550L

static MSIHANDLE s_hInstall; // Handle to the installation session

static void
LogErrorNumV(_In_ DWORD dwError, _In_z_ LPCTSTR szFunction, _Printf_format_string_ LPCTSTR szFormat, _In_ va_list args)
{
    LPTSTR szSystemMessage = NULL;
    FormatMessage(
        FORMAT_MESSAGE_FROM_SYSTEM | FORMAT_MESSAGE_ALLOCATE_BUFFER | FORMAT_MESSAGE_MAX_WIDTH_MASK,
        NULL,
        HRESULT_FROM_WIN32(dwError),
        MAKELANGID(LANG_NEUTRAL, SUBLANG_DEFAULT),
        (LPTSTR)&szSystemMessage,
        0,
        NULL);
    PMSIHANDLE hRecord = MsiCreateRecord(5);
    if (!(MSIHANDLE)hRecord)
        return;
    MsiRecordSetInteger(hRecord, 1, ERROR_EDUMSICA_ERRNO);
    MsiRecordSetString(hRecord, 2, szFunction);
    tstring str;
    vsprintf(str, szFormat, args);
    MsiRecordSetString(hRecord, 3, str.c_str());
    MsiRecordSetInteger(hRecord, 4, dwError);
    MsiRecordSetString(hRecord, 5, szSystemMessage ? szSystemMessage : TEXT(""));
    MsiProcessMessage(s_hInstall, INSTALLMESSAGE_ERROR, hRecord);
    LocalFree(szSystemMessage);
}

static void
LogErrorNum(_In_ DWORD dwError, _In_z_ LPCTSTR szFunction, _Printf_format_string_ LPCTSTR szFormat, ...)
{
    va_list args;
    va_start(args, szFormat);
    LogErrorNumV(dwError, szFunction, szFormat, args);
    va_end(args);
}

#define LOG_ERROR_NUM(dwError, szFormat, ...) LogErrorNum(dwError, TEXT(__FUNCTION__), szFormat, __VA_ARGS__)

static void
LogErrorV(_In_z_ LPCTSTR szFunction, _Printf_format_string_ LPCTSTR szFormat, _In_ va_list args)
{
    PMSIHANDLE hRecord = MsiCreateRecord(3);
    if (!(MSIHANDLE)hRecord)
        return;
    MsiRecordSetInteger(hRecord, 1, ERROR_EDUMSICA_ERRNO);
    MsiRecordSetString(hRecord, 2, szFunction);
    tstring str;
    vsprintf(str, szFormat, args);
    MsiRecordSetString(hRecord, 3, str.c_str());
    MsiProcessMessage(s_hInstall, INSTALLMESSAGE_ERROR, hRecord);
}

static void
LogError(_In_z_ LPCTSTR szFunction, _Printf_format_string_ LPCTSTR szFormat, ...)
{
    va_list args;
    va_start(args, szFormat);
    LogErrorV(szFunction, szFormat, args);
    va_end(args);
}

#define LOG_ERROR(szFormat, ...) LogError(TEXT(__FUNCTION__), szFormat, __VA_ARGS__)

static void CALLBACK
WintunLogger(_In_ WINTUN_LOGGER_LEVEL Level, _In_ DWORD64 Timestamp, _In_z_ LPCWSTR Message)
{
    UNREFERENCED_PARAMETER(Timestamp);
    PMSIHANDLE hRecord = MsiCreateRecord(2);
    if (!(MSIHANDLE)hRecord)
        return;
    LPCTSTR szTemplate;
    INSTALLMESSAGE eType;
    switch (Level)
    {
    case WINTUN_LOG_INFO:
        szTemplate = TEXT("Wintun: [1]");
        eType = INSTALLMESSAGE_INFO;
        break;
    case WINTUN_LOG_WARN:
        szTemplate = TEXT("Wintun warning: [1]");
        eType = INSTALLMESSAGE_INFO;
        break;
    case WINTUN_LOG_ERR:
        szTemplate = TEXT("Wintun error: [1]");
        eType = INSTALLMESSAGE_ERROR;
        break;
    default:
        return;
    }
    MsiRecordSetString(hRecord, 0, szTemplate);
    MsiRecordSetStringW(hRecord, 1, Message);
    MsiProcessMessage(s_hInstall, eType, hRecord);
}

static UINT
SetFormattedProperty(_In_ MSIHANDLE hInstall, _In_z_ LPCTSTR szPropertyName, _In_z_ LPCTSTR szFormat)
{
    PMSIHANDLE hRecord = MsiCreateRecord(1);
    if (!(MSIHANDLE)hRecord) {
        LOG_ERROR(TEXT("MsiCreateRecord failed"));
        return ERROR_OUTOFMEMORY;
    }
    UINT uiResult = MsiRecordSetString(hRecord, 0, szFormat);
    if (uiResult != ERROR_SUCCESS) {
        LOG_ERROR_NUM(uiResult, TEXT("MsiRecordSetString failed"));
        return uiResult;
    }
    tstring sValue;
    uiResult = MsiFormatRecord(hInstall, hRecord, sValue);
    if (uiResult != ERROR_SUCCESS) {
        LOG_ERROR_NUM(uiResult, TEXT("MsiFormatRecord failed"));
        return uiResult;
    }
    uiResult = MsiSetProperty(hInstall, szPropertyName, sValue.c_str());
    if (uiResult != ERROR_SUCCESS) {
        LOG_ERROR_NUM(uiResult, TEXT("MsiSetProperty(\"%s\") failed"), szPropertyName);
        return uiResult;
    }
    return ERROR_SUCCESS;
}

_Return_type_success_(return == ERROR_SUCCESS) UINT __stdcall
EvaluateComponents(_In_ MSIHANDLE hInstall)
{
    com_initializer com_init(NULL);
    s_hInstall = hInstall;

    // Get the wintun.dll component state.
    INSTALLSTATE iInstalled, iAction;
    UINT uiResult = MsiGetComponentState(hInstall, WINTUN_COMPONENT, &iInstalled, &iAction);
    if (uiResult == ERROR_SUCCESS) {
        if (iInstalled >= INSTALLSTATE_LOCAL && iAction > INSTALLSTATE_BROKEN && iAction < INSTALLSTATE_LOCAL)
        {
            // Wintun is installed, but should be degraded to advertised/removed.
            // Schedule Wintun driver deletition.
            SetFormattedProperty(hInstall, TEXT("RemoveWintunDriver"), TEXT("[") WINTUN_DIRECTORY TEXT("]") WINTUN_FILE_NAME);
        }
    } else
        LOG_ERROR_NUM(uiResult, TEXT("MsiGetComponentState(\"%s\") failed"), WINTUN_COMPONENT);

    static LPCTSTR szClientFilenames[] = {
        TEXT("eduVPN.Client.exe"),
        TEXT("LetsConnect.Client.exe"),
    };
    for (size_t i = 0; i < _countof(szClientFilenames); ++i) {
        // Get the client component state.
        uiResult = MsiGetComponentState(hInstall, szClientFilenames[i], &iInstalled, &iAction);
        if (uiResult == ERROR_SUCCESS) {
            if (iInstalled >= INSTALLSTATE_LOCAL && iAction >= INSTALLSTATE_REMOVED) {
                // Client component is installed and shall be removed/upgraded/reinstalled.
                // Schedule client termination.
                SetFormattedProperty(hInstall, TEXT("KillExecutableProcesses"), tstring_printf(TEXT("[COREDIR]%s"), szClientFilenames[i]).c_str());
            }
        } else if (uiResult != ERROR_UNKNOWN_COMPONENT)
            LOG_ERROR_NUM(uiResult, TEXT("MsiGetComponentState(\"%s\") failed"), szClientFilenames[i]);
    }

    return ERROR_SUCCESS;
}

_Return_type_success_(return == ERROR_SUCCESS) UINT __stdcall
RemoveWintunDriver(_In_ MSIHANDLE hInstall)
{
    com_initializer com_init(NULL);
    s_hInstall = hInstall;

    tstring sWintunPath;
    UINT uiResult = MsiGetProperty(hInstall, TEXT("CustomActionData"), sWintunPath);
    if (uiResult != ERROR_SUCCESS) {
        LOG_ERROR_NUM(uiResult, TEXT("MsiGetProperty(\"CustomActionData\") failed"));
        return ERROR_SUCCESS;
    }
    if (sWintunPath.empty())
        return ERROR_SUCCESS;

    library hWintun(LoadLibraryExW(sWintunPath.c_str(), NULL, LOAD_LIBRARY_SEARCH_APPLICATION_DIR | LOAD_LIBRARY_SEARCH_SYSTEM32));
    if (!hWintun)
    {
        LOG_ERROR_NUM(GetLastError(), TEXT("LoadLibraryExW(\"%ls\") failed"), sWintunPath.c_str());
        return ERROR_SUCCESS;
    }
    WINTUN_SET_LOGGER_FUNC *WintunSetLogger;
    WINTUN_DELETE_DRIVER_FUNC *WintunDeleteDriver;
#define X(Name) ((*(FARPROC *)&Name = GetProcAddress(hWintun, #Name)) == NULL)
    if (X(WintunSetLogger) ||
        X(WintunDeleteDriver))
#undef X
    {
        LOG_ERROR_NUM(GetLastError(), TEXT("GetProcAddress failed"));
        return ERROR_SUCCESS;
    }
    WintunSetLogger(WintunLogger);
    if (!WintunDeleteDriver())
        LOG_ERROR_NUM(GetLastError(), TEXT("WintunDeleteDriver failed"));
    return ERROR_SUCCESS;
}

typedef struct tagFILEID {
    DWORD dwVolumeSerialNumber;
    DWORD nFileIndexHigh;
    DWORD nFileIndexLow;
} FILEID;
typedef FILEID* LPFILEID;

_Return_type_success_(return != FALSE) static BOOL
CalculateFileId(_In_z_ LPCTSTR szPath, _Out_ LPFILEID id)
{
    file hFile(CreateFile(szPath, 0, 0, NULL, OPEN_EXISTING, FILE_ATTRIBUTE_NORMAL, NULL));
    if (!hFile)
        return FALSE;
    BY_HANDLE_FILE_INFORMATION info = { 0 };
    if (!GetFileInformationByHandle(hFile, &info))
        return FALSE;
    id->dwVolumeSerialNumber = info.dwVolumeSerialNumber;
    id->nFileIndexHigh = info.nFileIndexHigh;
    id->nFileIndexLow = info.nFileIndexLow;
    return TRUE;
}

_Return_type_success_(return == ERROR_SUCCESS) UINT __stdcall
KillExecutableProcesses(_In_ MSIHANDLE hInstall)
{
    com_initializer com_init(NULL);
    s_hInstall = hInstall;

    tstring szExecutable;
    UINT uiResult = MsiGetProperty(hInstall, TEXT("CustomActionData"), szExecutable);
    if (uiResult != ERROR_SUCCESS) {
        LOG_ERROR_NUM(uiResult, TEXT("MsiGetProperty(\"CustomActionData\") failed"));
        return ERROR_SUCCESS;
    }
    if (szExecutable.empty())
        return ERROR_SUCCESS;
    FILEID idExecutable;
    if (!CalculateFileId(szExecutable.c_str(), &idExecutable))
        return ERROR_SUCCESS;

    process_snapshot hSnapshot(CreateToolhelp32Snapshot(TH32CS_SNAPPROCESS, 0));
    if (!hSnapshot)
        return ERROR_SUCCESS;

    PROCESSENTRY32 pe32 = { sizeof(PROCESSENTRY32) };
    for (BOOL fSuccess = Process32First(hSnapshot, &pe32); fSuccess; fSuccess = Process32Next(hSnapshot, &pe32)) {
        process hProcess(OpenProcess(PROCESS_TERMINATE | PROCESS_QUERY_LIMITED_INFORMATION, FALSE, pe32.th32ProcessID));
        FILEID id;
        if (hProcess &&
            QueryFullProcessImageName(hProcess, 0, szExecutable) &&
            CalculateFileId(szExecutable.c_str(), &id) &&
            idExecutable.dwVolumeSerialNumber == id.dwVolumeSerialNumber &&
            idExecutable.nFileIndexHigh == id.nFileIndexHigh &&
            idExecutable.nFileIndexLow == id.nFileIndexLow &&
            TerminateProcess(hProcess, 0xC000026BL /*STATUS_DLL_INIT_FAILED_LOGOFF*/))
        {
            WaitForSingleObject(hProcess, INFINITE);

            PMSIHANDLE hRecord = MsiCreateRecord(3);
            if ((MSIHANDLE)hRecord) {
                MsiRecordSetString(hRecord, 0, TEXT("Killed \"[1]\" (pid [2])"));
                MsiRecordSetString(hRecord, 1, szExecutable.c_str());
                MsiRecordSetInteger(hRecord, 2, pe32.th32ProcessID);
                MsiProcessMessage(hInstall, INSTALLMESSAGE_INFO, hRecord);
            }
        }
    }
    return ERROR_SUCCESS;
}
