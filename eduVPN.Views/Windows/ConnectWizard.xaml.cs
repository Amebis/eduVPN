/*
    eduVPN - VPN for education and research

    Copyright: 2017-2024 The Commons Conservancy
    SPDX-License-Identifier: GPL-3.0+
*/

using eduEx.System;
using eduVPN.Models;
using eduVPN.ViewModels.VPN;
using eduVPN.Views.Resources;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;

namespace eduVPN.Views.Windows
{
    /// <summary>
    /// Interaction logic for ConnectWizard.xaml
    /// </summary>
    public partial class ConnectWizard : Window, IDisposable
    {
        #region Constants

        private const int WM_WININICHANGE = 0x001A;

        #endregion

        #region Fields

        /// <summary>
        /// VPN session state
        /// </summary>
        private SessionStatusType SessionState;

        /// <summary>
        /// Icon on the notification tray
        /// </summary>
        private System.Windows.Forms.NotifyIcon NotifyIcon;

        /// <summary>
        /// Cached icons to be used by <see cref="NotifyIcon"/>
        /// </summary>
        private Dictionary<SessionStatusType, Icon> Icons;

        /// <summary>
        /// Flag to prevent/force closing
        /// </summary>
        private bool DoClose = false;

        /// <summary>
        /// Server to connect to next after done disconnecting.
        /// </summary>
        private Server PendingServerConnect = null;

        #endregion

        #region Properties

