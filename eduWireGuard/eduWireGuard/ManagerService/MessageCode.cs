/*
    eduWireGuard - WireGuard Tunnel Manager Library for eduVPN

    Copyright: 2022-2024 The Commons Conservancy
    SPDX-License-Identifier: GPL-3.0+
*/

namespace eduWireGuard.ManagerService
{
    /// <summary>
    /// WireGuard Tunnel Manager Service message codes
    /// </summary>
    public enum MessageCode
    {
        Status,
        ActivateTunnel,
        DeactivateTunnel,
        GetTunnelConfig,
        TunnelConfig,
    }
}
