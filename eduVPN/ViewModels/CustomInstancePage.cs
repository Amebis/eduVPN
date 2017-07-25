/*
    eduVPN - End-user friendly VPN

    Copyright: 2017, The Commons Conservancy eduVPN Programme
    SPDX-License-Identifier: GPL-3.0+
*/

using eduOAuth;
using eduVPN.JSON;
using Prism.Commands;
using System;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
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
                            // Set busy flag.
                            TaskCount++;

                            try
                            {
                                // Set instance base URI.
                                Parent.AuthenticatingInstance.Base = new Uri(InstanceURI);

                                // Schedule API endpoints get.
                                var uri_builder = new UriBuilder(Parent.AuthenticatingInstance.Base);
                                uri_builder.Path += "info.json";
                                var api_get_task = JSON.Response.GetAsync(
                                    uri_builder.Uri,
                                    null,
                                    null,
                                    null,
                                    _abort.Token);

                                // Try to restore the access token from the settings.
                                Parent.AccessToken = null;
                                try
                                {
                                    var at = Properties.Settings.Default.AccessTokens[Parent.AuthenticatingInstance.Base.AbsoluteUri];
                                    if (at != null)
                                        Parent.AccessToken = AccessToken.FromBase64String(at);
                                } catch (Exception) { }

                                // Load API endpoints.
                                var api = new JSON.API();
                                api.LoadJSON((await api_get_task).Value);
                                Parent.AuthenticatingEndpoints = api;

                                if (Parent.AccessToken != null && Parent.AccessToken.Expires.HasValue && Parent.AccessToken.Expires.Value <= DateTime.Now)
                                {
                                    // The access token expired. Try refreshing it.
                                    try
                                    {
                                        Parent.AccessToken = await Parent.AccessToken.RefreshTokenAsync(
                                            Parent.AuthenticatingEndpoints.TokenEndpoint,
                                            null,
                                            _abort.Token);
                                    }
                                    catch (Exception)
                                    {
                                        Parent.AccessToken = null;
                                    }
                                }

                                // Connecting instance will be the same as authenticating.
                                Parent.ConnectingInstance = Parent.AuthenticatingInstance;
                                Parent.ConnectingEndpoints = Parent.AuthenticatingEndpoints;

                                if (Parent.AccessToken == null)
                                    Parent.CurrentPage = Parent.AuthorizationPage;
                                else
                                    Parent.CurrentPage = Parent.ProfileSelectPage;
                            }
                            catch (Exception ex)
                            {
                                ErrorMessage = ex.Message;
                            }
                            finally
                            {
                                // Clear busy flag.
                                TaskCount--;
                            }
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
