/*
    eduVPN - VPN for education and research

    Copyright: 2017-2024 The Commons Conservancy
    SPDX-License-Identifier: GPL-3.0+
*/

namespace eduVPN.ViewModels.VPN
{
    /// <summary>
    /// The reason session was terminated
    /// </summary>
    public enum TerminationReason
    {
        /// <summary>
        /// An exception occurred (including user cancellation).
        /// </summary>
        Failed,

        /// <summary>
        /// Session expired.
        /// </summary>
        Expired,

        /// <summary>
        /// User asked to renew the session.
        /// </summary>
        Renew,

        /// <summary>
        /// Another user signed in.
        /// </summary>
        AnotherUser,

        /// <summary>
        /// Tunnel failover test failed.
        /// </summary>
        TunnelFailover,
    }
}
