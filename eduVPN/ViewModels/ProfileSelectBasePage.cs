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
                {
                    _connect_profile = new DelegateCommand(
                        // execute
                        () =>
                        {
                        },

                        // canExecute
                        () => ErrorMessage == null && SelectedProfile != null);
                }
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
            ProfileList = new JSON.Collection<Models.ProfileInfo>();

            // Launch user info load in the background.
            ThreadPool.QueueUserWorkItem(new WaitCallback(
                param =>
                {
                    // Set busy flag (in the UI thread).
                    _dispatcher.Invoke(DispatcherPriority.Normal, (Action)(() => TaskCount++));

                    try
                    {
                        // Get and load user info.
                        var user_info = new Models.UserInfo();
                        user_info.LoadJSONAPIResponse(JSON.Response.Get(
                            Parent.AuthenticatingEndpoints.UserInfo,
                            null,
                            Parent.AccessToken,
                            null,
                            _abort.Token).Value, "user_info", _abort.Token);
                        _dispatcher.Invoke(DispatcherPriority.Normal, (Action)(() => UserInfo = user_info));
                    }
                    catch (OperationCanceledException) { }
                    catch (Exception ex)
                    {
                        // Notify the sender the profile list loading failed.
                        _dispatcher.Invoke(DispatcherPriority.Normal, (Action)(() => ErrorMessage = ex.Message));
                    }
                    finally
                    {
                        // Clear busy flag (in the UI thread).
                        _dispatcher.Invoke(DispatcherPriority.Normal, (Action)(() => TaskCount--));
                    }
                }));
        }

        #endregion

        #region Methods

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
