/*
    eduVPN - VPN for education and research

    Copyright: 2017-2020 The Commons Conservancy eduVPN Programme
    SPDX-License-Identifier: GPL-3.0+
*/

using System.Threading;

namespace System.Collections.Generic
{
    /// <summary>
    /// <see cref="Generic"/> namespace extension methods
    /// </summary>
    public static class Extensions
    {
        /// <summary>
        /// Gets a localized value from the dictionary
        /// </summary>
        /// <param name="i">Dictionary with locale name as keys</param>
        /// <param name="fallback">Fallback value, when the dictionary is empty</param>
        /// <returns>Localized value; <paramref name="fallback"/> when the dictionary is empty.</returns>
        public static T GetLocalized<T>(this Dictionary<string, T> i, T fallback = default)
        {
            // Get value according to thread UI culture.
            if (i.TryGetValue(Thread.CurrentThread.CurrentUICulture.IetfLanguageTag, out T value) ||
                i.TryGetValue(Thread.CurrentThread.CurrentUICulture.TwoLetterISOLanguageName, out value))
                return value;

            // Get value according to thread culture.
            if (i.TryGetValue(Thread.CurrentThread.CurrentCulture.IetfLanguageTag, out value) ||
                i.TryGetValue(Thread.CurrentThread.CurrentCulture.TwoLetterISOLanguageName, out value))
                return value;

            // Fallback to "en-US" or "en".
            if (i.TryGetValue("en-US", out value) ||
                i.TryGetValue("en", out value))
                return value;

            // Fallback to the first value.
            foreach (var v in i.Values)
                return v;

            return fallback;
        }
    }
}
