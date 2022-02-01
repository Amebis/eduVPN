/*
    OpenVPN.MSICA - MSI Custom Actions for OpenVPN

    Copyright: 2021-2022 The Commons Conservancy
    SPDX-License-Identifier: GPL-3.0+
*/

#pragma once

#include <Windows.h>
#include <Msi.h>
#include <MsiQuery.h>

#include <memory>
#include <string>

template<class _Elem, class _Traits, class _Ax>
inline UINT
MsiGetPropertyA(_In_ MSIHANDLE hInstall, _In_z_ LPCSTR szName, _Inout_ std::basic_string<_Elem, _Traits, _Ax>& sValue)
{
    _Elem szStackBuffer[0x100 / sizeof(_Elem)];
    DWORD dwSize = _countof(szStackBuffer);
    UINT uiResult;

    // Try with stack buffer first.
    uiResult = ::MsiGetPropertyA(hInstall, szName, szStackBuffer, &dwSize);
    if (uiResult == ERROR_SUCCESS) {
        // Copy from stack.
        sValue.assign(szStackBuffer, dwSize);
        return ERROR_SUCCESS;
    }
    else if (uiResult == ERROR_MORE_DATA) {
        // Allocate buffer on heap to read the string data into and read it.
        std::unique_ptr<_Elem[]> szBuffer(new _Elem[++dwSize]);
        uiResult = ::MsiGetPropertyA(hInstall, szName, szBuffer.get(), &dwSize);
        sValue.assign(szBuffer.get(), uiResult == ERROR_SUCCESS ? dwSize : 0);
        return uiResult;
    }
    else {
        // Return error code.
        return uiResult;
    }
}

template<class _Elem, class _Traits, class _Ax>
inline UINT
MsiGetPropertyW(_In_ MSIHANDLE hInstall, _In_z_ LPCWSTR szName, _Inout_ std::basic_string<_Elem, _Traits, _Ax>& sValue)
{
    _Elem szStackBuffer[0x100 / sizeof(_Elem)];
    DWORD dwSize = _countof(szStackBuffer);
    UINT uiResult;

    // Try with stack buffer first.
    uiResult = ::MsiGetPropertyW(hInstall, szName, szStackBuffer, &dwSize);
    if (uiResult == ERROR_SUCCESS) {
        // Copy from stack.
        sValue.assign(szStackBuffer, dwSize);
        return ERROR_SUCCESS;
    }
    else if (uiResult == ERROR_MORE_DATA) {
        // Allocate buffer on heap to read the string data into and read it.
        std::unique_ptr<_Elem[]> szBuffer(new _Elem[++dwSize]);
        uiResult = ::MsiGetPropertyW(hInstall, szName, szBuffer.get(), &dwSize);
        sValue.assign(szBuffer.get(), uiResult == ERROR_SUCCESS ? dwSize : 0);
        return uiResult;
    }
    else {
        // Return error code.
        return uiResult;
    }
}

template<class _Elem, class _Traits, class _Ax>
inline UINT
MsiFormatRecordA(_In_ MSIHANDLE hInstall, _In_ MSIHANDLE hRecord, _Inout_ std::basic_string<_Elem, _Traits, _Ax>& sValue)
{
    _Elem szStackBuffer[0x100 / sizeof(_Elem)];
    DWORD dwSize = _countof(szStackBuffer);
    UINT uiResult;

    // Try with stack buffer first.
    uiResult = ::MsiFormatRecordA(hInstall, hRecord, szStackBuffer, &dwSize);
    if (uiResult == ERROR_SUCCESS) {
        // Copy from stack.
        sValue.assign(szStackBuffer, dwSize);
        return ERROR_SUCCESS;
    }
    else if (uiResult == ERROR_MORE_DATA) {
        // Allocate buffer on heap to format the string data into and read it.
        std::unique_ptr<_Elem[]> szBuffer(new _Elem[++dwSize]);
        uiResult = ::MsiFormatRecordA(hInstall, hRecord, szBuffer.get(), &dwSize);
        sValue.assign(szBuffer.get(), uiResult == ERROR_SUCCESS ? dwSize : 0);
        return uiResult;
    }
    else {
        // Return error code.
        return uiResult;
    }
}

template<class _Elem, class _Traits, class _Ax>
inline UINT
MsiFormatRecordW(_In_ MSIHANDLE hInstall, _In_ MSIHANDLE hRecord, _Inout_ std::basic_string<_Elem, _Traits, _Ax>& sValue)
{
    _Elem szStackBuffer[0x100 / sizeof(_Elem)];
    DWORD dwSize = _countof(szStackBuffer);
    UINT uiResult;

    // Try with stack buffer first.
    uiResult = ::MsiFormatRecordW(hInstall, hRecord, szStackBuffer, &dwSize);
    if (uiResult == ERROR_SUCCESS) {
        // Copy from stack.
        sValue.assign(szStackBuffer, dwSize);
        return ERROR_SUCCESS;
    }
    else if (uiResult == ERROR_MORE_DATA) {
        // Allocate buffer on heap to format the string data into and read it.
        std::unique_ptr<_Elem[]> szBuffer(new _Elem[++dwSize]);
        uiResult = ::MsiFormatRecordW(hInstall, hRecord, szBuffer.get(), &dwSize);
        sValue.assign(szBuffer.get(), uiResult == ERROR_SUCCESS ? dwSize : 0);
        return uiResult;
    }
    else {
        // Return error code.
        return uiResult;
    }
}
