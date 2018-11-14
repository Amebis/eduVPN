/*
    eduVPN - VPN for education and research

    Copyright: 2017-2018 The Commons Conservancy eduVPN Programme
    SPDX-License-Identifier: GPL-3.0+
*/

using System.Windows.Controls;
using System.Windows.Input;

namespace eduVPN.Views.Panels
{
    /// <summary>
    /// Interaction logic for ConnectingInstanceSelectPanel.xaml, ConnectingProfileSelectPanel.xaml and ConnectingInstanceAndProfileSelectPanel.xaml
    /// </summary>
    public class ConnectingSelectPanel : UserControl
    {
        #region Methods

        /// <summary>
        /// Sets selected instance on the list as connecting instance.
        /// </summary>
        /// <param name="sender">Event sender</param>
        /// <param name="e">Event arguments</param>
        protected void InstanceList_SelectItem(object sender, InputEventArgs e)
        {
            if (DataContext is ViewModels.Panels.ConnectingSelectPanel view_model)
            {
                // Select connecting instance.
                if (view_model.SetConnectingInstance.CanExecute())
                    view_model.SetConnectingInstance.Execute();

                e.Handled = true;
            }
        }

        /// <summary>
        /// Sets selected instance on the list as connecting instance when <see cref="Key.Enter"/> or <see cref="Key.Space"/> is pressed.
        /// </summary>
        /// <param name="sender">Event sender</param>
        /// <param name="e">Event arguments</param>
        protected void InstanceList_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter ||
                e.Key == Key.Space)
                InstanceList_SelectItem(sender, e);
        }

        /// <summary>
        /// Sets selected profile on the list as connecting profile.
        /// </summary>
        /// <param name="sender">Event sender</param>
        /// <param name="e">Event arguments</param>
        protected void ProfileList_SelectItem(object sender, InputEventArgs e)
        {
            if (DataContext is ViewModels.Panels.ConnectingSelectPanel view_model)
            {
                // Connect selected profile.
                if (view_model.ConnectSelectedProfile.CanExecute())
                    view_model.ConnectSelectedProfile.Execute();

                e.Handled = true;
            }
        }

        /// <summary>
        /// Sets selected profile on the list as connecting profile when <see cref="Key.Enter"/> or <see cref="Key.Space"/> is pressed.
        /// </summary>
        /// <param name="sender">Event sender</param>
        /// <param name="e">Event arguments</param>
        protected void ProfileList_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter ||
                e.Key == Key.Space)
                ProfileList_SelectItem(sender, e);
        }

        #endregion
    }
}
