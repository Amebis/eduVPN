/*
    eduVPN - End-user friendly VPN

    Copyright: 2017, The Commons Conservancy eduVPN Programme
    SPDX-License-Identifier: GPL-3.0+
*/

namespace eduVPN.Models
{
    /// <summary>
    /// Status type
    /// </summary>
    public enum StatusType
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
        Connected
    }
}
