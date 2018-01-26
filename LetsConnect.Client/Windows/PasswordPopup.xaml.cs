/*
    eduVPN - End-user friendly VPN

Copyright: 2017, The Commons Conservancy eduVPN Programme
    SPDX-License-Identifier: GPL-3.0+
*/

namespace LetsConnect.Client.Windows
{
    /// <summary>
    /// Interaction logic for PasswordPopup.xaml
    /// </summary>
    public partial class PasswordPopup : eduVPN.Views.Windows.Window
    {
        #region Constructors

        /// <summary>
        /// Constructs a popup
        /// </summary>
        public PasswordPopup()
        {
            InitializeComponent();

            // Set initial focus.
            Loaded += (object sender, System.Windows.RoutedEventArgs e) => Password.Focus();
        }

        #endregion
    }
}
