/*
    eduVPN - End-user friendly VPN

    Copyright: 2017, The Commons Conservancy eduVPN Programme
    SPDX-License-Identifier: GPL-3.0+
*/

using eduOAuth;
using Prism.Commands;
using Prism.Mvvm;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Windows.Input;

namespace eduVPN
{
    public class InstanceViewModel : BindableBase
    {
        #region Fields

        private AuthorizationGrant _authorization_grant;

        #endregion

        #region Properties

        /// <summary>
        /// Selected Instance
        /// </summary>
        /// <remarks><c>null</c> if none selected.</remarks>
        public Instance SelectedInstance
        {
            get { return _selected_instance; }
            set
            {
                _selected_instance = value;
                RaisePropertyChanged();
                ((DelegateCommand<object>)AuthorizeSelectedInstanceCommand).RaiseCanExecuteChanged();
            }
        }
        private Instance _selected_instance;

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
                    ((DelegateCommand<object>)AuthorizeOtherInstanceCommand).RaiseCanExecuteChanged();
                }
            }
        }
        private string _instance_uri;

        #endregion

        #region Constructors

        /// <summary>
        /// Constructs a view model.
        /// </summary>
        public InstanceViewModel()
        {
            InstanceURI = "https://";
        }

        #endregion

        #region Commands

        /// <summary>
        /// Authorize Selected Instance Command
        /// </summary>
        public ICommand AuthorizeSelectedInstanceCommand
        {
            get
            {
                if (_authenticate_selected_instance_command == null)
                {
                    _authenticate_selected_instance_command = new DelegateCommand<object>(
                        // execute
                        param =>
                        {
                            var uri_builder = new UriBuilder(SelectedInstance.Base);
                            uri_builder.Path += "info.json";
                            ThreadPool.QueueUserWorkItem(new WaitCallback(Authorize), uri_builder.Uri);
                        },

                        // canExecute
                        param => SelectedInstance != null);
                }
                return _authenticate_selected_instance_command;
            }
        }
        private ICommand _authenticate_selected_instance_command;

        /// <summary>
        /// Authorize Other Instance Command
        /// </summary>
        public ICommand AuthorizeOtherInstanceCommand
        {
            get
            {
                if (_authenticate_other_instance_command == null)
                {
                    _authenticate_other_instance_command = new DelegateCommand<object>(
                        // execute
                        param => ThreadPool.QueueUserWorkItem(new WaitCallback(Authorize), new Uri(InstanceURI)),

                        // canExecute
                        param => {
                            try
                            {
                                var uri = new Uri(InstanceURI);
                                return true;
                            }
                            catch (Exception)
                            {
                                return false;
                            }
                        });
                }
                return _authenticate_other_instance_command;
            }
        }
        private ICommand _authenticate_other_instance_command;

        #endregion

        #region Methods

        private void Authorize(object api_uri)
        {
            // Get and load API endpoints.
            var api = new API();
            try
            {
                api.Load(API.Get(api_uri as Uri));
            }
            catch (Exception ex)
            {
                // TODO: Pass exception to the View.
                return;
            }

            // Opens authorization request in the browser.
            _authorization_grant = new AuthorizationGrant()
            {
                AuthorizationEndpoint = api.AuthorizationEndpoint,
                RedirectEndpoint = new Uri("nl.eduvpn.app.windows:/api/callback"),
                ClientID = "nl.eduvpn.app.windows",
                Scope = new List<string>(){ "config" },
                CodeChallengeAlgorithm = AuthorizationGrant.CodeChallengeAlgorithmType.S256
            };
            System.Diagnostics.Process.Start(_authorization_grant.AuthorizationURI.ToString());
        }

        #endregion
    }
}
