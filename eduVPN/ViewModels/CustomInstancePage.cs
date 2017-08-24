/*
    eduVPN - End-user friendly VPN

    Copyright: 2017, The Commons Conservancy eduVPN Programme
    SPDX-License-Identifier: GPL-3.0+
*/

using Prism.Commands;
using System;
using System.Collections.Generic;
using System.Windows.Input;

namespace eduVPN.ViewModels
{
    /// <summary>
    /// Custom instance source entry wizard page
    /// </summary>
    public class CustomInstancePage : ConnectWizardPage
    {
        #region Properties

        /// <summary>
        /// Instance base URI
        /// </summary>
        public string BaseURI
        {
            get { return _uri; }
            set
            {
                if (value != _uri)
                {
                    _uri = value;
                    RaisePropertyChanged();
                    ((DelegateCommandBase)SelectCustomInstance).RaiseCanExecuteChanged();
                }
            }
        }
        private string _uri;

        /// <summary>
        /// Authorize Other Instance Command
        /// </summary>
        public ICommand SelectCustomInstance
        {
            get
            {
                if (_select_custom_instance == null)
                {
                    _select_custom_instance = new DelegateCommand(
                        // execute
                        async () => {
                            Parent.Error = null;
                            Parent.ChangeTaskCount(+1);
                            try
                            {
                                // Set authentication instance.
                                var base_uri = new Uri(BaseURI);
                                var uri_builder = new UriBuilder(base_uri);
                                uri_builder.Path = "/favicon.ico";
                                Parent.Configuration.AuthenticatingInstance = new Models.InstanceInfo()
                                {
                                    Base = new Uri(BaseURI),
                                    DisplayName = base_uri.Host,
                                    Logo = uri_builder.Uri
                                };

                                // Restore the access token from the settings.
                                Parent.Configuration.AccessToken = await Parent.Configuration.AuthenticatingInstance.GetAccessTokenAsync(ConnectWizard.Abort.Token);

                                // Connecting instance will be the same as authenticating.
                                Parent.Configuration.ConnectingInstance = Parent.Configuration.AuthenticatingInstance;

                                if (Parent.Configuration.AccessToken == null)
                                    Parent.CurrentPage = Parent.AuthorizationPage;
                                else
                                    Parent.CurrentPage = Parent.ProfileSelectPage;
                            }
                            catch (Exception ex) { Parent.Error = ex; }
                            finally { Parent.ChangeTaskCount(-1); }
                        },

                        // canExecute
                        () => {
                            try { new Uri(BaseURI); }
                            catch (Exception) { return false; }
                            return true;
                        });
                }
                return _select_custom_instance;
            }
        }
        private ICommand _select_custom_instance;

        #endregion

        #region Constructors

        /// <summary>
        /// Constructs a view model.
        /// </summary>
        public CustomInstancePage(ConnectWizard parent) :
            base(parent)
        {
            BaseURI = "https://";
        }

        #endregion

        #region Methods

        protected override void DoNavigateBack()
        {
            Parent.CurrentPage = Parent.InstanceSourceSelectPage;
        }

        protected override bool CanNavigateBack()
        {
            return true;
        }

        #endregion
    }
}
