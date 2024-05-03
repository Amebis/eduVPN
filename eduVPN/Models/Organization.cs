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

        #region Methods

        protected Organization() { }

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

        #region Utf8Json

        public class Json
        {
            public string org_id { get; set; }
            public Uri secure_internet_home { get; set; }
            public object display_name { get; set; }
            public object keyword_list { get; set; }
        }

        /// <summary>
        /// Creates organization
        /// </summary>
        /// <param name="json">JSON object</param>
        public Organization(Json json)
        {
            if (json.org_id == null)
                throw new ArgumentException();
            Id = json.org_id;
            SecureInternetBase = json.secure_internet_home;
            LocalizedDisplayNames = json.display_name.ParseLocalized<string>();
            LocalizedKeywordSets = new Dictionary<string, HashSet<string>>(StringComparer.InvariantCultureIgnoreCase);
            foreach (var keywords in json.keyword_list.ParseLocalized<string>())
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
