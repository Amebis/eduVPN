/*
    eduVPN - End-user friendly VPN

    Copyright: 2017, The Commons Conservancy eduVPN Programme
    SPDX-License-Identifier: GPL-3.0+
*/

using eduVPN.JSON;
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
                if (_connect_profile == null)
                    _connect_profile = new DelegateCommand(DoConnectSelectedProfile, CanConnectSelectedProfile);
                return _connect_profile;
            }
        }
        private ICommand _connect_profile;

        /// <summary>
        /// User info
        /// </summary>
        public Models.UserInfo UserInfo
        {
            get { return _user_info; }
            set { _user_info = value; RaisePropertyChanged(); }
        }
        private Models.UserInfo _user_info;

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

            // Set blank user info. This prevents flickering of user disabled message,
            // since UserInfo.IsEnabled will be available for binding before page displays.
            UserInfo = new Models.UserInfo();

            // Launch user info load in the background.
            ThreadPool.QueueUserWorkItem(new WaitCallback(
                param =>
                {
                    Parent.Dispatcher.Invoke(DispatcherPriority.Normal, (Action)(() => TaskCount++));

                    try
                    {
                        // Get and load user info. (Don't catch the 401, loading the profile list will handle it.)
                        var user_info = new Models.UserInfo();
                        user_info.LoadJSONAPIResponse(JSON.Response.Get(
                            Parent.AuthenticatingEndpoints.UserInfo,
                            null,
                            Parent.AccessToken,
                            null,
                            ConnectWizard.Abort.Token).Value, "user_info", ConnectWizard.Abort.Token);
                        Parent.Dispatcher.Invoke(DispatcherPriority.Normal, (Action)(() => UserInfo = user_info));
                    }
                    catch (OperationCanceledException) { }
                    catch (Exception ex)
                    {
                        // Notify the sender the profile list loading failed.
                        Parent.Dispatcher.Invoke(DispatcherPriority.Normal, (Action)(() => ErrorMessage = ex.Message));
                    }
                    finally
                    {
                        Parent.Dispatcher.Invoke(DispatcherPriority.Normal, (Action)(() => TaskCount--));
                    }
                }));
        }

        /// <summary>
        /// Called when ConnectSelectedProfile command is invoked.
        /// </summary>
        protected virtual void DoConnectSelectedProfile()
        {
            Parent.ConnectingProfile = SelectedProfile;
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
            if (Parent.AuthenticatingInstance.IsCustom)
                Parent.CurrentPage = Parent.CustomInstancePage;
            else
                switch (Parent.AccessType)
                {
                    case AccessType.SecureInternet: Parent.CurrentPage = Parent.SecureInternetSelectPage; break;
                    case AccessType.InstituteAccess: Parent.CurrentPage = Parent.InstituteAccessSelectPage; break;
                }
        }

        protected override bool CanNavigateBack()
        {
            if (Parent.AuthenticatingInstance.IsCustom)
                return true;
            else
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
