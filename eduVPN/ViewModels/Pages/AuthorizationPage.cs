﻿/*
    eduVPN - VPN for education and research

    Copyright: 2017-2021 The Commons Conservancy eduVPN Programme
    SPDX-License-Identifier: GPL-3.0+
*/

using eduOAuth;
using eduVPN.Models;
using eduVPN.ViewModels.Windows;
using Prism.Commands;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using System.Windows.Threading;

namespace eduVPN.ViewModels.Pages
{
    /// <summary>
    /// Authorization wizard page
    /// </summary>
    public class AuthorizationPage : ConnectWizardStandardPage
    {
        #region Fields

        /// <summary>
        /// Page to return to after authorization is complete (or cancelled)
        /// </summary>
        private ConnectWizardStandardPage ReturnPage;

        /// <summary>
        /// Authorization cancellation token
        /// </summary>
        private CancellationTokenSource AuthorizationInProgress;

        #endregion

        #region Properties

        /// <summary>
        /// Cancel authorization
        /// </summary>
        public DelegateCommand Cancel
        {
            get
            {
                if (_Cancel == null)
                    _Cancel = new DelegateCommand(
                        () =>
                        {
                            try
                            {
                                AuthorizationInProgress?.Cancel();
                                Wizard.CurrentPage = ReturnPage;
                            }
                            catch (Exception ex) { Wizard.Error = ex; }
                        });
                return _Cancel;
            }
        }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private DelegateCommand _Cancel;

        #endregion

        #region Constructors

        /// <summary>
        /// Constructs a view model.
        /// </summary>
        /// <param name="wizard">The connecting wizard</param>
        public AuthorizationPage(ConnectWizard wizard) :
            base(wizard)
        {
        }

        #endregion

        #region Methods

        /// <summary>
        /// Triggers authorization for selected server asynchronously
        /// </summary>
        /// <param name="srv">Server</param>
        /// <returns>Asynchronous operation</returns>
        /// <exception cref="InvalidAccessTokenException">Authorization failed</exception>
        public async Task TriggerAuthorizationAsync(Server srv)
        {
            var e = new RequestAuthorizationEventArgs("config");
            var task = new Task(() => OnRequestAuthorization(srv, e), Window.Abort.Token, TaskCreationOptions.LongRunning);
            task.Start();
            await task;

            if (e.AccessToken is InvalidToken)
                throw new InvalidAccessTokenException(string.Format(Resources.Strings.ErrorInvalidAccessToken, srv));
        }

