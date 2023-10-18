/*
    eduEx - Extensions for .NET

    Copyright: 2017-2023 The Commons Conservancy
    SPDX-License-Identifier: GPL-3.0+
*/

using System;
using System.Runtime.InteropServices;
using System.Security;

namespace eduEx.Security
{
    public static class Extensions
    {
        /// <summary>
        /// Compares two secure strings for equality
        /// </summary>
        /// <param name="ss1">First secure string</param>
        /// <param name="ss2">Second secure string</param>
        /// <returns><c>true</c> when <paramref name="ss1"/> equals <paramref name="ss2"/>; <c>false</c> otherwise</returns>
        public static bool IsEqualTo(this SecureString ss1, SecureString ss2)
        {
            var bstr1 = IntPtr.Zero;
            var bstr2 = IntPtr.Zero;
            try
            {
                bstr1 = Marshal.SecureStringToBSTR(ss1);
                bstr2 = Marshal.SecureStringToBSTR(ss2);
                var x = 0;
                var length1 = Marshal.ReadInt32(bstr1, -4);
                var length2 = Marshal.ReadInt32(bstr2, -4);
                var equal = true;
                for (x = 0; x < length1 && x < length2; x += 2)
                {
                    var b1 = Marshal.ReadInt16(bstr1, x);
                    var b2 = Marshal.ReadInt16(bstr2, x);
                    equal &= b1 == b2;
                }
                equal &= x >= length1 && x >= length2;
                return equal;
            }
            finally
            {
                if (bstr2 != IntPtr.Zero) Marshal.ZeroFreeBSTR(bstr2);
                if (bstr1 != IntPtr.Zero) Marshal.ZeroFreeBSTR(bstr1);
            }
        }
    }
}
