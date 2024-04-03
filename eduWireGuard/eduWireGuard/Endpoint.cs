/*
    eduWireGuard - WireGuard Tunnel Manager Library for eduVPN

    Copyright: 2022-2024 The Commons Conservancy
    SPDX-License-Identifier: GPL-3.0+
*/

using System.Net;

namespace eduWireGuard
{
    /// <summary>
    /// (host):(port) pair
    /// </summary>
    public class Endpoint
    {
        #region Properties

        /// <summary>
        /// Endpoint host
        /// </summary>
        public string Host;

        /// <summary>
        /// Endpoint port
        /// </summary>
        public ushort Port;

        #endregion

        #region Constructors

        /// <summary>
        /// Constructs an Endpoint
        /// </summary>
        /// <param name="host">Host as hostname or IP address</param>
        /// <param name="port">Port</param>
        public Endpoint(string host, ushort port)
        {
            Host = host;
            Port = port;
        }

        /// <summary>
        /// Constructs an Endpoint
        /// </summary>
        /// <param name="host">Host as IP address</param>
        /// <param name="port">Port</param>
        public Endpoint(IPAddress host, ushort port)
        {
            Host = host.ToString();
            Port = port;
        }

        #endregion

        #region Methods

        public override string ToString()
        {
            return Host + ":" + Port;
        }

        /// <summary>
        /// Checks if host is empty.
        /// </summary>
        /// <returns>true if host is null or empty; false otherwise</returns>
        public bool IsEmpty()
        {
            return string.IsNullOrEmpty(Host);
        }

        #endregion
}
}
