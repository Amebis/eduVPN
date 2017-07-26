/*
    eduVPN - End-user friendly VPN

    Copyright: 2017, The Commons Conservancy eduVPN Programme
    SPDX-License-Identifier: GPL-3.0+
*/

using eduOAuth;
using Prism.Mvvm;
using System;
using System.Windows.Threading;

namespace eduVPN.ViewModels
{
    /// <summary>
    /// Connect wizard
    /// </summary>
    public class ConnectWizard : BindableBase
    {
        #region Properties

        /// <summary>
        /// User required VPN access type
        /// </summary>
        public AccessType AccessType
        {
            get { return _access_type; }
            set { if (value != _access_type) { _access_type = value; RaisePropertyChanged(); } }
        }
        private AccessType _access_type;

        /// <summary>
        /// Authenticating eduVPN instance
        /// </summary>
        /// <remarks><c>null</c> if none selected.</remarks>
        public Models.InstanceInfo AuthenticatingInstance
        {
            get { return _authenticating_instance; }
            set
            {
                _authenticating_instance = value;
                RaisePropertyChanged();
            }
        }
        private Models.InstanceInfo _authenticating_instance;

        /// <summary>
        /// Authenticating eduVPN instance API endpoints
        /// </summary>
        public Models.InstanceEndpoints AuthenticatingEndpoints
        {
            get { return _authenticating_endpoints; }
            set { _authenticating_endpoints = value; RaisePropertyChanged(); }
        }
        private Models.InstanceEndpoints _authenticating_endpoints;

        /// <summary>
        /// OAuth access token
        /// </summary>
        public AccessToken AccessToken
        {
            get { return _access_token; }
            set { _access_token = value; RaisePropertyChanged(); }
        }
        private AccessToken _access_token;

        /// <summary>
        /// Connecting eduVPN instance
        /// </summary>
        /// <remarks><c>null</c> if none selected.</remarks>
        public Models.InstanceInfo ConnectingInstance
        {
            get { return _connecting_instance; }
            set
            {
                _connecting_instance = value;
                RaisePropertyChanged();
            }
        }
        private Models.InstanceInfo _connecting_instance;

        /// <summary>
        /// Connecting eduVPN instance API endpoints
        /// </summary>
        public Models.InstanceEndpoints ConnectingEndpoints
        {
            get { return _connecting_endpoints; }
            set { _connecting_endpoints = value; RaisePropertyChanged(); }
        }
        private Models.InstanceEndpoints _connecting_endpoints;

        /// <summary>
        /// Connecting eduVPN instance profile
        /// </summary>
        /// <remarks><c>null</c> if none selected.</remarks>
        public Models.ProfileInfo ConnectingProfile
        {
            get { return _connecting_profile; }
            set { _connecting_profile = value; RaisePropertyChanged(); }
        }
        private Models.ProfileInfo _connecting_profile;

        #region Pages

        /// <summary>
        /// The page the wizard is currently displaying
        /// </summary>
        public ConnectWizardPage CurrentPage
        {
            get { return _current_page; }
            set
            {
                _current_page = value;
                RaisePropertyChanged();
                _current_page.OnActivate();
            }
        }
        private ConnectWizardPage _current_page;

        /// <summary>
        /// Access type page
        /// </summary>
        public AccessTypePage AccessTypePage
        {
            get
            {
                if (_access_type_page == null)
                    _access_type_page = new AccessTypePage(this);
                return _access_type_page;
            }
        }
        private AccessTypePage _access_type_page;

        /// <summary>
        /// Secure internet selection page
        /// </summary>
        /// <remarks>This wizard page is pre-created to allow instance list population in advance.</remarks>
        public SecureInternetSelectPage SecureInternetSelectPage { get => _secure_internet_select_page; }
        private SecureInternetSelectPage _secure_internet_select_page;

        /// <summary>
        /// Institute access selection page
        /// </summary>
        /// <remarks>This wizard page is pre-created to allow instance list population in advance.</remarks>
        public InstituteAccessSelectPage InstituteAccessSelectPage { get => _institute_access_select_page; }
        private InstituteAccessSelectPage _institute_access_select_page;

        /// <summary>
        /// Custom instance page
        /// </summary>
        public CustomInstancePage CustomInstancePage
        {
            get
            {
                if (_custom_instance_page == null)
                    _custom_instance_page = new CustomInstancePage(this);
                return _custom_instance_page;
            }
        }
        private CustomInstancePage _custom_instance_page;

        /// <summary>
        /// Authorization wizard page
        /// </summary>
        public AuthorizationPage AuthorizationPage
        {
            get
            {
                if (_authorization_page == null)
                    _authorization_page = new AuthorizationPage(this);
                return _authorization_page;
            }
        }
        private AuthorizationPage _authorization_page;

        /// <summary>
        /// Profile selection wizard page
        /// </summary>
        public ProfileSelectPage ProfileSelectPage
        {
            get
            {
                if (_profile_select_page == null)
                    _profile_select_page = new ProfileSelectPage(this);
                return _profile_select_page;
            }
        }
        private ProfileSelectPage _profile_select_page;

        /// <summary>
        /// Instance and profile selection wizard page (for federated authentication)
        /// </summary>
        public InstanceAndProfileSelectPage InstanceAndProfileSelectPage
        {
            get
            {
                if (_instance_and_profile_select_page == null)
                    _instance_and_profile_select_page = new InstanceAndProfileSelectPage(this);
                return _instance_and_profile_select_page;
            }
        }
        private InstanceAndProfileSelectPage _instance_and_profile_select_page;

        #endregion

        #endregion

        #region Constructors

        /// <summary>
        /// Constructs the wizard
        /// </summary>
        public ConnectWizard()
        {
            // Pre-create instance select pages to allow instance list population in advance.
            _secure_internet_select_page = new SecureInternetSelectPage(this);
            _institute_access_select_page = new InstituteAccessSelectPage(this);

            CurrentPage = AccessTypePage;

            Dispatcher.CurrentDispatcher.ShutdownStarted += (object sender, EventArgs e) => {
                // Persist settings to disk.
                Properties.Settings.Default.Save();
            };
        }

        #endregion
    }
}
