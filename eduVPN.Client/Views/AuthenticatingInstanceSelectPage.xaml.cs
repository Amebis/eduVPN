/*
    eduVPN - End-user friendly VPN

    Copyright: 2017, The Commons Conservancy eduVPN Programme
    SPDX-License-Identifier: GPL-3.0+
*/

using System.Windows.Controls;
using System.Windows.Input;

namespace eduVPN.Views
{
    /// <summary>
    /// Interaction logic for SecureInternetSelectPage.xaml and InstituteAccessSelectPage.xaml
    /// </summary>
    public partial class AuthenticatingInstanceSelectPage : ConnectWizardPage
    {
        #region Constructors

        /// <summary>
        /// Constructs a page.
        /// </summary>
        public AuthenticatingInstanceSelectPage()
        {
            InitializeComponent();
        }

        #endregion

        #region Methods

        private void InstanceList_SelectItem(object sender, InputEventArgs e)
        {
            if (sender is ListBoxItem listbox_item &&
                listbox_item.DataContext is Models.InstanceInfo instance &&
                DataContext is ViewModels.AuthenticatingInstanceSelectPage view_model)
            {
                // Authorize selected instance.
                if (view_model.AuthorizeInstance.CanExecute(instance))
                    view_model.AuthorizeInstance.Execute(instance);

                e.Handled = true;
            }
        }

        private void InstanceList_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter ||
                e.Key == Key.Space)
                InstanceList_SelectItem(sender, e);
        }

        #endregion
    }
}
