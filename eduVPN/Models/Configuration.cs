/*
    eduVPN - VPN for education and research

    Copyright: 2017-2024 The Commons Conservancy
    SPDX-License-Identifier: GPL-3.0+
*/

using System.Collections.Generic;
using System.Text.RegularExpressions;

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

        /// <summary>
        /// Information for proxied VPN connections
        /// </summary>
        public ProxyguardConfiguration ProxyguardConfiguration { get; }

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
            if (eduJSON.Parser.GetValue<Dictionary<string, object>>(obj, "proxy", out var obj2))
            {
                ProxyguardConfiguration = new ProxyguardConfiguration(obj2);

                // Locate "Endpoint = <ProxyguardConfiguration.Listen>" and append "\nProxyEndpoint = <ProxyguardConfiguration.Peer>" to it.
                VPNConfig = Regex.Replace(VPNConfig, @"^\s*Endpoint\s*=\s*(.*)$", delegate (Match m)
                {
                    if (string.Compare(m.Groups[1].Value, ProxyguardConfiguration.Listen, true) == 0)
                        return m.Value + "\nProxyEndpoint = " + ProxyguardConfiguration.Peer;
                    return m.Value;
                }, RegexOptions.Multiline | RegexOptions.IgnoreCase);
            }
        }

        #endregion
    }
}
