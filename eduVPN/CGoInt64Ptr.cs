/*
    eduVPN - VPN for education and research

    Copyright: 2017-2024 The Commons Conservancy
    SPDX-License-Identifier: GPL-3.0+
*/

using System;
using System.Runtime.InteropServices;

namespace eduVPN
{
    /// <summary>
    /// A blittable struct to allow (C.int64_t, *C.char) CGo function return types
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    struct CGoInt64Ptr
    {
        public long r0;
        public IntPtr r1;
    }
}