        /// <summary>
        /// Tray icon tool-tip text
        /// </summary>
        private string TrayIconToolTipText
        {
            get
            {
                return
                    (DataContext is ViewModels.Windows.ConnectWizard viewModel && viewModel.ConnectionPage.ActiveSession != null ?
                        string.Format("{0} - {1}\r\n{2}",
                            viewModel.ConnectionPage.ActiveSession.Server,
                            viewModel.ConnectionPage.ActiveSession.Profile,
                            viewModel.ConnectionPage.ActiveSession.StateDescription) :
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
                return Icons[
                    DataContext is ViewModels.Windows.ConnectWizard viewModel &&
                    viewModel.ConnectionPage.ActiveSession != null ? viewModel.ConnectionPage.ActiveSession.State : SessionStatusType.Disconnected];
            }
        }

        /// <summary>
        /// Use dark theme
        /// </summary>
        public bool UseDarkTheme
        {
            get => (bool)GetValue(UseDarkThemeProperty);
            set => SetValue(UseDarkThemeProperty, value);
        }

        public static readonly DependencyProperty UseDarkThemeProperty =
            DependencyProperty.Register(
                nameof(UseDarkTheme),
                typeof(bool),
                typeof(ConnectWizard),
                new FrameworkPropertyMetadata(
                    false,
                    FrameworkPropertyMetadataOptions.AffectsRender,
                    (DependencyObject d, DependencyPropertyChangedEventArgs e) =>
                    {
                        var src = new Uri(new Uri("pack://application:,,,/eduVPN.Views;component/Resources/"), (bool)e.NewValue ? "ColorsDark.xaml" : "Colors.xaml");
                        var mergedDictionary = Application.Current?.Resources.MergedDictionaries.FirstOrDefault(dict => dict.Contains("WindowColor"));
                        if (mergedDictionary != null)
                            mergedDictionary.Source = src;
                    }));

        #endregion

        #region Constructors

        /// <summary>
        /// Constructs a window
        /// </summary>
        public ConnectWizard()
        {
            // As eduVPN.ViewModels.Windows.ConnectWizard is IDisposable, special care needs to be taken.
            // The view itself does not dispose the view model it is using.
            void SetViewModel()
            {
                if (DataContext == null)
                    DataContext = new ViewModels.Windows.ConnectWizard();
            }
            SetViewModel();
            Loaded += (object sender, RoutedEventArgs e) => SetViewModel();

            void DisposeViewModel()
            {
                if (DataContext is IDisposable viewModel)
                {
                    DataContext = null;
                    viewModel.Dispose();
                }
            }
            Unloaded += (object sender, RoutedEventArgs e) => DisposeViewModel();
            Dispatcher.ShutdownStarted += (object sender, EventArgs e) => DisposeViewModel();

            RefreshUseDarkTheme();
            InitializeComponent();
        }

        #endregion

        #region Methods

        /// <inheritdoc/>
        protected override void OnInitialized(EventArgs e)
        {
            base.OnInitialized(e);

            // Restore window position. Please mind that screen's real-estate might have changed since the previous launch.
            if (eduVPN.Properties.Settings.Default.IsSignon)
            {
                Visibility = Visibility.Hidden;
                ShowInTaskbar = false;
            }
            switch (Properties.Settings.Default.WindowState)
            {
                case 1: WindowState = WindowState.Minimized; break;
                case 2: WindowState = WindowState.Maximized; break;
            }
            Height = !double.IsNaN(Properties.Settings.Default.WindowHeight) ?
                Math.Max(Math.Min(Properties.Settings.Default.WindowHeight, SystemParameters.VirtualScreenHeight), MinHeight) :
                MinHeight;
            Width = !double.IsNaN(Properties.Settings.Default.WindowWidth) ?
                Math.Max(Math.Min(Properties.Settings.Default.WindowWidth, SystemParameters.VirtualScreenWidth), MinWidth) :
                MinWidth;
            if (!double.IsNaN(Properties.Settings.Default.WindowLeft))
                Left = Math.Min(Math.Max(Properties.Settings.Default.WindowLeft, SystemParameters.VirtualScreenLeft), SystemParameters.VirtualScreenLeft + SystemParameters.VirtualScreenWidth - Width);
            if (!double.IsNaN(Properties.Settings.Default.WindowTop))
                Top = Math.Min(Math.Max(Properties.Settings.Default.WindowTop, SystemParameters.VirtualScreenTop), SystemParameters.VirtualScreenTop + SystemParameters.VirtualScreenHeight - Height);

            // Attach to application events.
            Application.Current.SessionEnding += Window_Save;

            // Preload icons to be used on system tray.
            var iconSize = System.Windows.Forms.SystemInformation.SmallIconSize;
            Icons = new Dictionary<SessionStatusType, Icon>();
            foreach (var statusType in Enum.GetValues(typeof(SessionStatusType)).Cast<SessionStatusType>())
            {
                var iconUri = new Uri(string.Format("pack://application:,,,/eduVPN.Views;component/Resources/SessionStatusType{0}Icon.ico", Enum.GetName(typeof(SessionStatusType), statusType)));
                try { Icons.Add(statusType, new Icon(Application.GetResourceStream(iconUri).Stream, iconSize)); }
                catch { Icons.Add(statusType, new Icon(Application.GetResourceStream(new Uri("pack://application:,,,/Resources/App.ico")).Stream, iconSize)); }
            }

            // Attach to view model events.
            var viewModel = DataContext as ViewModels.Windows.ConnectWizard;
            viewModel.QuitApplication += Exit_Click;

            // Set sys-tray menu data context to allow bindings to work.
            if (Resources["SystemTrayMenu"] is ContextMenu menu)
                menu.DataContext = viewModel;

            // Bind to HomePage changes to keep the sys-tray menu list of servers updated.
            InstituteAccessServers_CollectionChanged(this, null);
            viewModel.HomePage.InstituteAccessServers.CollectionChanged += InstituteAccessServers_CollectionChanged;
            SecureInternetServers_CollectionChanged(this, null);
            viewModel.HomePage.SecureInternetServers.CollectionChanged += SecureInternetServers_CollectionChanged;
            OwnServers_CollectionChanged(this, null);
            viewModel.HomePage.OwnServers.CollectionChanged += OwnServers_CollectionChanged;

            // Create notify icon, set default icon, and setup events.
            // We need to do this programmatically, since System.Windows.Forms.NotifyIcon is not WPF, but borrowed from WinForms.
            NotifyIcon = new System.Windows.Forms.NotifyIcon()
            {
                Text = TrayIconToolTipText,
                Icon = TrayIcon
            };
            NotifyIcon.Click += NotifyIcon_Click;
            NotifyIcon.BalloonTipClicked += NotifyIcon_BalloonTipClicked;

            // Bind to "ConnectionPage.ActiveSession.StateDescription" and "ConnectionPage.ActiveSession.State" property to keep tray icon up-to-date.
            viewModel.ConnectionPage.PropertyChanged += (object sender, PropertyChangedEventArgs e2) =>
            {
                if (e2.PropertyName == nameof(viewModel.ConnectionPage.ActiveSession))
                {
                    // Active session changed: sync the tray icon.
                    NotifyIcon.Text = TrayIconToolTipText;
                    NotifyIcon.Icon = TrayIcon;

                    // Active session changed: sync the tray menu checkboxes.
                    foreach (var item in (Resources["SystemTrayMenu"] as ContextMenu)?.Items)
                        if (item is MenuItem menuItem && menuItem.DataContext is Server server)
                            menuItem.IsChecked = viewModel.ConnectionPage.ActiveSession != null && viewModel.ConnectionPage.ActiveSession.Server.Equals(server);

                    if (viewModel.ConnectionPage.ActiveSession == null && PendingServerConnect != null)
                    {
                        Servers_Connect(PendingServerConnect);
                        PendingServerConnect = null;
                    }

                    if (viewModel.ConnectionPage.ActiveSession != null)
                    {
                        // Initialize VPN session state.
                        SessionState = viewModel.ConnectionPage.ActiveSession.State;

                        viewModel.ConnectionPage.ActiveSession.WarnExpiration += (object sender2, EventArgs e3) =>
                        {
                            NotifyIcon.ShowBalloonTip(
                                1000 * 60 * 5,
                                string.Format(Strings.SystemTrayBalloonRenewSessionTitle, viewModel.ConnectionPage.ActiveSession.Server),
                                string.Format(Strings.SystemTrayBalloonRenewSessionMessage, viewModel.ConnectionPage.ActiveSession.ValidTo.ToLocalTime().ToString("f")),
                                System.Windows.Forms.ToolTipIcon.Info);
                        };

                        // Bind to the session for property changes.
                        viewModel.ConnectionPage.ActiveSession.PropertyChanged += (object sender3, PropertyChangedEventArgs e3) =>
                        {
                            if (viewModel.ConnectionPage.ActiveSession != sender3)
                                return;

                            switch (e3.PropertyName)
                            {
                                case nameof(viewModel.ConnectionPage.ActiveSession.Profile):
                                case nameof(viewModel.ConnectionPage.ActiveSession.StateDescription):
                                    NotifyIcon.Text = TrayIconToolTipText;
                                    break;

                                case nameof(viewModel.ConnectionPage.ActiveSession.State):
                                    {
                                        NotifyIcon.Icon = TrayIcon;

                                        switch (viewModel.ConnectionPage.ActiveSession.State)
                                        {
                                            case SessionStatusType.Connected:
                                                // Client connected. Popup the balloon message.
                                                NotifyIcon.ShowBalloonTip(
                                                    1000 * 5,
                                                    string.Format(Strings.SystemTrayBalloonConnectedTitle, viewModel.ConnectionPage.ActiveSession.Profile),
                                                    string.Format(Strings.SystemTrayBalloonConnectedMessage, viewModel.ConnectionPage.ActiveSession.TunnelAddress, viewModel.ConnectionPage.ActiveSession.IPv6TunnelAddress),
                                                    System.Windows.Forms.ToolTipIcon.Info);
                                                break;

                                            default:
                                                if (SessionState == SessionStatusType.Connected)
                                                {
                                                    // Client has been disconnected. Popup the balloon message.
                                                    NotifyIcon.ShowBalloonTip(
                                                        1000 * 5,
                                                        eduVPN.Properties.Settings.Default.ClientTitle,
                                                        Strings.SystemTrayBalloonDisconnectedMessage,
                                                        System.Windows.Forms.ToolTipIcon.Warning);
                                                }
                                                break;
                                        }

                                        // Save VPN session state.
                                        SessionState = viewModel.ConnectionPage.ActiveSession.State;
                                    }
                                    break;
                            }
                        };
                    }
                    else
                        SessionState = SessionStatusType.Disconnected;
                }
            };

            viewModel.AutoReconnectFailed += (object sender, AutoReconnectFailedEventArgs e2) =>
            {
                NotifyIcon.ShowBalloonTip(
                    1000 * 60 * 5,
                    string.Format(Strings.SystemTrayBalloonAutoReconnectFailedTitle, e2.ConnectingServer),
                    string.Format(Strings.SystemTrayBalloonAutoReconnectFailedMessage2, e2.AuthenticatingServer),
                    System.Windows.Forms.ToolTipIcon.Warning);
            };

            LocationChanged += Window_LocationChanged;
            Closing += Window_Closing;

            NotifyIcon.Visible = true;
        }

        /// <inheritdoc/>
        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);
            var source = PresentationSource.FromVisual(this) as HwndSource;
            source.AddHook(WndProc);
            RefreshUseDarkTheme();
        }

