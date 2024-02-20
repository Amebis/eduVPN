/*
    eduVPN - VPN for education and research

    Copyright: 2017-2024 The Commons Conservancy
    SPDX-License-Identifier: GPL-3.0+
*/

using System.Collections.Generic;

namespace eduVPN.Models
{
    /// <summary>
    /// Institute access server
    /// </summary>
    public class InstituteAccessServer : DiscoverableServer
    {
        #region Properties

        /// <inheritdoc/>
        public override ServerType ServerType { get => ServerType.InstituteAccess; }

        #endregion

        #region Constructors

        /// <summary>
        /// Creates institute access server
        /// </summary>
        /// <param name="obj">Key/value dictionary with <c>identifier</c>, <c>display_name</c>, <c>profiles</c>, <c>delisted</c> elements.</param>
        public InstituteAccessServer(IReadOnlyDictionary<string, object> obj) : base(obj)
        {
        }

        #endregion
    }
}
