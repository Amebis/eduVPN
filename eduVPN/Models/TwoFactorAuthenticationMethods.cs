/*
    eduVPN - VPN for education and research

    Copyright: 2017, The Commons Conservancy eduVPN Programme
    SPDX-License-Identifier: GPL-3.0+
*/

using System;

namespace eduVPN.Models
{
    /// <summary>
    /// 2-Factor Authentication Methods
    /// </summary>
    [Flags]
    public enum TwoFactorAuthenticationMethods
    {
        /// <summary>
        /// No 2FA support (default)
        /// </summary>
        None = 0,

        /// <summary>
        /// Any 2FA method
        /// </summary>
        Any = TOTP | YubiKey,

        /// <summary>
        /// Time-based One-time Password
        /// </summary>
        TOTP = (1 << 0), // 1

        /// <summary>
        /// YubiKey Device
        /// </summary>
        YubiKey = (1 << 1), // 2
    }
}
