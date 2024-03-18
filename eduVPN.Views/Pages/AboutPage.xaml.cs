/*
    eduVPN - VPN for education and research

    Copyright: 2017-2024 The Commons Conservancy
    SPDX-License-Identifier: GPL-3.0+
*/

using System;
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
            if (Process.Start(eduVPN.Properties.Settings.Default.ClientAboutUri.AbsoluteUri) == null)
                throw new Exception(string.Format("Failed to spawn default browser on {0}", eduVPN.Properties.Settings.Default.ClientAboutUri.AbsoluteUri));
        }

        #endregion
    }
}
