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
    /// A blittable struct to allow (*C.char, *C.char, *C.char) CGo function return types
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    struct CGoPtrPtrPtr
    {
        public IntPtr r0;
        public IntPtr r1;
        public IntPtr r2;
    }
}
