/*
    eduVPN - VPN for education and research

    Copyright: 2017, The Commons Conservancy eduVPN Programme
    SPDX-License-Identifier: GPL-3.0+
*/

using System.Collections.Generic;

namespace System
{
    /// <summary>
    /// <see cref="System"/> namespace extension methods
    /// </summary>
    public static class Extensions
    {
        /// <summary>
        /// Returns binary representation of a HEX encoded string
        /// </summary>
        /// <param name="hex">HEX encoded string</param>
        /// <returns>Array of bytes</returns>
        public static byte[] FromHexToBin(this string hex)
        {
            var result = new List<byte>();
            byte x = 0xff;
            foreach (var c in hex)
            {
                byte n;
                if ('0' <= c && c <= '9') n = (byte)(c - '0');
                else if ('A' <= c && c <= 'F') n = (byte)(c - 'A' + 10);
                else if ('a' <= c && c <= 'f') n = (byte)(c - 'a' + 10);
                else continue;

                if ((x & 0xf) != 0)
                    x = (byte)(n << 4);
                else
                {
                    result.Add((byte)(x | n));
                    x = 0xff;
                }
            }

            return result.ToArray();
        }
    }
}
