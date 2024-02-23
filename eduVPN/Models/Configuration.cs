/*
    eduVPN - VPN for education and research

    Copyright: 2017-2024 The Commons Conservancy
    SPDX-License-Identifier: GPL-3.0+
*/

using System.Collections.Generic;

namespace eduVPN.Models
{
    /// <summary>
    /// VPN configuration
    /// </summary>
    public class Configuration
    {
        #region Properties

        /// <summary>
        /// VPN configuration
        /// </summary>
        public string VPNConfig { get; }

        /// <summary>
        /// VPN protocol
        /// </summary>
        public VPNProtocol Protocol { get; }

        /// <summary>
        /// Is default gateway?
        /// </summary>
        public bool IsDefaultGateway { get; }

        /// <summary>
        /// Should perform failover test?
        /// </summary>
        public bool ShouldFailover { get; }

        #endregion

        #region Constructors

        /// <summary>
        /// Creates VPN configuration
        /// </summary>
        /// <param name="obj">Key/value dictionary with <c>config</c>, <c>protocol</c>, <c>default_gateway</c> and <c>should_failover</c> elements.</param>
        public Configuration(IReadOnlyDictionary<string, object> obj)
        {
            VPNConfig = eduJSON.Parser.GetValue<string>(obj, "config");
            Protocol = (VPNProtocol)eduJSON.Parser.GetValue<long>(obj, "protocol");
            IsDefaultGateway = eduJSON.Parser.GetValue<bool>(obj, "default_gateway");
            ShouldFailover = eduJSON.Parser.GetValue<bool>(obj, "should_failover");
        }

        #endregion
    }
}
