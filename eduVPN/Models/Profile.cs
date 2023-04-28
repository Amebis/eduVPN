/*
    eduVPN - VPN for education and research

    Copyright: 2017-2023 The Commons Conservancy
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

        /// <summary>
        /// List of supported VPN protocols
        /// </summary>
        public HashSet<VPNProtocol> SupportedProtocols { get; }

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
            if (eduJSON.Parser.GetValue(obj, "supported_protocols", out List<object> vpnProtoList) && vpnProtoList != null)
            {
                SupportedProtocols = new HashSet<VPNProtocol>();
                foreach (var e in vpnProtoList)
                    if (e is long l)
                        SupportedProtocols.Add((VPNProtocol)l);
            }
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
