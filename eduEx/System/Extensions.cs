/*
    eduEx - Extensions for .NET

    Copyright: 2017-2023 The Commons Conservancy
    SPDX-License-Identifier: GPL-3.0+
*/

using System;
using System.Diagnostics;
using System.IO;

namespace eduEx.System
{
    /// <summary>
    /// <see cref="System"/> namespace extension methods
    /// </summary>
    public static class Extensions
    {
        /// <summary>
        /// Returns the copy of sub-array
        /// </summary>
        /// <typeparam name="T">Array element type</typeparam>
        /// <param name="data">The Array</param>
        /// <param name="index">Starting index</param>
        /// <returns>Sub-array</returns>
        [DebuggerStepThrough]
        public static T[] SubArray<T>(this T[] data, long index)
        {
            var result = new T[data.LongLength - index];
            Array.Copy(data, index, result, 0, result.LongLength);
            return result;
        }

        /// <summary>
        /// Returns the copy of sub-array
        /// </summary>
        /// <typeparam name="T">Array element type</typeparam>
        /// <param name="data">The Array</param>
        /// <param name="index">Starting index</param>
        /// <param name="length">Number of elements to copy</param>
        /// <returns>Sub-array</returns>
        [DebuggerStepThrough]
        public static T[] SubArray<T>(this T[] data, long index, long length)
        {
            var result = new T[length];
            Array.Copy(data, index, result, 0, length);
            return result;
        }

        /// <summary>
        /// Sets a range of elements in an array to the default value of each element type
        /// </summary>
        /// <typeparam name="T">Array element type</typeparam>
        /// <param name="data">The Array</param>
        /// <param name="index">Starting index</param>
        /// <param name="length">Number of elements to clear</param>
        public static void Clear<T>(this T[] data, long index, long length)
        {
            for (; index < length; index++)
                data[index] = default;
        }

        /// <summary>
        /// Returns binary representation of a HEX encoded string
        /// </summary>
        /// <param name="hex">HEX encoded string</param>
        /// <returns>Array of bytes</returns>
        public static byte[] FromHexToBin(this string hex)
        {
            using (var s = new MemoryStream(hex.Length / 2))
            {
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
                        s.WriteByte((byte)(x | n));
                        x = 0xff;
                    }
                }
                return s.GetBuffer().SubArray(0, (int)s.Length);
            }
        }

        /// <summary>
        /// Returns left part of the string
        /// </summary>
        /// <param name="str">String</param>
        /// <param name="length">Maximum length of the string to return</param>
        /// <returns>Left part of the string of <paramref name="length"/> characters or less</returns>
        public static string Left(this string str, int length)
        {
            if (string.IsNullOrEmpty(str))
                return str;

            return str.Length <= length ? str : str.Substring(0, length);
        }
    }
}
