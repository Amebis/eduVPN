/*
    eduVPN - End-user friendly VPN

    Copyright: 2017, The Commons Conservancy eduVPN Programme
    SPDX-License-Identifier: GPL-3.0+
*/

namespace eduVPN.ViewModels.VPN
{
    /// <summary>
    /// VPN session status type
    /// </summary>
    public enum VPNSessionStatusType
    {
        /// <summary>
        /// Connection is initializing (default).
        /// </summary>
        Initializing = 0,

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
