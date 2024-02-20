/*
    eduVPN - VPN for education and research

    Copyright: 2017-2024 The Commons Conservancy
    SPDX-License-Identifier: GPL-3.0+
*/

using System;
using System.Collections.Generic;

namespace eduVPN.Models
{
    /// <summary>
    /// An organization
    /// </summary>
    public class Organization : INamedEntity, IEntityWithKeywords
    {
        #region Properties

        /// <summary>
        /// Organization identifier
        /// </summary>
        public string Id { get; }

        /// <summary>
        /// Secure Internet home server base URI
        /// </summary>
        public Uri SecureInternetBase { get; }

        /// <summary>
        /// Localized display names
        /// </summary>
        public Dictionary<string, string> LocalizedDisplayNames { get; }

        /// <summary>
        /// Localized keyword sets
        /// </summary>
        public Dictionary<string, HashSet<string>> LocalizedKeywordSets { get; }

        #endregion

        #region Constructors

        /// <summary>
        /// Creates organization
        /// </summary>
        /// <param name="obj">Key/value dictionary with <c>display_name</c>, <c>org_id</c>, <c>secure_internet_home</c>, and <c>keyword_list</c> elements.</param>
        public Organization(IReadOnlyDictionary<string, object> obj)
        {
            Id = eduJSON.Parser.GetValue<string>(obj, "org_id");
            SecureInternetBase = eduJSON.Parser.GetValue(obj, "secure_internet_home", out string secureInternetHome) && secureInternetHome != null ? new Uri(secureInternetHome) : null;
            LocalizedDisplayNames = new Dictionary<string, string>(StringComparer.InvariantCultureIgnoreCase);
            eduJSON.Parser.GetDictionary(obj, "display_name", LocalizedDisplayNames);
            LocalizedKeywordSets = new Dictionary<string, HashSet<string>>(StringComparer.InvariantCultureIgnoreCase);
            var keywordList = new Dictionary<string, string>();
            if (eduJSON.Parser.GetDictionary(obj, "keyword_list", keywordList))
                foreach (var keywords in keywordList)
                {
                    var hash = new HashSet<string>(StringComparer.InvariantCultureIgnoreCase);
                    foreach (var keyword in keywords.Value.Split())
                        hash.Add(keyword);
                    LocalizedKeywordSets.Add(keywords.Key, hash);
                }
        }

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
    }
}
