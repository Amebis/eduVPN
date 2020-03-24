/*
    eduVPN - VPN for education and research

    Copyright: 2017-2020 The Commons Conservancy eduVPN Programme
    SPDX-License-Identifier: GPL-3.0+
*/

using eduOAuth;
using System;

namespace eduVPN.Models
{
    /// <summary>
    /// RequestInstanceAuthorization event arguments
    /// </summary>
    public class RequestInstanceAuthorizationEventArgs : EventArgs
    {
        #region Properties

        /// <summary>
        /// Authenticating instance
        /// </summary>
        public Instance Instance { get; }

        /// <summary>
        /// Authorization grant
        /// </summary>
        public AuthorizationGrant AuthorizationGrant { get; }

        /// <summary>
        /// Callback URI
        /// </summary>
        /// <remarks>Should be populated by callback URI on event end, or <c>null</c> if cancelled.</remarks>
        public Uri CallbackURI { get; set; }

        #endregion

        #region Constructors

        /// <summary>
        /// Constructs event arguments
        /// </summary>
        /// <param name="instance">Authenticating instance</param>
        /// <param name="authorization_grant">Authorization grant</param>
        public RequestInstanceAuthorizationEventArgs(Instance instance, AuthorizationGrant authorization_grant)
        {
            Instance = instance;
            AuthorizationGrant = authorization_grant;
        }

        #endregion
    }
}
