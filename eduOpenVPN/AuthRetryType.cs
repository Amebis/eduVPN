/*
    eduOpenVPN - OpenVPN Management Library for eduVPN (and beyond)

    Copyright: 2017-2023 The Commons Conservancy
    SPDX-License-Identifier: GPL-3.0+
*/

namespace eduOpenVPN
{
    /// <summary>
    /// OpenVPN authentication retry policy
    /// </summary>
    public enum AuthRetryType
    {
        /// <summary>
        /// Client will exit with a fatal error (this is the default)
        /// </summary>
        [ParameterValue("none")]
        None = 0,

        /// <summary>
        /// Client will retry the connection without requerying for an --auth-user-pass username/password. Use this option for unattended clients.
        /// </summary>
        [ParameterValue("nointeract")]
        NoInteract,

        /// <summary>
        /// Client will requery for an --auth-user-pass username/password and/or private key password before attempting a reconnection.
        /// </summary>
        [ParameterValue("interact")]
        Interact,
    }
}
