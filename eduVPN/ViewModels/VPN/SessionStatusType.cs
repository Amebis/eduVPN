/*
    eduVPN - VPN for education and research

    Copyright: 2017-2024 The Commons Conservancy
    SPDX-License-Identifier: GPL-3.0+
*/

namespace eduVPN.ViewModels.VPN
{
    /// <summary>
    /// VPN session status type
    /// </summary>
    public enum SessionStatusType
    {
        /// <summary>
        /// Connection state is disconnected (default).
        /// </summary>
        Disconnected = 0,

        /// <summary>
        /// Connection is waiting for other users to sign out.
        /// </summary>
        Waiting,

        /// <summary>
        /// Connection is initializing.
        /// </summary>
        Initializing,

        /// <summary>
        /// Client is connecting.
        /// </summary>
        Connecting,

        /// <summary>
        /// Client is connected.
        /// </summary>
        Connected,

        /// <summary>
        /// Client is disconnecting.
        /// </summary>
        Disconnecting,

        /// <summary>
        /// Connecting failed.
        /// </summary>
        Error
    }
}
