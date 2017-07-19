/*
    eduVPN - End-user friendly VPN

    Copyright: 2017, The Commons Conservancy eduVPN Programme
    SPDX-License-Identifier: GPL-3.0+
*/

using System;
using System.Windows;
using System.Windows.Input;

namespace eduVPN.View
{
    /// <summary>
    /// Interaction logic for ConnectWizardView.xaml
    /// </summary>
    public partial class ConnectWizardView : Window
    {
        #region Constructors

        public ConnectWizardView()
        {
            InitializeComponent();

            // Restore window position. Please mind that screen's real-estate might have changed since the previous launch.
            if (!double.IsNaN(eduVPN.Client.Properties.Settings.Default.WindowLeft))
                Left = Math.Min(Math.Max(eduVPN.Client.Properties.Settings.Default.WindowLeft, SystemParameters.VirtualScreenLeft), SystemParameters.VirtualScreenLeft + SystemParameters.VirtualScreenWidth - Width);
            if (!double.IsNaN(eduVPN.Client.Properties.Settings.Default.WindowTop))
                Top = Math.Min(Math.Max(eduVPN.Client.Properties.Settings.Default.WindowTop, SystemParameters.VirtualScreenTop), SystemParameters.VirtualScreenTop + SystemParameters.VirtualScreenHeight - Height);
        }

        #endregion

        #region Methods

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void Window_MouseDown(object sender, MouseButtonEventArgs e)
        {
            // Makes window draggable.
            if (e.ChangedButton == MouseButton.Left)
                DragMove();
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            // Save window position on closing.
            eduVPN.Client.Properties.Settings.Default.WindowTop = Top;
            eduVPN.Client.Properties.Settings.Default.WindowLeft = Left;
        }

        #endregion
    }
}
