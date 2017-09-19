/*
    eduVPN - End-user friendly VPN

    Copyright: 2017, The Commons Conservancy eduVPN Programme
    SPDX-License-Identifier: GPL-3.0+
*/

using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;

namespace eduVPN.Views
{
    /// <summary>
    /// Interaction logic for ConnectWizard.xaml
    /// </summary>
    public partial class ConnectWizard : Window, IDisposable
    {
        #region Fields

        private System.Windows.Forms.NotifyIcon _tray_icon;
        private Dictionary<Models.VPNSessionStatusType, Icon> _icons;
        private bool _do_close = false;

        #endregion

        #region Properties

        /// <summary>
        /// Authorization pop-up window
        /// </summary>
        public AuthorizationPopup AuthorizationPopup { get; set; }

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
            _icons = new Dictionary<Models.VPNSessionStatusType, Icon>();
            foreach (var status_type in Enum.GetValues(typeof(Models.VPNSessionStatusType)).Cast<Models.VPNSessionStatusType>())
            {
                var icon_uri = new Uri(String.Format("pack://application:,,,/Resources/VPNSessionStatusTypeIcon{0}.ico", Enum.GetName(typeof(Models.VPNSessionStatusType), status_type)));
                try { _icons.Add(status_type, new Icon(Application.GetResourceStream(icon_uri).Stream, icon_size)); }
                catch { _icons.Add(status_type, new Icon(Application.GetResourceStream(new Uri("pack://application:,,,/Resources/VPNSessionStatusTypeIconInitializing.ico")).Stream, icon_size)); }
            }

            var view_model = (ViewModels.ConnectWizard)DataContext;

            // Create notify icon, set default icon, and setup events.
            // We need to do this programatically, since System.Windows.Forms.NotifyIcon is not WPF, but borrowed from WinForms.
            _tray_icon = new System.Windows.Forms.NotifyIcon();
            _tray_icon.Icon = view_model.Sessions.Count > 0 && view_model.Sessions[0] != null ? _icons[view_model.Sessions[0].State] : _icons[Models.VPNSessionStatusType.Initializing];
            _tray_icon.Click += TrayIcon_Click;

            // Bind to "Sessions[0].State" property to update tray icon.
            view_model.Sessions.CollectionChanged +=
                (object sender_Sessions, NotifyCollectionChangedEventArgs e_Sessions) =>
                {
                    void UpdateState(object sender_Session, PropertyChangedEventArgs e_Session)
                    {
                        if (e_Session.PropertyName == "State")
                            _tray_icon.Icon = _icons[view_model.Sessions[0].State];
                    }

                    if (e_Sessions.NewStartingIndex == 0)
                    {
                        // First session added: Bind to the session for property changes.
                        view_model.Sessions[0].PropertyChanged += UpdateState;
                    }
                    else if (e_Sessions.OldStartingIndex == 0)
                    {
                        if (view_model.Sessions.Count > 0)
                        {
                            // First session removed, next snapped into its place: Bind to the session for property changes.
                            view_model.Sessions[0].PropertyChanged += UpdateState;
                        }
                        else
                        {
                            // First session removed, no more sessions: Reset tray icon to default.
                            _tray_icon.Icon = _icons[Models.VPNSessionStatusType.Initializing];
                        }
                    }
                };

            // Set context menu data context to allow bindings to work.
            if (Resources["SystemTrayMenu"] is ContextMenu menu)
                menu.DataContext = DataContext;

            base.OnInitialized(e);
        }

        private void Hide_Click(object sender, RoutedEventArgs e)
        {
            Hide();
        }

