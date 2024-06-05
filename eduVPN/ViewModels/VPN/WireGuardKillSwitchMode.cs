/*
    eduVPN - VPN for education and research

    Copyright: 2017-2024 The Commons Conservancy
    SPDX-License-Identifier: GPL-3.0+
*/

namespace eduVPN.ViewModels.VPN
{
    /// <summary>
    /// WireGuard kill-switch mode
    /// </summary>
    public enum WireGuardKillSwitchMode
    {
        /// <summary>
        /// Keep the kill-switch as configured downstream.
        /// </summary>
        Preserve = 0,

        /// <summary>
        /// Turn the kill-switch on for all default-gateway profiles.
        /// </summary>
        Enforce,

        /// <summary>
        /// Turn the kill-switch off for all default-gateway profiles.
        /// </summary>
        Remove,
    }
}
