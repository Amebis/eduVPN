/*
    eduOpenVPN - OpenVPN Management Library for eduVPN (and beyond)

    Copyright: 2017-2023 The Commons Conservancy
    SPDX-License-Identifier: GPL-3.0+
*/

namespace eduOpenVPN.Management
{
    /// <summary>
    /// Username and password authentication request event arguments
    /// </summary>
    public class UsernamePasswordAuthenticationRequestedEventArgs : PasswordAuthenticationRequestedEventArgs
    {
        #region Properties

        /// <summary>
        /// Username
        /// </summary>
        public string Username { get; set; }

        #endregion

        #region Constructors

        /// <summary>
        /// Constructs an event arguments
        /// </summary>
        /// <param name="realm">Realm title</param>
        public UsernamePasswordAuthenticationRequestedEventArgs(string realm) :
            base(realm)
        {
        }

        #endregion
    }
}
