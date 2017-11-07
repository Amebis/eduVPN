/*
    eduVPN - End-user friendly VPN

    Copyright: 2017, The Commons Conservancy eduVPN Programme
    SPDX-License-Identifier: GPL-3.0+
*/

using Prism.Commands;
using Prism.Mvvm;
using System;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Threading;
using System.Windows.Threading;

namespace eduVPN.ViewModels
{
    /// <summary>
    /// Instance and profile select panel
    /// </summary>
    public class ConnectingInstanceAndProfileSelectPanel : BindableBase
    {
        #region Properties

        /// <summary>
        /// The page parent
        /// </summary>
        public ConnectWizard Parent { get; }

        /// <summary>
        /// Selected instance source type
        /// </summary>
        public Models.InstanceSourceType InstanceSourceType { get; }

        /// <summary>
        /// Selected instance source
        /// </summary>
        public Models.InstanceSourceInfo InstanceSource
        {
            get { return Parent.InstanceSources[(int)InstanceSourceType]; }
        }

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

                    // Reset selected profile.
                    SelectedProfile = null;
                }
            }
        }
        private JSON.Collection<Models.ProfileInfo> _profile_list;

        /// <summary>
        /// Selected profile
        /// </summary>
        public Models.ProfileInfo SelectedProfile
        {
            get { return _selected_profile; }
            set { if (value != _selected_profile) { _selected_profile = value; RaisePropertyChanged(); }; }
        }
        private Models.ProfileInfo _selected_profile;

        /// <summary>
        /// Connect selected profile command
        /// </summary>
        public DelegateCommand ConnectSelectedProfile
        {
            get
            {
                if (_connect_selected_profile == null)
                {
                    _connect_selected_profile = new DelegateCommand(
                        // execute
                        () =>
                        {
                            Parent.ChangeTaskCount(+1);
                            try
                            {
                                // Start VPN session.
                                var param = new ConnectWizard.StartSessionParams(
                                    InstanceSourceType,
                                    InstanceSource.AuthenticatingInstance,
                                    InstanceSource.ConnectingInstance,
                                    SelectedProfile);
                                if (Parent.StartSession.CanExecute(param))
                                    Parent.StartSession.Execute(param);
                            }
                            catch (Exception ex) { Parent.Error = ex; }
                            finally { Parent.ChangeTaskCount(-1); }
                        },

                        // canExecute
                        () => SelectedProfile != null);

                    // Setup canExecute refreshing.
                    PropertyChanged += (object sender, PropertyChangedEventArgs e) => { if (e.PropertyName == nameof(SelectedProfile)) _connect_selected_profile.RaiseCanExecuteChanged(); };
                }

                return _connect_selected_profile;
            }
        }
        private DelegateCommand _connect_selected_profile;

        /// <summary>
        /// Menu label for <c>ForgetSelectedConfiguration</c> command
        /// </summary>
        public string ForgetSelectedConfigurationLabel
        {
            get { return string.Format(Resources.Strings.InstanceForget, InstanceSource.ConnectingInstance); }
        }

        /// <summary>
        /// Forget selected configuration command
        /// </summary>
        public DelegateCommand ForgetSelectedConfiguration
        {
            get
            {
                if (_forget_selected_configuration == null)
                {
                    _forget_selected_configuration = new DelegateCommand(
                        // execute
                        () =>
                        {
                            Parent.ChangeTaskCount(+1);
                            try
                            {
                                // Remove instance from history.
                                InstanceSource.ConnectingInstanceList.Remove(InstanceSource.ConnectingInstance);
                                InstanceSource.ConnectingInstance = InstanceSource.ConnectingInstanceList.FirstOrDefault();

                                // Return to starting page. Should the abscence of configurations from history resolve in different starting page of course.
                                if (Parent.StartingPage != Parent.CurrentPage)
                                    Parent.CurrentPage = Parent.StartingPage;
                            }
                            catch (Exception ex) { Parent.Error = ex; }
                            finally { Parent.ChangeTaskCount(-1); }
                        },

                        // canExecute
                        () =>
                            InstanceSource is Models.LocalInstanceSourceInfo &&
                            InstanceSource.ConnectingInstance != null &&
                            InstanceSource.ConnectingInstanceList.IndexOf(InstanceSource.ConnectingInstance) >= 0 &&
                            !Parent.Sessions.Any(session =>
                                session.AuthenticatingInstance.Equals(InstanceSource.AuthenticatingInstance) &&
                                session.ConnectingInstance.Equals(InstanceSource.ConnectingInstance)));

                    // Setup canExecute refreshing.
                    InstanceSource.PropertyChanged += (object sender, PropertyChangedEventArgs e) => { if (e.PropertyName == nameof(InstanceSource.ConnectingInstance)) _forget_selected_configuration.RaiseCanExecuteChanged(); };
                    InstanceSource.ConnectingInstanceList.CollectionChanged += (object sender, NotifyCollectionChangedEventArgs e) => _forget_selected_configuration.RaiseCanExecuteChanged();
                    Parent.Sessions.CollectionChanged += (object sender, NotifyCollectionChangedEventArgs e) => _forget_selected_configuration.RaiseCanExecuteChanged();
                }

                return _forget_selected_configuration;
            }
        }
        private DelegateCommand _forget_selected_configuration;

        #endregion

        #region Constructors

        /// <summary>
        /// Constructs a panel
        /// </summary>
        /// <param name="parent">The page parent</param>
        /// <param name="instance_source_type">Instance source type</param>
        public ConnectingInstanceAndProfileSelectPanel(ConnectWizard parent, Models.InstanceSourceType instance_source_type)
        {
            Parent = parent;
            InstanceSourceType = instance_source_type;

            // Trigger initial load.
            InstanceSource_PropertyChanged(this, new PropertyChangedEventArgs(nameof(InstanceSource.ConnectingInstance)));
            InstanceSource.PropertyChanged += InstanceSource_PropertyChanged;
        }

        #endregion

        #region Methods

        private void InstanceSource_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(InstanceSource.ConnectingInstance))
            {
                RaisePropertyChanged(nameof(ForgetSelectedConfigurationLabel));

                ProfileList = null;
                if (InstanceSource.ConnectingInstance != null)
                {
                    new Thread(new ThreadStart(
                        () =>
                        {
                            Parent.Dispatcher.Invoke(DispatcherPriority.Normal, (Action)(() => Parent.ChangeTaskCount(+1)));
                            try
                            {
                                // Get and load profile list.
                                var profile_list = InstanceSource.ConnectingInstance.GetProfileList(InstanceSource.AuthenticatingInstance, Window.Abort.Token);

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

        #endregion
    }
}
