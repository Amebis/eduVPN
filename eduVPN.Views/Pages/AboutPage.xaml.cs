/*
    eduVPN - VPN for education and research

    Copyright: 2017-2022 The Commons Conservancy eduVPN Programme
    SPDX-License-Identifier: GPL-3.0+
*/

using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;

namespace eduVPN.Views.Pages
{
    /// <summary>
    /// Interaction logic for AboutPage.xaml
    /// </summary>
    public partial class AboutPage : Page
    {
        #region Constructors

        /// <summary>
        /// Constructs a page
        /// </summary>
        public AboutPage()
        {
            InitializeComponent();
        }

        #endregion

        #region Methods

        /// <summary>
        /// Opens product website in default browser.
        /// </summary>
        /// <param name="sender">Event sender</param>
        /// <param name="e">Event arguments</param>
        protected void Website_Click(object sender, RoutedEventArgs e)
        {
            Process.Start(eduVPN.Properties.Settings.Default.ClientAboutUri.AbsoluteUri);
        }

        #endregion
    }
}
