/*
    eduVPN - End-user friendly VPN

    Copyright: 2017, The Commons Conservancy eduVPN Programme
    SPDX-License-Identifier: GPL-3.0+
*/

using Prism.Commands;
using System;
using System.Windows.Input;

namespace eduVPN.ViewModels
{
    /// <summary>
    /// Authorization wizard page
    /// </summary>
    public class AuthorizationPage : ConnectWizardPage
    {
        #region Properties

        /// <summary>
        /// Retry authorization command
        /// </summary>
        public ICommand Retry
        {
            get
            {
                if (_retry == null)
                    _retry = new DelegateCommand(RequestAuthorization);
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
                            Error = null;
                            TaskCount++;
                            try
                            {
                                // Process response and get access token.
                                Parent.AccessToken = await Parent.AuthenticatingInstance.AuthorizeAsync(new Uri(param), ConnectWizard.Abort.Token);

                                // Go to profile selection page.
                                if (Parent.ConnectingInstance == null)
                                    Parent.CurrentPage = Parent.InstanceAndProfileSelectPage;
                                else
                                    Parent.CurrentPage = Parent.ProfileSelectPage;
                            }
                            catch (Exception ex) { Error = ex; }
                            finally { TaskCount--; }
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
                            if (uri.Scheme + ":" + uri.AbsolutePath != Models.InstanceInfo.RedirectEndpoint) return false;

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

        /// <summary>
        /// Invokes client authorization process in the browser.
        /// </summary>
        private void RequestAuthorization()
        {
            Error = null;
            TaskCount++;
            try
            {
                Parent.AuthenticatingInstance.RequestAuthorization(ConnectWizard.Abort.Token);
            }
            catch (Exception ex) { Error = ex; }
            finally { TaskCount--; }
        }

        public override void OnActivate()
        {
            base.OnActivate();

            RequestAuthorization();
        }

        protected override void DoNavigateBack()
        {
            if (Parent.InstanceList is Models.InstanceInfoFederatedList)
                Parent.CurrentPage = Parent.AccessTypePage;
            else if (Parent.AuthenticatingInstance.IsCustom)
                Parent.CurrentPage = Parent.CustomInstancePage;
            else
                Parent.CurrentPage = Parent.InstanceSelectPage;
        }

        protected override bool CanNavigateBack()
        {
            return true;
        }

        #endregion
    }
}
