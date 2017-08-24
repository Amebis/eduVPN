/*
    eduVPN - End-user friendly VPN

    Copyright: 2017, The Commons Conservancy eduVPN Programme
    SPDX-License-Identifier: GPL-3.0+
*/

using System;
using System.ComponentModel;
using System.Drawing;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace eduVPN.Views
{
    /// <summary>
    /// Interaction logic for ConnectWizard.xaml
    /// </summary>
    public partial class ConnectWizard : Window, IDisposable
    {
        #region Fields

        private System.Windows.Forms.NotifyIcon _notify_icon;
        private Icon[] _icons;
        private bool _do_close = false;

        #endregion

        #region Constructors

        public ConnectWizard()
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

        protected override void OnInitialized(EventArgs e)
        {
            // Preload icons to be used on system tray.
            var icon_size = System.Windows.Forms.SystemInformation.SmallIconSize;
            _icons = new Icon[]
            {
                new Icon(Application.GetResourceStream(new Uri("pack://application:,,,/Resources/eduVPN.ico")).Stream, icon_size),
                new Icon(Application.GetResourceStream(new Uri("pack://application:,,,/Resources/eduVPNConnected.ico")).Stream, icon_size)
            };

            var view_model = (ViewModels.ConnectWizard)DataContext;

            // Create notify icon and set default icon.
            _notify_icon = new System.Windows.Forms.NotifyIcon()
            {
                Icon = _icons[view_model != null && view_model.Session != null && view_model.Session.State == Models.VPNSessionStatusType.Connected ? 1 : 0]
            };

            // Bind to "Session.State" property to update tray icon.
            view_model.PropertyChanged += (object sender1, PropertyChangedEventArgs e1) =>
            {
                if (e1.PropertyName == "Session")
                    view_model.Session.PropertyChanged += (object sender2, PropertyChangedEventArgs e2) =>
                    {
                        if (e2.PropertyName == "State")
                            _notify_icon.Icon = _icons[view_model.Session.State == Models.VPNSessionStatusType.Connected ? 1 : 0];
                    };
            };

            // Setup tray icon events.
            _notify_icon.Click += (object sender, EventArgs ea) =>
            {
                if (ea is System.Windows.Forms.MouseEventArgs e_mouse)
                {
                    switch (e_mouse.Button)
                    {
                        case System.Windows.Forms.MouseButtons.Left:
                            // (Re)activate window.
                            if (!IsActive)
                                Show();
                            Activate();
                            Focus();
                            break;

                        case System.Windows.Forms.MouseButtons.Right:
                            // Pop-up context menu.
                            if (Resources["eduVPNSystemTrayMenu"] is ContextMenu menu)
                                menu.IsOpen = true;
                            break;
                    }
                }
            };

            // Show icon when Connect Wizard is loaded. Hide icon when closed.
            Loaded += (object sender, RoutedEventArgs ea) => _notify_icon.Visible = true;
            Closing += (object sender, CancelEventArgs ea) => { if (_do_close) _notify_icon.Visible = false; };

            base.OnInitialized(e);
        }

        private void Hide_Click(object sender, RoutedEventArgs e)
        {
            Hide();
        }

        private void Window_MouseDown(object sender, MouseButtonEventArgs e)
        {
            // Makes window draggable.
            if (e.ChangedButton == MouseButton.Left)
                DragMove();
        }

        private void Window_Closing(object sender, CancelEventArgs e)
        {
            if (_do_close)
            {
                // Save window position on closing.
                eduVPN.Client.Properties.Settings.Default.WindowTop = Top;
                eduVPN.Client.Properties.Settings.Default.WindowLeft = Left;
            }
            else
            {
                // User/system tried to close our window other than [x] button.
                // Cancel close and revert to hide.
                e.Cancel = true;
                Hide();
            }
        }

        private void Exit_Click(object sender, RoutedEventArgs e)
        {
            _do_close = true;
            Close();
        }

        #endregion

        #region IDisposable Support
        private bool disposedValue = false; // To detect redundant calls

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    if (_notify_icon != null)
                    {
                        _notify_icon.Dispose();
                        _notify_icon = null;
                    }
                    if (_icons != null)
                    {
                        foreach (var i in _icons)
                            i?.Dispose();
                        _icons = null;
                    }
                }

                disposedValue = true;
            }
        }

        // This code added to correctly implement the disposable pattern.
        public void Dispose()
        {
            // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
            Dispose(true);
        }
        #endregion
    }
}
