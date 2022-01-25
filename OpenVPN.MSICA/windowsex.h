/*
    OpenVPN.MSICA - MSI Custom Actions for OpenVPN

    Copyright: 2022 The Commons Conservancy eduVPN Programme
    SPDX-License-Identifier: GPL-3.0+
*/

#pragma once

#include <Windows.h>

template<class _Elem, class _Traits, class _Ax>
inline BOOL
QueryFullProcessImageNameA(_In_ HANDLE hProcess, _In_ DWORD dwFlags, _Inout_ std::basic_string<_Elem, _Traits, _Ax>& sExeName)
{
    _Elem szStackBuffer[0x100 / sizeof(_Elem)];
    DWORD dwSize = _countof(szStackBuffer);

    // Try with stack buffer first.
    if (::QueryFullProcessImageNameA(hProcess, dwFlags, szStackBuffer, &dwSize)) {
        // Copy from stack.
        sExeName.assign(szStackBuffer, dwSize);
        return TRUE;
    }
    for (DWORD dwCapacity = 2 * 0x100 / sizeof(_Elem); GetLastError() == ERROR_INSUFFICIENT_BUFFER; dwCapacity *= 2) {
        // Allocate on heap and retry.
        std::unique_ptr<_Elem[]> szBuffer(new _Elem[dwCapacity]);
        dwSize = dwCapacity;
        if (::QueryFullProcessImageNameA(hProcess, dwFlags, szBuffer.get(), &dwSize)) {
            sExeName.assign(szBuffer.get(), dwSize);
            return TRUE;
        }
    }
    return FALSE;
}

template<class _Elem, class _Traits, class _Ax>
inline BOOL
QueryFullProcessImageNameW(_In_ HANDLE hProcess, _In_ DWORD dwFlags, _Inout_ std::basic_string<_Elem, _Traits, _Ax>& sExeName)
{
    _Elem szStackBuffer[0x100 / sizeof(_Elem)];
    DWORD dwSize = _countof(szStackBuffer);

    // Try with stack buffer first.
    if (::QueryFullProcessImageNameW(hProcess, dwFlags, szStackBuffer, &dwSize)) {
        // Copy from stack.
        sExeName.assign(szStackBuffer, dwSize);
        return TRUE;
    }
    for (DWORD dwCapacity = 2 * 0x100 / sizeof(_Elem); GetLastError() == ERROR_INSUFFICIENT_BUFFER; dwCapacity *= 2) {
        // Allocate on heap and retry.
        std::unique_ptr<_Elem[]> szBuffer(new _Elem[dwCapacity]);
        dwSize = dwCapacity;
        if (::QueryFullProcessImageNameW(hProcess, dwFlags, szBuffer.get(), &dwSize)) {
            sExeName.assign(szBuffer.get(), dwSize);
            return TRUE;
        }
    }
    return FALSE;
}
