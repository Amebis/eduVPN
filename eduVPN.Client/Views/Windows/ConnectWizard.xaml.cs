/*
    eduVPN - End-user friendly VPN

    Copyright: 2017, The Commons Conservancy eduVPN Programme
    SPDX-License-Identifier: GPL-3.0+
*/

using eduVPN.Models;
using eduVPN.ViewModels.VPN;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;

namespace eduVPN.Views.Windows
{
    /// <summary>
    /// Interaction logic for ConnectWizard.xaml
    /// </summary>
    public partial class ConnectWizard : Window, IDisposable
    {
        #region Fields

        private System.Windows.Forms.NotifyIcon _tray_icon;
        private Dictionary<VPNSessionStatusType, Icon> _icons;
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
            if (!double.IsNaN(Client.Properties.Settings.Default.WindowLeft))
                Left = Math.Min(Math.Max(Client.Properties.Settings.Default.WindowLeft, SystemParameters.VirtualScreenLeft), SystemParameters.VirtualScreenLeft + SystemParameters.VirtualScreenWidth - Width);
            if (!double.IsNaN(Client.Properties.Settings.Default.WindowTop))
                Top = Math.Min(Math.Max(Client.Properties.Settings.Default.WindowTop, SystemParameters.VirtualScreenTop), SystemParameters.VirtualScreenTop + SystemParameters.VirtualScreenHeight - Height);
        }

        #endregion

        #region Methods

