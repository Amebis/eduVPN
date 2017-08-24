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
        /// Connect to the internet securely, for example when using public WiFi.
        /// </summary>
        SecureInternet = 0,

        /// <summary>
        /// Access your institute's network from outside the institution.
        /// </summary>
        InstituteAccess = 1,
    }
}
