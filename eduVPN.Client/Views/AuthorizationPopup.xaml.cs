/*
    eduVPN - End-user friendly VPN

    Copyright: 2017, The Commons Conservancy eduVPN Programme
    SPDX-License-Identifier: GPL-3.0+
*/

using System.ComponentModel;

namespace eduVPN.Views
{
    /// <summary>
    /// Interaction logic for AuthorizationPopup.xaml
    /// </summary>
    public partial class AuthorizationPopup : Window
    {
        #region Constructors

        public AuthorizationPopup()
        {
            InitializeComponent();

            var view_model = (ViewModels.AuthorizationPopup)DataContext;

            // Dismiss the dialog once access token is set.
            view_model.PropertyChanged += (object sender, PropertyChangedEventArgs e) =>
            {
                if (e.PropertyName == nameof(view_model.AccessToken) && view_model.AccessToken != null)
                    DialogResult = true;
            };
        }

        #endregion
    }
}