        /// <summary>
        /// Called when a server requests user authorization
        /// </summary>
        /// <param name="sender">Server of type <see cref="Server"/> requiring authorization</param>
        /// <param name="e">Authorization request event arguments</param>
        public void OnRequestAuthorization(object sender, RequestAuthorizationEventArgs e)
        {
            if (!(sender is Server authenticatingServer))
                return;

            e.TokenOrigin = RequestAuthorizationEventArgs.TokenOriginType.None;
            e.AccessToken = null;

            lock (Properties.Settings.Default.AccessTokenCache)
            {
                if (e.SourcePolicy != RequestAuthorizationEventArgs.SourcePolicyType.ForceAuthorization)
                {
                    var key = authenticatingServer.Base.AbsoluteUri;
                    if (Properties.Settings.Default.AccessTokenCache.TryGetValue(key, out var accessToken))
                    {
                        if (!e.ForceRefresh && DateTime.Now < accessToken.Expires)
                        {
                            e.TokenOrigin = RequestAuthorizationEventArgs.TokenOriginType.Saved;
                            e.AccessToken = accessToken;
                            return;
                        }

                        // Token refresh was explicitly requested or the token expired. Refresh it.
                        if (accessToken is InvalidToken)
                        {
                            // Invalid token is not refreshable.
                            Properties.Settings.Default.AccessTokenCache.Remove(key);
                        }
                        else
                        {
                            // Get API endpoints. (Not called from the UI thread or already cached by now. Otherwise it would need to be spawned as a background task to avoid deadlock.)
                            var api = authenticatingServer.GetEndpoints(Window.Abort.Token);

                            // Prepare web request.
                            var request = WebRequest.Create(api.TokenEndpoint);
                            request.CachePolicy = Xml.Response.CachePolicy;
                            request.Proxy = null;
                            if (request is HttpWebRequest requestHTTP)
                                requestHTTP.UserAgent = Xml.Response.UserAgent;

                            try
                            {
                                accessToken = accessToken.RefreshToken(request, null, Window.Abort.Token);

                                // Update access token cache.
                                Properties.Settings.Default.AccessTokenCache[key] = accessToken;

                                // If we got here, return the token.
                                e.TokenOrigin = RequestAuthorizationEventArgs.TokenOriginType.Refreshed;
                                e.AccessToken = accessToken;
                                return;
                            }
                            catch (AccessTokenException ex)
                            {
                                if (ex.ErrorCode == AccessTokenException.ErrorCodeType.InvalidGrant)
                                {
                                    // The grant has been revoked. Drop the access token.
                                    Properties.Settings.Default.AccessTokenCache.Remove(key);
                                }
                                else
                                    throw;
                            }
                        }
                    }
                }

                if (e.SourcePolicy != RequestAuthorizationEventArgs.SourcePolicyType.SavedOnly)
                {
                    // We're in the background thread - notify via dispatcher.
                    Wizard.Dispatcher.Invoke(DispatcherPriority.Normal, (Action)(() =>
                    {
                        Wizard.TaskCount++;
                        ReturnPage = Wizard.CurrentPage;
                        Wizard.CurrentPage = this;
                        AuthorizationInProgress = new CancellationTokenSource();
                    }));
                    try
                    {
                        // Get API endpoints. (Not called from the UI thread. Otherwise it would need to be spawned as a background task to avoid deadlock.)
                        var api = authenticatingServer.GetEndpoints(Window.Abort.Token);

                        // Prepare new authorization grant.
                        AuthorizationGrant authorizationGrant = null;
                        Uri callbackUri = null;
                        var httpListener = new eduOAuth.HttpListener(IPAddress.Loopback, 0);
                        httpListener.HttpCallback += (object _, HttpCallbackEventArgs eHTTPCallback) =>
                        {
                            Wizard.Dispatcher.Invoke(DispatcherPriority.Normal, (Action)(() =>
                            {
                                callbackUri = eHTTPCallback.Uri;
                                AuthorizationInProgress.Cancel();
                                Wizard.CurrentPage = ReturnPage;
                            }));
                        };
                        httpListener.HttpRequest += (object _, HttpRequestEventArgs eHTTPRequest) =>
                        {
                            if (eHTTPRequest.Uri.AbsolutePath.ToLowerInvariant() == "/favicon.ico")
                            {
                                var res = System.Windows.Application.GetResourceStream(new Uri("pack://application:,,,/Resources/App.ico"));
                                eHTTPRequest.Type = res.ContentType;
                                eHTTPRequest.Content = res.Stream;
                            }
                        };
                        httpListener.Start();
                        try
                        {
                            // Make the authorization URI.
                            authorizationGrant = new AuthorizationGrant(
                                api.AuthorizationEndpoint,
                                new Uri(string.Format("http://{0}:{1}/callback", ((IPEndPoint)httpListener.LocalEndpoint).Address, ((IPEndPoint)httpListener.LocalEndpoint).Port)),
                                Properties.Settings.Default.ClientId + ".windows",
                                new HashSet<string>() { e.Scope },
                                AuthorizationGrant.CodeChallengeAlgorithmType.S256);
                            var authorizationUri = authorizationGrant.AuthorizationUri;
                            if (authenticatingServer is SecureInternetServer srv &&
                                srv.AuthenticationUriTemplate != null)
                            {
                                // Envelope authorization URI and organization identifier.
                                authorizationUri = new Uri(srv.AuthenticationUriTemplate
                                    .Replace("@RETURN_TO@", HttpUtility.UrlEncode(authorizationUri.ToString()))
                                    .Replace("@ORG_ID@", HttpUtility.UrlEncode(srv.OrganizationId)));
                            }

                            // Trigger authorization.
                            Process.Start(authorizationUri.ToString());

                            // Wait for a change: either callback is invoked, either user cancels.
                            CancellationTokenSource.CreateLinkedTokenSource(AuthorizationInProgress.Token, Window.Abort.Token).Token.WaitHandle.WaitOne();
                        }
                        finally
                        {
                            // Delay HTTP server shutdown allowing browser to finish loading content.
                            new Thread(new ThreadStart(() =>
                            {
                                Window.Abort.Token.WaitHandle.WaitOne(5 * 1000);
                                httpListener.Stop();
                            })).Start();
                        }

                        if (callbackUri == null)
                            throw new OperationCanceledException();

                        // Get access token from authorization grant.
                        var request = WebRequest.Create(api.TokenEndpoint);
                        request.CachePolicy = Xml.Response.CachePolicy;
                        request.Proxy = null;
                        if (request is HttpWebRequest requestHTTP)
                            requestHTTP.UserAgent = Xml.Response.UserAgent;
                        e.AccessToken = authorizationGrant.ProcessResponse(
                            HttpUtility.ParseQueryString(callbackUri.Query),
                            request,
                            null,
                            Window.Abort.Token);
                        Window.Abort.Token.ThrowIfCancellationRequested();

                        // Save access token to the cache.
                        e.TokenOrigin = RequestAuthorizationEventArgs.TokenOriginType.Authorized;
                        Properties.Settings.Default.AccessTokenCache[authenticatingServer.Base.AbsoluteUri] = e.AccessToken;
                    }
                    finally { Wizard.Dispatcher.Invoke(DispatcherPriority.Normal, (Action)(() => Wizard.TaskCount--)); }
                }
            }
        }

        /// <summary>
        /// Called when a server requests authorization delete
        /// </summary>
        /// <param name="sender">Server of type <see cref="Server"/> requiring authorization</param>
        /// <param name="e">Authorization forget event arguments</param>
        public void OnForgetAuthorization(object sender, ForgetAuthorizationEventArgs e)
        {
            if (!(sender is Server authenticatingServer))
                return;

            // Remove access token from cache.
            lock (Properties.Settings.Default.AccessTokenCache)
                Properties.Settings.Default.AccessTokenCache.Remove(authenticatingServer.Base.AbsoluteUri);
        }

        #endregion
    }
}
