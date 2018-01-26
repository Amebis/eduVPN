/*
    eduVPN - End-user friendly VPN

    Copyright: 2017, The Commons Conservancy eduVPN Programme
    SPDX-License-Identifier: GPL-3.0+
*/

using System.Windows.Controls;

namespace eduVPN.Client.Panels
{
    /// <summary>
    /// Interaction logic for TOTPEnrollmentPanel.xaml
    /// </summary>
    public partial class TOTPEnrollmentPanel : UserControl
    {
        #region Constructors

        /// <summary>
        /// Constructs a panel
        /// </summary>
        public TOTPEnrollmentPanel()
        {
            InitializeComponent();

            // Set initial focus.
            Loaded += (object sender, System.Windows.RoutedEventArgs e) => Response.Focus();
        }

        #endregion
    }
}
