/*
    eduVPN - VPN for education and research

    Copyright: 2017-2021 The Commons Conservancy eduVPN Programme
    SPDX-License-Identifier: GPL-3.0+
*/

using System.Windows.Controls;
using System.Windows.Input;

namespace eduVPN.Views.Pages
{
    /// <summary>
    /// Interaction logic for SelectSecureInternetServerPage.xaml
    /// </summary>
    public partial class SelectSecureInternetServerPage : Page
    {
        #region Constructors

        /// <summary>
        /// Constructs a page.
        /// </summary>
        public SelectSecureInternetServerPage()
        {
            InitializeComponent();
        }

        #endregion

        #region Methods

        /// <summary>
        /// Confirms secure internet server selection on the list.
        /// </summary>
        /// <param name="sender">Event sender</param>
        /// <param name="e">Event arguments</param>
        protected void SecureInternetServers_SelectItem(object sender, InputEventArgs e)
        {
            if (DataContext is ViewModels.Pages.SelectSecureInternetServerPage viewModel)
            {
                // Confirm selected server.
                if (viewModel.ConfirmSecureInternetServerSelection.CanExecute())
                    viewModel.ConfirmSecureInternetServerSelection.Execute();

                e.Handled = true;
            }
        }

        /// <summary>
        /// Confirms secure internet server selection on the list when <see cref="Key.Enter"/> or <see cref="Key.Space"/> is pressed.
        /// </summary>
        /// <param name="sender">Event sender</param>
        /// <param name="e">Event arguments</param>
        protected void SecureInternetServers_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter ||
                e.Key == Key.Space)
                SecureInternetServers_SelectItem(sender, e);
        }

        #endregion
    }
}
