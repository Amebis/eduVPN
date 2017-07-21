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
    /// Profile selection wizard page
    /// </summary>
    public class ProfileSelectPage : ConnectWizardPage
    {
        #region Properties

        /// <summary>
        /// List of available profiles
        /// </summary>
        public JSON.Collection<JSON.Profile> ProfileList
        {
            get { return _profile_list; }
            set { _profile_list = value; RaisePropertyChanged(); }
        }
        private JSON.Collection<JSON.Profile> _profile_list;

        /// <summary>
        /// Selected profile
        /// </summary>
        /// <remarks><c>null</c> if none selected.</remarks>
        public JSON.Profile SelectedProfile
        {
            get { return _selected_profile; }
            set
            {
                _selected_profile = value;
                RaisePropertyChanged();
                ((DelegateCommandBase)ConnectSelectedProfile).RaiseCanExecuteChanged();
            }
        }
        private JSON.Profile _selected_profile;

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
                        () => SelectedProfile != null);
                }
                return _connect_profile;
            }
        }
        private ICommand _connect_profile;

        /// <summary>
        /// User info
        /// </summary>
        public JSON.UserInfo UserInfo
        {
            get { return _user_info; }
            set { _user_info = value; RaisePropertyChanged(); }
        }
        private JSON.UserInfo _user_info;

        #endregion

        #region Constructors

        /// <summary>
        /// Constructs a profile selection wizard page.
        /// </summary>
        /// <param name="parent">The page parent</param>
        public ProfileSelectPage(ConnectWizard parent) :
            base(parent)
        {
            ProfileList = new JSON.Collection<JSON.Profile>();
            // Launch profile list load in the background.
            ThreadPool.QueueUserWorkItem(new WaitCallback(
                param =>
                {
                    try
                    {
                        // Set busy flag (in the UI thread).
                        _dispatcher.Invoke(DispatcherPriority.Normal, (Action)(() => IsBusy = true));

                        // Trigger profile list loading.
                        var profile_list_load_task = JSON.Response.GetAsync(
                            Parent.Endpoints.ProfileList,
                            null,
                            Parent.AccessToken,
                            null,
                            _abort.Token);

                        // Trigger user info loading.
                        var user_info_load_task = JSON.Response.GetAsync(
                            Parent.Endpoints.UserInfo,
                            null,
                            Parent.AccessToken,
                            null,
                            _abort.Token);

                        // Wait for profile list.
                        try { profile_list_load_task.Wait(_abort.Token); }
                        catch (AggregateException ex) { throw ex.InnerException; }

                        // Load profile list.
                        var profile_list = new JSON.Collection<JSON.Profile>();
                        profile_list.LoadJSONAPIResponse(profile_list_load_task.Result.Value, "profile_list", _abort.Token);

                        // Send the loaded profile list back to the UI thread.
                        _dispatcher.Invoke(DispatcherPriority.Normal, (Action)(() =>
                        {
                            ProfileList = profile_list;
                            ErrorMessage = null;
                        }));

                        // Wait for and process user info.
                        try
                        {
                            user_info_load_task.Wait(_abort.Token);
                            var user_info = new JSON.UserInfo();
                            user_info.LoadJSONAPIResponse(user_info_load_task.Result.Value, "user_info", _abort.Token);
                            _dispatcher.Invoke(DispatcherPriority.Normal, (Action)(() => UserInfo = user_info));
                        }
                        catch (Exception) { }
                    }
                    catch (OperationCanceledException) {}
                    catch (Exception ex)
                    {
                        // Notify the sender the profile list loading failed.
                        _dispatcher.Invoke(DispatcherPriority.Normal, (Action)(() => ErrorMessage = ex.Message));
                    }
                    finally
                    {
                        // Clear busy flag (in the UI thread).
                        _dispatcher.Invoke(DispatcherPriority.Normal, (Action)(() => IsBusy = false));
                    }
                }));
        }

        #endregion

        #region Methods

        protected override void DoNavigateBack()
        {
            if (Parent.Instance.IsCustom)
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
            if (Parent.Instance.IsCustom)
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
