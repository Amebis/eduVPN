/*
    eduVPN - VPN for education and research

    Copyright: 2017-2019 The Commons Conservancy eduVPN Programme
    SPDX-License-Identifier: GPL-3.0+
*/

using eduVPN.Models;
using eduVPN.ViewModels.Windows;
using Prism.Commands;
using System;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;

namespace eduVPN.ViewModels.Panels
{
    /// <summary>
    /// Connecting instance and/or profile select panel base class
    /// </summary>
    public class ConnectingSelectPanel : Panel
    {
        #region Properties

        /// <summary>
        /// Selected instance source type
        /// </summary>
        public InstanceSourceType InstanceSourceType { get; }

        /// <summary>
        /// Selected instance source
        /// </summary>
        public InstanceSource InstanceSource
        {
            get { return Wizard.InstanceSources[(int)InstanceSourceType]; }
        }

        /// <summary>
        /// Selected instance
        /// </summary>
        /// <remarks><c>null</c> if none selected.</remarks>
        public Instance SelectedInstance
        {
            get { return _selected_instance; }
            set { SetProperty(ref _selected_instance, value); }
        }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private Instance _selected_instance;

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
                            Wizard.ChangeTaskCount(+1);
                            try
                            {
                                Wizard.InstanceSourceType = InstanceSourceType;
                                InstanceSource.ConnectingInstance = SelectedInstance;

                                // Go to profile selection page.
                                Wizard.CurrentPage = Wizard.ConnectingProfileSelectPage;
                            }
                            catch (Exception ex) { Wizard.Error = ex; }
                            finally { Wizard.ChangeTaskCount(-1); }
                        },

