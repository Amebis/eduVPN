/*
    eduVPN - VPN for education and research

    Copyright: 2017-2023 The Commons Conservancy
    SPDX-License-Identifier: GPL-3.0+
*/

using System;
using System.Runtime.InteropServices;

namespace eduVPN
{
    /// <summary>
    /// A blittable struct to allow (C.int, *C.char) CGo function return types
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    struct CGoIntPtr
    {
        public int r0;
        public IntPtr r1;
    }
}
