/*
    eduVPN - VPN for education and research

    Copyright: 2017-2020 The Commons Conservancy eduVPN Programme
    SPDX-License-Identifier: GPL-3.0+
*/

using System.Windows;
using System.Windows.Controls;

namespace eduVPN.Views.Pages
{
    /// <summary>
    /// Interaction logic for CustomInstancePage.xaml
    /// </summary>
    public partial class CustomInstancePage : ConnectWizardPage
    {
        #region Constructors

        /// <summary>
        /// Constructs a page.
        /// </summary>
        public CustomInstancePage()
        {
            if (Properties.Settings.Default.CustomInstanceHistory == null)
                Properties.Settings.Default.CustomInstanceHistory = new System.Collections.Specialized.StringCollection();

            InitializeComponent();

            // Set initial focus.
            Loaded += (object sender, System.Windows.RoutedEventArgs e) => InstanceHostname.Focus();
        }

        #endregion

        #region Methods

        /// <summary>
        /// Saves the instance hostname to the history list
        /// </summary>
        /// <param name="sender">Event sender (ignored)</param>
        /// <param name="e">Event arguments (ignored)</param>
        /// <remarks>Occurs when a <see cref="Button"/> is clicked.</remarks>
        protected void Button_Click(object sender, RoutedEventArgs e)
        {
            if (FindName("InstanceHostname") is ComboBox InstanceHostname)
            {
                var hostname = InstanceHostname.Text;

                if (!Properties.Settings.Default.CustomInstanceHistory.Contains(hostname))
                    Properties.Settings.Default.CustomInstanceHistory.Insert(0, hostname);
            }
        }

        #endregion
    }
}
