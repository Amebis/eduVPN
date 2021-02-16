/*
    eduVPN - VPN for education and research

    Copyright: 2017-2021 The Commons Conservancy eduVPN Programme
    SPDX-License-Identifier: GPL-3.0+
*/

using System;
using System.Collections.Generic;

namespace eduVPN.Models
{
    /// <summary>
    /// An organization
    /// </summary>
    public class Organization : JSON.ILoadableItem, INamedEntity
    {
        #region Properties

        /// <summary>
        /// Organization identifier
        /// </summary>
        public string Id { get; private set; }

        /// <summary>
        /// Secure Internet home server base URI
        /// </summary>
        public Uri SecureInternetBase { get; private set; }

        /// <summary>
        /// Localized display names
        /// </summary>
        public Dictionary<string, string> LocalizedDisplayNames { get; } = new Dictionary<string, string>(StringComparer.InvariantCultureIgnoreCase);

        /// <summary>
        /// Localized keyword sets
        /// </summary>
        public Dictionary<string, HashSet<string>> LocalizedKeywordSets { get; } = new Dictionary<string, HashSet<string>>(StringComparer.InvariantCultureIgnoreCase);

        #endregion

        #region Methods

        /// <inheritdoc/>
        public override string ToString()
        {
            return LocalizedDisplayNames.GetLocalized(Id);
        }

        /// <inheritdoc/>
        public override bool Equals(object obj)
        {
            if (this == obj)
                return true;
            if (obj == null || GetType() != obj.GetType())
                return false;

            var other = obj as Organization;
            if (!Id.Equals(other.Id))
                return false;

            return true;
        }

        /// <inheritdoc/>
        public override int GetHashCode()
        {
            return Id.GetHashCode();
        }

        #endregion

        #region ILoadableItem Support

        /// <summary>
        /// Loads organization from a dictionary object (provided by JSON)
        /// </summary>
        /// <param name="obj">Key/value dictionary with <c>display_name</c>, <c>org_id</c>, <c>secure_internet_home</c>, and <c>keyword_list</c> elements.</param>
        /// <exception cref="eduJSON.InvalidParameterTypeException"><paramref name="obj"/> type is not <c>Dictionary&lt;string, object&gt;</c></exception>
        public virtual void Load(object obj)
        {
            if (!(obj is Dictionary<string, object> obj2))
                throw new eduJSON.InvalidParameterTypeException(nameof(obj), typeof(Dictionary<string, object>), obj.GetType());

            // Set organization identifier.
            Id = eduJSON.Parser.GetValue<string>(obj2, "org_id");

            // Set secure internet home server base URI.
            SecureInternetBase = new Uri(eduJSON.Parser.GetValue<string>(obj2, "secure_internet_home"));

            // Set display name.
            eduJSON.Parser.GetDictionary(obj2, "display_name", LocalizedDisplayNames);

            // Set keyword list.
            LocalizedKeywordSets.Clear();
            var keywordList = new Dictionary<string, string>();
            if (eduJSON.Parser.GetDictionary(obj2, "keyword_list", keywordList))
                foreach (var keywords in keywordList)
                {
                    var hash = new HashSet<string>(StringComparer.InvariantCultureIgnoreCase);
                    foreach (var keyword in keywords.Value.Split())
                        hash.Add(keyword);
                    LocalizedKeywordSets.Add(keywords.Key, hash);
                }
        }

        #endregion
    }
}
