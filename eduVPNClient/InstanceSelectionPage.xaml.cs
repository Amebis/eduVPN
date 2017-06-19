/*
    eduVPN - End-user friendly VPN

    Copyright: 2017, The Commons Conservancy eduVPN Programme
    SPDX-License-Identifier: GPL-3.0+
*/

using eduVPN;
using System;
using System.Windows.Controls;
using System.Windows.Input;

namespace eduVPNClient
{
    /// <summary>
    /// Interaction logic for InstanceSelectionPage.xaml
    /// </summary>
    public partial class InstanceSelectionPage : Page
    {
        #region Constructors

        public InstanceSelectionPage()
        {
            InitializeComponent();
        }

        #endregion

        #region Handlers

        private void InstanceList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // User selected an instance.
            var instance_list = (InstanceList)FindResource("InstanceList");

            foreach (var i in e.AddedItems)
            {
                var instance = (Instance)i;
                if (instance.Base == new Uri("nl.eduvpn.app.windows:other"))
                {
                    // User selected "Other instance".
                    NavigationCommands.GoToPage.Execute("OtherInstance.xaml", this);
                }
                else
                {
                    // A known instance was selected.
                    // TODO: Start OAuth authentication and navigate client to the waiting page.
                }
            }
        }

        #endregion
    }
}
