/*
    eduVPN - VPN for education and research

    Copyright: 2017-2019 The Commons Conservancy eduVPN Programme
    SPDX-License-Identifier: GPL-3.0+
*/

using System;
using System.Net;
using eduVPN.Models;
using eduVPN.ViewModels.VPN;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Web;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using System.Diagnostics;


namespace eduVPN.Views.Windows
{
    /// <summary>
    /// Interaction logic for ConnectWizard.xaml
    /// </summary>
    public partial class ConnectWizard : Window, IDisposable
    {
        #region Fields

        /// <summary>
        /// HTTP listener for OAuth authorization callback and response
        /// </summary>
        private eduOAuth.HttpListener _http_listener;

        /// <summary>
        /// Authorization pop-up windows
        /// </summary>
        private Dictionary<string, System.Windows.Window> _authorization_popups = new Dictionary<string, System.Windows.Window>();

        /// <summary>
        /// VPN session state
        /// </summary>
        private VPNSessionStatusType _session_state;

        /// <summary>
        /// Tray icon
        /// </summary>
        private System.Windows.Forms.NotifyIcon _tray_icon;

        /// <summary>
        /// Cached icons to be used by <see cref="_tray_icon"/>
        /// </summary>
        private Dictionary<VPNSessionStatusType, Icon> _icons;

        /// <summary>
        /// Flag to prevent/force closing
        /// </summary>
        private bool _do_close = false;

        #endregion

        #region Properties

        /// <summary>
        /// Tray icon tool-tip text
        /// </summary>
        private string TrayIconToolTipText
        {
            get
            {
                var view_model = (ViewModels.Windows.ConnectWizard)DataContext;
                return
                    (view_model != null && view_model.ActiveSession.ConnectingProfile != null ?
                        String.Format("{0} - {1}\r\n{2}",
                            view_model.ActiveSession.ConnectingProfile?.Instance,
                            view_model.ActiveSession.ConnectingProfile,
                            view_model.ActiveSession.StateDescription) :
                        eduVPN.Properties.Settings.Default.ClientTitle).Left(63);
            }
        }

        /// <summary>
        /// Tray icon
        /// </summary>
        private Icon TrayIcon
        {
            get
            {
                var view_model = (ViewModels.Windows.ConnectWizard)DataContext;
                return view_model != null ? _icons[view_model.ActiveSession.State] : null;
            }
        }

        #endregion

        #region Constructors

        /// <summary>
        /// Constructs a window
        /// </summary>
        public ConnectWizard()
        {
            // Launch HTTP listener on the loopback interface.
            _http_listener = new eduOAuth.HttpListener(IPAddress.Loopback, 0);
            _http_listener.HttpCallback += HttpListener_HttpCallback;
            _http_listener.HttpRequest += HttpListener_HttpRequest;
            _http_listener.Start();

            InitializeComponent();
        }

        #endregion

        #region Methods

