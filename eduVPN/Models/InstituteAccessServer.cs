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
    public class InstituteAccessServer : Server, INamedEntity
    {
        #region Properties

        /// <summary>
        /// Localized display names
        /// </summary>
        public Dictionary<string, string> LocalizedDisplayNames { get; } = new Dictionary<string, string>(StringComparer.InvariantCultureIgnoreCase);

        /// <summary>
        /// Localized keyword sets
        /// </summary>
        public Dictionary<string, HashSet<string>> LocalizedKeywordSets { get; } = new Dictionary<string, HashSet<string>>(StringComparer.InvariantCultureIgnoreCase);

        #endregion

        #region Constructors

        /// <summary>
        /// Constructs a server
        /// </summary>
        public InstituteAccessServer() : base()
        {
        }

        /// <summary>
        /// Constructs an Institute Access server manually
        /// </summary>
        /// <param name="b">Server base URI</param>
        public InstituteAccessServer(Uri b) : base(b)
        {
        }

        #endregion

        #region Methods

        /// <inheritdoc/>
        public override string ToString()
        {
            return LocalizedDisplayNames.Count > 0 ? LocalizedDisplayNames.GetLocalized() : Base.Host;
        }

        #endregion

        #region ILoadableItem Support

        /// <summary>
        /// Loads institute access server from a dictionary object (provided by JSON)
        /// </summary>
        /// <param name="obj">Key/value dictionary with <c>display_name</c>, <c>keyword_list</c> and other elements.</param>
        /// <exception cref="eduJSON.InvalidParameterTypeException"><paramref name="obj"/> type is not <c>Dictionary&lt;string, object&gt;</c></exception>
        public override void Load(object obj)
        {
            if (!(obj is Dictionary<string, object> obj2))
                throw new eduJSON.InvalidParameterTypeException(nameof(obj), typeof(Dictionary<string, object>), obj.GetType());

            base.Load(obj);

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
