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
    public class AuthorizationPageViewModel : ConnectWizardPageViewModel
    {
        #region Fields

        /// <summary>
        /// Authorization worker thread
        /// </summary>
        private Thread _worker;

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
                _worker.Abort();
                _worker.Join();
            }

            // Reset error message.
            ErrorMessage = null;

            // Launch authorization thread.
            _worker = new Thread(
                () =>
                {
                    try
                    {
                        // Get and load API endpoints.
                        var api = new API();
                        api.Load(JSONContents.Get(Parent.InstanceURI, null, _abort.Token).Value);

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

        #endregion
    }
}
