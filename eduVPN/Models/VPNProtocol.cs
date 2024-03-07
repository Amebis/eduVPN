/*
    eduVPN - VPN for education and research

    Copyright: 2017-2024 The Commons Conservancy
    SPDX-License-Identifier: GPL-3.0+
*/

namespace eduVPN.Models
{
    /// <summary>
    /// Known VPN protocols
    /// </summary>
    /// <see cref="eduvpn-common/types/protocol/protocol.go"/>
    public enum VPNProtocol
    {
        Unknown,

        /// <summary>
        /// OpenVPN
        /// </summary>
        OpenVPN,

        /// <summary>
        /// WireGuard
        /// </summary>
        WireGuard,

        /// <summary>
        /// WireGuard with Proxyguard
        /// </summary>
        WireGuardProxy
    }
}
