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
                            IsBusy = true;

                            try
                            {
                                // Set instance base URI.
                                Parent.Instance.Base = new Uri(InstanceURI);

                                // Get and load API endpoints.
                                var api = new JSON.API();
                                var uri_builder = new UriBuilder(Parent.Instance.Base);
                                uri_builder.Path += "info.json";
                                api.LoadJSON((await JSON.Response.GetAsync(
                                    uri_builder.Uri,
                                    null,
                                    null,
                                    /*Parent.Instance.PublicKey*/ null, // TODO: Ask François about the purpose of public_key record in federation.json.
                                    _abort.Token)).Value);
                                Parent.Endpoints = api;

                                // Try to restore the access token from the settings.
                                Parent.AccessToken = null;
                                if (Properties.Settings.Default.AccessTokens != null)
                                {
                                    var at = Properties.Settings.Default.AccessTokens[Parent.Instance.Base.AbsoluteUri];
                                    if (at != null)
                                    {
                                        using (var stream = new MemoryStream(Convert.FromBase64String(at)))
                                        {
                                            var formatter = new BinaryFormatter();
                                            Parent.AccessToken = (AccessToken)formatter.Deserialize(stream);
                                        }
                                    }
                                }

                                if (Parent.AccessToken != null)
                                    Parent.CurrentPage = Parent.ProfileSelectPage;
                                else
                                    Parent.CurrentPage = Parent.AuthorizationPage;
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
