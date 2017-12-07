/*
    eduVPN - End-user friendly VPN

    Copyright: 2017, The Commons Conservancy eduVPN Programme
    SPDX-License-Identifier: GPL-3.0+
*/

using System;

namespace eduVPN.Models
{
    /// <summary>
    /// <c>RequestTwoFactorEnrollment</c> event parameters
    /// </summary>
    public class RequestTwoFactorEnrollmentEventArgs : EventArgs
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
        /// Constructs event parameters
        /// </summary>
        /// <param name="enrollment_uri">Enrollment URI</param>
        /// <param name="profile">Connecting profile</param>
        public RequestTwoFactorEnrollmentEventArgs(Uri enrollment_uri, Profile profile)
        {
            EnrollmentUri = enrollment_uri;
            Profile = profile;
        }

        #endregion
    }
}