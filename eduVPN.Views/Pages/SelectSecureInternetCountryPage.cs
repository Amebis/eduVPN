/*
    eduVPN - VPN for education and research

    Copyright: 2017-2023 The Commons Conservancy
    SPDX-License-Identifier: GPL-3.0+
*/

using System.Windows.Controls;
using System.Windows.Input;

namespace eduVPN.Views.Pages
{
    /// <summary>
    /// Interaction logic for SelectSecureInternetCountryPage.xaml
    /// </summary>
    public partial class SelectSecureInternetCountryPage : Page
    {
        #region Constructors

        /// <summary>
        /// Constructs a page.
        /// </summary>
        public SelectSecureInternetCountryPage()
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
        protected void SecureInternetCountries_SelectItem(object sender, InputEventArgs e)
        {
            if (DataContext is ViewModels.Pages.SelectSecureInternetCountryPage viewModel)
            {
                // Confirm selected server.
                if (viewModel.ConfirmSecureInternetCountrySelection.CanExecute())
                    viewModel.ConfirmSecureInternetCountrySelection.Execute();

                e.Handled = true;
            }
        }

        /// <summary>
        /// Confirms secure internet server selection on the list when <see cref="Key.Enter"/> or <see cref="Key.Space"/> is pressed.
        /// </summary>
        /// <param name="sender">Event sender</param>
        /// <param name="e">Event arguments</param>
        protected void SecureInternetCountries_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter ||
                e.Key == Key.Space)
                SecureInternetCountries_SelectItem(sender, e);
        }

        #endregion
    }
}