        [DebuggerStepThrough]
        private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            if (msg == WM_WININICHANGE &&
                lParam != IntPtr.Zero &&
                string.Equals(Marshal.PtrToStringUni(lParam), "ImmersiveColorSet", StringComparison.OrdinalIgnoreCase))
                RefreshUseDarkTheme();
            return IntPtr.Zero;
        }

        private void RefreshUseDarkTheme()
        {
            UseDarkTheme = Registry.GetValue(@"HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Themes\Personalize", "AppsUseLightTheme", 1) is int appsUseLightTheme && appsUseLightTheme == 0;
        }

        /// <summary>
        /// Closes or hides connect wizard window.
        /// </summary>
        /// <param name="sender">Event sender (ignored)</param>
        /// <param name="e">Event arguments</param>
        /// <remarks>Occurs directly after <see cref="Close"/> is called, and can be handled to cancel window closure.</remarks>
        private void Window_Closing(object sender, CancelEventArgs e)
        {
            if (DoClose)
            {
                // Hide tray icon when closed.
                NotifyIcon.Visible = false;

                // Save window position on closing.
                Window_Save(sender, e);
            }
            else
            {
                // User/system tried to close our window.
                // Cancel close and revert to hide.
                e.Cancel = true;
                Hide();

                if (!Properties.Settings.Default.SystemTrayMinimizedWarned)
                {
                    // Notify user that the eduVPN client did not close, but was "minimized" to system tray.
                    NotifyIcon.ShowBalloonTip(
                        1000 * 5,
                        eduVPN.Properties.Settings.Default.ClientTitle,
                        Strings.SystemTrayBalloonHiddenMessage,
                        System.Windows.Forms.ToolTipIcon.Info);

                    Properties.Settings.Default.SystemTrayMinimizedWarned = true;
                }
            }
        }

        /// <summary>
        /// Saves window location.
        /// </summary>
        /// <param name="sender">Event sender</param>
        /// <param name="e">Event arguments</param>
        /// <remarks>Occurs when the window's location changes.</remarks>
        private void Window_LocationChanged(object sender, EventArgs e)
        {
            Window_Save(sender, e);
        }

        /// <summary>
        /// Reactivates connect wizard window on left mouse click; or pop-up context menu on right mouse click.
        /// </summary>
        /// <param name="sender">Event sender</param>
        /// <param name="e">Event arguments of type <see cref="System.Windows.Forms.MouseEventArgs"/></param>
        /// <remarks>Occurs when the user clicks the icon in the notification area.</remarks>
        private void NotifyIcon_Click(object sender, EventArgs e)
        {
            if (e is System.Windows.Forms.MouseEventArgs eMouse)
            {
                switch (eMouse.Button)
                {
                    case System.Windows.Forms.MouseButtons.Left:
                        Open_Click(sender, new RoutedEventArgs());
                        break;

                    case System.Windows.Forms.MouseButtons.Right:
                        // Pop-up context menu.
                        if (Resources["SystemTrayMenu"] is ContextMenu menu)
                        {
                            menu.IsOpen = true;
                            menu.Focus();
                        }
                        break;
                }
            }
        }

        /// <summary>
        /// Performs operations when user clicked the balloon notification in the systray.
        /// </summary>
        /// <param name="sender">Event sender</param>
        /// <param name="e">Event arguments</param>
        private void NotifyIcon_BalloonTipClicked(object sender, EventArgs e)
        {
            Open_Click(this, new RoutedEventArgs());
        }

        /// <summary>
        /// Restores the connecting wizard window from the system tray.
        /// </summary>
        /// <param name="sender">Event sender (ignored)</param>
        /// <param name="e">Event arguments (ignored)</param>
        /// <remarks>Occurs when a <see cref="MenuItem"/> is clicked.</remarks>
        private void Open_Click(object sender, RoutedEventArgs e)
        {
            // (Re)activate window.
            if (WindowState == WindowState.Minimized)
                WindowState = WindowState.Normal;
            Topmost = true;
            try
            {
                if (!IsActive)
                    Show();
                Activate();
                Focus();
            }
            catch (ArgumentException ex) when ((uint)ex.HResult is 0x80070057)
            {
                // Window.Show method can cause ArgumentException when internally calling
                // CompositionTarget.SetRootVisual method.
                // Reference: https://github.com/emoacht/Monitorian/commit/861d43dc34ad47f1243f3770086d857fe83aeeec
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
        public void Exit_Click(object sender, EventArgs e)
        {
            DoClose = true;
            Close();
        }

        /// <summary>
        /// Saves window position.
        /// </summary>
        /// <param name="sender">Event sender (ignored)</param>
        /// <param name="e">Event arguments (ignored)</param>
        private void Window_Save(object sender, EventArgs e)
        {
            Properties.Settings.Default.WindowState = (int)WindowState;
            if (WindowState == WindowState.Normal)
            {
                Properties.Settings.Default.WindowTop = Top;
                Properties.Settings.Default.WindowLeft = Left;
                Properties.Settings.Default.WindowHeight = Height;
                Properties.Settings.Default.WindowWidth = Width;
            }
        }

        /// <summary>
        /// Updates Institute Access servers in the system-tray menu
        /// </summary>
        /// <param name="sender">Event sender (ignored)</param>
        /// <param name="e">Event arguments (ignored)</param>
        private void InstituteAccessServers_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            var viewModel = DataContext as ViewModels.Windows.ConnectWizard;
            Servers_CollectionChanged("InstituteAccess", viewModel.HomePage.InstituteAccessServers);
        }

        /// <summary>
        /// Updates Secure Internet server in the system-tray menu
        /// </summary>
        /// <param name="sender">Event sender (ignored)</param>
        /// <param name="e">Event arguments (ignored)</param>
        private void SecureInternetServers_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            var viewModel = DataContext as ViewModels.Windows.ConnectWizard;
            Servers_CollectionChanged("SecureInternet", viewModel.HomePage.SecureInternetServers);
        }

        /// <summary>
        /// Updates own servers in the system-tray menu
        /// </summary>
        /// <param name="sender">Event sender (ignored)</param>
        /// <param name="e">Event arguments (ignored)</param>
        private void OwnServers_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            var viewModel = DataContext as ViewModels.Windows.ConnectWizard;
            Servers_CollectionChanged("OwnServers", viewModel.HomePage.OwnServers);
        }

        /// <summary>
        /// Updates system-tray menu available server list
        /// </summary>
        /// <param name="sectionName">x:Name of the MenuItem marking start of the server section</param>
        /// <param name="servers">Collection of available servers</param>
        private void Servers_CollectionChanged(string sectionName, IEnumerable<Server> servers)
        {
            var viewModel = DataContext as ViewModels.Windows.ConnectWizard;
            var menu = Resources["SystemTrayMenu"] as ContextMenu;
            var inSection = false;
            for (int i = 0; i < menu.Items.Count; ++i)
            {
                if (inSection)
                {
                    while (!(menu.Items[i] is Separator))
                        menu.Items.RemoveAt(i);
                    foreach (var menuItem in servers
                        .OrderBy(server => server.ToString())
                        .Select(server =>
                        {
                            var item = new MenuItem();
                            item.DataContext = server;
                            item.Header = server.ToString();
                            item.IsChecked = viewModel.ConnectionPage.ActiveSession != null && viewModel.ConnectionPage.ActiveSession.Server.Equals(server);
                            item.Click += (object sender, RoutedEventArgs e) =>
                            {
                                var activeSession = viewModel.ConnectionPage.ActiveSession;
                                if (activeSession != null)
                                {
                                    var connect = !activeSession.Server.Equals(server);
                                    if (activeSession.Disconnect.CanExecute(true))
                                        activeSession.Disconnect.Execute(true);

                                    // Defer connecting as we need to disconnect gracefully first.
                                    if (connect)
                                        PendingServerConnect = server;
                                }
                                else
                                    Servers_Connect(server);
                            };
                            return item;
                        }))
                        menu.Items.Insert(i++, menuItem);
                    break;
                }
                else if (menu.Items[i] is MenuItem menuItem && menuItem.Name == sectionName)
                    inSection = true;
            }
        }

        /// <summary>
        /// Connects to given server
        /// </summary>
        /// <param name="server">Server to connect to</param>
        private void Servers_Connect(Server server)
        {
            var viewModel = DataContext as ViewModels.Windows.ConnectWizard;
            if (server is InstituteAccessServer iaServer)
            {
                viewModel.HomePage.SelectedInstituteAccessServer = iaServer;
                if (viewModel.HomePage.ConfirmInstituteAccessServerSelection.CanExecute())
                    viewModel.HomePage.ConfirmInstituteAccessServerSelection.Execute();
            }
            else if (server is SecureInternetServer siServer)
            {
                viewModel.HomePage.SelectedSecureInternetServer = siServer;
                if (viewModel.HomePage.ConfirmSecureInternetServerSelection.CanExecute())
                    viewModel.HomePage.ConfirmSecureInternetServerSelection.Execute();
            }
            else
            {
                viewModel.HomePage.SelectedOwnServer = server;
                if (viewModel.HomePage.ConfirmOwnServerSelection.CanExecute())
                    viewModel.HomePage.ConfirmOwnServerSelection.Execute();
            }
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
                    if (NotifyIcon != null)
                        NotifyIcon.Dispose();

                    if (Icons != null)
                        foreach (var i in Icons)
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
