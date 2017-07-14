/*
    eduVPN - End-user friendly VPN

    Copyright: 2017, The Commons Conservancy eduVPN Programme
    SPDX-License-Identifier: GPL-3.0+
*/

using System.Windows;
using System.Windows.Controls;

namespace eduVPNClient.View
{
    /// <summary>
    /// Interaction logic for CustomInstancePage.xaml
    /// </summary>
    public partial class CustomInstancePage : Page
    {
        #region Constructors

        /// <summary>
        /// Constructs a page.
        /// </summary>
        public CustomInstancePage()
        {
            if (Properties.Settings.Default.CustomInstanceHistory == null)
                Properties.Settings.Default.CustomInstanceHistory = new System.Collections.Specialized.StringCollection();

            InitializeComponent();
        }

        #endregion

        private void Button_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            var instance_uri = InstanceURI.Text;

            if (!Properties.Settings.Default.CustomInstanceHistory.Contains(instance_uri))
                Properties.Settings.Default.CustomInstanceHistory.Insert(0, instance_uri);
        }
    }
}