                        // canExecute
                        () => SelectedInstance != null);

                    // Setup canExecute refreshing.
                    PropertyChanged += (object sender, PropertyChangedEventArgs e) => { if (e.PropertyName == nameof(SelectedInstance)) _set_connecting_instance.RaiseCanExecuteChanged(); };
                }

                return _set_connecting_instance;
            }
        }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private DelegateCommand _set_connecting_instance;

        /// <summary>
        /// Menu label for <see cref="ForgetSelectedInstance"/> command
        /// </summary>
        public string ForgetSelectedInstanceLabel
        {
            get {
                return String.Format(
                    Resources.Strings.InstanceForget,
                    InstanceSource is LocalInstanceSource ? SelectedInstance?.ToString() : InstanceSourceType.GetLocalizableName());
            }
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
                            Wizard.ChangeTaskCount(+1);
                            try
                            {
                                if (InstanceSource is LocalInstanceSource)
                                    ForgetInstance(SelectedInstance);
                                else
                                    ForgetInstanceSource();

                                // Update settings.
                                Properties.Settings.Default[Properties.Settings.InstanceDirectoryId[(int)InstanceSourceType] + "InstanceSourceSettings"] = new Xml.InstanceSourceSettings() { InstanceSource = InstanceSource.ToSettings() };

                                // Return to starting page. Should the abscence of configurations from history resolve in different starting page of course.
                                if (Wizard.StartingPage != Wizard.CurrentPage)
                                    Wizard.CurrentPage = Wizard.StartingPage;
                            }
                            catch (Exception ex) { Wizard.Error = ex; }
                            finally { Wizard.ChangeTaskCount(-1); }
                        },

                        // canExecute
                        () =>
                            InstanceSource is LocalInstanceSource &&
                            SelectedInstance != null &&
                            InstanceSource.ConnectingInstanceList.IndexOf(SelectedInstance) >= 0 &&
                            !Wizard.Sessions.Any(session => session.ConnectingProfile.Instance.Equals(SelectedInstance)) ||
                            !Wizard.Sessions.Any(session => InstanceSource.ConnectingInstanceList.Any(instance => session.ConnectingProfile.Instance.Equals(instance))));

                    // Setup canExecute refreshing.
                    PropertyChanged += (object sender, PropertyChangedEventArgs e) => { if (e.PropertyName == nameof(SelectedInstance)) _forget_selected_instance.RaiseCanExecuteChanged(); };
                    InstanceSource.ConnectingInstanceList.CollectionChanged += (object sender, NotifyCollectionChangedEventArgs e) => _forget_selected_instance.RaiseCanExecuteChanged();
                    Wizard.Sessions.CollectionChanged += (object sender, NotifyCollectionChangedEventArgs e) => _forget_selected_instance.RaiseCanExecuteChanged();
                }

                return _forget_selected_instance;
            }
        }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private DelegateCommand _forget_selected_instance;

        /// <summary>
        /// Currently selected profile
        /// </summary>
        public Profile SelectedProfile
        {
            get { return _selected_profile; }
            set { SetProperty(ref _selected_profile, value); }
        }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private Profile _selected_profile;

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
                            Wizard.ChangeTaskCount(+1);
                            try
                            {
                                // Set connecting instance.
                                Wizard.InstanceSourceType = InstanceSourceType;
                                InstanceSource.ConnectingInstance = SelectedProfile.Instance;

                                // Start VPN session.
                                var param = new ConnectWizard.StartSessionParams(InstanceSourceType, SelectedProfile);
                                if (Wizard.StartSession.CanExecute(param))
                                    Wizard.StartSession.Execute(param);
                            }
                            catch (Exception ex) { Wizard.Error = ex; }
                            finally { Wizard.ChangeTaskCount(-1); }
                        },

                        // canExecute
                        () => SelectedProfile != null);

                    // Setup canExecute refreshing.
                    PropertyChanged += (object sender, PropertyChangedEventArgs e) => { if (e.PropertyName == nameof(SelectedProfile)) _connect_selected_profile.RaiseCanExecuteChanged(); };
                }

                return _connect_selected_profile;
            }
        }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private DelegateCommand _connect_selected_profile;

        /// <summary>
        /// Menu label for <see cref="ForgetSelectedProfile"/> command
        /// </summary>
        public string ForgetSelectedProfileLabel
        {
            get { return String.Format(Resources.Strings.InstanceForget, SelectedProfile); }
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
                            Wizard.ChangeTaskCount(+1);
                            try
                            {
                                if (InstanceSource is LocalInstanceSource instance_source_local)
                                {
                                    // Remove the profile from history.
                                    var profile = SelectedProfile;
                                    var instance = profile.Instance;
                                    var forget_instance = true;
                                    for (var i = instance_source_local.ConnectingProfileList.Count; i-- > 0;)
                                        if (instance_source_local.ConnectingProfileList[i].Equals(profile))
                                            instance_source_local.ConnectingProfileList.RemoveAt(i);
                                        else if (instance_source_local.ConnectingProfileList[i].Instance.Equals(instance))
                                            forget_instance = false;

                                    if (forget_instance)
                                        ForgetInstance(instance);

                                    // Update settings.
                                    Properties.Settings.Default[Properties.Settings.InstanceDirectoryId[(int)InstanceSourceType] + "InstanceSourceSettings"] = new Xml.InstanceSourceSettings() { InstanceSource = InstanceSource.ToSettings() };

                                    // Return to starting page. Should the abscence of configurations from history resolve in different starting page of course.
                                    if (Wizard.StartingPage != Wizard.CurrentPage)
                                        Wizard.CurrentPage = Wizard.StartingPage;
                                }
                            }
                            catch (Exception ex) { Wizard.Error = ex; }
                            finally { Wizard.ChangeTaskCount(-1); }
                        },

                        // canExecute
                        () =>
                            InstanceSource is LocalInstanceSource instance_source_local &&
                            SelectedProfile != null &&
                            instance_source_local.ConnectingProfileList.IndexOf(SelectedProfile) >= 0 &&
                            !Wizard.Sessions.Any(session => session.ConnectingProfile.Equals(SelectedProfile)));

                    // Setup canExecute refreshing.
                    PropertyChanged += (object sender, PropertyChangedEventArgs e) => { if (e.PropertyName == nameof(SelectedProfile)) _forget_selected_profile.RaiseCanExecuteChanged(); };
                    if (InstanceSource is LocalInstanceSource instance_source_local2) instance_source_local2.ConnectingProfileList.CollectionChanged += (object sender, NotifyCollectionChangedEventArgs e) => _forget_selected_profile.RaiseCanExecuteChanged();
                    Wizard.Sessions.CollectionChanged += (object sender, NotifyCollectionChangedEventArgs e) => _forget_selected_profile.RaiseCanExecuteChanged();
                }

                return _forget_selected_profile;
            }
        }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private DelegateCommand _forget_selected_profile;

        #endregion

        #region Constructors

        /// <summary>
        /// Constructs a panel
        /// </summary>
        /// <param name="wizard">The connecting wizard</param>
        /// <param name="instance_source_type">Instance source type</param>
        public ConnectingSelectPanel(ConnectWizard wizard, InstanceSourceType instance_source_type) :
            base(wizard)
        {
            InstanceSourceType = instance_source_type;

            PropertyChanged += (object sender, PropertyChangedEventArgs e) =>
            {
                if (e.PropertyName == nameof(SelectedInstance))
                    RaisePropertyChanged(nameof(ForgetSelectedInstanceLabel));
                else if (e.PropertyName == nameof(SelectedProfile))
                    RaisePropertyChanged(nameof(ForgetSelectedProfileLabel));
            };
        }

        #endregion

        #region Methods

        /// <summary>
        /// Removes given instance from history
        /// </summary>
        /// <param name="instance">Instance</param>
        private void ForgetInstance(Instance instance)
        {
            InstanceSource.ForgetInstance(instance);

            // Test if it is safe to remove authorization token and certificate.
            for (var source_index = (int)InstanceSourceType._start; source_index < (int)InstanceSourceType._end; source_index++)
                if (Wizard.InstanceSources[source_index]?.AuthenticatingInstance != null && Wizard.InstanceSources[source_index].AuthenticatingInstance.Equals(instance))
                    return;

            instance.Forget();
        }

        /// <summary>
        /// Removes entire instance source from history
        /// </summary>
        private void ForgetInstanceSource()
        {
            // Deselect instance to prevent flickering of the profile list.
            SelectedInstance = null;
            SelectedProfile = null;

            InstanceSource.Forget();

            if (InstanceSource is DistributedInstanceSource instance_source_distributed)
            {
                // Distributed authenticating instance source
                var authenticating_instance = instance_source_distributed.AuthenticatingInstance;
                if (authenticating_instance != null)
                {
                    instance_source_distributed.AuthenticatingInstance = null;

                    // Test if it is safe to remove authorization token and certificate.
                    for (var source_index = (int)InstanceSourceType._start; source_index < (int)InstanceSourceType._end; source_index++)
                        if (source_index != (int)InstanceSourceType &&
                            Wizard.InstanceSources[source_index]?.AuthenticatingInstance != null && Wizard.InstanceSources[source_index].AuthenticatingInstance.Equals(authenticating_instance))
                            return;

                    authenticating_instance.Forget();
                }
            }
        }

        #endregion
    }
}
