/*
    eduVPN - VPN for education and research

Copyright: 2017-2020 The Commons Conservancy eduVPN Programme
    SPDX-License-Identifier: GPL-3.0+
*/

namespace eduVPN.Views.Windows
{
    /// <summary>
    /// Interaction logic for PasswordPopup.xaml
    /// </summary>
    public partial class PasswordPopup : Window
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
