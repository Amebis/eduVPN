/*
    eduVPN - VPN for education and research

    Copyright: 2017-2019 The Commons Conservancy eduVPN Programme
    SPDX-License-Identifier: GPL-3.0+
*/

using System.Windows.Controls;

namespace eduVPN.Views.Panels
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
