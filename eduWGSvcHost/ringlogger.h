/*
    eduVPN - VPN for education and research

    Copyright: 2022 The Commons Conservancy
    SPDX-License-Identifier: GPL-3.0+
*/

#pragma once

#include <Windows.h>
#include <algorithm>
#include <memory>
#include <string>
#include <WinStd/Win.h>

namespace wg
{
    class ringlogger
    {
    private:
        typedef std::unique_ptr<unsigned char[], winstd::UnmapViewOfFile_delete<unsigned char[]>> file_mapping_view;

        class unix_timestamp
        {
        private:
            static const long long epoch = 116444736000000000ll;
            long long m_ns;

        public:
            unix_timestamp(_In_ long long ns) noexcept : m_ns(ns)
            {}

            bool empty() const noexcept
            {
                return m_ns == 0;
            }

            static unix_timestamp now() noexcept
            {
                FILETIME now;
                GetSystemTimeAsFileTime(&now);
                ULARGE_INTEGER x;
                x.HighPart = now.dwHighDateTime;
                x.LowPart = now.dwLowDateTime;
                return unix_timestamp((x.QuadPart - epoch) * 100);
            }

            long long ns() const noexcept
            {
                return m_ns;
            }

            void to_string(_Out_writes_z_(27) char* str) const
            {
                ULARGE_INTEGER x;
                x.QuadPart = ((m_ns / 100) + epoch);
                FILETIME ft = { x.LowPart, x.HighPart };
                SYSTEMTIME st_utc, st_local;
                if (!FileTimeToSystemTime(&ft, &st_utc))
                    throw winstd::win_runtime_error("FileTimeToSystemTime failed");
                if (!SystemTimeToTzSpecificLocalTime(NULL, &st_utc, &st_local))
                    throw winstd::win_runtime_error("SystemTimeToTzSpecificLocalTime failed");
                char ns[10];
                snprintf(ns, _countof(ns), "%u", (unsigned long)(m_ns % 1000000000ull));
                for (char* p = ns + strlen(ns); p != &ns[_countof(ns)]; p++)
                    *p = '0';
                snprintf(str, 27, "%04u-%02u-%02u %02u:%02u:%02u.%.6s", st_local.wYear, st_local.wMonth, st_local.wDay, st_local.wHour, st_local.wMinute, st_local.wSecond, ns);
            }
        };

        class line
        {
        private:
            static const unsigned int max_line_length = 512;
            static const int offset_time_ns = 0;
            static const int offset_line = 8;

            file_mapping_view& m_view;
            size_t m_start;

        public:
            line(_In_ file_mapping_view& view, _In_ unsigned int index) noexcept :
                m_view(view),
                m_start((size_t)log::header_bytes() + (size_t)index * bytes())
            {}

            static int bytes() noexcept
            {
                return max_line_length + offset_line;
            }

            unix_timestamp timestamp() const noexcept
            {
                return unix_timestamp(*(long long*)&m_view[m_start + offset_time_ns]);
            }

            void set_timestamp(_In_ const unix_timestamp& value) noexcept
            {
                *(long long*)&m_view[m_start + offset_time_ns] = value.ns();
            }

            std::string text() const
            {
                auto text_bytes = (const char*)&m_view[m_start + offset_line];
                auto end = (char*)memchr(text_bytes, 0, max_line_length);
                return std::string(text_bytes, end ? end : text_bytes + max_line_length);
            }

            void set_text(_In_opt_z_ const char* value)
            {
                if (!value)
                {
                    memset(&m_view[m_start + offset_line], 0, max_line_length);
                    return;
                }
                auto bytes_to_write = std::min<size_t>(max_line_length - 1, strlen(value));
                *(char*)&m_view[m_start + offset_line + bytes_to_write] = 0;
                memcpy(&m_view[m_start + offset_line], value, bytes_to_write);
            }

            std::string to_string() const
            {
                auto time = timestamp();
                if (time.empty())
                    return "";
                auto text = this->text();
                if (text.empty())
                    return "";
                char time_str[27];
                time.to_string(time_str);
                return winstd::string_printf("%s: %s", time_str, text.c_str());
            }
        };

        class log
        {
        private:
            static const unsigned int max_lines = 2048;
            static const int offset_magic = 0;
            static const int offset_next_index = 4;
            static const int offset_lines = 8;

            file_mapping_view& m_view;

        public:
            log(_In_ file_mapping_view& view) noexcept : m_view(view)
            {}

            static int header_bytes() noexcept
            {
                return offset_lines;
            }

            static int bytes() noexcept
            {
                return header_bytes() + line::bytes() * max_lines;
            }

            static unsigned int expected_magic() noexcept
            {
                return 0xbadbabe;
            }

            unsigned int magic() const
            {
                return *(unsigned int*)&m_view[offset_magic];
            }

            void set_magic(_In_ unsigned int value)
            {
                *(unsigned int*)&m_view[offset_magic] = value;
            }

            unsigned int next_index() const
            {
                return *(unsigned int*)&m_view[offset_next_index];
            }

            void set_next_index(_In_ unsigned int value)
            {
                *(unsigned int*)&m_view[offset_next_index] = value;
            }

