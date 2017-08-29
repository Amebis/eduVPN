/*
    eduVPN - End-user friendly VPN

    Copyright: 2017, The Commons Conservancy eduVPN Programme
    SPDX-License-Identifier: GPL-3.0+
*/

using Prism.Commands;
using System;
using System.Threading;
using System.Windows.Input;
using System.Windows.Threading;

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
            set { _profile_list = value; RaisePropertyChanged(); }
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
                _selected_profile = value;
                RaisePropertyChanged();
                ((DelegateCommandBase)ConnectSelectedProfile).RaiseCanExecuteChanged();
            }
        }
        private Models.ProfileInfo _selected_profile;

        /// <summary>
        /// Connect selected profile command
        /// </summary>
        public ICommand ConnectSelectedProfile
        {
            get
            {
                lock (_connect_profile_lock)
                {
                    if (_connect_profile == null)
                        _connect_profile = new DelegateCommand(DoConnectSelectedProfile, CanConnectSelectedProfile);

                    return _connect_profile;
                }
            }
        }
        private ICommand _connect_profile;
        private object _connect_profile_lock = new object();

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
            ProfileList = null;

            // Reset selected profile, to prevent automatic continuation to
            // status page.
            SelectedProfile = null;
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

            if (Parent.InstanceSource is Models.FederatedInstanceSourceInfo)
                Parent.CurrentPage = Parent.InstanceSourceSelectPage;
            else if (Parent.InstanceSource is Models.DistributedInstanceSourceInfo)
                Parent.CurrentPage = Parent.AuthenticatingInstanceSelectPage;
            else if (Parent.InstanceSource.IndexOf(Parent.Configuration.AuthenticatingInstance) >= 0)
                Parent.CurrentPage = Parent.AuthenticatingInstanceSelectPage;
            else
                Parent.CurrentPage = Parent.CustomInstancePage;
        }

        protected override bool CanNavigateBack()
        {
            return true;
        }

        #endregion
    }
}