        /// <inheritdoc/>
        protected override void OnInitialized(EventArgs e)
        {
            base.OnInitialized(e);

            // Restore window position. Please mind that screen's real-estate might have changed since the previous launch.
            if (!double.IsNaN(Properties.Settings.Default.WindowLeft))
                Left = Math.Min(Math.Max(Properties.Settings.Default.WindowLeft, SystemParameters.VirtualScreenLeft), SystemParameters.VirtualScreenLeft + SystemParameters.VirtualScreenWidth - Width);
            if (!double.IsNaN(Properties.Settings.Default.WindowTop))
                Top = Math.Min(Math.Max(Properties.Settings.Default.WindowTop, SystemParameters.VirtualScreenTop), SystemParameters.VirtualScreenTop + SystemParameters.VirtualScreenHeight - Height);

            Application.Current.SessionEnding += (object sender, SessionEndingCancelEventArgs e_session_end) =>
            {
                // Save window position on logout.
                Properties.Settings.Default.WindowTop = Top;
                Properties.Settings.Default.WindowLeft = Left;
            };

            // Preload icons to be used on system tray.
            var icon_size = System.Windows.Forms.SystemInformation.SmallIconSize;
            _icons = new Dictionary<VPNSessionStatusType, Icon>();
            foreach (var status_type in Enum.GetValues(typeof(VPNSessionStatusType)).Cast<VPNSessionStatusType>())
            {
                var icon_uri = new Uri(String.Format("pack://application:,,,/Resources/VPNSessionStatusTypeIcon{0}.ico", Enum.GetName(typeof(VPNSessionStatusType), status_type)));
                try { _icons.Add(status_type, new Icon(Application.GetResourceStream(icon_uri).Stream, icon_size)); }
                catch { _icons.Add(status_type, new Icon(Application.GetResourceStream(new Uri("pack://application:,,,/Resources/VPNSessionStatusTypeIconInitializing.ico")).Stream, icon_size)); }
            }

            // Attach to view model events.
            var view_model = (ViewModels.Windows.ConnectWizard)DataContext;
            view_model.RequestInstanceAuthorization += ConnectWizard_RequestInstanceAuthorization;
            view_model.RequestOpenVPNPasswordAuthentication += ConnectWizard_RequestOpenVPNPasswordAuthentication;
            view_model.RequestOpenVPNUsernamePasswordAuthentication += ConnectWizard_RequestOpenVPNUsernamePasswordAuthentication;
            view_model.RequestTwoFactorAuthentication += ConnectWizard_RequestTwoFactorAuthentication;
            view_model.PromptSelfUpdate += ConnectWizard_PromptSelfUpdate;
            view_model.QuitApplication += ConnectWizard_QuitApplication;

            // Create notify icon, set default icon, and setup events.
            // We need to do this programatically, since System.Windows.Forms.NotifyIcon is not WPF, but borrowed from WinForms.
            _tray_icon = new System.Windows.Forms.NotifyIcon()
            {
                Text = TrayIconToolTipText,
                Icon = TrayIcon
            };
            _tray_icon.Click += TrayIcon_Click;

            // Bind to "ActiveSession.StateDescription" and "ActiveSession.State" property to keep tray icon up-to-date.
            view_model.PropertyChanged += (object sender, PropertyChangedEventArgs e2) =>
            {
                if (e2.PropertyName == nameof(view_model.ActiveSession))
                {
                    // Active session changed: sync the tray icon.
                    _tray_icon.Text = TrayIconToolTipText;
                    _tray_icon.Icon = TrayIcon;

                    if (view_model.ActiveSession != VPNSession.Blank)
                    {
                        // Initialize VPN session state.
                        _session_state = view_model.ActiveSession.State;

                        // Bind to the session for property changes.
                        view_model.ActiveSession.PropertyChanged += (object sender_Session, PropertyChangedEventArgs e_Session) =>
                        {
                            switch (e_Session.PropertyName)
                            {
                                case nameof(view_model.ActiveSession.ConnectingProfile):
                                case nameof(view_model.ActiveSession.StateDescription):
                                    _tray_icon.Text = TrayIconToolTipText;
                                    break;

                                case nameof(view_model.ActiveSession.State):
                                    {
                                        _tray_icon.Icon = TrayIcon;

                                        if (!IsVisible)
                                        {
                                            // Client is minimized.
                                            switch (view_model.ActiveSession.State)
                                            {
                                                case VPNSessionStatusType.Connected:
                                                    // Client connected. Popup the balloon message.
                                                    _tray_icon.ShowBalloonTip(
                                                        5000,
                                                        String.Format(Views.Resources.Strings.SystemTrayBalloonConnectedTitle, view_model.ActiveSession.ConnectingProfile),
                                                        String.Format(Views.Resources.Strings.SystemTrayBalloonConnectedMessage, view_model.ActiveSession.TunnelAddress, view_model.ActiveSession.IPv6TunnelAddress),
                                                        System.Windows.Forms.ToolTipIcon.Info);
                                                    break;

                                                default:
                                                    if (_session_state == VPNSessionStatusType.Connected)
                                                    {
                                                        // Client has been disconnected. Popup the balloon message.
                                                        _tray_icon.ShowBalloonTip(
                                                            5000,
                                                            eduVPN.Properties.Settings.Default.ClientTitle,
                                                            Views.Resources.Strings.SystemTrayBalloonDisconnectedMessage,
                                                            System.Windows.Forms.ToolTipIcon.Info);
                                                    }
                                                    break;
                                            }
                                        }

                                        // Save VPN session state.
                                        _session_state = view_model.ActiveSession.State;
                                    }
                                    break;
                            }
                        };
                    }
                }
            };

            Loaded += Window_Loaded;
            Closing += Window_Closing;

            // Set context menu data context to allow bindings to work.
            if (Resources["SystemTrayMenu"] is ContextMenu menu)
                menu.DataContext = DataContext;
        }

