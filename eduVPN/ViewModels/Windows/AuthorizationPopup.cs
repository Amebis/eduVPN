/*
    eduVPN - VPN for education and research

    Copyright: 2017-2018 The Commons Conservancy eduVPN Programme
    SPDX-License-Identifier: GPL-3.0+
*/

using eduOAuth;
using eduVPN.Models;
using System;
using System.Diagnostics;

namespace eduVPN.ViewModels.Windows
{
    /// <summary>
    /// Authorization pop-up base window
    /// </summary>
    public class AuthorizationPopup : Window
    {
        #region Properties

        /// <summary>
        /// Authenticating instance
        /// </summary>
        /// <remarks><c>null</c> if none selected.</remarks>
        public Instance Instance { get; }

        /// <summary>
        /// Authorization grant
        /// </summary>
        public AuthorizationGrant AuthorizationGrant { get; }

        /// <summary>
        /// Callback URI
        /// </summary>
        /// <remarks>Should be populated by callback URI.</remarks>
        public Uri CallbackURI
        {
            get { return _callback_uri; }
            set { SetProperty(ref _callback_uri, value); }
        }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private Uri _callback_uri;

        #endregion

        #region Constructors

        /// <summary>
        /// Creates a pop-up window
        /// </summary>
        /// <param name="sender">Event sender (ignored)</param>
        /// <param name="e">Event arguments</param>
        public AuthorizationPopup(object sender, RequestInstanceAuthorizationEventArgs e)
        {
            Instance = e.Instance;
            AuthorizationGrant = e.AuthorizationGrant;
        }

        #endregion
    }
}
