/*
    eduVPN - VPN for education and research

    Copyright: 2017-2019 The Commons Conservancy eduVPN Programme
    SPDX-License-Identifier: GPL-3.0+
*/

using System.Security;

namespace eduVPN.Models
{
    /// <summary>
    /// 2-Factor Authentication enrollment credentials
    /// </summary>
    public class TwoFactorEnrollmentCredentials
    {
        #region Properties

        /// <summary>
        /// Token generator response
        /// </summary>
        public SecureString Response { get; set; }

        #endregion
    }
}
