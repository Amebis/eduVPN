/*
    eduVPN - End-user friendly VPN

    Copyright: 2017, The Commons Conservancy eduVPN Programme
    SPDX-License-Identifier: GPL-3.0+
*/

using eduOAuth;
using Prism.Commands;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Windows.Input;
using System.Windows.Threading;

namespace eduVPN.ViewModel
{
    /// <summary>
    /// Authorization wizard page
    /// </summary>
    public class AuthorizationPageViewModel : ConnectWizardPageViewModel, IDisposable
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
                    _retry = new DelegateCommand(Authorize);
                return _retry;
            }
        }
        private ICommand _retry;

        #endregion

        #region Constructors

        /// <summary>
        /// Constructs an authorization wizard page
        /// </summary>
        /// <param name="parent"></param>
        public AuthorizationPageViewModel(ConnectWizardViewModel parent) :
            base(parent)
        {
        }

        #endregion

        #region Methods

        public override void OnActivate()
        {
            Authorize();
            base.OnActivate();
        }

        /// <summary>
        /// Invokes client authorization process
        /// </summary>
        private void Authorize()
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
                    try
                    {
                        var abort = CancellationTokenSource.CreateLinkedTokenSource(_abort.Token, _cancel.Token);

                        // Get and load API endpoints.
                        var api = new API();
                        api.Load(JSONContents.Get(Parent.InstanceURI, null, abort.Token).Value);

                        // Opens authorization request in the browser.
                        _authorization_grant = new AuthorizationGrant()
                        {
                            AuthorizationEndpoint = api.AuthorizationEndpoint,
                            RedirectEndpoint = new Uri("org.eduvpn.app:/api/callback"),
                            ClientID = "org.eduvpn.app",
                            Scope = new List<string>() { "config" },
                            CodeChallengeAlgorithm = AuthorizationGrant.CodeChallengeAlgorithmType.S256
                        };
                        System.Diagnostics.Process.Start(_authorization_grant.AuthorizationURI.ToString());
                    }
                    catch (OperationCanceledException) { }
                    catch (Exception ex)
                    {
                        // Notify the sender the authorization failed.
                        _dispatcher.Invoke(DispatcherPriority.Normal, (Action)(() => ErrorMessage = ex.Message));
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

            if (Parent.IsCustomInstance)
                Parent.CurrentPage = Parent.CustomInstancePage;
            else
                Parent.CurrentPage = Parent.InstanceSelectPage;
        }

        protected override bool CanNavigateBack()
        {
            return true;
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
