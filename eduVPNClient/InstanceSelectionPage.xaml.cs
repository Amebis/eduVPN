/*
    eduVPN - End-user friendly VPN

    Copyright: 2017, The Commons Conservancy eduVPN Programme
    SPDX-License-Identifier: GPL-3.0+
*/

using System.Windows;
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
        }

        private void Back_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            NavigationService.Navigate(((MainWindow)Window.GetWindow(this)).AccessType);
        }
    }
}
