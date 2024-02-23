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
    /// An eduVPN profile
    /// </summary>
    public class Profile : INamedEntity
    {
        #region Properties

        /// <summary>
        /// Profile ID
        /// </summary>
        public string Id { get; }

        /// <summary>
        /// Localized display names
        /// </summary>
        public Dictionary<string, string> LocalizedDisplayNames { get; }

        #endregion

        #region Constructors

        /// <summary>
        /// Creates profile
        /// </summary>
        /// <param name="id">Profile Id</param>
        /// <param name="obj">Key/value dictionary with <c>display_name</c> and <c>profile_id</c> elements. <c>profile_id</c> is required. <c>display_name</c> and <c>profile_id</c> elements should be strings.</param>
        public Profile(string id, Dictionary<string, object> obj)
        {
            Id = id;
            LocalizedDisplayNames = new Dictionary<string, string>(StringComparer.InvariantCultureIgnoreCase);
            eduJSON.Parser.GetDictionary(obj, "display_name", LocalizedDisplayNames);
        }

        #endregion

        #region Methods

        /// <inheritdoc/>
        public override string ToString()
        {
            return LocalizedDisplayNames.Count > 0 ? LocalizedDisplayNames.GetLocalized() : Id;
        }

        #endregion
    }
}
