/*
    eduVPN - End-user friendly VPN

    Copyright: 2017, The Commons Conservancy eduVPN Programme
    SPDX-License-Identifier: GPL-3.0+
*/

namespace eduVPN.JSON
{
    /// <summary>
    /// eduVPN message type
    /// </summary>
    public enum MessageType
    {
        /// <summary>
        /// A plain-text message (default)
        /// </summary>
        Notification = 0,

        /// <summary>
        /// A plain text "message of the day" (MotD) of the service, to be displayed to users on login or when establishing a connection to the VPN
        /// </summary>
        MotD,

        /// <summary>
        /// Scheduled maintenance
        /// </summary>
        Maintenance
    }
}