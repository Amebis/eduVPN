/*
    eduVPN - End-user friendly VPN

    Copyright: 2017, The Commons Conservancy eduVPN Programme
    SPDX-License-Identifier: GPL-3.0+
*/

using eduOAuth;
using System;

namespace eduVPN.ViewModels
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
        public Models.InstanceInfo Instance { get; }

        /// <summary>
        /// Access token
        /// </summary>
        /// <remarks>Should be populated by access token on event end, or <c>null</c> if authorization failed.</remarks>
        public AccessToken AccessToken { get; set; }

        #endregion

        #region Constructors

        /// <summary>
        /// Constructs an RequestInstanceAuthorization event arguments
        /// </summary>
        /// <param name="instance"></param>
        public RequestInstanceAuthorizationEventArgs(Models.InstanceInfo instance)
        {
            Instance = instance;
        }

        #endregion
    }
}
