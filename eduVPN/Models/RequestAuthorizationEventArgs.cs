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
        #region Data Types

        /// <summary>
        /// Access token retrieval source policy
        /// </summary>
        public enum SourcePolicyType
        {
            /// <summary>
            /// Load access token from settings if available; or authorize otherwise
            /// </summary>
            /// <remarks>This is the default.</remarks>
            Any = 0,

            /// <summary>
            /// Load access token from settings if available
            /// </summary>
            SavedOnly,

            /// <summary>
            /// Authorize client always
            /// </summary>
            ForceAuthorization,
        }

        #endregion

        #region Properties

        /// <summary>
        /// Requested access token scope
        /// </summary>
        public string Scope { get; }

        /// <summary>
        /// Access token retrieval policy
        /// </summary>
        public SourcePolicyType SourcePolicy { get; set; }

        /// <summary>
        /// Access token
        /// </summary>
        /// <remarks>Should be populated by access token on event end, or <c>null</c> if authorization failed.</remarks>
        public AccessToken AccessToken { get; set; }

        #endregion

        #region Constructors

        /// <summary>
        /// Constructs event arguments
        /// </summary>
        /// <param name="scope">Requested access token scope</param>
        public RequestAuthorizationEventArgs(string scope)
        {
            Scope = scope;
        }

        #endregion
    }
}
