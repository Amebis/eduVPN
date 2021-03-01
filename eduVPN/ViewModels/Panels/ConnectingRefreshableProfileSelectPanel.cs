/*
    eduVPN - VPN for education and research

    Copyright: 2017-2021 The Commons Conservancy eduVPN Programme
    SPDX-License-Identifier: GPL-3.0+
*/

using eduVPN.Models;
using eduVPN.ViewModels.Windows;
using System.ComponentModel;

namespace eduVPN.ViewModels.Panels
{
    /// <summary>
    /// Connecting profile select panel with refreshable profile list
    /// </summary>
    public class ConnectingRefreshableProfileSelectPanel : ConnectingRefreshableProfileListSelectPanel
    {
        #region Constructors

        /// <summary>
        /// Constructs a panel
        /// </summary>
        /// <param name="wizard">The connecting wizard</param>
        /// <param name="instance_source_type">Instance source type</param>
        public ConnectingRefreshableProfileSelectPanel(ConnectWizard wizard, InstanceSourceType instance_source_type) :
            base(wizard, instance_source_type)
        {
            PropertyChanged += (object sender, PropertyChangedEventArgs e) =>
            {
                if (e.PropertyName == nameof(ProfileList) &&
                    ProfileList != null &&
                    ProfileList.Count == 1 &&
                    Wizard.Error == null)
                {
                    // The profile list has been loaded with exactly one profile available.
                    // And there is no error condition to report.
                    // Therefore, auto-select the profile and connect!
                    SelectedProfile = ProfileList[0];
                    if (ConnectSelectedProfile.CanExecute())
                        ConnectSelectedProfile.Execute();
                }
            };
        }

        #endregion
    }
}
