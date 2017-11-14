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
    /// Interaction logic for ConnectingInstanceSelectPanel.xaml, ConnectingProfileSelectPanel.xaml and ConnectingInstanceAndProfileSelectPanel.xaml
    /// </summary>
    public class ConnectingSelectBasePanel : UserControl
    {
        #region Methods

        protected void InstanceList_SelectItem(object sender, InputEventArgs e)
        {
            if (DataContext is ViewModels.ConnectingInstanceSelectPanel view_model)
            {
                // Select connecting instance.
                if (view_model.SelectInstance.CanExecute())
                    view_model.SelectInstance.Execute();

                e.Handled = true;
            }
        }

        protected void InstanceList_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter ||
                e.Key == Key.Space)
                InstanceList_SelectItem(sender, e);
        }

        protected void ProfileList_SelectItem(object sender, InputEventArgs e)
        {
            if (DataContext is ViewModels.ConnectingInstanceAndProfileSelectPanel view_model)
            {
                // Connect selected profile.
                if (view_model.ConnectSelectedProfile.CanExecute())
                    view_model.ConnectSelectedProfile.Execute();

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
