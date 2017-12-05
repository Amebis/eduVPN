/*
    eduVPN - End-user friendly VPN

    Copyright: 2017, The Commons Conservancy eduVPN Programme
    SPDX-License-Identifier: GPL-3.0+
*/

using eduOAuth;
using eduVPN.Models;
using Prism.Commands;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Web;

namespace eduVPN.ViewModels.Windows
{
    /// <summary>
    /// Authorization pop-up
    /// </summary>
    public class AuthorizationPopup : Window
    {
        #region Fields

        /// <summary>
        /// OAuth pending authorization grant
        /// </summary>
        private AuthorizationGrant _authorization_grant;

        /// <summary>
        /// Registered client redirect callback URI (endpoint)
        /// </summary>
        private readonly string _redirect_endpoint = "org.eduvpn.app:/api/callback";

        #endregion

        #region Properties

        /// <summary>
        /// Authenticating eduVPN instance
        /// </summary>
        /// <remarks><c>null</c> if none selected.</remarks>
        public Instance AuthenticatingInstance
        {
            get { return _authenticating_instance; }
            set { SetProperty(ref _authenticating_instance, value); }
        }
        private Instance _authenticating_instance;

        /// <summary>
        /// Requested access token scope
        /// </summary>
        public string Scope
        {
            get { return _scope; }
            set { SetProperty(ref _scope, value); }
        }
        private string _scope;

        /// <summary>
        /// OAuth access token
        /// </summary>
        public AccessToken AccessToken
        {
            get { return _access_token; }
            set { SetProperty(ref _access_token, value); }
        }
        private AccessToken _access_token;

        /// <summary>
        /// Retry authorization command
        /// </summary>
        public DelegateCommand RequestAuthorization
        {
            get
            {
                if (_request_authorization == null)
                {
                    _request_authorization = new DelegateCommand(
                        // execute
                        () =>
                        {
                            ChangeTaskCount(+1);
                            try
                            {
                                // Prepare new authorization grant.
                                _authorization_grant = new AuthorizationGrant()
                                {
                                    AuthorizationEndpoint = AuthenticatingInstance.GetEndpoints(Abort.Token).AuthorizationEndpoint,
                                    RedirectEndpoint = new Uri(_redirect_endpoint),
                                    ClientID = "org.eduvpn.app",
                                    Scope = new List<string>() { Scope },
                                    CodeChallengeAlgorithm = AuthorizationGrant.CodeChallengeAlgorithmType.S256
                                };

                                // Open authorization request in the browser.
                                System.Diagnostics.Process.Start(_authorization_grant.AuthorizationURI.ToString());

                                Error = null;
                            }
                            catch (Exception ex) { Error = ex; }
                            finally { ChangeTaskCount(-1); }
                        },

                        // canExecute
                        () => AuthenticatingInstance != null && Scope != null);

                    // Setup canExecute refreshing.
                    PropertyChanged += (object sender, PropertyChangedEventArgs e) => { if (e.PropertyName == nameof(AuthenticatingInstance) || e.PropertyName == nameof(Scope)) _request_authorization.RaiseCanExecuteChanged(); };
                }

                return _request_authorization;
            }
        }
        private DelegateCommand _request_authorization;

        /// <summary>
        /// Authorize command
        /// </summary>
        public DelegateCommand<string> Authorize
        {
            get
            {
                if (_authorize == null)
                    _authorize = new DelegateCommand<string>(
                        // execute
                        async uri =>
                        {
                            ChangeTaskCount(+1);
                            try
                            {
                                // Process response and get access token.
                                AccessToken = await _authorization_grant.ProcessResponseAsync(
                                    HttpUtility.ParseQueryString(new Uri(uri).Query),
                                    AuthenticatingInstance.GetEndpoints(Abort.Token).TokenEndpoint,
                                    null,
                                    Abort.Token);

                                // Authorization grants are non-reusable.
                                _authorization_grant = null;

                                Error = null;
                            }
                            catch (Exception ex) { Error = ex; }
                            finally { ChangeTaskCount(-1); }
                        },

                        // canExecute
                        uri =>
                        {
                            Uri parsed_uri;

                            // URI must be:
                            // - A non-NULL string
                            if (!(uri is string)) return false;
                            // - Valid URI (parsable)
                            try { parsed_uri = new Uri(uri); }
                            catch { return false; }
                            // - Must match the redirect endpoint provided in request.
                            if (parsed_uri.GetLeftPart(UriPartial.Path) != _redirect_endpoint) return false;

                            return true;
                        });

                return _authorize;
            }
        }
        private DelegateCommand<string> _authorize;

        #endregion
    }
}
