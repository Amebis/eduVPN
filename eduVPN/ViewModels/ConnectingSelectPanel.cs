/*
    eduVPN - End-user friendly VPN

    Copyright: 2017, The Commons Conservancy eduVPN Programme
    SPDX-License-Identifier: GPL-3.0+
*/

using Prism.Commands;
using Prism.Mvvm;
using System;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;

namespace eduVPN.ViewModels
{
    /// <summary>
    /// Connecting instance and/or profile select panel base class
    /// </summary>
    public class ConnectingSelectPanel : BindableBase
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
        public Models.InstanceSource InstanceSource
        {
            get { return Parent.InstanceSources[(int)InstanceSourceType]; }
        }

        /// <summary>
        /// Currently selected instance
        /// </summary>
        public Models.Instance SelectedInstance
        {
            get { return _selected_instance; }
            set { SetProperty(ref _selected_instance, value); }
        }
        private Models.Instance _selected_instance;

        /// <summary>
        /// Set connecting instance command
        /// </summary>
        public DelegateCommand SetConnectingInstance
        {
            get
            {
                if (_set_connecting_instance == null)
                {
                    _set_connecting_instance = new DelegateCommand(
                        // execute
                        () =>
                        {
                            Parent.ChangeTaskCount(+1);
                            try
                            {
                                Parent.InstanceSourceType = InstanceSourceType;
                                InstanceSource.ConnectingInstance = SelectedInstance;

                                // Go to profile selection page.
                                Parent.CurrentPage = Parent.ConnectingProfileSelectPage;
                            }
                            catch (Exception ex) { Parent.Error = ex; }
                            finally { Parent.ChangeTaskCount(-1); }
                        },

                        // canExecute
                        () => SelectedInstance != null);

                    // Setup canExecute refreshing.
                    PropertyChanged += (object sender, PropertyChangedEventArgs e) => { if (e.PropertyName == nameof(SelectedInstance)) _set_connecting_instance.RaiseCanExecuteChanged(); };
                }

                return _set_connecting_instance;
            }
        }
        private DelegateCommand _set_connecting_instance;

        /// <summary>
        /// Menu label for <c>ForgetSelectedInstance</c> command
        /// </summary>
        public string ForgetSelectedInstanceLabel
        {
            get { return string.Format(Resources.Strings.InstanceForget, SelectedInstance); }
        }

        /// <summary>
        /// Forget selected instance command
        /// </summary>
        public DelegateCommand ForgetSelectedInstance
        {
            get
            {
                if (_forget_selected_instance == null)
                {
                    _forget_selected_instance = new DelegateCommand(
                        // execute
                        () =>
                        {
                            Parent.ChangeTaskCount(+1);
                            try
                            {
                                if (InstanceSource is Models.LocalInstanceSource instance_source_local)
                                {
                                    // Remove all instance profiles from history.
                                    var instance = SelectedInstance;
                                    for (var i = instance_source_local.ConnectingProfileList.Count; i-- > 0;)
                                        if (instance_source_local.ConnectingProfileList[i].Instance.Equals(instance))
                                            instance_source_local.ConnectingProfileList.RemoveAt(i);

                                    // Remove the instance from history.
                                    instance_source_local.ConnectingInstanceList.Remove(instance);
                                    if (instance_source_local.ConnectingInstance != null && instance_source_local.ConnectingInstance.Equals(instance))
                                    {
                                        // This was the connecting instance.
                                        instance_source_local.ConnectingInstance = instance_source_local.ConnectingInstanceList.FirstOrDefault();
                                    }

                                    // Reset selection.
                                    SelectedInstance = instance_source_local.ConnectingInstance;

                                    // Return to starting page. Should the abscence of configurations from history resolve in different starting page of course.
                                    if (Parent.StartingPage != Parent.CurrentPage)
                                        Parent.CurrentPage = Parent.StartingPage;
                                }
                            }
                            catch (Exception ex) { Parent.Error = ex; }
                            finally { Parent.ChangeTaskCount(-1); }
                        },

                        // canExecute
                        () =>
                            InstanceSource is Models.LocalInstanceSource &&
                            SelectedInstance != null &&
                            InstanceSource.ConnectingInstanceList.IndexOf(SelectedInstance) >= 0 &&
                            !Parent.Sessions.Any(session => session.ConnectingProfile.Instance.Equals(SelectedInstance)));

                    // Setup canExecute refreshing.
                    PropertyChanged += (object sender, PropertyChangedEventArgs e) => { if (e.PropertyName == nameof(SelectedInstance)) _forget_selected_instance.RaiseCanExecuteChanged(); };
                    InstanceSource.ConnectingInstanceList.CollectionChanged += (object sender, NotifyCollectionChangedEventArgs e) => _forget_selected_instance.RaiseCanExecuteChanged();
                    Parent.Sessions.CollectionChanged += (object sender, NotifyCollectionChangedEventArgs e) => _forget_selected_instance.RaiseCanExecuteChanged();
                }

                return _forget_selected_instance;
            }
        }
        private DelegateCommand _forget_selected_instance;

