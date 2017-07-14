/*
    eduVPN - End-user friendly VPN

    Copyright: 2017, The Commons Conservancy eduVPN Programme
    SPDX-License-Identifier: GPL-3.0+
*/

using Prism.Mvvm;
using System;

namespace eduVPN.ViewModel
{
    /// <summary>
    /// Connect wizard
    /// </summary>
    public class ConnectWizardViewModel : BindableBase
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
                _current_page.OnActivate();
                RaisePropertyChanged();
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
    }
}
