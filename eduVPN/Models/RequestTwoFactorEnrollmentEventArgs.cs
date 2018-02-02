/*
    eduVPN - VPN for education and research

    Copyright: 2017, The Commons Conservancy eduVPN Programme
    SPDX-License-Identifier: GPL-3.0+
*/

using System;

namespace eduVPN.Models
{
    /// <summary>
    /// <see cref="ViewModels.Windows.ConnectWizard.RequestTwoFactorEnrollment"/> event parameters
    /// </summary>
    public class RequestTwoFactorEnrollmentEventArgs : EventArgs
    {
        #region Properties

        /// <summary>
        /// User
        /// </summary>
        public UserInfo User { get; }

        /// <summary>
        /// Authenticating instance
        /// </summary>
        public Instance AuthenticatingInstance { get; }

        /// <summary>
        /// Connecting profile
        /// </summary>
        public Profile Profile { get; }

        /// <summary>
        /// 2-Factor Authentication enrollment credentials
        /// </summary>
        /// <remarks>Should be set to selected 2-Factor Authentication method credentials.</remarks>
        public TwoFactorEnrollmentCredentials Credentials { get; set; }

        #endregion

        #region Constructors

        /// <summary>
        /// Constructs event parameters
        /// </summary>
        /// <param name="user">User</param>
        /// <param name="authenticating_instance">Authenticating instance</param>
        /// <param name="profile">Connecting profile</param>
        public RequestTwoFactorEnrollmentEventArgs(UserInfo user, Instance authenticating_instance, Profile profile)
        {
            User = user;
            AuthenticatingInstance = authenticating_instance;
            Profile = profile;
        }

        #endregion
    }
}
