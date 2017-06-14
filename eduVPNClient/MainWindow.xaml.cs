/*
    eduVPN - End-user friendly VPN

    Copyright: 2017, The Commons Conservancy eduVPN Programme
    SPDX-License-Identifier: GPL-3.0+
*/

using System;
using System.Windows;
using System.Windows.Input;

namespace eduVPNClient
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

            // Restore window position. Please mind that screen's real-estate might have changed since the previous launch.
            if (!double.IsNaN(Properties.Settings.Default.WindowLeft))
                Left = Math.Min(Math.Max(Properties.Settings.Default.WindowLeft, SystemParameters.VirtualScreenLeft), SystemParameters.VirtualScreenLeft + SystemParameters.VirtualScreenWidth - Width);
            if (!double.IsNaN(Properties.Settings.Default.WindowTop))
                Top = Math.Min(Math.Max(Properties.Settings.Default.WindowTop, SystemParameters.VirtualScreenTop), SystemParameters.VirtualScreenTop + SystemParameters.VirtualScreenHeight - Height);
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void GoToPage_ExecuteHandler(object sender, ExecutedRoutedEventArgs e)
        {
            NavigationFrame.NavigationService.Navigate(new Uri((string)e.Parameter, UriKind.Relative));
        }

        private void GoToPage_CanExecuteHandler(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = true;
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
            Properties.Settings.Default.WindowTop = Top;
            Properties.Settings.Default.WindowLeft = Left;
            Properties.Settings.Default.Save();
        }
    }
}
