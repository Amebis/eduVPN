/*
    eduVPN - VPN for education and research

    Copyright: 2017-2022 The Commons Conservancy
    SPDX-License-Identifier: GPL-3.0+
*/

using System.Windows.Controls;

namespace eduVPN.Views.Pages
{
    /// <summary>
    /// Interaction logic for SelectOwnServerPage.xaml
    /// </summary>
    public partial class SelectOwnServerPage : Page
    {
        #region Constructors

        /// <summary>
        /// Constructs a page
        /// </summary>
        public SelectOwnServerPage()
        {
            InitializeComponent();

            // Set initial focus.
            Loaded += (object sender, System.Windows.RoutedEventArgs e) => Hostname.Focus();
        }

        #endregion
    }
}
