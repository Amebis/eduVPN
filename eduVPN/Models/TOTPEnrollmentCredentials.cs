/*
    eduVPN - VPN for education and research

    Copyright: 2017-2019 The Commons Conservancy eduVPN Programme
    SPDX-License-Identifier: GPL-3.0+
*/

using System.Security;

namespace eduVPN.Models
{
    /// <summary>
    /// TOTP enrollment credentials
    /// </summary>
    public class TOTPEnrollmentCredentials : TwoFactorEnrollmentCredentials
    {
        #region Properties

        /// <summary>
        /// TOTP secret
        /// </summary>
        public SecureString Secret { get; set; }

        #endregion
    }
}
