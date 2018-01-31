/*
    eduVPN - End-user friendly VPN

    Copyright: 2017, The Commons Conservancy eduVPN Programme
    SPDX-License-Identifier: GPL-3.0+
*/

using System.Windows.Controls;

namespace eduVPN.Views.Panels
{
    /// <summary>
    /// Interaction logic for YubiKeyAuthenticationPanel.xaml
    /// </summary>
    public partial class YubiKeyAuthenticationPanel : UserControl
    {
        #region Constructors

        /// <summary>
        /// Constructs a panel
        /// </summary>
        public YubiKeyAuthenticationPanel()
        {
            InitializeComponent();

            // Set initial focus.
            Loaded += (object sender, System.Windows.RoutedEventArgs e) => Response.Focus();
        }

        #endregion
    }
}
