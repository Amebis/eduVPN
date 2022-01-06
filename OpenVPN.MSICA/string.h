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
    std::string s{};
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
    std::wstring s{};
    va_list args2;
    va_copy(args2, args);
    s.resize((size_t)_vsnwprintf_s(nullptr, 0, _TRUNCATE, fmt, args2) + 1);
    va_end(args2);
    vswprintf(const_cast<wchar_t*>(s.data()), s.capacity(), fmt, args);
    s.pop_back();
    return s;
}
