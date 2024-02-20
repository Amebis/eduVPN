/*
    eduVPN - VPN for education and research

    Copyright: 2017-2024 The Commons Conservancy
    SPDX-License-Identifier: GPL-3.0+
*/

namespace eduVPN.Models
{
    /// <summary>
    /// The type/role of eduVPN server
    /// </summary>
    /// <see cref="eduvpn-common/types/server/server.go"/>
    public enum ServerType
    {
        Unknown,
        InstituteAccess,
        SecureInternet,
        Own,
    }
}
