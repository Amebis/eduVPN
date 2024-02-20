/*
    eduOpenVPN - OpenVPN Management Library for eduVPN (and beyond)

    Copyright: 2017-2024 The Commons Conservancy
    SPDX-License-Identifier: GPL-3.0+
*/

using System;
using System.Net;

namespace eduOpenVPN.Management
{
    /// <summary>
    /// <see cref="Session.StateReported"/> event arguments
    /// </summary>
    public class StateReportedEventArgs : TimestampedEventArgs
    {
        #region Properties

        /// <summary>
        /// OpenVPN state
        /// </summary>
        public OpenVPNStateType State { get; }

        /// <summary>
        /// Descriptive string (optional)
        /// </summary>
        public string Message { get; }

        /// <summary>
        /// TUN/TAP local IPv4 address (optional)
        /// </summary>
        public IPAddress Tunnel { get; }

        /// <summary>
        /// TUN/TAP local IPv6 address (optional)
        /// </summary>
        public IPAddress IPv6Tunnel { get; }

        /// <summary>
        /// Remote server address and port (optional)
        /// </summary>
        public IPEndPoint Remote { get; }

        /// <summary>
        /// Local address and port (optional)
        /// </summary>
        public IPEndPoint Local { get; }

        #endregion

        #region Constructors

        /// <summary>
        /// Constructs an event arguments
        /// </summary>
        /// <param name="timestamp">Timestamp of the state</param>
        /// <param name="state">OpenVPN state</param>
        /// <param name="message">Descriptive string (optional)</param>
        /// <param name="tunnel">TUN/TAP local IPv4 address (optional)</param>
        /// <param name="ipv6Tunnel">TUN/TAP local IPv6 address (optional)</param>
        /// <param name="remote">Remote server address and port (optional)</param>
        /// <param name="local">Local address and port (optional)</param>
        public StateReportedEventArgs(DateTimeOffset timestamp, OpenVPNStateType state, string message, IPAddress tunnel, IPAddress ipv6Tunnel, IPEndPoint remote, IPEndPoint local) :
            base(timestamp)
        {
            State = state;
            Message = message;
            Tunnel = tunnel;
            IPv6Tunnel = ipv6Tunnel;
            Remote = remote;
            Local = local;
        }

        #endregion
    }
}