            unsigned int insert_next_index()
            {
                return (unsigned int)InterlockedIncrement((LONG volatile*)&m_view[offset_next_index]);
            }

            static unsigned int line_count() noexcept
            {
                return max_lines;
            }

            line operator[](_In_ unsigned int i) const
            {
                return line(m_view, i % max_lines);
            }

            void clear()
            {
                memset(&m_view[0], 0, bytes());
            }
        };

        log m_log;
        std::string m_tag;
        winstd::file m_file;
        winstd::file_mapping m_mmap;
        file_mapping_view m_view;

    public:
        ringlogger(_In_z_ LPCTSTR filename, _In_z_ const char* tag) :
            m_log(m_view),
            m_tag(tag)
        {
            m_file = CreateFile(filename, GENERIC_READ | GENERIC_WRITE, FILE_SHARE_READ | FILE_SHARE_WRITE | FILE_SHARE_DELETE, NULL, OPEN_EXISTING, FILE_ATTRIBUTE_NORMAL, NULL);
            if (!m_file)
            {
                if (GetLastError() != ERROR_FILE_NOT_FOUND)
                    throw winstd::win_runtime_error("Failed to open ring logger file");
                m_file = CreateFile(filename, GENERIC_READ | GENERIC_WRITE, FILE_SHARE_READ | FILE_SHARE_WRITE | FILE_SHARE_DELETE, NULL, CREATE_ALWAYS, FILE_ATTRIBUTE_NORMAL, NULL);
                if (!m_file)
                    throw winstd::win_runtime_error("Failed to create ring logger file");
            }
            if (SetFilePointer(m_file, log::bytes(), NULL, FILE_BEGIN) == INVALID_SET_FILE_POINTER)
                throw winstd::win_runtime_error("Failed to seek in ring logger file");
            if (!SetEndOfFile(m_file))
                throw winstd::win_runtime_error("Failed to set EOF in ring logger file");
            //if (SetFilePointer(m_file, 0, NULL, FILE_BEGIN) == INVALID_SET_FILE_POINTER)
            //    throw winstd::win_runtime_error("Failed to seek in ring logger file");
            m_mmap = CreateFileMapping(m_file, NULL, PAGE_READWRITE, 0, 0, NULL);
            if (!m_mmap)
                throw winstd::win_runtime_error("Failed to create ring logger file mapping");
            m_view.reset((unsigned char*)MapViewOfFile(m_mmap, FILE_MAP_ALL_ACCESS, 0, 0, log::bytes()));
            if (!m_view)
                throw winstd::win_runtime_error("Failed to map view of ring logger file mapping");
            if (m_log.magic() != m_log.expected_magic())
            {
                m_log.clear();
                m_log.set_magic(m_log.expected_magic());
            }
        }

        void write(_In_z_ const char* line)
        {
            auto time = unix_timestamp::now();
            auto entry = m_log[m_log.insert_next_index() - 1];
            entry.set_timestamp(unix_timestamp(0));
            entry.set_text(nullptr);
            while (iswspace(*line)) ++line;
            const char* line_end = line;
            for (const char* p = line; *p;)
                if (!iswspace(*(p++)))
                    line_end = p;
            if (line_end - line > 0xffffffff)
                throw std::invalid_argument("Log line too big");
            entry.set_text(winstd::string_printf("[%s] %.*s", m_tag.c_str(), (unsigned int)(line_end - line), line).c_str());
            entry.set_timestamp(time);
        }

        void write_to(_In_ HANDLE hFile)
        {
            auto start = m_log.next_index();
            for (unsigned int i = 0; i < m_log.line_count(); ++i)
            {
                auto entry = m_log[i + start];
                if (entry.timestamp().empty())
                    continue;
                auto text = entry.to_string();
                if (text.empty())
                    continue;
                text += "\r\n";
                if (text.size() > MAXDWORD)
                    throw std::invalid_argument("Log line too big");
                DWORD written;
                if (!WriteFile(hFile, text.c_str(), (DWORD)text.size(), &written, NULL))
                    throw winstd::win_runtime_error("WriteFile failed");
            }
            FlushFileBuffers(hFile);
        }

        static const unsigned int cursor_all = (unsigned int)-1;

        void follow_from_cursor(_Inout_ unsigned int& cursor, _In_ HANDLE hFile)
        {
            auto i = cursor;
            auto all = cursor == cursor_all;
            if (all)
                i = m_log.next_index();
            for (unsigned int l = 0; l < m_log.line_count(); ++l, ++i)
            {
                if (!all && i % m_log.line_count() == m_log.next_index() % m_log.line_count())
                    break;
                auto entry = m_log[i];
                if (entry.timestamp().empty())
                {
                    if (all)
                        continue;
                    break;
                }
                cursor = (i + 1) % m_log.line_count();
                auto text = entry.to_string();
                if (text.empty())
                    continue;
                text += "\r\n";
                if (text.size() > MAXDWORD)
                    throw std::invalid_argument("Log line too big");
                DWORD written;
                if (!WriteFile(hFile, text.c_str(), (DWORD)text.size(), &written, NULL))
                    throw winstd::win_runtime_error("WriteFile failed");
            }
            FlushFileBuffers(hFile);
        }
    };
}