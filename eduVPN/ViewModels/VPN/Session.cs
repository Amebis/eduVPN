/*
    eduVPN - VPN for education and research

    Copyright: 2017-2023 The Commons Conservancy
    SPDX-License-Identifier: GPL-3.0+
*/

using eduVPN.Models;
using eduVPN.ViewModels.Windows;
using Microsoft.Win32;
using Prism.Commands;
using Prism.Mvvm;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Threading;
using System.Timers;
using System.Windows.Threading;

namespace eduVPN.ViewModels.VPN
{
    /// <summary>
    /// VPN session base class
    /// </summary>
    public class Session : BindableBase, IDisposable
    {
        #region Constants

        /// <summary>
        /// List of hardware IDs specific to network adapters used for VPN and tunneling
        /// </summary>
        private static readonly string[] VPNHardwareIds = new string[] { "WireGuard", "Wintun", "root\\tap0901", "tap0901", "ovpn-dco" };

        #endregion

        #region Fields

        /// <summary>
        /// Session renewal in progress
        /// </summary>
        protected volatile bool RenewInProgress;

        /// <summary>
        /// Session termination token
        /// </summary>
        protected readonly CancellationTokenSource SessionInProgress;

        /// <summary>
        /// Quit token
        /// </summary>
        protected readonly CancellationTokenSource SessionAndWindowInProgress;

        /// <summary>
        /// Timer to defer failover test
        /// </summary>
        protected System.Timers.Timer TunnelFailoverTest;

        #endregion

        #region Properties

        /// <summary>
        /// The connecting wizard
        /// </summary>
        public ConnectWizard Wizard { get; }

        /// <summary>
        /// Connecting eduVPN server
        /// </summary>
        public Server Server { get; }

        /// <summary>
        /// Connecting eduVPN server profile
        /// </summary>
        public Profile Profile { get => Server.Profiles.Current; }

        /// <summary>
        /// Profile configuration
        /// </summary>
        protected string ProfileConfig { get; }

        /// <summary>
        /// VPN session worker
        /// </summary>
        public Thread Thread { get; }

        /// <summary>
        /// Client connection state
        /// </summary>
        public SessionStatusType State
        {
            get => _State;
            protected set => SetProperty(ref _State, value);
        }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private SessionStatusType _State;

        /// <summary>
        /// Descriptive string (used mostly on <see cref="eduOpenVPN.OpenVPNStateType.Reconnecting"/> and <see cref="eduOpenVPN.OpenVPNStateType.Exiting"/> to show the reason for the disconnect)
        /// </summary>
        public string StateDescription
        {
            get => _StateDescription;
            protected set => SetProperty(ref _StateDescription, value);
        }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private string _StateDescription = "";

        /// <summary>
        /// TUN/TAP local IPv4 address
        /// </summary>
        /// <remarks><c>null</c> when not connected</remarks>
        public IPAddress TunnelAddress
        {
            get => _TunnelAddress;
            protected set
            {
                if (SetProperty(ref _TunnelAddress, value) && value != null)
                {
                    TunnelFailoverTest.Stop();
                    TunnelFailoverTest.Start();
                }
            }
        }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private IPAddress _TunnelAddress;

        /// <summary>
        /// TUN/TAP local IPv6 address
        /// </summary>
        /// <remarks><c>null</c> when not connected</remarks>
        public IPAddress IPv6TunnelAddress
        {
            get => _IPv6TunnelAddress;
            protected set => SetProperty(ref _IPv6TunnelAddress, value);
        }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private IPAddress _IPv6TunnelAddress;

        /// <summary>
        /// Time when connected state recorded
        /// </summary>
        /// <remarks><c>null</c> when not connected</remarks>
        public DateTimeOffset? ConnectedAt
        {
            get => _ConnectedAt;
            protected set
            {
                if (SetProperty(ref _ConnectedAt, value))
                    RaisePropertyChanged(nameof(ConnectedTime));
            }
        }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private DateTimeOffset? _ConnectedAt;

