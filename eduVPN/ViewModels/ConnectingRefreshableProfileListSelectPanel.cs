/*
    eduVPN - End-user friendly VPN

    Copyright: 2017, The Commons Conservancy eduVPN Programme
    SPDX-License-Identifier: GPL-3.0+
*/

using System;
using System.Threading;
using System.Windows.Threading;

namespace eduVPN.ViewModels
{
    /// <summary>
    /// Connecting select panel with refreshable profile list
    /// </summary>
    public class ConnectingRefreshableProfileListSelectPanel : ConnectingSelectPanel
    {
        #region Constructors

        /// <summary>
        /// Constructs a panel
        /// </summary>
        /// <param name="parent">The page parent</param>
        /// <param name="instance_source_type">Instance source type</param>
        public ConnectingRefreshableProfileListSelectPanel(ConnectWizard parent, Models.InstanceSourceType instance_source_type) :
            base(parent, instance_source_type)
        {
            PropertyChanged += (object sender, System.ComponentModel.PropertyChangedEventArgs e) =>
            {
                if (e.PropertyName == nameof(SelectedInstance))
                {
                    ProfileList = null;
                    if (SelectedInstance != null)
                    {
                        new Thread(new ThreadStart(
                            () =>
                            {
                                Parent.Dispatcher.Invoke(DispatcherPriority.Normal, (Action)(() => Parent.ChangeTaskCount(+1)));
                                try
                                {
                                    // Get and load profile list.
                                    var profile_list = SelectedInstance.GetProfileList(InstanceSource.AuthenticatingInstance, Window.Abort.Token);

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
            };
        }

        #endregion
    }
}
