/*
    OpenVPN.MSICA - MSI Custom Actions for OpenVPN

    Copyright: 2022 The Commons Conservancy
    SPDX-License-Identifier: GPL-3.0+
*/

#pragma once

#include <Windows.h>
#include <assert.h>
#include <objbase.h>

#include <memory>

#define WINSTD_NONCOPYABLE(C) \
private: \
    inline    C        (_In_ const C &h) noexcept; \
    inline C& operator=(_In_ const C &h) noexcept;

#define WINSTD_NONMOVABLE(C) \
private: \
    inline    C        (_Inout_ C &&h) noexcept; \
    inline C& operator=(_Inout_ C &&h) noexcept;

#define WINSTD_HANDLE_IMPL(C, INVAL) \
public: \
    inline    C        (                        ) noexcept                                            {                                                                    } \
    inline    C        (_In_opt_ handle_type   h) noexcept : handle<handle_type, INVAL>(          h ) {                                                                    } \
    inline    C        (_Inout_  C           &&h) noexcept : handle<handle_type, INVAL>(std::move(h)) {                                                                    } \
    inline C& operator=(_In_opt_ handle_type   h) noexcept                                            { handle<handle_type, INVAL>::operator=(          h ); return *this; } \
    inline C& operator=(_Inout_  C           &&h) noexcept                                            { handle<handle_type, INVAL>::operator=(std::move(h)); return *this; } \
WINSTD_NONCOPYABLE(C)

namespace winstd
{
    template <class T, const T INVAL>
    class handle
    {
    public:
        typedef T handle_type;
        static const T invalid;

        inline handle() noexcept : m_h(invalid) {}
        inline handle(_In_opt_ handle_type h) noexcept : m_h(h) {}
        inline handle(_Inout_ handle<handle_type, INVAL> &&h) noexcept { m_h = h.m_h; h.m_h = invalid; }

    private:
        inline handle(_In_ const handle<handle_type, INVAL> &h) noexcept {};
        inline handle<handle_type, INVAL>& operator=(_In_ const handle<handle_type, INVAL> &h) noexcept {};

    public:
        inline handle<handle_type, INVAL>& operator=(_In_opt_ handle_type h) noexcept
        {
            attach(h);
            return *this;
        }

        #pragma warning(suppress: 26432) // Move constructor is also present, but not detected by code analysis somehow.
        inline handle<handle_type, INVAL>& operator=(_Inout_ handle<handle_type, INVAL> &&h) noexcept
        {
            if (this != std::addressof(h)) {
                if (m_h != invalid)
                    free_internal();
                m_h   = h.m_h;
                h.m_h = invalid;
            }
            return *this;
        }

        inline operator handle_type() const { return m_h; }
        inline handle_type*& operator*() const { assert(m_h != invalid); return *m_h; }
        inline handle_type* operator&() { assert(m_h == invalid); return &m_h; }
        inline handle_type operator->() const { assert(m_h != invalid); return m_h; }
        inline bool operator!() const { return m_h == invalid; }
        inline bool operator<(_In_opt_ handle_type h) const { return m_h < h; }
        inline bool operator<=(_In_opt_ handle_type h) const { return !operator>(h); }
        inline bool operator>=(_In_opt_ handle_type h) const { return !operator<(h); }
        inline bool operator>(_In_opt_ handle_type h) const { return h < m_h; }
        inline bool operator!=(_In_opt_ handle_type h) const { return !operator==(h); }
        inline bool operator==(_In_opt_ handle_type h) const { return m_h == h; }

        inline void attach(_In_opt_ handle_type h) noexcept
        {
            if (m_h != invalid)
                free_internal();
            m_h = h;
        }

        inline handle_type detach()
        {
            handle_type h = m_h;
            m_h = invalid;
            return h;
        }

        inline void free()
        {
            if (m_h != invalid) {
                free_internal();
                m_h = invalid;
            }
        }

    protected:
        virtual void free_internal() noexcept = 0;

    protected:
        handle_type m_h;
    };

    template <class T, const T INVAL>
    const T handle<T, INVAL>::invalid = INVAL;

    template<HANDLE INVALID>
    class win_handle : public handle<HANDLE, INVALID>
    {
        WINSTD_HANDLE_IMPL(win_handle, INVALID)

    public:
        virtual ~win_handle()
        {
            if (m_h != invalid)
                CloseHandle(m_h);
        }

    protected:
        void free_internal() noexcept override
        {
            CloseHandle(m_h);
        }
    };

    class library : public handle<HMODULE, NULL>
    {
        WINSTD_HANDLE_IMPL(library, NULL)

    public:
        virtual ~library()
        {
            if (m_h != invalid)
                FreeLibrary(m_h);
        }

    protected:
        void free_internal() noexcept override
        {
            FreeLibrary(m_h);
        }
    };

    class com_initializer
    {
        WINSTD_NONCOPYABLE(com_initializer)
        WINSTD_NONMOVABLE(com_initializer)

    public:
        inline com_initializer(_In_opt_ LPVOID pvReserved) noexcept
        {
            m_result = CoInitialize(pvReserved);
        }

        inline com_initializer(_In_opt_ LPVOID pvReserved, _In_ DWORD dwCoInit) noexcept
        {
            m_result = CoInitializeEx(pvReserved, dwCoInit);
        }

        virtual ~com_initializer()
        {
            if (SUCCEEDED(m_result))
                CoUninitialize();
        }

        inline HRESULT status() const noexcept
        {
            return m_result;
        }

    protected:
        HRESULT m_result;
    };

    template <class _Ty>
    struct LocalFree_delete
    {
        typedef LocalFree_delete<_Ty> _Myt;

        LocalFree_delete() {}

        template <class _Ty2> LocalFree_delete(const LocalFree_delete<_Ty2>&) {}

        void operator()(_Ty *_Ptr) const
        {
            LocalFree(_Ptr);
        }
    };
}