        /// <summary>
        /// Running time connected
        /// </summary>
        /// <remarks><c>null</c> when not connected</remarks>
        public TimeSpan? ConnectedTime => ConnectedAt != null ? DateTimeOffset.UtcNow - ConnectedAt : null;

        /// <summary>
        /// Number of bytes that have been received from the server
        /// </summary>
        /// <remarks><c>null</c> when not connected</remarks>
        public ulong? RxBytes
        {
            get => _BytesIn;
            protected set => SetProperty(ref _BytesIn, value);
        }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private ulong? _BytesIn;

        /// <summary>
        /// Number of bytes that have been sent to the server
        /// </summary>
        /// <remarks><c>null</c> when not connected</remarks>
        public ulong? TxBytes
        {
            get => _BytesOut;
            protected set => SetProperty(ref _BytesOut, value);
        }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private ulong? _BytesOut;

        /// <summary>
        /// Session expiry times
        /// </summary>
        protected Expiration Expiration
        {
            get => _Expiration;
            set
            {
                if (SetProperty(ref _Expiration, value))
                {
                    RaisePropertyChanged(nameof(ValidFrom));
                    RaisePropertyChanged(nameof(ValidTo));
                    RaisePropertyChanged(nameof(Expired));
                    RaisePropertyChanged(nameof(ShowExpiredTime));
                    RaisePropertyChanged(nameof(ExpiresTime));
                    RaisePropertyChanged(nameof(OfferRenewal));
                }
            }
        }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private Expiration _Expiration;

        /// <summary>
        /// Session valid from date
        /// </summary>
        /// <remarks><c>DateTimeOffset.MinValue</c> when unknown or not available</remarks>
        public DateTimeOffset ValidFrom => Expiration.StartedAt;

        /// <summary>
        /// Session expiration date
        /// </summary>
        /// <remarks><c>DateTimeOffset.MaxValue</c> when unknown or not available</remarks>
        public DateTimeOffset ValidTo => Expiration.EndAt;

        /// <summary>
        /// Is the session expired?
        /// </summary>
        public bool Expired => ValidTo <= DateTimeOffset.Now;

        /// <summary>
        /// Should expiration time be shown?
        /// </summary>
        public bool ShowExpiredTime
        {
            get
            {
                var now = DateTimeOffset.Now;
                return
                    Expiration != null &&
                    Expiration.CountdownAt <= now &&
                    now <= ValidTo;
            }
        }

        /// <summary>
        /// Remaining time before the session expires; or <see cref="TimeSpan.MaxValue"/> when session does not expire
        /// </summary>
        public TimeSpan ExpiresTime
        {
            get
            {
                var v = ValidTo;
                return v != DateTimeOffset.MaxValue ?
                    v - DateTimeOffset.Now :
                    TimeSpan.MaxValue;
            }
        }

        /// <summary>
        /// Occurs when GUI should warn user about session expiration.
        /// </summary>
        public event EventHandler WarnExpiration;

        /// <summary>
        /// Should UI offer session renewal?
        /// </summary>
        public bool OfferRenewal
        {
            get
            {
                var now = DateTimeOffset.Now;
#if DEBUG
                DateTimeOffset from = ValidFrom, to = ValidTo;
                return
                    from != DateTimeOffset.MinValue && to != DateTimeOffset.MaxValue &&
                    (now - from).TotalMinutes > 1;
#else
                return
                    Expiration != null &&
                    Expiration.ButtonAt <= DateTimeOffset.Now &&
                    now <= ValidTo;
#endif
            }
        }

        /// <summary>
        /// Renews and restarts the session
        /// </summary>
        public DelegateCommand Renew
        {
            get
            {
                if (_Renew == null)
                {
                    _Renew = new DelegateCommand(
                        () =>
                        {
                            RenewInProgress = true;
                            State = SessionStatusType.Disconnecting;
                            SessionInProgress.Cancel();
                            _Renew.RaiseCanExecuteChanged();
                            _Disconnect?.RaiseCanExecuteChanged();
                        },
                        () => !RenewInProgress && State == SessionStatusType.Connected);
                    PropertyChanged += (object sender, PropertyChangedEventArgs e) => { if (e.PropertyName == nameof(State)) _Renew.RaiseCanExecuteChanged(); };
                }
                return _Renew;
            }
        }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private DelegateCommand _Renew;

