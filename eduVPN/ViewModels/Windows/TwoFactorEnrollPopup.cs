/*
    eduVPN - End-user friendly VPN

    Copyright: 2017, The Commons Conservancy eduVPN Programme
    SPDX-License-Identifier: GPL-3.0+
*/

using eduVPN.Models;
using System;

namespace eduVPN.ViewModels.Windows
{
    /// <summary>
    /// 2-Factor Authentication enrollment pop-up
    /// </summary>
    public class TwoFactorEnrollPopup : Window
    {
        #region Properties

        /// <summary>
        /// Enrollment URI
        /// </summary>
        public Uri EnrollmentUri { get; }

        /// <summary>
        /// Connecting profile
        /// </summary>
        public Profile Profile { get; }

        #endregion

        #region Constructors

        /// <summary>
        /// Creates a pop-up window
        /// </summary>
        /// <param name="sender">Profile select panel</param>
        /// <param name="e">Event parameters</param>
        public TwoFactorEnrollPopup(object sender, RequestTwoFactorEnrollmentEventArgs e)
        {
            EnrollmentUri = e.EnrollmentUri;
            Profile = e.Profile;
        }

        #endregion
    }
}
