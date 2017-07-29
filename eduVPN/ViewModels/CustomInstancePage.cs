/*
    eduVPN - End-user friendly VPN

    Copyright: 2017, The Commons Conservancy eduVPN Programme
    SPDX-License-Identifier: GPL-3.0+
*/

using eduOAuth;
using eduVPN.JSON;
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

                                try
                                {
                                    // Get and load API endpoints.
                                    var api = new Models.InstanceEndpoints();
                                    var uri_builder = new UriBuilder(Parent.AuthenticatingInstance.Base);
                                    uri_builder.Path += "info.json";
                                    api.LoadJSON((await JSON.Response.GetAsync(
                                        uri: uri_builder.Uri,
                                        ct: ConnectWizard.Abort.Token)).Value, ConnectWizard.Abort.Token);
                                    Parent.AuthenticatingEndpoints = api;
                                }
                                catch (OperationCanceledException) { throw; }
                                catch (Exception ex) { throw new AggregateException(Resources.Strings.ErrorEndpointsLoad, ex); }

                                // Try to restore the access token from the settings.
                                Parent.AccessToken = null;
                                try
                                {
                                    var at = Properties.Settings.Default.AccessTokens[Parent.AuthenticatingEndpoints.AuthorizationEndpoint.AbsoluteUri];
                                    if (at != null)
                                        Parent.AccessToken = AccessToken.FromBase64String(at);
                                }
                                catch (Exception) { }
                                if (Parent.AccessToken != null && Parent.AccessToken.Expires.HasValue && Parent.AccessToken.Expires.Value <= DateTime.Now)
                                {
                                    // The access token expired. Try refreshing it.
                                    try
                                    {
                                        Parent.AccessToken = await Parent.AccessToken.RefreshTokenAsync(
                                            Parent.AuthenticatingEndpoints.TokenEndpoint,
                                            null,
                                            ConnectWizard.Abort.Token);
                                    }
                                    catch (Exception) { Parent.AccessToken = null; }
                                }

                                // Connecting instance will be the same as authenticating.
                                Parent.ConnectingInstance = Parent.AuthenticatingInstance;
                                Parent.ConnectingEndpoints = Parent.AuthenticatingEndpoints;

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
