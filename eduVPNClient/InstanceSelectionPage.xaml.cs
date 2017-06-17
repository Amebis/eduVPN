/*
    eduVPN - End-user friendly VPN

    Copyright: 2017, The Commons Conservancy eduVPN Programme
    SPDX-License-Identifier: GPL-3.0+
*/

using System.Windows.Controls;

namespace eduVPNClient
{
    /// <summary>
    /// Interaction logic for InstanceSelectionPage.xaml
    /// </summary>
    public partial class InstanceSelectionPage : Page
    {
        public InstanceSelectionPage()
        {
            InitializeComponent();
            InstanceList.ItemsSource = App.Instances;
        }
    }
}
