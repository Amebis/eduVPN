/*
    eduVPN - End-user friendly VPN

    Copyright: 2017, The Commons Conservancy eduVPN Programme
    SPDX-License-Identifier: GPL-3.0+
*/

namespace LetsConnect.Client.Pages
{
    /// <summary>
    /// Interaction logic for CustomInstancePage.xaml
    /// </summary>
    public partial class CustomInstancePage : eduVPN.Views.Pages.CustomInstancePage
    {
        #region Constructors

        /// <summary>
        /// Constructs a page.
        /// </summary>
        public CustomInstancePage()
        {
            InitializeComponent();

            // Set initial focus.
            Loaded += (object sender, System.Windows.RoutedEventArgs e) => InstanceHostname.Focus();
        }

        #endregion
    }
}