        /// <inheritdoc/>
        protected override void OnClosed(EventArgs e)
        {
            // Stop the OAuth listener.
            _http_listener.Stop();

            base.OnClosed(e);
        }

        /// <summary>
        /// Processes OAuth HTTP callback and reactivates the connecting wizard window.
        /// </summary>
        /// <param name="sender">HTTP peer/client of type <see cref="System.Net.Sockets.TcpClient"/></param>
        /// <param name="e">Event arguments</param>
        /// <remarks>Occurs when OAuth callback received.</remarks>
        private void HttpListener_HttpCallback(object sender, eduOAuth.HttpCallbackEventArgs e)
        {
            // Get popup window out of "state" parameter.
            var query = HttpUtility.ParseQueryString(e.Uri.Query);
            if (!_authorization_popups.TryGetValue(query["state"], out var popup))
                throw new HttpException(400, Views.Resources.Strings.ErrorOAuthState);

            Dispatcher.Invoke(DispatcherPriority.Normal, (Action)(() =>
            {
                // Set the callback URI. This will close the pop-up.
                ((ViewModels.Windows.AuthorizationPopup)popup.DataContext).CallbackURI = e.Uri;

                // (Re)activate main window.
                Open_Click(sender, new RoutedEventArgs());
            }));
        }

        /// <summary>
        /// Returns /favicon.ico.
        /// </summary>
        /// <param name="sender">HTTP peer/client of type <see cref="System.Net.Sockets.TcpClient"/></param>
        /// <param name="e">Event arguments</param>
        /// <remarks>Occurs when browser requests data.</remarks>
        private void HttpListener_HttpRequest(object sender, eduOAuth.HttpRequestEventArgs e)
        {
            if (e.Uri.AbsolutePath.ToLowerInvariant() == "/favicon.ico")
            {
                var res = Application.GetResourceStream(new Uri("pack://application:,,,/Resources/App.ico"));
                e.Type = res.ContentType;
                e.Content = res.Stream;
            }
        }

        /// <summary>
        /// Shows tray icon.
        /// </summary>
        /// <param name="sender">Event sender (ignored)</param>
        /// <param name="e">Event arguments (ignored)</param>
        /// <remarks>Occurs when the element is laid out, rendered, and ready for interaction.</remarks>
        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            // Show tray icon when Connect Wizard is loaded.
            _tray_icon.Visible = true;
        }

