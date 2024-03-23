/*
    eduVPN - VPN for education and research

    Copyright: 2017-2024 The Commons Conservancy
    SPDX-License-Identifier: GPL-3.0+
*/

using System;

namespace eduVPN.Models
{
    /// <summary>
    /// AutoReconnectFailed event arguments
    /// </summary>
    public class AutoReconnectFailedEventArgs : EventArgs
    {
        #region Fields

        /// <summary>
        /// Authenticating server
        /// </summary>
        public readonly Server AuthenticatingServer;

        /// <summary>
        /// Connecting server
        /// </summary>
        public readonly Server ConnectingServer;

        #endregion

        #region Constructors

        /// <summary>
        /// Constructs an event arguments
        /// </summary>
        /// <param name="authenticatingServer">Authenticating server</param>
        /// <param name="connectingServer">Connecting server</param>
        public AutoReconnectFailedEventArgs(Server authenticatingServer, Server connectingServer)
        {
            AuthenticatingServer = authenticatingServer;
            ConnectingServer = connectingServer;
        }

        #endregion
    }
}
