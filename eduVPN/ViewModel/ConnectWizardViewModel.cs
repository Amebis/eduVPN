/*
    eduVPN - End-user friendly VPN

    Copyright: 2017, The Commons Conservancy eduVPN Programme
    SPDX-License-Identifier: GPL-3.0+
*/

using eduOAuth;
using Prism.Mvvm;
using System;

namespace eduVPN.ViewModel
{
    /// <summary>
    /// Connect wizard
    /// </summary>
    public class ConnectWizardViewModel : BindableBase, IDisposable
    {
        #region Properties

        /// <summary>
        /// The page the wizard is currently displaying
        /// </summary>
        public ConnectWizardPageViewModel CurrentPage
        {
            get { return _current_page; }
            set
            {
                _current_page = value;
                RaisePropertyChanged();
                _current_page.OnActivate();
            }
        }
        private ConnectWizardPageViewModel _current_page;

        /// <summary>
        /// Access type page
        /// </summary>
        public AccessTypePageViewModel AccessTypePage
        {
            get
            {
                if (_access_type_page == null)
                    _access_type_page = new AccessTypePageViewModel(this);
                return _access_type_page;
            }
        }
        private AccessTypePageViewModel _access_type_page;

        /// <summary>
        /// User required VPN access type
        /// </summary>
        public AccessTypeType AccessType
        {
            get { return _access_type; }
            set { if (value != _access_type) { _access_type = value; RaisePropertyChanged(); } }
        }
        private AccessTypeType _access_type;

        /// <summary>
        /// Instance selection page
        /// </summary>
        /// <remarks>This wizard page is pre-created to allow instance list population in advance.</remarks>
        public InstanceSelectPageViewModel InstanceSelectPage { get => _instance_select_page; }
        private InstanceSelectPageViewModel _instance_select_page;

        /// <summary>
        /// Custom instance page
        /// </summary>
        public CustomInstancePageViewModel CustomInstancePage
        {
            get
            {
                if (_custom_instance_page == null)
                    _custom_instance_page = new CustomInstancePageViewModel(this);
                return _custom_instance_page;
            }
        }
        private CustomInstancePageViewModel _custom_instance_page;

        /// <summary>
        /// Selected instance base URI.
        /// </summary>
        public Uri InstanceURI
        {
            get { return _instance_uri; }
            set { if (value != _instance_uri) { _instance_uri = value; RaisePropertyChanged(); } }
        }
        private Uri _instance_uri;

        /// <summary>
        /// Is the instance custom (user-provided)?
        /// </summary>
        public bool IsCustomInstance
        {
            get { return _is_custom_instance; }
            set { if (value != _is_custom_instance) { _is_custom_instance = value; RaisePropertyChanged(); } }
        }
        private bool _is_custom_instance;

        /// <summary>
        /// Authorization wizard page
        /// </summary>
        public AuthorizationPageViewModel AuthorizationPage
        {
            get
            {
                if (_authorization_page == null)
                    _authorization_page = new AuthorizationPageViewModel(this);
                return _authorization_page;
            }
        }
        private AuthorizationPageViewModel _authorization_page;

        /// <summary>
        /// Instance API endpoints
        /// </summary>
        public API Endpoints
        {
            get { return _endpoints; }
            set { _endpoints = value; RaisePropertyChanged(); }
        }
        private API _endpoints;

        /// <summary>
        /// OAuth access token
        /// </summary>
        public AccessToken AccessToken
        {
            get { return _access_token; }
            set { _access_token = value; RaisePropertyChanged(); }
        }
        private AccessToken _access_token;

        #endregion

        #region Constructors

        /// <summary>
        /// Constructs the wizard
        /// </summary>
        public ConnectWizardViewModel()
        {
            // Pre-create instance select page to allow instance list population in advance.
            _instance_select_page = new InstanceSelectPageViewModel(this);

            CurrentPage = AccessTypePage;
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
                    if (_authorization_page != null)
                        _authorization_page.Dispose();
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
