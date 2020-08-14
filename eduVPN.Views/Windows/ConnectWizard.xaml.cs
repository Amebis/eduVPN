/*
    eduVPN - VPN for education and research

    Copyright: 2017-2020 The Commons Conservancy eduVPN Programme
    SPDX-License-Identifier: GPL-3.0+
*/

using eduEx.System;
using eduVPN.ViewModels.VPN;
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
        private VPNSessionStatusType SessionState;

        /// <summary>
        /// Icon on the notification tray
        /// </summary>
        private System.Windows.Forms.NotifyIcon NotifyIcon;

        /// <summary>
        /// Cached icons to be used by <see cref="NotifyIcon"/>
        /// </summary>
        private Dictionary<VPNSessionStatusType, Icon> Icons;

        /// <summary>
        /// Flag to prevent/force closing
        /// </summary>
        private bool DoClose = false;

        #endregion

        #region Properties

        /// <summary>
        /// Tray icon tool-tip text
        /// </summary>
        private string TrayIconToolTipText
        {
            get
            {
                var viewModel = (ViewModels.Windows.ConnectWizard)DataContext;
                return
                    (viewModel != null && viewModel.ConnectionPage.ActiveSession.ConnectingProfile != null ?
                        string.Format("{0} - {1}\r\n{2}",
                            viewModel.ConnectionPage.ActiveSession.ConnectingProfile?.Server,
                            viewModel.ConnectionPage.ActiveSession.ConnectingProfile,
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
                var viewModel = (ViewModels.Windows.ConnectWizard)DataContext;
                return viewModel != null ? Icons[viewModel.ConnectionPage.ActiveSession.State] : null;
            }
        }

        /// <summary>
        /// Use dark theme
        /// </summary>
        public bool UseDarkTheme
        {
            get { return (bool)GetValue(UseDarkThemeProperty); }
            set { SetValue(UseDarkThemeProperty, value); }
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
                        var mergedDictionary = Application.Current.Resources.MergedDictionaries.FirstOrDefault(dict => dict.Contains("WindowColor"));
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
            if (!double.IsNaN(Properties.Settings.Default.WindowLeft))
                Left = Math.Min(Math.Max(Properties.Settings.Default.WindowLeft, SystemParameters.VirtualScreenLeft), SystemParameters.VirtualScreenLeft + SystemParameters.VirtualScreenWidth - Width);
            if (!double.IsNaN(Properties.Settings.Default.WindowTop))
                Top = Math.Min(Math.Max(Properties.Settings.Default.WindowTop, SystemParameters.VirtualScreenTop), SystemParameters.VirtualScreenTop + SystemParameters.VirtualScreenHeight - Height);

            Application.Current.SessionEnding += (object sender, SessionEndingCancelEventArgs e2) =>
            {
                // Save window position on logout.
                Properties.Settings.Default.WindowTop = Top;
                Properties.Settings.Default.WindowLeft = Left;
            };

            // Preload icons to be used on system tray.
            var iconSize = System.Windows.Forms.SystemInformation.SmallIconSize;
            Icons = new Dictionary<VPNSessionStatusType, Icon>();
            foreach (var statusType in Enum.GetValues(typeof(VPNSessionStatusType)).Cast<VPNSessionStatusType>())
            {
                var iconUri = new Uri(string.Format("pack://application:,,,/Resources/VPNSessionStatusTypeIcon{0}.ico", Enum.GetName(typeof(VPNSessionStatusType), statusType)));
                try { Icons.Add(statusType, new Icon(Application.GetResourceStream(iconUri).Stream, iconSize)); }
                catch { Icons.Add(statusType, new Icon(Application.GetResourceStream(new Uri("pack://application:,,,/Resources/VPNSessionStatusTypeIconInitializing.ico")).Stream, iconSize)); }
            }

            // Attach to view model events.
            var viewModel = (ViewModels.Windows.ConnectWizard)DataContext;
            viewModel.QuitApplication += ConnectWizard_QuitApplication;

            // Create notify icon, set default icon, and setup events.
            // We need to do this programatically, since System.Windows.Forms.NotifyIcon is not WPF, but borrowed from WinForms.
            NotifyIcon = new System.Windows.Forms.NotifyIcon()
            {
                Text = TrayIconToolTipText,
                Icon = TrayIcon
            };
            NotifyIcon.Click += NotifyIcon_Click;

            // Bind to "ConnectionPage.ActiveSession.StateDescription" and "ConnectionPage.ActiveSession.State" property to keep tray icon up-to-date.
            viewModel.ConnectionPage.PropertyChanged += (object sender, PropertyChangedEventArgs e2) =>
            {
                if (e2.PropertyName == nameof(viewModel.ConnectionPage.ActiveSession))
                {
                    // Active session changed: sync the tray icon.
                    NotifyIcon.Text = TrayIconToolTipText;
                    NotifyIcon.Icon = TrayIcon;

                    if (viewModel.ConnectionPage.ActiveSession != VPNSession.Blank)
                    {
                        // Initialize VPN session state.
                        SessionState = viewModel.ConnectionPage.ActiveSession.State;

                        // Bind to the session for property changes.
                        viewModel.ConnectionPage.ActiveSession.PropertyChanged += (object _, PropertyChangedEventArgs e3) =>
                        {
                            switch (e3.PropertyName)
                            {
                                case nameof(viewModel.ConnectionPage.ActiveSession.ConnectingProfile):
                                case nameof(viewModel.ConnectionPage.ActiveSession.StateDescription):
                                    NotifyIcon.Text = TrayIconToolTipText;
                                    break;

                                case nameof(viewModel.ConnectionPage.ActiveSession.State):
                                    {
                                        NotifyIcon.Icon = TrayIcon;

                                        if (!IsVisible)
                                        {
                                            // Client is minimized.
                                            switch (viewModel.ConnectionPage.ActiveSession.State)
                                            {
                                                case VPNSessionStatusType.Connected:
                                                    // Client connected. Popup the balloon message.
                                                    NotifyIcon.ShowBalloonTip(
                                                        5000,
                                                        string.Format(Views.Resources.Strings.SystemTrayBalloonConnectedTitle, viewModel.ConnectionPage.ActiveSession.ConnectingProfile),
                                                        string.Format(Views.Resources.Strings.SystemTrayBalloonConnectedMessage, viewModel.ConnectionPage.ActiveSession.TunnelAddress, viewModel.ConnectionPage.ActiveSession.IPv6TunnelAddress),
                                                        System.Windows.Forms.ToolTipIcon.Info);
                                                    break;

                                                default:
                                                    if (SessionState == VPNSessionStatusType.Connected)
                                                    {
                                                        // Client has been disconnected. Popup the balloon message.
                                                        NotifyIcon.ShowBalloonTip(
                                                            5000,
                                                            eduVPN.Properties.Settings.Default.ClientTitle,
                                                            Views.Resources.Strings.SystemTrayBalloonDisconnectedMessage,
                                                            System.Windows.Forms.ToolTipIcon.Info);
                                                    }
                                                    break;
                                            }
                                        }

                                        // Save VPN session state.
                                        SessionState = viewModel.ConnectionPage.ActiveSession.State;
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
        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);
            HwndSource source = PresentationSource.FromVisual(this) as HwndSource;
            source.AddHook(WndProc);
            RefreshUseDarkTheme();
        }

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
        /// Shows tray icon.
        /// </summary>
        /// <param name="sender">Event sender (ignored)</param>
        /// <param name="e">Event arguments (ignored)</param>
        /// <remarks>Occurs when the element is laid out, rendered, and ready for interaction.</remarks>
        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            // Show tray icon when Connect Wizard is loaded.
            NotifyIcon.Visible = true;
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

                // Dismiss all pop-ups.
                foreach (Window popup in OwnedWindows)
                    popup.Close();

                // Save window position on closing.
                Properties.Settings.Default.WindowTop = Top;
                Properties.Settings.Default.WindowLeft = Left;
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
                        10000,
                        eduVPN.Properties.Settings.Default.ClientTitle,
                        Views.Resources.Strings.SystemTrayBalloonHiddenMessage,
                        System.Windows.Forms.ToolTipIcon.Info);

                    Properties.Settings.Default.SystemTrayMinimizedWarned = true;
                }
            }
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
                            menu.IsOpen = true;
                        break;
                }
            }
        }

        /// <summary>
        /// Restores the connecting wizard window from the system tray.
        /// </summary>
        /// <param name="sender">Event sender (ignored)</param>
        /// <param name="e">Event arguments (ignored)</param>
        /// <remarks>Occurs when a <see cref="MenuItem"/> is clicked.</remarks>
        public void Open_Click(object sender, RoutedEventArgs e)
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
            DoClose = true;
            Close();
        }

        /// <summary>
        /// Closes connecting wizard window to quit the application.
        /// </summary>
        /// <param name="sender">Event sender (ignored)</param>
        /// <param name="e">Event arguments (ignored)</param>
        /// <remarks>Occurs when application should quit.</remarks>
        private void ConnectWizard_QuitApplication(object sender, EventArgs e)
        {
            DoClose = true;
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
