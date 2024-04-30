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

        #region Utf8Json

        public class Json
        {
            public string config { get; set; }
            public long protocol { get; set; }
            public bool default_gateway { get; set; }
            public List<string> dns_search_domains { get; set; }
            public bool should_failover { get; set; }
            public ProxyguardConfiguration.Json proxy { get; set; }
        }

        /// <summary>
        /// Creates VPN configuration
        /// </summary>
        /// <param name="json">JSON object</param>
        public Configuration(Json json)
        {
            VPNConfig = json.config;
            Protocol = (VPNProtocol)json.protocol;
            IsDefaultGateway = json.default_gateway;
            ShouldFailover = json.should_failover;
            if (json.proxy != null)
            {
                ProxyguardConfiguration = new ProxyguardConfiguration(json.proxy);

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
