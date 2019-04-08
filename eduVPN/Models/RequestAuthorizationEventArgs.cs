/*
    eduVPN - VPN for education and research

    Copyright: 2017-2019 The Commons Conservancy eduVPN Programme
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

        /// <summary>
        /// Token origin type
        /// </summary>
        public enum TokenOriginType
        {
            /// <summary>
            /// No valid access token
            /// </summary>
            /// <remarks>This is the default.</remarks>
            None = 0,

            /// <summary>
            /// Access token was loaded from settings
            /// </summary>
            Saved,

            /// <summary>
            /// Access token was loaded from settings and refreshed
            /// </summary>
            Refreshed,

            /// <summary>
            /// Access token was freshly authorized
            /// </summary>
            Authorized,
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
        /// If the token is to be loaded from the settings, should expiration time be honoured (<c>false</c>) or token refresh be forced (<c>true</c>)
        /// </summary>
        public bool ForceRefresh { get; set; }

        /// <summary>
        /// Access token origin
        /// </summary>
        /// <remarks>Should be set appropriately on event end, or <see cref="TokenOriginType.None"/> if no authorization token available.</remarks>
        public TokenOriginType TokenOrigin { get; set; }

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