        /// <summary>
        /// List of available profiles
        /// </summary>
        public ObservableCollection<Models.Profile> ProfileList
        {
            get { return _profile_list; }
            set
            {
                if (SetProperty(ref _profile_list, value))
                    SelectedProfile = null;
            }
        }
        private ObservableCollection<Models.Profile> _profile_list;

        /// <summary>
        /// Currently selected profile
        /// </summary>
        public Models.Profile SelectedProfile
        {
            get { return _selected_profile; }
            set { SetProperty(ref _selected_profile, value); }
        }
        private Models.Profile _selected_profile;

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
        /// Menu label for <c>ForgetSelectedProfile</c> command
        /// </summary>
        public string ForgetSelectedProfileLabel
        {
            get { return string.Format(Resources.Strings.InstanceForget, SelectedProfile); }
        }

        /// <summary>
        /// Forget selected profile command
        /// </summary>
        public DelegateCommand ForgetSelectedProfile
        {
            get
            {
                if (_forget_selected_profile == null)
                {
                    _forget_selected_profile = new DelegateCommand(
                        // execute
                        () =>
                        {
                            Parent.ChangeTaskCount(+1);
                            try
                            {
                                if (InstanceSource is Models.LocalInstanceSource instance_source_local)
                                {
                                    // Remove the profile from history.
                                    var profile = SelectedProfile;
                                    var instance = profile.Instance;
                                    var remove_instance = true;
                                    for (var i = instance_source_local.ConnectingProfileList.Count; i-- > 0;)
                                        if (instance_source_local.ConnectingProfileList[i].Equals(profile))
                                            instance_source_local.ConnectingProfileList.RemoveAt(i);
                                        else if (instance_source_local.ConnectingProfileList[i].Instance.Equals(instance))
                                            remove_instance = false;

                                    // Reset selection.
                                    SelectedProfile = null;

                                    if (remove_instance)
                                    {
                                        // Remove the instance from history.
                                        instance_source_local.ConnectingInstanceList.Remove(instance);
                                        if (instance_source_local.ConnectingInstance != null && instance_source_local.ConnectingInstance.Equals(instance))
                                        {
                                            // This was also the connecting instance.
                                            instance_source_local.ConnectingInstance = instance_source_local.ConnectingInstanceList.FirstOrDefault();
                                        }

                                        // Return to starting page. Should the abscence of configurations from history resolve in different starting page of course.
                                        if (Parent.StartingPage != Parent.CurrentPage)
                                            Parent.CurrentPage = Parent.StartingPage;
                                    }
                                }
                            }
                            catch (Exception ex) { Parent.Error = ex; }
                            finally { Parent.ChangeTaskCount(-1); }
                        },

                        // canExecute
                        () =>
                            InstanceSource is Models.LocalInstanceSource instance_source_local &&
                            SelectedProfile != null &&
                            instance_source_local.ConnectingProfileList.IndexOf(SelectedProfile) >= 0 &&
                            !Parent.Sessions.Any(session => session.ConnectingProfile.Equals(SelectedProfile)));

                    // Setup canExecute refreshing.
                    PropertyChanged += (object sender, PropertyChangedEventArgs e) => { if (e.PropertyName == nameof(SelectedProfile)) _forget_selected_profile.RaiseCanExecuteChanged(); };
                    if (InstanceSource is Models.LocalInstanceSource instance_source_local2) instance_source_local2.ConnectingProfileList.CollectionChanged += (object sender, NotifyCollectionChangedEventArgs e) => _forget_selected_profile.RaiseCanExecuteChanged();
                    Parent.Sessions.CollectionChanged += (object sender, NotifyCollectionChangedEventArgs e) => _forget_selected_profile.RaiseCanExecuteChanged();
                }

                return _forget_selected_profile;
            }
        }
        private DelegateCommand _forget_selected_profile;

        #endregion

        #region Constructors

        /// <summary>
        /// Constructs a panel
        /// </summary>
        /// <param name="parent">The page parent</param>
        /// <param name="instance_source_type">Instance source type</param>
        public ConnectingSelectPanel(ConnectWizard parent, Models.InstanceSourceType instance_source_type)
        {
            Parent = parent;
            InstanceSourceType = instance_source_type;

            PropertyChanged += (object sender, PropertyChangedEventArgs e) =>
            {
                if (e.PropertyName == nameof(SelectedInstance))
                    RaisePropertyChanged(nameof(ForgetSelectedInstanceLabel));

                if (e.PropertyName == nameof(SelectedProfile))
                    RaisePropertyChanged(nameof(ForgetSelectedProfileLabel));
            };
        }

        #endregion
    }
}
