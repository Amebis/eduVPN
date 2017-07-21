/*
    eduVPN - End-user friendly VPN

    Copyright: 2017, The Commons Conservancy eduVPN Programme
    SPDX-License-Identifier: GPL-3.0+
*/

using eduOAuth;
using eduVPN.JSON;
using Prism.Commands;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Web;
using System.Windows.Input;
using System.Windows.Threading;

namespace eduVPN.ViewModels
{
    /// <summary>
    /// Authorization wizard page
    /// </summary>
    public class AuthorizationPage : ConnectWizardPage, IDisposable
    {
        #region Fields

        /// <summary>
        /// Authorization worker thread
        /// </summary>
        private Thread _worker;

        /// <summary>
        /// Token used to cancel unfinished authorizaton processes in case of user cancel or retry.
        /// </summary>
        private CancellationTokenSource _cancel;

        /// <summary>
        /// OAuth pending authorization grant
        /// </summary>
        private AuthorizationGrant _authorization_grant;

        /// <summary>
        /// Registered client redirect callback URI (endpoint)
        /// </summary>
        private const string _redirect_endpoint = "org.eduvpn.app:/api/callback";

        #endregion

        #region Properties

        /// <summary>
        /// Retry authorization command
        /// </summary>
        public ICommand Retry
        {
            get
            {
                if (_retry == null)
                    _retry = new DelegateCommand(TriggerAuthorization);
                return _retry;
            }
        }
        private ICommand _retry;

        public ICommand Authorize
        {
            get
            {
                if (_authorize == null)
                    _authorize = new DelegateCommand<string>(
                        // execute
                        async param =>
                        {
                            // Set busy flag.
                            IsBusy = true;

                            try
                            {
                                ErrorMessage = null;

                                // Process response and get access token.
                                Parent.AccessToken = await _authorization_grant.ProcessResponseAsync(
                                    HttpUtility.ParseQueryString(new Uri(param).Query),
                                    Parent.Endpoints.TokenEndpoint,
                                    _abort.Token);

                                // Go to profile selection page.
                                Parent.CurrentPage = Parent.ProfileSelectPage;
                            }
                            catch (Exception ex)
                            {
                                ErrorMessage = ex.Message;
                            }
                            finally
                            {
                                // Clear busy flag.
                                IsBusy = false;
                            }
                        },

                        // canExecute
                        param =>
                        {
                            Uri uri;

                            // URI must be:
                            // - non-NULL
                            if (param == null) return false;
                            // - Valid URI (parsable)
                            try { uri = new Uri(param); }
                            catch (Exception) { return false; }
                            // - Must match the redirect endpoint provided in request.
                            if (uri.Scheme + ":" + uri.AbsolutePath != _redirect_endpoint) return false;

                            return true;
                        });
                return _authorize;
            }
        }
        private ICommand _authorize;

        #endregion

        #region Constructors

        /// <summary>
        /// Constructs an authorization wizard page
        /// </summary>
        /// <param name="parent"></param>
        public AuthorizationPage(ConnectWizard parent) :
            base(parent)
        {
        }

        #endregion

        #region Methods

        public override void OnActivate()
        {
            TriggerAuthorization();
            base.OnActivate();
        }

        /// <summary>
        /// Invokes client authorization process
        /// </summary>
        private void TriggerAuthorization()
        {
            if (_worker != null)
            {
                // Abort pending authorization.
                _cancel.Cancel();
            }

            // Reset error message.
            ErrorMessage = null;

            // Launch authorization thread.
            _cancel = new CancellationTokenSource();
            _worker = new Thread(
                () =>
                {
                    // Set busy flag (in the UI thread).
                    _dispatcher.Invoke(DispatcherPriority.Normal, (Action)(() => IsBusy = true));

                    try
                    {
                        var abort = CancellationTokenSource.CreateLinkedTokenSource(_abort.Token, _cancel.Token);

                        // Get and load API endpoints.
                        var api = new JSON.API();
                        var uri_builder = new UriBuilder(Parent.Instance.Base);
                        uri_builder.Path += "info.json";
                        api.LoadJSON(JSON.Response.Get(
                            uri_builder.Uri,
                            null,
                            null,
                            /*Parent.Instance.PublicKey*/ null, // TODO: Ask François about the purpose of public_key record in federation.json.
                            abort.Token).Value);

                        // Opens authorization request in the browser.
                        _authorization_grant = new AuthorizationGrant()
                        {
                            AuthorizationEndpoint = api.AuthorizationEndpoint,
                            RedirectEndpoint = new Uri(_redirect_endpoint),
                            ClientID = "org.eduvpn.app",
                            Scope = new List<string>() { "config" },
                            CodeChallengeAlgorithm = AuthorizationGrant.CodeChallengeAlgorithmType.S256
                        };
                        System.Diagnostics.Process.Start(_authorization_grant.AuthorizationURI.ToString());

                        // Save API endpoints.
                        Parent.Endpoints = api;
                    }
                    catch (OperationCanceledException) { }
                    catch (Exception ex)
                    {
                        // Notify the sender the authorization failed.
                        _dispatcher.Invoke(DispatcherPriority.Normal, (Action)(() => ErrorMessage = ex.Message));
                    }
                    finally
                    {
                        // Clear busy flag (in the UI thread).
                        _dispatcher.Invoke(DispatcherPriority.Normal, (Action)(() => IsBusy = false));
                    }
                });
            _worker.Start();
        }

        protected override void DoNavigateBack()
        {
            if (_worker != null)
            {
                // Abort pending authorization.
                _cancel.Cancel();
                _worker = null;
            }

            if (Parent.Instance.IsCustom)
                Parent.CurrentPage = Parent.CustomInstancePage;
            else
                switch (Parent.AccessType)
                {
                    case AccessType.SecureInternet: Parent.CurrentPage = Parent.SecureInternetSelectPage; break;
                    case AccessType.InstituteAccess: Parent.CurrentPage = Parent.InstituteAccessSelectPage; break;
                }
        }

        protected override bool CanNavigateBack()
        {
            if (Parent.Instance.IsCustom)
                return true;
            else
                switch (Parent.AccessType)
                {
                    case AccessType.SecureInternet: return true;
                    case AccessType.InstituteAccess: return true;
                    default: return false;
                }
        }

        #endregion

        #region IDisposable Support
        private bool disposedValue = false; // To detect redundant calls

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    if (_cancel != null)
                        _cancel.Dispose();
                }

                disposedValue = true;
            }
        }

        // This code added to correctly implement the disposable pattern.
        public void Dispose()
        {
            // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
            Dispose(true);
        }
        #endregion
    }
}
