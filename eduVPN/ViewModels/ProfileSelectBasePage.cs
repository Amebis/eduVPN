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
            set
            {
                if (value != _selected_profile)
                {
                    _selected_profile = value;
                    RaisePropertyChanged();
                    ConnectSelectedProfile.RaiseCanExecuteChanged();
                }
            }
        }
        private Models.ProfileInfo _selected_profile;

        /// <summary>
        /// Connect selected profile command
        /// </summary>
        public DelegateCommand ConnectSelectedProfile
        {
            get
            {
                lock (_connect_selected_profile_lock)
                {
                    if (_connect_selected_profile == null)
                        _connect_selected_profile = new DelegateCommand(DoConnectSelectedProfile, CanConnectSelectedProfile);

                    return _connect_selected_profile;
                }
            }
        }
        private DelegateCommand _connect_selected_profile;
        private object _connect_selected_profile_lock = new object();

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

        public override void OnActivate()
        {
            base.OnActivate();

            // Reset profile list. It should get reloaded by the inheriting page.
            // This will also reset selected profile, to prevent automatic continuation
            // to the status page.
            ProfileList = null;
        }

        /// <summary>
        /// Called when ConnectSelectedProfile command is invoked.
        /// </summary>
        protected virtual void DoConnectSelectedProfile()
        {
            Parent.Configuration.ConnectingProfile = SelectedProfile;
            Parent.CurrentPage = Parent.StatusPage;
        }

        /// <summary>
        /// Called to test if ConnectSelectedProfile command is enabled.
        /// </summary>
        /// <returns><c>true</c> if enabled; <c>false</c> otherwise</returns>
        protected virtual bool CanConnectSelectedProfile()
        {
            return SelectedProfile != null;
        }

        protected override void DoNavigateBack()
        {
            base.DoNavigateBack();

            if (Parent.InstanceSource is Models.LocalInstanceSourceInfo)
            {
                if (Parent.InstanceSource.IndexOf(Parent.Configuration.AuthenticatingInstance) >= 0)
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
