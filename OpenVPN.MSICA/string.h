/*
    OpenVPN.MSICA - MSI Custom Actions for OpenVPN

    Copyright: 2021-2022 The Commons Conservancy eduVPN Programme
    SPDX-License-Identifier: GPL-3.0+
*/

#pragma once

#include <sal.h>
#include <stdarg.h>
#include <stdio.h>
#include <wchar.h>

#include <string>

#ifdef UNICODE
typedef std::wstring tstring;
#else
typedef std::string tstring;
#endif

static inline std::string
string_printf_v(_Printf_format_string_ const char* fmt, _In_ va_list args)
{
    std::string s;
    va_list args2;
    va_copy(args2, args);
    s.resize((size_t)vsnprintf(nullptr, 0, fmt, args2) + 1);
    va_end(args2);
    vsprintf_s(const_cast<char*>(s.data()), s.capacity(), fmt, args);
    s.pop_back();
    return s;
}

static inline std::wstring
string_printf_v(_Printf_format_string_ const wchar_t* fmt, _In_ va_list args)
{
    wchar_t szStackBuffer[0x100 / sizeof(wchar_t)];
    std::wstring s;
    va_list args2;
    int iResult;

    // Try with stack buffer first.
    va_copy(args2, args);
    iResult = _vsnwprintf_s(szStackBuffer, _countof(szStackBuffer), _TRUNCATE, fmt, args2);
    va_end(args2);
    if (iResult >= 0) {
        // Copy from stack.
        s.assign(szStackBuffer, iResult);
        return s;
    }
    for (size_t nCapacity = 2 * 0x100 / sizeof(wchar_t); ; nCapacity *= 2) {
        // Allocate on heap and retry.
        std::unique_ptr<wchar_t[]> szBuffer(new wchar_t[nCapacity]);
        va_copy(args2, args);
        iResult = _vsnwprintf_s(szBuffer.get(), nCapacity, _TRUNCATE, fmt, args2);
        va_end(args2);
        if (iResult >= 0) {
            s.assign(szBuffer.get(), iResult);
            return s;
        }
    }
}

static inline std::string
string_printf(_Printf_format_string_ const char* fmt, ...)
{
    va_list args;
    va_start(args, fmt);
    std::string s(std::move(string_printf_v(fmt, args)));
    va_end(args);
    return s;
}

static inline std::wstring
string_printf(_Printf_format_string_ const wchar_t* fmt, ...)
{
    va_list args;
    va_start(args, fmt);
    std::wstring s(std::move(string_printf_v(fmt, args)));
    va_end(args);
    return s;
}
