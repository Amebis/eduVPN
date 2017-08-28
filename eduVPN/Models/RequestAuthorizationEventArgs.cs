/*
    eduVPN - End-user friendly VPN

    Copyright: 2017, The Commons Conservancy eduVPN Programme
    SPDX-License-Identifier: GPL-3.0+
*/

using eduOAuth;
using System;

namespace eduVPN.Models
{
    /// <summary>
    /// RequestAuthorization event arguments
    /// </summary>
    public class RequestAuthorizationEventArgs : EventArgs
    {
        #region Properties

        /// <summary>
        /// Access token
        /// </summary>
        /// <remarks>Should be populated by access token on event end, or <c>null</c> if authorization failed.</remarks>
        public AccessToken AccessToken { get; set; }

        #endregion
    }
}
