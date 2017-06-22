/*
    eduVPN - End-user friendly VPN

    Copyright: 2017, The Commons Conservancy eduVPN Programme
    SPDX-License-Identifier: GPL-3.0+
*/

using eduOAuth;
using Prism.Commands;
using Prism.Interactivity.InteractionRequest;
using Prism.Mvvm;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows.Input;

namespace eduVPN
{
    public class InstanceViewModel : BindableBase
    {
        #region Fields

        /// <summary>
        /// OAuth pending authorization grant.
        /// </summary>
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
                ((DelegateCommandBase)AuthorizeSelectedInstanceCommand).RaiseCanExecuteChanged();
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
                    ((DelegateCommandBase)AuthorizeOtherInstanceCommand).RaiseCanExecuteChanged();
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
            // Default model values.
            InstanceURI = "https://";
        }

        #endregion

        #region Authorization

        /// <summary>
        /// Authorize Selected Instance Command
        /// </summary>
        public ICommand AuthorizeSelectedInstanceCommand
        {
            get
            {
                if (_authenticate_selected_instance_command == null)
                {
                    _authenticate_selected_instance_command = new DelegateCommand(
                        // execute
                        async () =>
                        {
                            var uri_builder = new UriBuilder(SelectedInstance.Base);
                            uri_builder.Path += "info.json";
                            await Authorize(uri_builder.Uri);
                        },

                        // canExecute
                        () => SelectedInstance != null);
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
                    _authenticate_other_instance_command = new DelegateCommand(
                        // execute
                        async () => await Authorize(new Uri(InstanceURI)),

                        // canExecute
                        () => {
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

        private async Task Authorize(Uri api_uri)
        {
            try
            {
                // Get and load API endpoints.
                var api = new API();
                api.Load(await API.GetAsync(api_uri));

                // Opens authorization request in the browser.
                _authorization_grant = new AuthorizationGrant()
                {
                    AuthorizationEndpoint = api.AuthorizationEndpoint,
                    RedirectEndpoint = new Uri("nl.eduvpn.app.windows:/api/callback"),
                    ClientID = "nl.eduvpn.app.windows",
                    Scope = new List<string>() { "config" },
                    CodeChallengeAlgorithm = AuthorizationGrant.CodeChallengeAlgorithmType.S256
                };
                System.Diagnostics.Process.Start(_authorization_grant.AuthorizationURI.ToString());
            }
            catch (Exception ex)
            {
                // Notify view of the problem.
                NotificationRequest.Raise(
                    new Notification
                    {
                        Content = ex.Message,
                        Title = Properties.Resources.ErrorTitle
                    });
            }
        }

        public InteractionRequest<INotification> NotificationRequest
        {
            get
            {
                if (_notification_request == null)
                    _notification_request = new InteractionRequest<INotification>();
                return _notification_request;
            }
        }
        private InteractionRequest<INotification> _notification_request;

        #endregion
    }
}
