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
    /// Proxyguard configuration
    /// </summary>
    public class ProxyguardConfiguration
    {
        #region Properties

        /// <summary>
        /// Source port for the client TCP connection
        /// </summary>
        public int SourcePort { get; }

        /// <summary>
        /// The ip:port for the client UDP connection, this is the value that is replaced in the config
        /// </summary>
        public string Listen { get; }

        /// <summary>
        /// URI of the upstream server
        /// </summary>
        /// <remarks>Note that this exactly matches the "ProxyEndpoint" key in the WireGuard config</remarks>
        public Uri Peer;

        #endregion

        #region Constructors

        /// <summary>
        /// Creates Proxyguard configuration
        /// </summary>
        /// <param name="obj">Key/value dictionary with <c>source_port</c>, <c>listen</c> and <c>peer</c> elements.</param>
        public ProxyguardConfiguration(IReadOnlyDictionary<string, object> obj)
        {
            SourcePort = eduJSON.Parser.GetValue(obj, "source_port", out long source_port) && 0 <= source_port && source_port <= 0xffff ? (int)source_port : 0;
            Listen = eduJSON.Parser.GetValue<string>(obj, "listen");
            Peer = new Uri(eduJSON.Parser.GetValue<string>(obj, "peer"));
        }

        #endregion
    }
}
