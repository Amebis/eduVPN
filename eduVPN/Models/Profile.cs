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

        #region Methods

        /// <inheritdoc/>
        public override string ToString()
        {
            return LocalizedDisplayNames.Count > 0 ? LocalizedDisplayNames.GetLocalized() : Id;
        }

        #endregion

        #region Utf8Json

        public class Json
        {
            public object display_name { get; set; }
        }

        /// <summary>
        /// Creates profile
        /// </summary>
        /// <param name="id">Profile Id</param>
        /// <param name="json">JSON object</param>
        public Profile(string id, Dictionary<string, object> obj)
        {
            Id = id;
            LocalizedDisplayNames = obj.TryGetValue("display_name", out var display_name) ?
                display_name.ParseLocalized<string>() :
                throw new ArgumentException();
        }

        /// <summary>
        /// Creates profile
        /// </summary>
        /// <param name="id">Profile Id</param>
        /// <param name="json">JSON object</param>
        public Profile(string id, Json json)
        {
            Id = id;
            LocalizedDisplayNames = json.display_name.ParseLocalized<string>();
        }

        #endregion
    }
}
