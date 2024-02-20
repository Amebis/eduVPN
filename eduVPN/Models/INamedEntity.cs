/*
    eduVPN - VPN for education and research

    Copyright: 2017-2024 The Commons Conservancy
    SPDX-License-Identifier: GPL-3.0+
*/

using System.Collections.Generic;

namespace eduVPN.Models
{
    /// <summary>
    /// An entitiy with localized display names
    /// </summary>
    public interface INamedEntity
    {
        /// <summary>
        /// Localized display names
        /// </summary>
        Dictionary<string, string> LocalizedDisplayNames { get; }
    }
}
