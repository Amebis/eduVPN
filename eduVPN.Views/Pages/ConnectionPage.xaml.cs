/*
    eduVPN - VPN for education and research

    Copyright: 2017-2020 The Commons Conservancy eduVPN Programme
    SPDX-License-Identifier: GPL-3.0+
*/

using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace eduVPN.Views.Pages
{
    /// <summary>
    /// Interaction logic for ConnectionPage.xaml
    /// </summary>
    public partial class ConnectionPage : Page
    {
        #region Constructors

        /// <summary>
        /// Constructs a page
        /// </summary>
        public ConnectionPage()
        {
            InitializeComponent();
        }

        #endregion

        #region Methods

        /// <summary>
        /// Opens support contact with default handler.
        /// </summary>
        /// <param name="sender">Event sender</param>
        /// <param name="e">Event arguments</param>
        private void SupportContact_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button senderButton &&
                senderButton.DataContext is Uri uri)
                Process.Start(uri.AbsoluteUri);
        }

        /// <summary>
        /// Confirms profile selection on the list.
        /// </summary>
        /// <param name="sender">Event sender</param>
        /// <param name="e">Event arguments</param>
        protected void Profiles_SelectItem(object sender, InputEventArgs e)
        {
            if (DataContext is ViewModels.Pages.ConnectionPage viewModel &&
                sender is ListBoxItem item &&
                item.DataContext is Models.Profile profile)
            {
                viewModel.SelectedProfile = profile;

                // Toggle selected profile.
                if (viewModel.ConfirmProfileSelection.CanExecute())
                    viewModel.ConfirmProfileSelection.Execute();

                e.Handled = true;
            }
        }

        /// <summary>
        /// Confirms profile selection on the list when <see cref="Key.Enter"/> or <see cref="Key.Space"/> is pressed.
        /// </summary>
        /// <param name="sender">Event sender</param>
        /// <param name="e">Event arguments</param>
        protected void Profiles_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter ||
                e.Key == Key.Space)
                Profiles_SelectItem(sender, e);
        }

        #endregion
    }
}