        /// <summary>
        /// Disconnect command
        /// </summary>
        public DelegateCommand Disconnect
        {
            get
            {
                if (_Disconnect == null)
                    _Disconnect = new DelegateCommand(
                        () =>
                        {
                            State = SessionStatusType.Disconnecting;
                            SessionInProgress.Cancel();
                            _Disconnect.RaiseCanExecuteChanged();
                        },
                        () => !SessionInProgress.IsCancellationRequested);
                return _Disconnect;
            }
        }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private DelegateCommand _Disconnect;

        /// <summary>
        /// Show log command
        /// </summary>
        public virtual DelegateCommand ShowLog { get; } = new DelegateCommand(() => { }, () => false);

        #endregion

        #region Constructors

        /// <summary>
        /// Creates a VPN session
        /// </summary>
        /// <param name="wizard">The connecting wizard</param>
        /// <param name="server">Connecting eduVPN server</param>
        /// <param name="profileConfig">Initial profile configuration</param>
        /// <param name="expiration">VPN expiry times</param>
        public Session(ConnectWizard wizard, Server server, string profileConfig, Expiration expiration)
        {
            Wizard = wizard;
            Server = server;
            ProfileConfig = profileConfig;
            Expiration = expiration;
            Thread = Thread.CurrentThread;

            SessionInProgress = new CancellationTokenSource();
            SessionAndWindowInProgress = CancellationTokenSource.CreateLinkedTokenSource(SessionInProgress.Token, Window.Abort.Token);

            PropertyChanged += (object sender, PropertyChangedEventArgs e) =>
            {
                // Disconnect session when expired.
                if ((e.PropertyName == nameof(State) || e.PropertyName == nameof(Expired)) &&
                    (State == SessionStatusType.Waiting || State == SessionStatusType.Initializing || State == SessionStatusType.Connecting || State == SessionStatusType.Connected) &&
                    Expired &&
                    Disconnect.CanExecute())
                    Disconnect.Execute();
            };

            // First, OpenVPN and WireGuard sessions report the traffic once every 5 seconds.
            // Second, OpenVPN takes some more time to configure networking and may report failed tunnel if
            // we probe too early in the connection process.
            // Third, upon connect, OpenVPN session sets TunnelAddress once. Than resets it. Then sets it again.
            // We shouldn't react to every TunnelAddress setting.
            // Therefore, the tunnel failover test must be delayable and cancelable.
            TunnelFailoverTest = new System.Timers.Timer(5 * 1000) { AutoReset = false };
            TunnelFailoverTest.Elapsed += (object sender, ElapsedEventArgs e2) =>
            {
                var tunnelAddress = TunnelAddress;
                if (tunnelAddress == null)
                    return;
                foreach (var iface in NetworkInterface.GetAllNetworkInterfaces()
                    .Where(n =>
                        n.OperationalStatus == OperationalStatus.Up &&
                        n.NetworkInterfaceType != NetworkInterfaceType.Loopback))
                {
                    var props = iface.GetIPProperties();
                    var unicast = props.UnicastAddresses.FirstOrDefault(ip => ip.Address.Equals(tunnelAddress));
                    if (unicast != null)
                    {
                        var gw = props.GatewayAddresses.FirstOrDefault(ip => ip.Address.AddressFamily == AddressFamily.InterNetwork);
                        string gateway;
                        if (gw != null && !gw.Address.Equals(IPAddress.Any))
                            gateway = gw.Address.ToString();
                        else
                        {
                            // The eduVPN server is always the first usable IP in the subnet by convention.
#pragma warning disable CS0618 // Type or member is obsolete
                            gateway = new IPAddress((int)tunnelAddress.Address & (int)unicast.IPv4Mask.Address | IPAddress.HostToNetworkOrder(1)).ToString();
#pragma warning restore CS0618
                        }
                        using (var operationInProgress = new Engine.CancellationTokenCookie(SessionAndWindowInProgress.Token))
                            try
                            {
                                //throw new Exception("test"); // Mock traffic failure for testing.
                                Engine.StartFailover(operationInProgress, gateway, props.GetIPv4Properties().Mtu);
                            }
                            catch (OperationCanceledException) { }
                            catch (Exception ex) { Wizard.TryInvoke((Action)(() => Wizard.Error = new Exception(Resources.Strings.WarningNoTrafficDetected, ex))); }
                    }
                }
            };

            Engine.ReportTraffic += Engine_ReportTraffic;
        }

