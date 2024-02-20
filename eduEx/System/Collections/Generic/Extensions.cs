/*
    eduVPN - VPN for education and research

    Copyright: 2017-2024 The Commons Conservancy
    SPDX-License-Identifier: GPL-3.0+
*/

using System.Linq;
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
        public static T GetLocalized<T>(this IReadOnlyDictionary<string, T> i, T fallback = default)
        {
            // Get value according to thread UI culture.
            if (i.TryGetValue(Thread.CurrentThread.CurrentUICulture.IetfLanguageTag, out var value) ||
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

        /// <summary>
        /// Removes multiple elements from the dictionary
        /// </summary>
        /// <typeparam name="TKey">The type of the keys in the dictionary</typeparam>
        /// <typeparam name="TValue">The type of the values in the dictionary</typeparam>
        /// <param name="i">Dictionary to remove elements from</param>
        /// <param name="predicate">Function that returns true for elements to delete or false to keep</param>
        public static void RemoveAll<TKey, TValue>(this IDictionary<TKey, TValue> i, Func<TValue, bool> predicate)
        {
            foreach (var key in i.Keys.Where(k => predicate(i[k])).ToList())
                i.Remove(key);
        }
    }
}
