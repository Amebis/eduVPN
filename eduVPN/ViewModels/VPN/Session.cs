﻿/*
    eduVPN - VPN for education and research

    Copyright: 2017-2024 The Commons Conservancy
    SPDX-License-Identifier: GPL-3.0+
*/

using eduEx;
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
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Runtime.InteropServices;
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

        #region PInvoke

        [DllImport("kernel32.dll")]
        protected static extern bool QueryPerformanceCounter(out long value);

        [DllImport("kernel32.dll")]
        protected static extern bool QueryPerformanceFrequency(out long value);

        #endregion

        #region Fields

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
        protected Thread TunnelFailoverTest;

        /// <summary>
        /// Time ticks in QueryPerformanceCounter when connected state recorded
        /// </summary>
        /// <remarks><c>null</c> when not connected</remarks>
        protected long? ConnectedAt;

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
        protected Configuration Config { get; }

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
            protected set
            {
                if (SetProperty(ref _State, value))
                {
                    switch (value)
                    {
                        case SessionStatusType.Testing:
                            TunnelFailoverTest.Start();
                            break;

                        case SessionStatusType.Connected:
                            QueryPerformanceCounter(out var now);
                            ConnectedAt = now;
                            RaisePropertyChanged(nameof(ConnectedTime));
                            break;

                        default:
                            ConnectedAt = null;
                            RaisePropertyChanged(nameof(ConnectedTime));
                            break;
                    }
                    RaisePropertyChanged(nameof(OfferFailover));
                    _Failover?.RaiseCanExecuteChanged();
                    _Renew?.RaiseCanExecuteChanged();
                }

            }
        }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private SessionStatusType _State;

        /// <summary>
        /// The reason session was terminated
        /// </summary>
        public TerminationReason TerminationReason
        {
            get => _TerminationReason;
            protected set => SetProperty(ref _TerminationReason, value);
        }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private TerminationReason _TerminationReason;

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
        public IPPrefix TunnelAddress
        {
            get => _TunnelAddress;
            protected set => SetProperty(ref _TunnelAddress, value);
        }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private IPPrefix _TunnelAddress;

        /// <summary>
        /// TUN/TAP local IPv6 address
        /// </summary>
        /// <remarks><c>null</c> when not connected</remarks>
        public IPPrefix IPv6TunnelAddress
        {
            get => _IPv6TunnelAddress;
            protected set => SetProperty(ref _IPv6TunnelAddress, value);
        }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private IPPrefix _IPv6TunnelAddress;

        /// <summary>
        /// Running time connected
        /// </summary>
        /// <remarks><c>null</c> when not connected</remarks>
        public TimeSpan? ConnectedTime
        {
            get
            {
                if (ConnectedAt == null)
                    return null;
                QueryPerformanceCounter(out var now);
                QueryPerformanceFrequency(out var frequency);
                return new TimeSpan((now - ConnectedAt.Value) * 10000000 / frequency);
            }
        }

        /// <summary>
        /// Number of bytes that have been received from the server
        /// </summary>
        /// <remarks>0 when not connected</remarks>
        public ulong RxBytes
        {
            get => _RxBytes;
            protected set => SetProperty(ref _RxBytes, value);
        }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private ulong _RxBytes;

        /// <summary>
        /// Number of bytes that have been sent to the server
        /// </summary>
        /// <remarks>0 when not connected</remarks>
        public ulong TxBytes
        {
            get => _TxBytes;
            protected set => SetProperty(ref _TxBytes, value);
        }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private ulong _TxBytes;

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
                    RaisePropertyChanged(nameof(ShowExpirationTime));
                    RaisePropertyChanged(nameof(ExpirationTime));
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
        /// Should expiration time be shown?
        /// </summary>
        public bool ShowExpirationTime
        {
            get
            {
                var now = DateTimeOffset.UtcNow;
                return
                    Expiration.CountdownAt <= now &&
                    now <= ValidTo;
            }
        }

        /// <summary>
        /// Remaining time before the session expires; or <see cref="TimeSpan.MaxValue"/> when session does not expire
        /// </summary>
        public TimeSpan ExpirationTime
        {
            get
            {
                var v = ValidTo;
                return v != DateTimeOffset.MaxValue ?
                    v - DateTimeOffset.UtcNow :
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
                var now = DateTimeOffset.UtcNow;
#if DEBUG
                DateTimeOffset from = ValidFrom, to = ValidTo;
                return
                    from != DateTimeOffset.MinValue && to != DateTimeOffset.MaxValue &&
                    (now - from).TotalMinutes > 1;
#else
                return
                    Expiration != null &&
                    Expiration.ButtonAt <= now &&
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
                            TerminationReason = TerminationReason.Renew;
                            State = SessionStatusType.Disconnecting;
                            SessionInProgress.Cancel();
                            _Disconnect?.RaiseCanExecuteChanged();
                        },
                        () => TerminationReason != TerminationReason.Renew && (State == SessionStatusType.Testing || State == SessionStatusType.Connected));
                }
                return _Renew;
            }
        }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private DelegateCommand _Renew;

        /// <summary>
        /// Should UI offer TCP failover button?
        /// </summary>
        public bool OfferFailover
        {
            get => Config.ShouldFailover && State == SessionStatusType.Testing && _OfferFailover;
        }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private bool _OfferFailover = false;

        /// <summary>
        /// Renews and restarts the session
        /// </summary>
        public DelegateCommand Failover
        {
            get
            {
                if (_Failover == null)
                {
                    _Failover = new DelegateCommand(
                        () =>
                        {
                            TerminationReason = TerminationReason.TunnelFailover;
                            State = SessionStatusType.Disconnecting;
                            SessionInProgress.Cancel();
                            _Disconnect?.RaiseCanExecuteChanged();
                        },
                        () => TerminationReason != TerminationReason.TunnelFailover && Config.ShouldFailover && State == SessionStatusType.Testing);
                }
                return _Failover;
            }
        }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private DelegateCommand _Failover;

        /// <summary>
        /// Disconnect command
        /// </summary>
        public DelegateCommand<bool?> Disconnect
        {
            get
            {
                if (_Disconnect == null)
                    _Disconnect = new DelegateCommand<bool?>(
                        isInteractive =>
                        {
                            // Clear server/profile to auto-start on next launch.
                            if (isInteractive != null && isInteractive.Value)
                                Properties.Settings.Default.LastSelectedServer = null;

                            State = SessionStatusType.Disconnecting;
                            SessionInProgress.Cancel();
                            _Disconnect.RaiseCanExecuteChanged();
                        },
                        isInteractive => !SessionInProgress.IsCancellationRequested);
                return _Disconnect;
            }
        }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private DelegateCommand<bool?> _Disconnect;

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
        /// <param name="config">Initial profile configuration</param>
        /// <param name="expiration">VPN expiry times</param>
        public Session(ConnectWizard wizard, Server server, Configuration config, Expiration expiration)
        {
            Wizard = wizard;
            Server = server;
            Config = config;
            Expiration = expiration;
            Thread = Thread.CurrentThread;

            SessionInProgress = new CancellationTokenSource();
            SessionAndWindowInProgress = CancellationTokenSource.CreateLinkedTokenSource(SessionInProgress.Token, Window.Abort.Token);

            PropertyChanged += (object sender, PropertyChangedEventArgs e) =>
            {
                // Disconnect session when expired.
                if ((e.PropertyName == nameof(State) || e.PropertyName == nameof(ExpirationTime)) &&
                    (State == SessionStatusType.Waiting || State == SessionStatusType.Initializing || State == SessionStatusType.Connecting || State == SessionStatusType.Testing || State == SessionStatusType.Connected) &&
                    ValidTo <= DateTimeOffset.UtcNow)
                {
                    TerminationReason = TerminationReason.Expired;
                    if (Disconnect.CanExecute(false))
                        Disconnect.Execute(false);
                }
            };

            // Tunnel failover test won't work for OpenVPN if spawned immediately after OpenVPN reports connection:
            // First, OpenVPN and WireGuard sessions report the traffic once every 5 seconds.
            // Second, OpenVPN takes some more time to configure networking and may report failed tunnel if
            // we probe too early in the connection process.
            // Third, upon connect, OpenVPN session sets TunnelAddress once. Than resets it. Then sets it again.
            // We shouldn't react to every TunnelAddress setting.
            TunnelFailoverTest = new Thread(new ThreadStart(() =>
            {
                Wizard.TryInvoke((Action)(() => Wizard.TaskCount++));
                try
                {
                    var timer = new System.Timers.Timer(2 * 1000) { AutoReset = false };
                    timer.Elapsed += (object sender, ElapsedEventArgs e2) =>
                    {
                        _OfferFailover = true;
                        Wizard.TryInvoke((Action)(() => RaisePropertyChanged(nameof(OfferFailover))));
                    };
                    timer.Start();
                    do
                    {
                        IPPrefix tunnelAddress = null;
                        Wizard.TryInvoke((Action)(() =>
                        {
                            if (TunnelAddress?.Address != null && TunnelAddress.Address.AddressFamily == AddressFamily.InterNetwork)
                                tunnelAddress = TunnelAddress;
                        }));
                        if (tunnelAddress == null)
                            continue;
                        var gateway = Engine.CalculateGateway(tunnelAddress);
                        foreach (var iface in NetworkInterface.GetAllNetworkInterfaces()
                            .Where(n =>
                                n.OperationalStatus == OperationalStatus.Up &&
                                n.NetworkInterfaceType != NetworkInterfaceType.Loopback))
                        {
                            if (SessionAndWindowInProgress.IsCancellationRequested)
                                return;
                            var props = iface.GetIPProperties();
                            var unicast = props.UnicastAddresses.FirstOrDefault(ip => ip.Address.Equals(tunnelAddress.Address));
                            if (unicast == null)
                                continue;
                            using (var operationInProgress = new Engine.CancellationTokenCookie(SessionAndWindowInProgress.Token))
                                try
                                {
                                    //SessionAndWindowInProgress.Token.WaitHandle.WaitOne(30000); // Mock slow online detection test.
                                    //throw new Exception("test"); // Mock online detection test failure.
                                    bool online = !Engine.StartFailover(operationInProgress, gateway, props.GetIPv4Properties().Mtu);
                                    Wizard.TryInvoke((Action)(() =>
                                    {
                                        if (online)
                                        {
                                            // The online detection test succeeded.
                                            Wizard.Error = null;
                                            State = SessionStatusType.Connected;
                                        }
                                        else
                                        {
                                            if (Config.ShouldFailover)
                                            {
                                                // Failover to TCP.
                                                Wizard.Error = null;
                                                TerminationReason = TerminationReason.TunnelFailover;
                                                if (Disconnect.CanExecute(false))
                                                    Disconnect.Execute(false);
                                            }
                                            else
                                            {
                                                // Report no traffic detected. Advisory-only. Nevertheless, show the green "Connected" status.
                                                Wizard.Error = new Exception(Resources.Strings.WarningNoTrafficDetected);
                                                State = SessionStatusType.Connected;
                                            }
                                        }
                                    }));
                                    return;
                                }
                                catch (OperationCanceledException) { return; }
                                catch (Exception ex) { Wizard.TryInvoke((Action)(() => Wizard.Error = ex)); }
                        }
                    } while (!SessionAndWindowInProgress.Token.WaitHandle.WaitOne(250));
                }
                catch (OperationCanceledException) { }
                catch (Exception ex) { Wizard.TryInvoke((Action)(() => Wizard.Error = ex)); }
                finally { Wizard.TryInvoke((Action)(() => Wizard.TaskCount--)); }
            }));

            Engine.ReportTraffic += Engine_ReportTraffic;
        }

        #endregion

        #region Methods

        private void Engine_ReportTraffic(object sender, Engine.ReportTrafficEventArgs e)
        {
            e.RxBytes += (long)RxBytes;
            e.TxBytes += (long)TxBytes;
        }

        /// <summary>
        /// Executes the session
        /// </summary>
        public void Execute()
        {
            // It is important to start updating the timers, to allow session self-disconnection on expiration
            // even if the session is waiting for other users to sign out.
            var sessionExpirationWarning = DateTimeOffset.UtcNow;
            var propertyUpdater = new DispatcherTimer(
                new TimeSpan(0, 0, 0, 1),
                DispatcherPriority.Normal,
                (object senderTimer, EventArgs eTimer) =>
                {
                    RaisePropertyChanged(nameof(ConnectedTime));
                    RaisePropertyChanged(nameof(ShowExpirationTime));
                    RaisePropertyChanged(nameof(ExpirationTime));
                    RaisePropertyChanged(nameof(OfferRenewal));
                    RaisePropertyChanged(nameof(OfferFailover));
                    var now = DateTimeOffset.UtcNow;
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
                void onWTSChange(CGo.SessionMonitor.Event e, uint sessionId)
                {
                    switch (e)
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
                                Wizard.TryInvoke((Action)(() =>
                                {
                                    TerminationReason = TerminationReason.AnotherUser;
                                    SessionInProgress.Cancel();
                                    _Disconnect?.RaiseCanExecuteChanged();
                                }));
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
                        Engine.SetState(Engine.State.Connecting);
                        try
                        {
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

                            if (Config == null)
                                throw new Exception("Configuration is not available");
                            if (Config.VPNConfig == null)
                                throw new Exception("OpenVPN configuration is not available");
                            if (Server == null)
                                throw new Exception("Server is not available");
                            if (Server.Id == null)
                                throw new Exception("Server ID is not available");
                            if (Profile == null)
                                throw new Exception("Profile is not available");
                            if (Profile.Id == null)
                                throw new Exception("Profile ID is not available");

                            // Finally!
                            Run();
                        }
                        finally
                        {
                            Engine.SetState(Engine.State.Disconnecting);
                        }
                    }
                    finally
                    {
                        Engine.SetState(Engine.State.Disconnected);

                        // Cleanup session in eduvpn-common to have the /disconnect call *before* attempting to reconnect.
                        Trace.TraceInformation("Tunnel cleanup");
                        using (var operationInProgress = new Engine.CancellationTokenCookie(Window.Abort.Token))
                            try { Engine.Cleanup(operationInProgress); } catch { }

                        Wizard.TryInvoke((Action)(() =>
                        {
                            // Cleanup status properties.
                            StateDescription = "";
                            State = SessionStatusType.Disconnected;

                            if (!Window.Abort.IsCancellationRequested)
                            {
                                // Client is not quitting.
                                switch (TerminationReason)
                                {
                                    case TerminationReason.Expired:
                                        break;
                                    case TerminationReason.Renew:
                                        Wizard.RenewAndConnect(Server);
                                        break;
                                    case TerminationReason.AnotherUser:
                                        // Have the reconnect handle waiting for no more another users signed in.
                                        Wizard.Connect(Server, Properties.Settings.Default.PreferTCP);
                                        break;
                                    case TerminationReason.TunnelFailover:
                                        Wizard.Connect(Server, true, true);
                                        break;
                                    default:
                                        // TODO: The code below immediately reconnected on any failures.
                                        // Should a persistent failure occur on VPN connection setup,
                                        // this leads to a crazy loop of reconnecting. Implement smarter!
                                        //if (!SessionInProgress.IsCancellationRequested)
                                        //    Wizard.Connect(Server, Properties.Settings.Default.PreferTCP);
                                        break;
                                }
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
