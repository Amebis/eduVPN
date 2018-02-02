/*
    eduVPN - VPN for education and research

    Copyright: 2017, The Commons Conservancy eduVPN Programme
    SPDX-License-Identifier: GPL-3.0+
*/

namespace System
{
    /// <summary>
    /// <see cref="System"/> namespace extension methods
    /// </summary>
    public static class Extensions
    {
        /// <summary>
        /// Returns left part of the string
        /// </summary>
        /// <param name="str">String</param>
        /// <param name="length">Maximum length of the string to return</param>
        /// <returns>Left part of the string of <paramref name="length"/> characters or less</returns>
        public static string Left(this string str, int length)
        {
            if (String.IsNullOrEmpty(str))
                return str;

            return str.Length <= length ? str : str.Substring(0, length);
        }
    }
}
