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
    /// Institute access server
    /// </summary>
    public class DiscoverableServer : Server, IEntityWithKeywords
    {
        #region Properties

        /// <summary>
        /// Was this server delisted from discovery?
        /// </summary>
        public bool Delisted { get; }

        /// <summary>
        /// Localized keyword sets
        /// </summary>
        public Dictionary<string, HashSet<string>> LocalizedKeywordSets { get; }

        #endregion

        #region Constructors

        /// <summary>
        /// Constructs a secure internet/institute access server
        /// </summary>
        /// <param name="id">Server/organization identifier as used by eduvpn-common</param>
        protected DiscoverableServer(string id) : base(id)
        { }

        #endregion

        #region Utf8Json

        /// <summary>
        /// Constructs a secure internet/institute access server
        /// </summary>
        /// <param name="json">JSON object</param>
        public DiscoverableServer(Json json) : base(json)
        {
            Delisted = json.delisted;
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
