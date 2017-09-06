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
    /// Instance and profile select panel base class
    /// </summary>
    public class ConnectingInstanceAndProfileSelectPanel : ConfigurationSelectBasePanel
    {
        #region Properties

        /// <summary>
        /// Authenticating instance
        /// </summary>
        public Models.InstanceInfo AuthenticatingInstance
        {
            get { return _authenticating_instance; }
            set { if (value != _authenticating_instance) { _authenticating_instance = value; RaisePropertyChanged(); } }
        }
        private Models.InstanceInfo _authenticating_instance;

        /// <summary>
        /// Selected connecting instance
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
                    ConnectProfile.RaiseCanExecuteChanged();

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
                                    var profile_list = _selected_instance.GetProfileList(AuthenticatingInstance, Window.Abort.Token);

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
            set { if (value != _selected_profile) { _selected_profile = value; RaisePropertyChanged(); } }
        }
        protected Models.ProfileInfo _selected_profile;

        /// <summary>
        /// Connect selected profile command
        /// </summary>
        public DelegateCommand<Models.ProfileInfo> ConnectProfile
        {
            get
            {
                if (_connect_profile == null)
                    _connect_profile = new DelegateCommand<Models.ProfileInfo>(
                        // execute
                        profile =>
                        {
                            Parent.ChangeTaskCount(+1);
                            try
                            {
                                // Start VPN session.
                                var param = new ConnectWizard.StartSessionParams(
                                    InstanceSourceType,
                                    new Models.VPNConfiguration()
                                    {
                                        AuthenticatingInstance = AuthenticatingInstance,
                                        ConnectingInstance = SelectedInstance,
                                        ConnectingProfile = profile
                                    });
                                if (Parent.StartSession.CanExecute(param))
                                    Parent.StartSession.Execute(param);
                            }
                            catch (Exception ex) { Parent.Error = ex; }
                            finally { Parent.ChangeTaskCount(-1); }
                        },

                        // canExecute
                        profile => SelectedInstance != null && profile != null);

                return _connect_profile;
            }
        }
        private DelegateCommand<Models.ProfileInfo> _connect_profile;

        #endregion

        #region Constructors

        /// <summary>
        /// Constructs a panel
        /// </summary>
        /// <param name="parent">The page parent</param>
        /// <param name="instance_source_type">Instance source type</param>
        /// <param name="authenticating_instance">Authenticating instance</param>
        public ConnectingInstanceAndProfileSelectPanel(ConnectWizard parent, Models.InstanceSourceType instance_source_type) :
            base(parent, instance_source_type)
        {
        }

        #endregion
    }
}
