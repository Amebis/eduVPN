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
    /// Custom instance entry wizard page
    /// </summary>
    public class CustomInstancePage : ConnectWizardPage
    {
        #region Properties

        /// <summary>
        /// Instance URI
        /// </summary>
        public string InstanceURI
        {
            get { return _instance_uri; }
            set
            {
                if (value != _instance_uri)
                {
                    _instance_uri = value;
                    RaisePropertyChanged();
                    ((DelegateCommandBase)AuthorizeCustomInstance).RaiseCanExecuteChanged();
                }
            }
        }
        private string _instance_uri;

        /// <summary>
        /// Authorize Other Instance Command
        /// </summary>
        public ICommand AuthorizeCustomInstance
        {
            get
            {
                if (_authorize_instance == null)
                {
                    _authorize_instance = new DelegateCommand(
                        // execute
                        async () => {
                            Error = null;
                            TaskCount++;
                            try
                            {
                                // Set instance base URI.
                                Parent.AuthenticatingInstance.Base = new Uri(InstanceURI);

                                // Restore the access token from the settings.
                                Parent.AccessToken = await Parent.AuthenticatingInstance.GetAccessTokenAsync(ConnectWizard.Abort.Token);

                                // Connecting instance will be the same as authenticating.
                                Parent.ConnectingInstance = Parent.AuthenticatingInstance;

                                if (Parent.AccessToken == null)
                                    Parent.CurrentPage = Parent.AuthorizationPage;
                                else
                                    Parent.CurrentPage = Parent.ProfileSelectPage;
                            }
                            catch (Exception ex) { Error = ex; }
                            finally { TaskCount--; }
                        },

                        // canExecute
                        () => {
                            try { new Uri(InstanceURI); }
                            catch (Exception) { return false; }
                            return true;
                        });
                }
                return _authorize_instance;
            }
        }
        private ICommand _authorize_instance;

        #endregion

        #region Constructors

        /// <summary>
        /// Constructs a view model.
        /// </summary>
        public CustomInstancePage(ConnectWizard parent) :
            base(parent)
        {
            InstanceURI = "https://";
        }

        #endregion

        #region Methods

        protected override void DoNavigateBack()
        {
            switch (Parent.AccessType)
            {
                case AccessType.SecureInternet: Parent.CurrentPage = Parent.SecureInternetSelectPage; break;
                case AccessType.InstituteAccess: Parent.CurrentPage = Parent.InstituteAccessSelectPage; break;
            }
        }

        protected override bool CanNavigateBack()
        {
            switch (Parent.AccessType)
            {
                case AccessType.SecureInternet: return true;
                case AccessType.InstituteAccess: return true;
                default: return false;
            }
        }

        #endregion
    }
}
