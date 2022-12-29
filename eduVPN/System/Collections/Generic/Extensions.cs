/*
    eduVPN - VPN for education and research

    Copyright: 2017-2023 The Commons Conservancy
    SPDX-License-Identifier: GPL-3.0+
*/

using eduVPN.Models;

namespace System.Collections.Generic
{
    /// <summary>
    /// <see cref="Generic"/> namespace extension methods
    /// </summary>
    public static class Extensions
    {
        /// <summary>
        /// Adds element to index
        /// </summary>
        /// <param name="idx">Index</param>
        /// <param name="el">Element</param>
        public static void Index<T>(this Dictionary<string, HashSet<T>> idx, T el) where T : INamedEntity
        {
            // Index element's ToString() (e.g. DisplayName).
            foreach (var word in el.ToString().Split())
                idx.Index(word, el);

            // Index localized display names.
            foreach (var displayName in el.LocalizedDisplayNames)
                foreach (var word in displayName.Value.Split())
                    idx.Index(word, el);

            // Index localized keywords.
            foreach (var keywordSet in el.LocalizedKeywordSets)
                foreach (var keyword in keywordSet.Value)
                    idx.Index(keyword, el);
        }

        /// <summary>
        /// Adds element to the index by subkeys
        /// </summary>
        /// <param name="idx">Index</param>
        /// <param name="str">String to be sub-keyed</param>
        /// <param name="el">Element</param>
        private static void Index<T>(this Dictionary<string, HashSet<T>> idx, string str, T el)
        {
            if (str.Length > 2)
            {
                for (var start = 0; start < str.Length - 2; ++start)
                    for (var end = start + 2; end <= str.Length; ++end)
                        idx.Add(str.Substring(start, end - start), el);
            }
            else
                idx.Add(str, el);
        }

        /// <summary>
        /// Adds element to the index by key
        /// </summary>
        /// <param name="idx">Index</param>
        /// <param name="key">Key</param>
        /// <param name="el">Element</param>
        /// <returns>true if the object is added to the index; false if the object is already present.</returns>
        private static bool Add<T>(this Dictionary<string, HashSet<T>> idx, string key, T el)
        {
            if (idx.TryGetValue(key, out var set))
                return set.Add(el);
            else
            {
                idx[key] = new HashSet<T>() { el };
                return true;
            }
        }
    }
}
