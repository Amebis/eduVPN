/*
    eduOpenVPN - OpenVPN Management Library for eduVPN (and beyond)

    Copyright: 2017-2024 The Commons Conservancy
    SPDX-License-Identifier: GPL-3.0+
*/

using System;

namespace eduOpenVPN.Management
{
    /// <summary>
    /// Authentication events base class
    /// </summary>
    public class AuthenticationEventArgs : EventArgs
    {
        #region Fields

        /// <summary>
        /// Realm title
        /// </summary>
        public readonly string Realm;

        #endregion

        #region Constructors

        /// <summary>
        /// Constructs an event arguments
        /// </summary>
        /// <param name="realm">Realm title</param>
        public AuthenticationEventArgs(string realm)
        {
            Realm = realm;
        }

        #endregion
    }
}