        private void TrayIcon_Click(object sender, EventArgs ea)
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
                        if (Resources["SystemTrayMenu"] is ContextMenu menu)
                            menu.IsOpen = true;
                        break;
                }
            }
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            // Show tray icon when Connect Wizard is loaded.
            _tray_icon.Visible = true;
        }

        private void Window_Closing(object sender, CancelEventArgs e)
        {
            if (_do_close)
            {
                // Hide tray icon when closed.
                _tray_icon.Visible = false;

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

        private void Open_Click(object sender, RoutedEventArgs e)
        {
            // (Re)activate window.
            if (!IsActive)
                Show();
            Activate();
            Focus();
        }

        private void Exit_Click(object sender, RoutedEventArgs e)
        {
            _do_close = true;
            Close();
        }

        /// <summary>
        /// Called when one of the ConnectWizard's instances requests user authorization
        /// </summary>
        /// <param name="sender"><c>eduVPN.ViewModels.ConnectWizard</c> requiring instance authorization</param>
        /// <param name="e">Authorization request event arguments</param>
        private void ConnectWizard_RequestInstanceAuthorization(object sender, ViewModels.RequestInstanceAuthorizationEventArgs e)
        {
            if (AuthorizationPopup != null)
            {
                // Close previous authorization pop-up.
                AuthorizationPopup.Close();
            }

            // Create a new authorization pop-up.
            AuthorizationPopup = new AuthorizationPopup() { Owner = this };

            // Trigger authorization.
            var view_model = AuthorizationPopup.DataContext as ViewModels.AuthorizationPopup;
            if (view_model.RequestAuthorization.CanExecute(e.Instance))
                view_model.RequestAuthorization.Execute(e.Instance);

            // Run the authorization pop-up and pass the access token to be returned to the event sender.
            if (AuthorizationPopup.ShowDialog() == true)
                e.AccessToken = view_model.AccessToken;

            AuthorizationPopup = null;
        }

        /// <summary>
        /// Called when an OpenVPN session requests a password authentication
        /// </summary>
        /// <param name="sender"><c>eduVPN.ViewModels.ConnectWizard</c> requiring authentication</param>
        /// <param name="e">Authentication request event arguments</param>
        private void ConnectWizard_RequestOpenVPNPasswordAuthentication(object sender, eduOpenVPN.Management.PasswordAuthenticationRequestedEventArgs e)
        {
            // Create a new authentication pop-up.
            PasswordPopup popup = new PasswordPopup() { Owner = this };

            // Set authenticating realm.
            var view_model = (ViewModels.AuthenticationPopup)popup.DataContext;
            view_model.Session = sender as ViewModels.VPNSession;
            view_model.Realm = e.Realm;

            // Run the authentication pop-up and pass the credentials to be returned to the event sender.
            if (popup.ShowDialog() == true)
                e.Password = (new NetworkCredential("", popup.Password.Password)).SecurePassword;
        }

        /// <summary>
        /// Called when an OpenVPN session requests a password authentication
        /// </summary>
        /// <param name="sender"><c>eduVPN.ViewModels.ConnectWizard</c> requiring authentication</param>
        /// <param name="e">Authentication request event arguments</param>
        private void ConnectWizard_RequestOpenVPNUsernamePasswordAuthentication(object sender, eduOpenVPN.Management.UsernamePasswordAuthenticationRequestedEventArgs e)
        {
            // Create a new authentication pop-up.
            UsernamePasswordPopup popup = new UsernamePasswordPopup() { Owner = this };

            // Set authenticating realm.
            var view_model = (ViewModels.AuthenticationPopup)popup.DataContext;
            view_model.Session = sender as ViewModels.VPNSession;
            view_model.Realm = e.Realm;

            // TODO: Load previous user name and change initial focus to the "Password" field.

            // Run the authentication pop-up and pass the credentials to be returned to the event sender.
            if (popup.ShowDialog() == true)
            {
                e.UserName = view_model.UserName;
                e.Password = (new NetworkCredential("", popup.Password.Password)).SecurePassword;
            }
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
                    if (_tray_icon != null)
                        _tray_icon.Dispose();

                    if (_icons != null)
                        foreach (var i in _icons)
                            if (i.Value != null)
                                i.Value.Dispose();
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
