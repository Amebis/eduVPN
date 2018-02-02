/*
    eduVPN - VPN for education and research

    Copyright: 2017, The Commons Conservancy eduVPN Programme
    SPDX-License-Identifier: GPL-3.0+
*/

using System.Windows.Controls;

namespace eduVPN.Views.Panels
{
    /// <summary>
    /// Interaction logic for TOTPAuthenticationPanel.xaml
    /// </summary>
    public partial class TOTPAuthenticationPanel : UserControl
    {
        #region Constructors

        /// <summary>
        /// Constructs a panel
        /// </summary>
        public TOTPAuthenticationPanel()
        {
            InitializeComponent();

            // Set initial focus.
            Loaded += (object sender, System.Windows.RoutedEventArgs e) => Response.Focus();
        }

        #endregion
    }
}
