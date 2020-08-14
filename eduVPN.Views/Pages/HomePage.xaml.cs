/*
    eduVPN - VPN for education and research

    Copyright: 2017-2020 The Commons Conservancy eduVPN Programme
    SPDX-License-Identifier: GPL-3.0+
*/

using System.Windows.Controls;
using System.Windows.Input;

namespace eduVPN.Views.Pages
{
    /// <summary>
    /// Interaction logic for HomePage.xaml
    /// </summary>
    public partial class HomePage : Page
    {
        #region Constructors

        /// <summary>
        /// Constructs a page
        /// </summary>
        public HomePage()
        {
            InitializeComponent();
        }

        #endregion

        #region Methods

        /// <summary>
        /// Confirms server selection on the list.
        /// </summary>
        /// <param name="sender">Event sender</param>
        /// <param name="e">Event arguments</param>
        protected void InstituteAccessServers_SelectItem(object sender, InputEventArgs e)
        {
            if (DataContext is ViewModels.Pages.HomePage viewModel)
            {
                // Authorize selected server.
                if (viewModel.ConfirmInstituteAccessServerSelection.CanExecute())
                    viewModel.ConfirmInstituteAccessServerSelection.Execute();

                e.Handled = true;
            }
        }

        /// <summary>
        /// Confirms server selection on the list when <see cref="Key.Enter"/> or <see cref="Key.Space"/> is pressed.
        /// </summary>
        /// <param name="sender">Event sender</param>
        /// <param name="e">Event arguments</param>
        protected void InstituteAccessServers_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter ||
                e.Key == Key.Space)
                InstituteAccessServers_SelectItem(sender, e);
        }

        /// <summary>
        /// Confirms secure internet server selection on the list.
        /// </summary>
        /// <param name="sender">Event sender</param>
        /// <param name="e">Event arguments</param>
        protected void SecureInternetServers_SelectItem(object sender, InputEventArgs e)
        {
            if (DataContext is ViewModels.Pages.HomePage viewModel)
            {
                // Authorize selected organization.
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

        /// <summary>
        /// Confirms own server selection on the list
        /// </summary>
        /// <param name="sender">Event sender</param>
        /// <param name="e">Event arguments</param>
        protected void OwnServers_SelectItem(object sender, InputEventArgs e)
        {
            if (DataContext is ViewModels.Pages.HomePage viewModel)
            {
                // Authorize selected server.
                if (viewModel.ConfirmOwnServerSelection.CanExecute())
                    viewModel.ConfirmOwnServerSelection.Execute();

                e.Handled = true;
            }
        }

        /// <summary>
        /// Confirms own server selection on the list when <see cref="Key.Enter"/> or <see cref="Key.Space"/> is pressed.
        /// </summary>
        /// <param name="sender">Event sender</param>
        /// <param name="e">Event arguments</param>
        protected void OwnServers_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter ||
                e.Key == Key.Space)
                OwnServers_SelectItem(sender, e);
        }

        #endregion
    }
}
