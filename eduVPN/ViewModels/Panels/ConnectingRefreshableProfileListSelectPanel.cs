/*
    eduVPN - End-user friendly VPN

    Copyright: 2017, The Commons Conservancy eduVPN Programme
    SPDX-License-Identifier: GPL-3.0+
*/

using eduVPN.Models;
using eduVPN.ViewModels.Windows;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
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
                // and this property is set by constructor>InstanceSource_PropertyChanged call chain.
                if (value != _profile_list)
                {
                    _profile_list = value;
                    RaisePropertyChanged();
                }
            }
        }
        private ObservableCollection<Profile> _profile_list;

        #endregion

        #region Constructors

        /// <summary>
        /// Constructs a panel
        /// </summary>
        /// <param name="parent">The page parent</param>
        /// <param name="instance_source_type">Instance source type</param>
        public ConnectingRefreshableProfileListSelectPanel(ConnectWizard parent, InstanceSourceType instance_source_type) :
            base(parent, instance_source_type)
        {
            // Trigger initial load.
            InstanceSource_PropertyChanged(this, new PropertyChangedEventArgs(nameof(InstanceSource.ConnectingInstance)));

            // Register to receive property change events and reload the profile list.
            InstanceSource.PropertyChanged += InstanceSource_PropertyChanged;
        }

        #endregion

        #region Methods

        private void InstanceSource_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(InstanceSource.ConnectingInstance))
            {
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