        #endregion

        #region Methods

        private void Engine_ReportTraffic(object sender, Engine.ReportTrafficEventArgs e)
        {
            if (RxBytes != null)
                e.RxBytes += (long)RxBytes;
            if (TxBytes != null)
                e.TxBytes += (long)TxBytes;
        }

        /// <summary>
        /// Executes the session
        /// </summary>
        public void Execute()
        {
            // It is important to start updating the timers, to allow session self-disconnection on expiration
            // even if the session is waiting for other users to sign out.
            var sessionExpirationWarning = DateTimeOffset.Now;
            var propertyUpdater = new DispatcherTimer(
                new TimeSpan(0, 0, 0, 1),
                DispatcherPriority.Normal,
                (object senderTimer, EventArgs eTimer) =>
                {
                    RaisePropertyChanged(nameof(ConnectedTime));
                    RaisePropertyChanged(nameof(Expired));
                    RaisePropertyChanged(nameof(ShowExpiredTime));
                    RaisePropertyChanged(nameof(ExpiresTime));
                    RaisePropertyChanged(nameof(OfferRenewal));
                    var now = DateTimeOffset.Now;
                    var x = Expiration.NotificationAt.FirstOrDefault(t => sessionExpirationWarning < t && t <= now);
                    if (x != default)
                    {
                        WarnExpiration?.Invoke(this, null);
                        sessionExpirationWarning = x;
                        Expiration.NotificationAt.Remove(x);
                    }
                },
                Wizard.Dispatcher);
            propertyUpdater.Start();
            try
            {
                CancellationTokenSource waitingForForeignSessionSignOutInProgress = null;
                var foreignSessions = new List<uint>();
                bool suspendToWait = false;
                void onWTSChange(CGo.SessionMonitor.Event evnt, uint sessionId)
                {
                    switch (evnt)
                    {
                        case CGo.SessionMonitor.Event.SessionReportLogon:
                            // Other users are already signed-in and we will not start a VPN connection at all.
                            Trace.TraceInformation("User session {0} is signed in", sessionId);
                            foreignSessions.Add(sessionId);
                            waitingForForeignSessionSignOutInProgress = new CancellationTokenSource();
                            break;

                        case CGo.SessionMonitor.Event.SessionLogon:
                            // Another user signed in.
                            Trace.TraceInformation("User session {0} is signing in", sessionId);
                            lock (foreignSessions)
                                foreignSessions.Add(sessionId);
                            if (waitingForForeignSessionSignOutInProgress == null)
                            {
                                // Our session is connected (not waiting). Disconnect and spawn a new session waiting.
                                suspendToWait = true;
                                SessionInProgress.Cancel();
                                Wizard.TryInvoke((Action)(() => _Disconnect?.RaiseCanExecuteChanged()));
                            }
                            break;

                        case CGo.SessionMonitor.Event.SessionLogoff:
                            // Other user signed out.
                            Trace.TraceInformation("User session {0} is signing out", sessionId);
                            lock (foreignSessions)
                            {
                                foreignSessions.Remove(sessionId);
                                if (foreignSessions.Count == 0)
                                    waitingForForeignSessionSignOutInProgress?.Cancel();
                            }
                            break;
                    }
                }
                using (var wtsMonitor = new CGo.SessionMonitor(onWTSChange))
                {
                    Wizard.TryInvoke((Action)(() => State = SessionStatusType.Waiting));
                    try
                    {
                        if (waitingForForeignSessionSignOutInProgress != null)
                        {
                            // Other users are signed in. Wait for all of them to sign out or our session is cancelled (by user, by expiration, by quitting the client).
                            var ex = new Exception(Resources.Strings.WarningAnotherUserSession);
                            Wizard.TryInvoke((Action)(() => Wizard.Error = ex));
                            var ct = CancellationTokenSource.CreateLinkedTokenSource(SessionAndWindowInProgress.Token, waitingForForeignSessionSignOutInProgress.Token);
                            ct.Token.WaitHandle.WaitOne();
                            waitingForForeignSessionSignOutInProgress = null;
                            Wizard.TryInvoke((Action)(() => { if (Wizard.Error == ex) Wizard.Error = null; }));
                            if (SessionAndWindowInProgress.Token.IsCancellationRequested)
                                throw new OperationCanceledException();
                        }
                        Wizard.TryInvoke((Action)(() => State = SessionStatusType.Initializing));

                        // Is the default gateway a VPN already?
                        using (var hklmKey = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry64))
                        {
                            using (var networkKey = hklmKey.OpenSubKey("SYSTEM\\CurrentControlSet\\Control\\Network\\{4D36E972-E325-11CE-BFC1-08002BE10318}", false))
                            {
                                foreach (var iface in NetworkInterface.GetAllNetworkInterfaces()
                                    .Where(n =>
                                        n.OperationalStatus == OperationalStatus.Up &&
                                        n.NetworkInterfaceType != NetworkInterfaceType.Loopback &&
                                        n.GetIPProperties()?.GatewayAddresses.Count > 0))
                                {
                                    try
                                    {
                                        using (var connectionKey = networkKey.OpenSubKey(iface.Id + "\\Connection", false))
                                        {
                                            var pnpInstanceId = connectionKey.GetValue("PnPInstanceId").ToString();
                                            using (var deviceKey = hklmKey.OpenSubKey("SYSTEM\\CurrentControlSet\\Enum\\" + pnpInstanceId, false))
                                            {
                                                var hardwareIds = deviceKey.GetValue("HardwareID") as string[];
                                                if (hardwareIds.FirstOrDefault(hwid => VPNHardwareIds.Contains(hwid)) != null)
                                                    Wizard.TryInvoke((Action)(() => Wizard.Error = new Exception(Resources.Strings.WarningDefaultGatewayIsVPN)));
                                            }
                                        }
                                    }
                                    catch (OperationCanceledException) { throw; }
                                    catch
                                    {
                                        // Detecting default gateway VPN is advisory-only. Not the end of the world if the test fails.
                                    }
                                }
                            }
                        }

                        // Finally!
                        Run();
                    }
                    finally
                    {
                        Wizard.TryInvoke((Action)(() =>
                        {
                            // Cleanup status properties.
                            State = SessionStatusType.Disconnected;
                            StateDescription = "";

                            if (!Window.Abort.IsCancellationRequested && !Expired)
                            {
                                // Client is not quitting and our session is not expired. Maybe we should (renew and) reconnect?
                                if (RenewInProgress)
                                    Wizard.RenewAndConnect(Server);
                                else if (suspendToWait)
                                    Wizard.Connect(Server);
                                else if (!SessionInProgress.IsCancellationRequested)
                                    Wizard.Connect(Server);
                            }
                        }));
                    }
                }
            }
            finally { propertyUpdater.Stop(); }
        }

        /// <summary>
        /// Run the session
        /// </summary>
        protected virtual void Run()
        {
            throw new NotImplementedException();
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
                    Engine.ReportTraffic -= Engine_ReportTraffic;

                    SessionInProgress.Cancel();
                    SessionInProgress.Dispose();

                    SessionAndWindowInProgress.Cancel();
                    SessionAndWindowInProgress.Dispose();

                    TunnelFailoverTest.Stop();
                    TunnelFailoverTest.Dispose();
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
