/*
    eduOpenVPN - OpenVPN Management Library for eduVPN (and beyond)

    Copyright: 2017-2024 The Commons Conservancy
    SPDX-License-Identifier: GPL-3.0+
*/

using System;

namespace eduOpenVPN.Management
{
    /// <summary>
    /// <see cref="Session.RemoteReported"/> event arguments
    /// </summary>
    public class RemoteReportedEventArgs : EventArgs
    {
        #region Fields

        /// <summary>
        /// Hostname or IP address
        /// </summary>
        public readonly string Host;

        /// <summary>
        /// IP Port
        /// </summary>
        public readonly int Port;

        /// <summary>
        /// Protocol
        /// </summary>
        public readonly ProtoType Protocol;

        /// <summary>
        /// Required action for the given remote
        /// </summary>
        public RemoteAction Action;

        #endregion

        #region Constructors

        /// <summary>
        /// Constructs an event arguments
        /// </summary>
        /// <param name="host">Hostname or IP address</param>
        /// <param name="port">IP Port</param>
        /// <param name="protocol">Protocol</param>
        public RemoteReportedEventArgs(string host, int port, ProtoType protocol)
        {
            Host = host;
            Port = port;
            Protocol = protocol;

            // Default action is accept.
            Action = new RemoteAcceptAction();
        }

        #endregion
    }
}