        /// <inheritdoc/>
        protected override void OnInitialized(EventArgs e)
        {
            // Preload icons to be used on system tray.
            var icon_size = System.Windows.Forms.SystemInformation.SmallIconSize;
            _icons = new Dictionary<VPNSessionStatusType, Icon>();
            foreach (var status_type in Enum.GetValues(typeof(VPNSessionStatusType)).Cast<VPNSessionStatusType>())
            {
                var icon_uri = new Uri(String.Format("pack://application:,,,/Resources/VPNSessionStatusTypeIcon{0}.ico", Enum.GetName(typeof(VPNSessionStatusType), status_type)));
                try { _icons.Add(status_type, new Icon(Application.GetResourceStream(icon_uri).Stream, icon_size)); }
                catch { _icons.Add(status_type, new Icon(Application.GetResourceStream(new Uri("pack://application:,,,/Resources/VPNSessionStatusTypeIconInitializing.ico")).Stream, icon_size)); }
            }

            var view_model = (ViewModels.Windows.ConnectWizard)DataContext;

            // Create notify icon, set default icon, and setup events.
            // We need to do this programatically, since System.Windows.Forms.NotifyIcon is not WPF, but borrowed from WinForms.
            _tray_icon = new System.Windows.Forms.NotifyIcon();
            _tray_icon.Icon = _icons[view_model.ActiveSession.State];
            _tray_icon.Click += TrayIcon_Click;

            // Bind to "ActiveSession.State" property to update tray icon.
            view_model.PropertyChanged += (object sender, PropertyChangedEventArgs e2) =>
                {
                    if (e2.PropertyName == nameof(view_model.ActiveSession))
                    {
                        if (view_model.ActiveSession != VPNSession.Blank)
                        {
                            // Active session changed: Bind to the session for property changes.
                            view_model.ActiveSession.PropertyChanged += (object sender_Session, PropertyChangedEventArgs e_Session) =>
                                {
                                    if (e_Session.PropertyName == nameof(view_model.ActiveSession.State))
                                        _tray_icon.Icon = _icons[view_model.ActiveSession.State];
                                };
                        }
                        else
                        {
                            // Active session ended, no more sessions: Reset tray icon to default.
                            _tray_icon.Icon = _icons[view_model.ActiveSession.State];
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
                Client.Properties.Settings.Default.WindowTop = Top;
                Client.Properties.Settings.Default.WindowLeft = Left;
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
        /// <param name="sender"><c>eduVPN.ViewModels.Windows.ConnectWizard</c> requiring instance authorization</param>
        /// <param name="e">Authorization request event arguments</param>
        private void ConnectWizard_RequestInstanceAuthorization(object sender, RequestInstanceAuthorizationEventArgs e)
        {
            if (AuthorizationPopup != null)
            {
                // Close previous authorization pop-up.
                AuthorizationPopup.Close();
            }

            // Create a new authorization pop-up.
            AuthorizationPopup = new AuthorizationPopup() { Owner = this };

            // Trigger authorization.
            var view_model = AuthorizationPopup.DataContext as ViewModels.Windows.AuthorizationPopup;
            view_model.AuthenticatingInstance = e.Instance;
            view_model.Scope = e.Scope;
            if (view_model.RequestAuthorization.CanExecute())
                view_model.RequestAuthorization.Execute();

            // Run the authorization pop-up and pass the access token to be returned to the event sender.
            if (AuthorizationPopup.ShowDialog() == true)
                e.AccessToken = view_model.AccessToken;

            AuthorizationPopup = null;
        }

        /// <summary>
        /// Called when an OpenVPN session requests a password authentication
        /// </summary>
        /// <param name="sender"><c>eduVPN.ViewModels.Windows.ConnectWizard</c> requiring authentication</param>
        /// <param name="e">Authentication request event arguments</param>
        private void ConnectWizard_RequestOpenVPNPasswordAuthentication(object sender, eduOpenVPN.Management.PasswordAuthenticationRequestedEventArgs e)
        {
            var view_model = new ViewModels.Windows.PasswordPopup(sender, e);

            // Create a new authentication pop-up.
            PasswordPopup popup = new PasswordPopup() { Owner = this, DataContext = view_model };

            // Set the event args to fill with data.
            popup.OK.CommandParameter = e;

            // Run the authentication pop-up and pass the credentials to be returned to the event sender.
            if (popup.ShowDialog() == true)
            {
                // Password was not set using MVVP, since <PasswordBox> control does not support binding.
                e.Password = (new NetworkCredential("", popup.Password.Password)).SecurePassword;
            }
        }

        /// <summary>
        /// Called when an OpenVPN session requests a password authentication
        /// </summary>
        /// <param name="sender"><c>eduVPN.ViewModels.Windows.ConnectWizard</c> requiring authentication</param>
        /// <param name="e">Authentication request event arguments</param>
        private void ConnectWizard_RequestOpenVPNUsernamePasswordAuthentication(object sender, eduOpenVPN.Management.UsernamePasswordAuthenticationRequestedEventArgs e)
        {
            var view_model = new ViewModels.Windows.UsernamePasswordPopup(sender, e);

            // Load previous username.
            var session = (VPNSession)sender;
            var profile_id = session.ConnectingProfile.Instance.Base.AbsoluteUri + "|" + session.ConnectingProfile.ID;
            try { view_model.Username = Client.Properties.Settings.Default.UsernameHistory[profile_id]; }
            catch { view_model.Username = null; }

            // Create a new authentication pop-up.
            UsernamePasswordPopup popup = new UsernamePasswordPopup() { Owner = this, DataContext = view_model };
            popup.Loaded += (object sender_popup, RoutedEventArgs e_popup) =>
            {
                // Set initial focus.
                if (view_model.Username == null || view_model.Username.Length == 0)
                    popup.Username.Focus();
                else
                    popup.Password.Focus();
            };

            // Set the event args to fill with data.
            popup.OK.CommandParameter = e;

            // Run the authentication pop-up and pass the credentials to be returned to the event sender.
            if (popup.ShowDialog() == true)
            {
                // Password was not set using MVVP, since <PasswordBox> control does not support binding.
                e.Password = (new NetworkCredential("", popup.Password.Password)).SecurePassword;

                // Save username for the next time.
                Client.Properties.Settings.Default.UsernameHistory[profile_id] = view_model.Username;
            }
        }

        private void ConnectWizard_RequestTwoFactorAuthentication(object sender, eduOpenVPN.Management.UsernamePasswordAuthenticationRequestedEventArgs e)
        {
            var view_model = new ViewModels.Windows.TwoFactorAuthenticationPopup(sender, e);

            // Load previous "username". 2-Factor Authentication method actually.
            var session = (VPNSession)sender;
            var profile_id = session.ConnectingProfile.Instance.Base.AbsoluteUri + "|" + session.ConnectingProfile.ID;
            var previous_method_selected = false;
            try
            {
                var method_id = Client.Properties.Settings.Default.UsernameHistory[profile_id];
                var method = view_model.MethodList.FirstOrDefault(m => m.ID == method_id);
                if (method != null)
                {
                    view_model.SelectedMethod = method;
                    previous_method_selected = true;
                }
            }
            catch { }

            // Create a new authentication pop-up.
            TwoFactorAuthenticationPopup popup = new TwoFactorAuthenticationPopup() { Owner = this, DataContext = view_model };
            popup.Loaded += (object sender_popup, RoutedEventArgs e_popup) =>
            {
                // Set initial focus.
                if (!previous_method_selected && view_model.MethodList.Count > 1)
                    popup.Method.Focus();
            };

            // Set the event args to fill with data.
            popup.OK.CommandParameter = e;

            // Run the authentication pop-up and pass the credentials to be returned to the event sender.
            if (popup.ShowDialog() == true)
            {
                // Save "username" for the next time.
                Client.Properties.Settings.Default.UsernameHistory[profile_id] = view_model.SelectedMethod.ID;
            }
        }

        private void ConnectWizard_PromptSelfUpdate(object sender, PromptSelfUpdateEventArgs e)
        {
            // Create a new prompt pop-up.
            var popup = new SelfUpdatePopup() { Owner = this };

            // Populate pop-up view model.
            var view_model = popup.DataContext as ViewModels.Windows.SelfUpdatePopup;
            view_model.InstalledVersion = e.InstalledVersion;
            view_model.AvailableVersion = e.AvailableVersion;

            // Run the pop-up and pass the selected action to be returned to the event sender.
            if (popup.ShowDialog() == true)
                e.Action = view_model.Action;
        }

        private void ConnectWizard_QuitApplication(object sender, EventArgs e)
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
