/*
    eduVPN - End-user friendly VPN

    Copyright: 2017, The Commons Conservancy eduVPN Programme
    SPDX-License-Identifier: GPL-3.0+
*/

using Prism.Commands;
using System;
using System.Threading;
using System.Windows.Threading;

namespace eduVPN.ViewModels
{
    /// <summary>
    /// Distributed/federated authenticated configuration history panel base class
    /// </summary>
    public class ConnectingInstanceAndProfileSelectPanel : ConfigurationHistoryPanel
    {
        #region Properties

        /// <summary>
        /// Selected instance
        /// </summary>
        /// <remarks><c>null</c> if none selected.</remarks>
        public Models.InstanceInfo SelectedInstance
        {
            get { return _selected_instance; }
            set
            {
                if (value != _selected_instance)
                {
                    _selected_instance = value;
                    RaisePropertyChanged();

                    ProfileList = new JSON.Collection<Models.ProfileInfo>();
                    if (_selected_instance != null)
                    {
                        new Thread(new ThreadStart(
                            () =>
                            {
                                Parent.Dispatcher.Invoke(DispatcherPriority.Normal, (Action)(() => Parent.ChangeTaskCount(+1)));
                                try
                                {
                                    // Get and load profile list.
                                    var profile_list = _selected_instance.GetProfileList(ConfigurationHistory[0].AuthenticatingInstance, Window.Abort.Token);

                                    // Send the loaded profile list back to the UI thread.
                                    // We're not navigating to another page and OnActivate() will not be called to auto-reset error message. Therefore, reset it manually.
                                    Parent.Dispatcher.Invoke(DispatcherPriority.Normal, (Action)(
                                        () =>
                                        {
                                            ProfileList = profile_list;
                                            Parent.Error = null;
                                        }));
                                }
                                catch (OperationCanceledException) { }
                                catch (Exception ex) { Parent.Dispatcher.Invoke(DispatcherPriority.Normal, (Action)(() => Parent.Error = ex)); }
                                finally { Parent.Dispatcher.Invoke(DispatcherPriority.Normal, (Action)(() => Parent.ChangeTaskCount(-1))); }
                            })).Start();
                    }
                }
            }
        }
        private Models.InstanceInfo _selected_instance;

        /// <summary>
        /// List of available profiles
        /// </summary>
        public JSON.Collection<Models.ProfileInfo> ProfileList
        {
            get { return _profile_list; }
            set
            {
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
        protected Models.ProfileInfo _selected_profile;

        /// <summary>
        /// Connect selected profile command
        /// </summary>
        public DelegateCommand ConnectSelectedProfile
        {
            get
            {
                if (_connect_selected_profile == null)
                    _connect_selected_profile = new DelegateCommand(
                        // execute
                        () =>
                        {
                            // Save selected instance.
                            ConfigurationHistory[0].ConnectingInstance = SelectedInstance;

                            // Save connecting profile
                            ConfigurationHistory[0].ConnectingProfile = SelectedProfile;

                            // Reset selected profile, to prevent repetitive triggering.
                            SelectedProfile = null;

                            // Set configuration.
                            Parent.Configuration = ConfigurationHistory[0];

                            // Go to status page.
                            Parent.CurrentPage = Parent.StatusPage;
                        },

                        // canExecute
                        () => SelectedProfile != null);

                return _connect_selected_profile;
            }
        }
        private DelegateCommand _connect_selected_profile;

        #endregion

        #region Constructors

        /// <summary>
        /// Constructs history panel
        /// </summary>
        /// <param name="parent">The page parent</param>
        /// <param name="instance_source_type">Instance source type</param>
        public ConnectingInstanceAndProfileSelectPanel(ConnectWizard parent, Models.InstanceSourceType instance_source_type) :
            base(parent, instance_source_type)
        {
        }

        #endregion

        #region Methods

        public override void OnActivate()
        {
            base.OnActivate();

            if (ConfigurationHistory.Count > 0)
            {
                // Initialize selected instance.
                SelectedInstance = ConfigurationHistory[0].ConnectingInstance;
            }
        }

        #endregion
    }
}
