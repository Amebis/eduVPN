/*
    eduVPN - End-user friendly VPN

    Copyright: 2017, The Commons Conservancy eduVPN Programme
    SPDX-License-Identifier: GPL-3.0+
*/

using Prism.Mvvm;
using System;

namespace eduVPN.ViewModels
{
    /// <summary>
    /// Connect wizard
    /// </summary>
    public class ConnectWizard : BindableBase, IDisposable
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
        /// eduVPN instance selected
        /// </summary>
        /// <remarks><c>null</c> if none selected.</remarks>
        public Instance Instance
        {
            get { return _instance; }
            set
            {
                _instance = value;
                RaisePropertyChanged();
            }
        }
        private Instance _instance;

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
        /// Instance selection page
        /// </summary>
        /// <remarks>This wizard page is pre-created to allow instance list population in advance.</remarks>
        public InstanceSelectPage InstanceSelectPage { get => _instance_select_page; }
        private InstanceSelectPage _instance_select_page;

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

        #endregion

        #endregion

        #region Constructors

        /// <summary>
        /// Constructs the wizard
        /// </summary>
        public ConnectWizard()
        {
            // Pre-create instance select page to allow instance list population in advance.
            _instance_select_page = new InstanceSelectPage(this);

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
