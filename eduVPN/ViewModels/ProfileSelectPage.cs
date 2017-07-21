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
        public JSON.Collection<Profile> ProfileList
        {
            get { return _profile_list; }
            set { _profile_list = value; RaisePropertyChanged(); }
        }
        private JSON.Collection<Profile> _profile_list;


        /// <summary>
        /// Selected profile
        /// </summary>
        /// <remarks><c>null</c> if none selected.</remarks>
        public Profile SelectedProfile
        {
            get { return _selected_profile; }
            set
            {
                _selected_profile = value;
                RaisePropertyChanged();
                ((DelegateCommandBase)ConnectSelectedProfile).RaiseCanExecuteChanged();
            }
        }
        private Profile _selected_profile;

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

        #endregion

        #region Constructors

        /// <summary>
        /// Constructs an profile selection wizard page.
        /// </summary>
        /// <param name="parent">The page parent</param>
        public ProfileSelectPage(ConnectWizard parent) :
            base(parent)
        {
            ProfileList = new Collection<Profile>();

            // Launch profile list load in the background.
            ThreadPool.QueueUserWorkItem(new WaitCallback(
                param =>
                {
                    try
                    {
                        // Set busy flag (in the UI thread).
                        _dispatcher.Invoke(DispatcherPriority.Normal, (Action)(() => IsBusy = true));

                        // Get and load profile list.
                        var profile_list = new Collection<Profile>();
                        profile_list.LoadJSONAPIResponse(JSON.Response.Get(
                            Parent.Instance.Endpoints.ProfileList,
                            null,
                            Parent.Instance.AccessToken,
                            null,
                            _abort.Token).Value,
                            "profile_list");

                        // Send the loaded profile list back to the UI thread.
                        _dispatcher.Invoke(DispatcherPriority.Normal, (Action)(() =>
                        {
                            ProfileList = profile_list;
                            ErrorMessage = null;
                        }));
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
                Parent.CurrentPage = Parent.InstanceSelectPage;
        }

        protected override bool CanNavigateBack()
        {
            return true;
        }

        #endregion
    }
}
