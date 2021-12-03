/*
    OpenVPN.MSICA - MSI Custom Actions for OpenVPN

    Copyright: 2021 The Commons Conservancy eduVPN Programme
    SPDX-License-Identifier: GPL-3.0+
*/

// Prevent inclusion of winsock.h in windows.h.
#define _WINSOCKAPI_

#include <Windows.h>
#include <Msi.h>
#include <MsiQuery.h>
#include <stdarg.h>
#include <wchar.h>
#include <wintun.h>

#include "msiex.h"
#include "string.h"

using namespace std;

#define WINTUN_COMPONENT             TEXT("wintun.dll")
#define WINTUN_FILE_NAME             TEXT("wintun.dll")
#define WINTUN_DIRECTORY             TEXT("OPENVPNDIR")
#define WUNTUN_REMOVE_DRIVER_CA_NAME TEXT("RemoveWintunDriver")
#define ERROR_MSICA_ERRNO            2550L

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
    MsiRecordSetInteger(hRecord, 1, ERROR_MSICA_ERRNO);
    MsiRecordSetString(hRecord, 2, szFunction);
    MsiRecordSetString(hRecord, 3, string_printf_v(szFormat, args).c_str());
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
    MsiRecordSetInteger(hRecord, 1, ERROR_MSICA_ERRNO);
    MsiRecordSetString(hRecord, 2, szFunction);
    MsiRecordSetString(hRecord, 3, string_printf_v(szFormat, args).c_str());
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


_Return_type_success_(return == ERROR_SUCCESS) UINT __stdcall
EvaluateWintunDriver(_In_ MSIHANDLE hInstall)
{
    s_hInstall = hInstall;

    // Get the wintun.dll component state.
    INSTALLSTATE iInstalled, iAction;
    UINT uiResult = MsiGetComponentState(hInstall, WINTUN_COMPONENT, &iInstalled, &iAction);
    if (uiResult != ERROR_SUCCESS) {
        LOG_ERROR_NUM(uiResult, TEXT("MsiGetComponentState(\"%s\") failed"), WINTUN_COMPONENT);
        return ERROR_SUCCESS;
    }

    if (iInstalled >= INSTALLSTATE_LOCAL && iAction > INSTALLSTATE_BROKEN && iAction < INSTALLSTATE_LOCAL)
    {
        // Wintun is installed, but should be degraded to advertised/removed.
        // Schedule Wintun driver deletition.
        PMSIHANDLE hRecord = MsiCreateRecord(1);
        if (!(MSIHANDLE)hRecord) {
            LOG_ERROR(TEXT("MsiCreateRecord failed"));
            return ERROR_SUCCESS;
        }
        uiResult = MsiRecordSetString(hRecord, 0, TEXT("\"[") WINTUN_DIRECTORY TEXT("]") WINTUN_FILE_NAME TEXT("\""));
        if (uiResult != ERROR_SUCCESS) {
            LOG_ERROR_NUM(uiResult, TEXT("MsiRecordSetString failed"));
            return ERROR_SUCCESS;
        }
        tstring sRemoveWintunDriver;
        uiResult = MsiFormatRecord(hInstall, hRecord, sRemoveWintunDriver);
        if (uiResult != ERROR_SUCCESS) {
            LOG_ERROR_NUM(uiResult, TEXT("MsiFormatRecord failed"));
            return ERROR_SUCCESS;
        }
        uiResult = MsiSetProperty(hInstall, WUNTUN_REMOVE_DRIVER_CA_NAME, sRemoveWintunDriver.c_str());
        if (uiResult != ERROR_SUCCESS) {
            LOG_ERROR_NUM(uiResult, TEXT("MsiSetProperty(\"%s\") failed"), WUNTUN_REMOVE_DRIVER_CA_NAME);
            return ERROR_SUCCESS;
        }
    }

    return ERROR_SUCCESS;
}

_Return_type_success_(return == ERROR_SUCCESS) UINT __stdcall
RemoveWintunDriver(_In_ MSIHANDLE hInstall)
{
    s_hInstall = hInstall;

    wstring sSequence;
    UINT uiResult = MsiGetPropertyW(hInstall, L"CustomActionData", sSequence);
    if (uiResult != ERROR_SUCCESS) {
        LOG_ERROR_NUM(uiResult, TEXT("MsiGetPropertyW(\"CustomActionData\") failed"));
        return ERROR_SUCCESS;
    }
    if (sSequence.empty())
        return ERROR_SUCCESS;
    int nArgs;
    LPWSTR* szArg = CommandLineToArgvW(sSequence.c_str(), &nArgs);
    if (szArg == NULL) {
        LOG_ERROR_NUM(GetLastError(), TEXT("CommandLineToArgvW(\"%ls\") failed"), sSequence.c_str());
        return ERROR_SUCCESS;
    }
    if (nArgs < 1) {
        LOG_ERROR(TEXT("Syntax: <%s path>"), WINTUN_FILE_NAME);
        goto cleanup_szArg;
    }
    HMODULE hWintun = LoadLibraryExW(szArg[0], NULL, LOAD_LIBRARY_SEARCH_APPLICATION_DIR | LOAD_LIBRARY_SEARCH_SYSTEM32);
    if (!hWintun)
    {
        LOG_ERROR_NUM(GetLastError(), TEXT("LoadLibraryExW(\"%ls\") failed"), szArg[0]);
        goto cleanup_szArg;
    }
    WINTUN_SET_LOGGER_FUNC *WintunSetLogger;
    WINTUN_DELETE_DRIVER_FUNC *WintunDeleteDriver;
#define X(Name) ((*(FARPROC *)&Name = GetProcAddress(hWintun, #Name)) == NULL)
    if (X(WintunSetLogger) ||
        X(WintunDeleteDriver))
#undef X
    {
        LOG_ERROR_NUM(GetLastError(), TEXT("GetProcAddress failed"));
        goto cleanup_hWintun;
    }
    WintunSetLogger(WintunLogger);
    if (!WintunDeleteDriver())
        LOG_ERROR_NUM(GetLastError(), TEXT("WintunDeleteDriver(\"%ls\") failed"), szArg[1]);
cleanup_hWintun:
    FreeLibrary(hWintun);
cleanup_szArg:
    LocalFree(szArg);
    return ERROR_SUCCESS;
}
