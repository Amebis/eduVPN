/*
    eduVPN - End-user friendly VPN

    Copyright: 2017, The Commons Conservancy eduVPN Programme
    SPDX-License-Identifier: GPL-3.0+
*/


namespace eduVPN.Models
{
    /// <summary>
    /// Instance source type
    /// </summary>
    public enum InstanceSourceType
    {
        /// <summary>
        /// Unknown (default)
        /// </summary>
        _unknown = 0,

        /// <summary>
        /// Connect to the internet securely, for example when using public WiFi.
        /// </summary>
        SecureInternet = 1,

        /// <summary>
        /// Access your institute's network from outside the institution.
        /// </summary>
        InstituteAccess = 2,

        /// <summary>
        /// Start-of-enum
        /// </summary>
        _start = 1,

        /// <summary>
        /// End-of-enum
        /// </summary>
        _end = 3
    }
}
