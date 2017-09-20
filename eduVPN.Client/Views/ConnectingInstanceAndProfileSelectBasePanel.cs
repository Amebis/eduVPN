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
    /// Interaction logic for ConnectingProfileSelectPanel.xaml and ConnectingInstanceAndProfileSelectPanel.xaml
    /// </summary>
    public class ConnectingInstanceAndProfileSelectBasePanel : UserControl
    {
        #region Methods

        protected void ProfileList_SelectItem(object sender, InputEventArgs e)
        {
            if (sender is ListBoxItem listbox_item &&
                listbox_item.DataContext is Models.ProfileInfo profile &&
                DataContext is ViewModels.ConnectingInstanceAndProfileSelectPanel view_model)
            {
                // Connect selected profile.
                if (view_model.ConnectProfile.CanExecute(profile))
                    view_model.ConnectProfile.Execute(profile);

                e.Handled = true;
            }
        }

        protected void ProfileList_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter ||
                e.Key == Key.Space)
                ProfileList_SelectItem(sender, e);
        }

        #endregion
    }
}
