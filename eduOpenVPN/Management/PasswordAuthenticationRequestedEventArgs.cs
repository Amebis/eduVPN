/*
    eduOpenVPN - OpenVPN Management Library for eduVPN (and beyond)

    Copyright: 2017-2024 The Commons Conservancy
    SPDX-License-Identifier: GPL-3.0+
*/

using System.Security;

namespace eduOpenVPN.Management
{
    /// <summary>
    /// Authentication events base class
    /// </summary>
    public class PasswordAuthenticationRequestedEventArgs : AuthenticationEventArgs
    {
        #region Properties

        /// <summary>
        /// Password
        /// </summary>
        public SecureString Password { get; set; }

        #endregion

        #region Constructors

        /// <summary>
        /// Constructs an event arguments
        /// </summary>
        /// <param name="realm">Realm title</param>
        public PasswordAuthenticationRequestedEventArgs(string realm) :
            base(realm)
        {
        }

        #endregion
    }
}
