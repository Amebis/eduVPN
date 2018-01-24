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
using System.Web;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;

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

        /// <summary>
        /// HTTP listener for OAuth authorization callback and response
        /// </summary>
        private eduOAuth.HttpListener _listener = new eduOAuth.HttpListener(IPAddress.Loopback, 0);

        /// <summary>
        /// Authorization pop-up windows
        /// </summary>
        private Dictionary<string, AuthorizationPopup> _authorization_popups = new Dictionary<string, AuthorizationPopup>();

        #endregion

        #region Constructors

        /// <summary>
        /// Constructs a window
        /// </summary>
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
                                    {
                                        _tray_icon.Icon = _icons[view_model.ActiveSession.State];

                                        if (view_model.ActiveSession.State == VPNSessionStatusType.Connected && !IsVisible)
                                        {
                                            // Client connected while "minimized". Popup the balloon message.
                                            _tray_icon.ShowBalloonTip(
                                                5000,
                                                string.Format(Client.Resources.Strings.SystemTrayBalloonConnectedTitle, view_model.ActiveSession.ConnectingProfile),
                                                string.Format(Client.Resources.Strings.SystemTrayBalloonConnectedMessage, view_model.ActiveSession.TunnelAddress, view_model.ActiveSession.IPv6TunnelAddress),
                                                System.Windows.Forms.ToolTipIcon.Info);
                                        }
                                    }
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

            Application.Current.SessionEnding += (object sender, SessionEndingCancelEventArgs e_session_end) =>
            {
                // Save view settings on logout.
                Client.Properties.Settings.Default.WindowTop = Top;
                Client.Properties.Settings.Default.WindowLeft = Left;
                Client.Properties.Settings.Default.Save();

                // Save view model settings on logout.
                Properties.Settings.Default.Save();
            };

            // Bind to HttpCallback to process OAuth authorization grant.
            _listener.HttpCallback += (object sender, eduOAuth.HttpCallbackEventArgs e_callback) =>
            {
                var query = HttpUtility.ParseQueryString(e_callback.Uri.Query);
                if (!_authorization_popups.TryGetValue(query["state"], out var popup))
                    throw new HttpException(400, Client.Resources.Strings.ErrorOAuthState);

                Dispatcher.Invoke(DispatcherPriority.Normal, (Action)(() =>
                {
                    // Set the callback URI. This will close the pop-up.
                    ((ViewModels.Windows.AuthorizationPopup)popup.DataContext).CallbackURI = e_callback.Uri;

                    // (Re)activate main window.
                    if (!IsActive)
                        Show();
                    Topmost = true;
                    try
                    {
                        Activate();
                        Focus();
                    }
                    finally
                    {
                        Topmost = false;
                    }
                }));
            };

            // Launch HTTP listener on the loopback interface.
            _listener.Start();

            base.OnInitialized(e);
        }

        /// <inheritdoc/>
        protected override void OnClosed(EventArgs e)
        {
            // Stop the OAuth listener.
            _listener.Stop();

            base.OnClosed(e);
        }

        private void Hide_Click(object sender, RoutedEventArgs e)
        {
            Hide();

            if (!Client.Properties.Settings.Default.SystemTrayMinimizedWarned)
            {
                // Notify user that the eduVPN client did not close, but was "minimized" to system tray.
                _tray_icon.ShowBalloonTip(
                    10000,
                    Client.Resources.Strings.MainWindowTitle,
                    Client.Resources.Strings.SystemTrayBalloonHiddenMessage,
                    System.Windows.Forms.ToolTipIcon.Info);

                Client.Properties.Settings.Default.SystemTrayMinimizedWarned = true;
            }
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

                // Dismiss all pop-ups.
                foreach (Window popup in OwnedWindows)
                    popup.Close();

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
        /// <param name="sender"><see cref="eduVPN.ViewModels.Windows.ConnectWizard"/> requiring instance authorization</param>
        /// <param name="e">Authorization request event arguments</param>
        private void ConnectWizard_RequestInstanceAuthorization(object sender, RequestInstanceAuthorizationEventArgs e)
        {
            var view_model = new ViewModels.Windows.AuthorizationPopup(sender, e);

            // Create a new authorization pop-up.
            var popup = new AuthorizationPopup() { Owner = this, DataContext = view_model };
            view_model.PropertyChanged += (object sender2, PropertyChangedEventArgs e2) =>
            {
                if (e2.PropertyName == nameof(view_model.CallbackURI) && view_model.CallbackURI != null)
                {
                    // Close the authorization pop-up after the callback URI is set.
                    popup.DialogResult = true;
                }
            };

            // Set the redirect URI and make the final authorization URI.
            view_model.AuthorizationGrant.RedirectEndpoint = new Uri(string.Format("http://{0}:{1}/callback", IPAddress.Loopback, ((IPEndPoint)_listener.LocalEndpoint).Port));
            var authorization_uri = view_model.AuthorizationGrant.AuthorizationURI;

            // Extract the state. We use it as a key to support multiple pending authorizations.
            var query = HttpUtility.ParseQueryString(authorization_uri.Query);
            _authorization_popups.Add(query["state"], popup);

            // Trigger authorization.
            System.Diagnostics.Process.Start(authorization_uri.ToString());

            // Run the authorization pop-up and pass the access token to be returned to the event sender.
            if (popup.ShowDialog() == true)
                e.CallbackURI = view_model.CallbackURI;

            _authorization_popups.Remove(query["state"]);
        }

        /// <summary>
        /// Called when user tries to connect to a profile that required 2-Factor Authentication user is not enrolled with
        /// </summary>
        /// <param name="sender"><see cref="eduVPN.ViewModels.Panels.ConnectingSelectPanel"/> requiring enrollment</param>
        /// <param name="e">Enrollment event arguments</param>
        private void ConnectWizard_RequestTwoFactorEnrollment(object sender, RequestTwoFactorEnrollmentEventArgs e)
        {
            var view_model = new ViewModels.Windows.TwoFactorEnrollmentPopup(sender, e);

            // Create a new 2FA enroll pop-up.
            var popup = new TwoFactorEnrollmentPopup() { Owner = this, DataContext = view_model };
            popup.Loaded += (object sender_popup, RoutedEventArgs e_popup) =>
            {
                // Set initial focus.
                if (view_model.SelectedMethod == null && view_model.MethodList.Count > 0)
                {
                    view_model.SelectedMethod = view_model.MethodList[0];
                    if (view_model.MethodList.Count > 1)
                        popup.Method.Focus();
                }
            };

            // Set the event args to fill with data.
            popup.OK.CommandParameter = e;

            // Run the 2FA enrollment pop-up and pass the credentials to be returned to the event sender.
            popup.ShowDialog();
        }

        /// <summary>
        /// Called when an OpenVPN session requests a password authentication
        /// </summary>
        /// <param name="sender"><see cref="eduVPN.ViewModels.Windows.ConnectWizard"/> requiring authentication</param>
        /// <param name="e">Authentication request event arguments</param>
        private void ConnectWizard_RequestOpenVPNPasswordAuthentication(object sender, eduOpenVPN.Management.PasswordAuthenticationRequestedEventArgs e)
        {
            var view_model = new ViewModels.Windows.PasswordPopup(sender, e);

            // Create a new authentication pop-up.
            var popup = new PasswordPopup() { Owner = this, DataContext = view_model };

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
        /// <param name="sender"><see cref="eduVPN.ViewModels.Windows.ConnectWizard"/> requiring authentication</param>
        /// <param name="e">Authentication request event arguments</param>
        private void ConnectWizard_RequestOpenVPNUsernamePasswordAuthentication(object sender, eduOpenVPN.Management.UsernamePasswordAuthenticationRequestedEventArgs e)
        {
            var view_model = new ViewModels.Windows.UsernamePasswordPopup(sender, e);

            // Create a new authentication pop-up.
            var popup = new UsernamePasswordPopup() { Owner = this, DataContext = view_model };
            popup.Loaded += (object sender_popup, RoutedEventArgs e_popup) =>
            {
                // Set initial focus.
                if (string.IsNullOrEmpty(view_model.Username))
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
            }
        }

        private void ConnectWizard_RequestTwoFactorAuthentication(object sender, eduOpenVPN.Management.UsernamePasswordAuthenticationRequestedEventArgs e)
        {
            var view_model = new ViewModels.Windows.TwoFactorAuthenticationPopup(sender, e);

            // Create a new authentication pop-up.
            var popup = new TwoFactorAuthenticationPopup() { Owner = this, DataContext = view_model };
            popup.Loaded += (object sender_popup, RoutedEventArgs e_popup) =>
            {
                // Set initial focus.
                if (view_model.SelectedMethod == null && view_model.MethodList.Count > 0)
                {
                    view_model.SelectedMethod = view_model.MethodList[0];
                    if (view_model.MethodList.Count > 1)
                        popup.Method.Focus();
                }
            };

            // Set the event args to fill with data.
            popup.OK.CommandParameter = e;

            // Run the authentication pop-up and pass the credentials to be returned to the event sender.
            popup.ShowDialog();
        }

        private void ConnectWizard_PromptSelfUpdate(object sender, PromptSelfUpdateEventArgs e)
        {
            var view_model = new ViewModels.Windows.SelfUpdatePopup(sender, e);

            // Create a new prompt pop-up.
            var popup = new SelfUpdatePopup() { Owner = this, DataContext = view_model };

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
        /// <summary>
        /// Flag to detect redundant <see cref="Dispose(bool)"/> calls.
        /// </summary>
        private bool disposedValue = false;

        /// <summary>
        /// Called to dispose the object.
        /// </summary>
        /// <param name="disposing">Dispose managed objects</param>
        /// <remarks>
        /// To release resources for inherited classes, override this method.
        /// Call <c>base.Dispose(disposing)</c> within it to release parent class resources, and release child class resources if <paramref name="disposing"/> parameter is <c>true</c>.
        /// This method can get called multiple times for the same object instance. When the child specific resources should be released only once, introduce a flag to detect redundant calls.
        /// </remarks>
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

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting resources.
        /// </summary>
        /// <remarks>
        /// This method calls <see cref="Dispose(bool)"/> with <c>disposing</c> parameter set to <c>true</c>.
        /// To implement resource releasing override the <see cref="Dispose(bool)"/> method.
        /// </remarks>
        public void Dispose()
        {
            // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
            Dispose(true);
        }
        #endregion
    }
}
