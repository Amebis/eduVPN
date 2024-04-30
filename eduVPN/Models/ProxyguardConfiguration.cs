/*
    eduVPN - VPN for education and research

    Copyright: 2017-2024 The Commons Conservancy
    SPDX-License-Identifier: GPL-3.0+
*/

using System;

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

        #region Utf8Json

        public class Json
        {
            public int source_port;
            public string listen;
            public string peer;
        }

        /// <summary>
        /// Creates Proxyguard configuration
        /// </summary>
        /// <param name="json">JSON object</param>
        public ProxyguardConfiguration(Json json)
        {
            SourcePort = 0 <= json.source_port && json.source_port <= 0xffff ? json.source_port : 0;
            Listen = json.listen;
            Peer = new Uri(json.peer);
        }

        #endregion
    }
}
