/*
    eduVPN - VPN for education and research

    Copyright: 2017-2024 The Commons Conservancy
    SPDX-License-Identifier: GPL-3.0+
*/

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
        /// <param name="json">JSON object</param>
        public InstituteAccessServer(Json json) : base(json)
        { }

        #endregion
    }
}
