﻿/*
    eduVPN - VPN for education and research

    Copyright: 2017-2021 The Commons Conservancy eduVPN Programme
    SPDX-License-Identifier: GPL-3.0+
*/

using eduVPN.Models;
using eduVPN.ViewModels.Windows;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Threading;
using System.Windows.Threading;

namespace eduVPN.ViewModels.Panels
{
    /// <summary>
    /// Connecting select panel with refreshable profile list
    /// </summary>
    public class ConnectingRefreshableProfileListSelectPanel : ConnectingSelectPanel
    {
        #region Properties

        /// <summary>
        /// List of available profiles
        /// </summary>
        public ObservableCollection<Profile> ProfileList
        {
            get { return _profile_list; }
            set
            {
                // CA2214: Must not be set using SetProperty<>(), since SetProperty<>() is virtual,
                // and this property is set by constructor>OnPropertyChanged call chain.
                if (value != _profile_list)
                {
                    _profile_list = value;
                    RaisePropertyChanged();
                }
            }
        }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private ObservableCollection<Profile> _profile_list;

        #endregion

        #region Constructors

        /// <summary>
        /// Constructs a panel
        /// </summary>
        /// <param name="wizard">The connecting wizard</param>
        /// <param name="instance_source_type">Instance source type</param>
        public ConnectingRefreshableProfileListSelectPanel(ConnectWizard wizard, InstanceSourceType instance_source_type) :
            base(wizard, instance_source_type)
        {
            // Trigger initial load.
            OnPropertyChanged(this, new PropertyChangedEventArgs(nameof(SelectedInstance)));

            // Register to receive property change events and reload the profile list.
            PropertyChanged += OnPropertyChanged;
        }

        #endregion

        #region Methods

        private void OnPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(SelectedInstance))
            {
                ProfileList = null;
                if (SelectedInstance != null)
                {
                    new Thread(new ThreadStart(
                        () =>
                        {
                            Wizard.Dispatcher.Invoke(DispatcherPriority.Normal, (Action)(() => Wizard.ChangeTaskCount(+1)));
                            try
                            {
                                // Get and load profile list.
                                var profile_list = SelectedInstance.GetProfileList(InstanceSource.GetAuthenticatingInstance(SelectedInstance), Window.Abort.Token);

                                // Send the loaded profile list back to the UI thread.
                                Wizard.Dispatcher.Invoke(DispatcherPriority.Normal, (Action)(() => ProfileList = profile_list));
                            }
                            catch (OperationCanceledException) { }
                            catch (Exception ex) { Wizard.Dispatcher.Invoke(DispatcherPriority.Normal, (Action)(() => Wizard.Error = ex)); }
                            finally { Wizard.Dispatcher.Invoke(DispatcherPriority.Normal, (Action)(() => Wizard.ChangeTaskCount(-1))); }
                        })).Start();
                }
            }
        }

        #endregion
    }
}
