/*
    eduVPN - End-user friendly VPN

    Copyright: 2017, The Commons Conservancy eduVPN Programme
    SPDX-License-Identifier: GPL-3.0+
*/

using Prism.Commands;

namespace eduVPN.ViewModels
{
    /// <summary>
    /// Profile selection base wizard page
    /// </summary>
    public class ProfileSelectBasePage : ConnectWizardPage
    {
        #region Properties

        /// <summary>
        /// List of available profiles
        /// </summary>
        public JSON.Collection<Models.ProfileInfo> ProfileList
        {
            get { return _profile_list; }
            set {
                if (value != _profile_list)
                {
                    _profile_list = value;
                    RaisePropertyChanged();

                    // The list of profiles changed, reset selected profile.
                    SelectedProfile = null;
                }
            }
        }
        private JSON.Collection<Models.ProfileInfo> _profile_list;

        /// <summary>
        /// Selected profile
        /// </summary>
        /// <remarks><c>null</c> if none selected.</remarks>
        public Models.ProfileInfo SelectedProfile
        {
            get { return _selected_profile; }
            set { if (value != _selected_profile) { _selected_profile = value; RaisePropertyChanged(); } }
        }
        private Models.ProfileInfo _selected_profile;

        /// <summary>
        /// Connect selected profile command
        /// </summary>
        public DelegateCommand<Models.ProfileInfo> ConnectProfile
        {
            get
            {
                if (_connect_profile == null)
                    _connect_profile = new DelegateCommand<Models.ProfileInfo>(DoConnectSelectedProfile, CanConnectSelectedProfile);

                return _connect_profile;
            }
        }
        private DelegateCommand<Models.ProfileInfo> _connect_profile;

        #endregion

        #region Constructors

        /// <summary>
        /// Constructs a profile selection wizard page.
        /// </summary>
        /// <param name="parent">The page parent</param>
        public ProfileSelectBasePage(ConnectWizard parent) :
            base(parent)
        {
        }

        #endregion

        #region Methods

        /// <summary>
        /// Called when ConnectProfile command is invoked.
        /// </summary>
        protected virtual void DoConnectSelectedProfile(Models.ProfileInfo profile)
        {
            // Save selected profile.
            Parent.Configuration.ConnectingProfile = profile;

            // Go to status page.
            Parent.CurrentPage = Parent.StatusPage;
        }

        /// <summary>
        /// Called to test if ConnectProfile command is enabled.
        /// </summary>
        /// <returns><c>true</c> if enabled; <c>false</c> otherwise</returns>
        protected virtual bool CanConnectSelectedProfile(Models.ProfileInfo profile)
        {
            return profile != null;
        }

        protected override void DoNavigateBack()
        {
            base.DoNavigateBack();

            if (Parent.Configuration.InstanceSource is Models.LocalInstanceSourceInfo)
            {
                if (Parent.Configuration.InstanceSource.IndexOf(Parent.Configuration.AuthenticatingInstance) >= 0)
                    Parent.CurrentPage = Parent.AuthenticatingInstanceSelectPage;
                else
                    Parent.CurrentPage = Parent.CustomInstancePage;
            } else
                Parent.CurrentPage = Parent.InstanceSourceSelectPage;
        }

        protected override bool CanNavigateBack()
        {
            return true;
        }

        #endregion
    }
}