        /// <summary>
        /// Closes or hides connect wizard window.
        /// </summary>
        /// <param name="sender">Event sender (ignored)</param>
        /// <param name="e">Event arguments</param>
        /// <remarks>Occurs directly after <see cref="Close"/> is called, and can be handled to cancel window closure.</remarks>
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
                Properties.Settings.Default.WindowTop = Top;
                Properties.Settings.Default.WindowLeft = Left;
            }
            else
            {
                // User/system tried to close our window using something else than [x] button.
                // Cancel close and revert to hide.
                e.Cancel = true;
                Hide();
            }
        }

        /// <summary>
        /// Reactivates connect wizard window on left mouse click; or pop-up context menu on right mouse click.
        /// </summary>
        /// <param name="sender">Event sender</param>
        /// <param name="e">Event arguments of type <see cref="System.Windows.Forms.MouseEventArgs"/></param>
        /// <remarks>Occurs when the user clicks the icon in the notification area.</remarks>
        private void TrayIcon_Click(object sender, EventArgs e)
        {
            if (e is System.Windows.Forms.MouseEventArgs e_mouse)
            {
                switch (e_mouse.Button)
                {
                    case System.Windows.Forms.MouseButtons.Left:
                        Open_Click(sender, new RoutedEventArgs());
                        break;

                    case System.Windows.Forms.MouseButtons.Right:
                        // Pop-up context menu.
                        if (Resources["SystemTrayMenu"] is ContextMenu menu)
                            menu.IsOpen = true;
                        break;
                }
            }
        }

        /// <summary>
        /// Hides the connecting wizard window to the system tray.
        /// </summary>
        /// <param name="sender">Event sender (ignored)</param>
        /// <param name="e">Event arguments (ignored)</param>
        /// <remarks>Occurs when a <see cref="Button"/> is clicked.</remarks>
        protected void Hide_Click(object sender, RoutedEventArgs e)
        {
            Hide();

            if (!Properties.Settings.Default.SystemTrayMinimizedWarned)
            {
                // Notify user that the eduVPN client did not close, but was "minimized" to system tray.
                _tray_icon.ShowBalloonTip(
                    10000,
                    eduVPN.Properties.Settings.Default.ClientTitle,
                    Views.Resources.Strings.SystemTrayBalloonHiddenMessage,
                    System.Windows.Forms.ToolTipIcon.Info);

                Properties.Settings.Default.SystemTrayMinimizedWarned = true;
            }
        }

        /// <summary>
        /// Restores the connecting wizard window from the system tray.
        /// </summary>
        /// <param name="sender">Event sender (ignored)</param>
        /// <param name="e">Event arguments (ignored)</param>
        /// <remarks>Occurs when a <see cref="MenuItem"/> is clicked.</remarks>
        protected void Open_Click(object sender, RoutedEventArgs e)
        {
            // (Re)activate window.
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
        }

        /// <summary>
        /// Exits application.
        /// </summary>
        /// <param name="sender">Event sender (ignored)</param>
        /// <param name="e">Event arguments (ignored)</param>
        /// <remarks>Occurs when a <see cref="MenuItem"/> is clicked.</remarks>
        protected void Exit_Click(object sender, RoutedEventArgs e)
        {
            _do_close = true;
            Close();
        }

        /// <summary>
        /// Triggers OAuth authorization request and pops-up user authorization prompt.
        /// </summary>
        /// <param name="sender"><see cref="eduVPN.ViewModels.Windows.ConnectWizard"/> requiring instance authorization</param>
        /// <param name="e">Event arguments. This method fills <see cref="RequestInstanceAuthorizationEventArgs.CallbackURI"/> member with received authorization grant.</param>
        /// <remarks>Occurs when instance requests authorization.</remarks>
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
            view_model.AuthorizationGrant.RedirectEndpoint = new Uri(String.Format("http://{0}:{1}/callback", ((IPEndPoint)_http_listener.LocalEndpoint).Address, ((IPEndPoint)_http_listener.LocalEndpoint).Port));
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
        /// Pops-up password prompt.
        /// </summary>
        /// <param name="sender">OpenVPN session of <see cref="OpenVPNSession"/> type.</param>
        /// <param name="e">Event arguments. This method fills it with user input.</param>
        /// <remarks>Occurs when OpenVPN requests a password.</remarks>
        private void ConnectWizard_RequestOpenVPNPasswordAuthentication(object sender, eduOpenVPN.Management.PasswordAuthenticationRequestedEventArgs e)
        {
            var view_model = new ViewModels.Windows.PasswordPopup(sender, e);

            // Create a new authentication pop-up.
            var popup = new PasswordPopup() { Owner = this, DataContext = view_model };

            // Set the event args to fill with data to be returned to the event sender.
            ((Button)popup.FindName("OK")).CommandParameter = e;

            // Run the authentication pop-up and pass the credentials to be returned to the event sender.
            if (popup.ShowDialog() == true && popup.FindName("Password") is PasswordBox Password)
            {
                // Password was not set using MVVP, since <PasswordBox> control does not support binding.
                e.Password = (new NetworkCredential("", Password.Password)).SecurePassword;
            }
        }

        /// <summary>
        /// Pops-up username and password prompt.
        /// </summary>
        /// <param name="sender">OpenVPN session of <see cref="OpenVPNSession"/> type.</param>
        /// <param name="e">Event arguments. This method fills it with user input.</param>
        /// <remarks>Occurs when OpenVPN requests a username and password.</remarks>
        private void ConnectWizard_RequestOpenVPNUsernamePasswordAuthentication(object sender, eduOpenVPN.Management.UsernamePasswordAuthenticationRequestedEventArgs e)
        {
            var view_model = new ViewModels.Windows.UsernamePasswordPopup(sender, e);

            // Create a new authentication pop-up.
            var popup = new UsernamePasswordPopup() { Owner = this, DataContext = view_model };
            popup.Loaded += (object sender_popup, RoutedEventArgs e_popup) =>
            {
                // Set initial focus.
                (popup.FindName(String.IsNullOrEmpty(view_model.Username) ? "Username" : "Password") as Control)?.Focus();
            };

            // Set the event args to fill with data to be returned to the event sender.
            ((Button)popup.FindName("OK")).CommandParameter = e;

            // Run the authentication pop-up and pass the credentials to be returned to the event sender.
            if (popup.ShowDialog() == true && popup.FindName("Password") is PasswordBox Password)
            {
                // Password was not set using MVVP, since <PasswordBox> control does not support binding.
                e.Password = (new NetworkCredential("", Password.Password)).SecurePassword;
            }
        }

        /// <summary>
        /// Pops-up 2-Factor Authentication prompt.
        /// </summary>
        /// <param name="sender">OpenVPN session of <see cref="OpenVPNSession"/> type.</param>
        /// <param name="e">Event arguments. This method fills it with user input.</param>
        /// <remarks>Occurs when 2-Factor Authentication requested.</remarks>
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
                        (popup.FindName("Method") as Control)?.Focus();
                }
            };

            // Set the event args to fill with data to be returned to the event sender.
            ((Button)popup.FindName("OK")).CommandParameter = e;

            // Run the authentication pop-up.
            popup.ShowDialog();
        }

        /// <summary>
        /// Pops up self-update prompt.
        /// </summary>
        /// <param name="sender">Connection wizard view model of <see cref="ViewModels.Windows.ConnectWizard"/> type.</param>
        /// <param name="e">Event arguments. This method fills it with user input.</param>
        /// <remarks>Occurs when product update is available.</remarks>
        private void ConnectWizard_PromptSelfUpdate(object sender, PromptSelfUpdateEventArgs e)
        {
            var view_model = new ViewModels.Windows.SelfUpdatePopup(sender, e);

            // Create a new prompt pop-up.
            var popup = new SelfUpdatePopup() { Owner = this, DataContext = view_model };

            // Run the pop-up and pass the selected action to be returned to the event sender.
            if (popup.ShowDialog() == true)
                e.Action = view_model.Action;
        }

        /// <summary>
        /// Closes connecting wizard window to quit the application.
        /// </summary>
        /// <param name="sender">Event sender (ignored)</param>
        /// <param name="e">Event arguments (ignored)</param>
        /// <remarks>Occurs when application should quit.</remarks>
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
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
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
