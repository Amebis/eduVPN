/*
    eduVPN - VPN for education and research

    Copyright: 2017-2022 The Commons Conservancy
    SPDX-License-Identifier: GPL-3.0+
*/

using eduVPN.Models;
using eduVPN.ViewModels.Windows;
using Microsoft.Win32;
using Prism.Commands;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Threading;

namespace eduVPN.ViewModels.VPN
{
    /// <summary>
    /// WireGuard session
    /// </summary>
    public class WireGuardSession : Session
    {
        #region PInvoke

        [DllImport("shell32.dll", SetLastError = true)]
        static extern IntPtr CommandLineToArgvW([MarshalAs(UnmanagedType.LPWStr)] string lpCmdLine, out int pNumArgs);

        private static string[] CommandLineToArgs(string commandLine)
        {
            var argv = CommandLineToArgvW(commandLine, out var argc);
            if (argv == IntPtr.Zero)
                throw new Win32Exception();
            try
            {
                var args = new string[argc];
                for (var i = 0; i < args.Length; i++)
                {
                    var p = Marshal.ReadIntPtr(argv, i * IntPtr.Size);
                    args[i] = Marshal.PtrToStringUni(p);
                }
                return args;
            }
            finally
            {
                Marshal.FreeHGlobal(argv);
            }
        }

        #endregion

        #region Fields

        /// <summary>
        /// WireGuard tunnel name
        /// </summary>
        /// <remarks>Tunnel name determines .conf.dpapi and .log filenames.</remarks>
        private readonly string TunnelName;

        /// <summary>
        /// WireGuard working folder
        /// </summary>
        public static string WorkingFolder
        {
            get
            {
                lock (WorkingFolderLock)
                {
                    if (_WorkingFolder == null)
                    {
                        using (var hklmKey = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry64))
                        {
                            using (var key = hklmKey.OpenSubKey("SYSTEM\\CurrentControlSet\\Services\\eduWGManager" + Properties.Settings.Default.WireGuardTunnelManagerServiceInstance, false))
                            {
                                var path = Path.Combine(
                                    Path.GetDirectoryName(CommandLineToArgs(key.GetValue("ImagePath").ToString())[0].TrimEnd()),
                                    "config");
                                if (!Directory.Exists(path))
                                    throw new FileNotFoundException();
                                _WorkingFolder = path;
                            }
                        }
                    }
                    return _WorkingFolder;
                }
            }
        }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private static string _WorkingFolder;

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private static readonly object WorkingFolderLock = new object();

        /// <summary>
        /// Tunnel deactivate token
        /// </summary>
        private CancellationTokenSource DeactivateInProgress;

        /// <summary>
        /// Tunnel renew token
        /// </summary>
        private CancellationTokenSource RenewInProgress;

        #endregion

        #region Properties

        /// <summary>
        /// WireGuard tunnel log
        /// </summary>
        private string LogPath => Path.Combine(WorkingFolder, TunnelName + ".txt");

        /// <inheritdoc/>
        public override DelegateCommand Renew
        {
            get
            {
                if (_Renew == null)
                {
                    _Renew = new DelegateCommand(
                        () =>
                        {
                            RenewInProgress.Cancel();
                            _Renew.RaiseCanExecuteChanged();
                        },
                        () => RenewInProgress != null && State == SessionStatusType.Connected && !RenewInProgress.IsCancellationRequested);
                    PropertyChanged += (object sender, PropertyChangedEventArgs e) => { if (e.PropertyName == nameof(State)) _Renew.RaiseCanExecuteChanged(); };
                }
                return _Renew;
            }
        }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private DelegateCommand _Renew;

        /// <inheritdoc/>
        public override DelegateCommand Disconnect
        {
            get
            {
                if (_Disconnect == null)
                    _Disconnect = new DelegateCommand(
                        () =>
                        {
                            // Deactivate tunnel.
                            State = SessionStatusType.Disconnecting;
                            DeactivateInProgress.Cancel();
                        },
                        () => DeactivateInProgress != null);
                return _Disconnect;
            }
        }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private DelegateCommand _Disconnect;

        /// <inheritdoc/>
        public override DelegateCommand ShowLog
        {
            get
            {
                if (_ShowLog == null)
                    _ShowLog = new DelegateCommand(
                        () => Process.Start(LogPath),
                        () => File.Exists(LogPath));
                return _ShowLog;
            }
        }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private DelegateCommand _ShowLog;

        #endregion

        #region Constructors

        /// <summary>
        /// Creates an WireGuard session
        /// </summary>
        /// <param name="wizard">The connecting wizard</param>
        /// <param name="connectingProfile">Connecting eduVPN profile</param>
        /// <param name="profileConfig">Initial profile configuration</param>
        public WireGuardSession(ConnectWizard wizard, Profile connectingProfile, Xml.Response profileConfig) :
            base(wizard, connectingProfile, profileConfig)
        {
            TunnelName = connectingProfile.Server.Base.Host;
            if (TunnelName.Length > 32)
                TunnelName = TunnelName.Substring(0, 32);
            DeactivateInProgress = new CancellationTokenSource();
        }

        #endregion

        #region Methods

        /// <inheritdoc/>
        protected override void Run()
        {
            Wizard.TryInvoke((Action)(() => Wizard.TaskCount++));
            try
            {
                var propertyUpdater = new DispatcherTimer(
                    new TimeSpan(0, 0, 0, 1),
                    DispatcherPriority.Normal,
                    (object sender, EventArgs e) => _ShowLog?.RaiseCanExecuteChanged(),
                    Wizard.Dispatcher);
                propertyUpdater.Start();
                try
                {
                    retry:
                    // Connect to WireGuard Tunnel Manager Service to activate the tunnel.
                    using (var managerSession = new eduWireGuard.ManagerService.Session())
                    {
                        Wizard.TryInvoke((Action)(() => State = SessionStatusType.Connecting));
                        try
                        {
                            managerSession.Activate(
                                "eduWGManager" + Properties.Settings.Default.WireGuardTunnelManagerServiceInstance,
                                TunnelName,
                                ProfileConfig.Value,
                                3000,
                                Window.Abort.Token);
                        }
                        catch (OperationCanceledException) { throw; }
                        catch (Exception ex) { throw new AggregateException(Resources.Strings.ErrorWireGuardTunnelManagerService, ex); }

                        IPAddress tunnelAddress = null, ipv6TunnelAddress = null;
                        using (var reader = new StringReader(ProfileConfig.Value))
                        {
                            var iface = new eduWireGuard.Interface(reader);
                            foreach (var a in iface.Addresses)
                                switch (a.Address.AddressFamily)
                                {
                                    case AddressFamily.InterNetwork:
                                        tunnelAddress = a.Address;
                                        break;
                                    case AddressFamily.InterNetworkV6:
                                        ipv6TunnelAddress = a.Address;
                                        break;
                                }
                        }

                        RenewInProgress = new CancellationTokenSource();
                        Wizard.TryInvoke((Action)(() =>
                        {
                            _Renew?.RaiseCanExecuteChanged();
                            _Disconnect?.RaiseCanExecuteChanged();
                            Wizard.TaskCount--;
                            TunnelAddress = tunnelAddress;
                            IPv6TunnelAddress = ipv6TunnelAddress;
                            State = SessionStatusType.Connected;
                        }));

                        // Wait for a change and update stats.
                        var ct = CancellationTokenSource.CreateLinkedTokenSource(new CancellationToken[]{
                            DeactivateInProgress.Token,
                            RenewInProgress.Token,
                            Window.Abort.Token
                        });
                        do
                        {
                            try
                            {
                                var cfg = managerSession.GetTunnelConfig(ct.Token);
                                ulong rxBytes = 0, txBytes = 0;
                                DateTimeOffset lastHandshake = DateTimeOffset.MinValue;
                                foreach (var peer in cfg.Peers)
                                {
                                    rxBytes += peer.RxBytes;
                                    txBytes += peer.TxBytes;
                                    if (lastHandshake < peer.LastHandshake)
                                        lastHandshake = peer.LastHandshake;
                                }
                                Wizard.TryInvoke((Action)(() =>
                                {
                                    if (ConnectedAt == null && lastHandshake != DateTimeOffset.MinValue)
                                        ConnectedAt = lastHandshake;
                                    BytesIn = rxBytes;
                                    BytesOut = txBytes;
                                }));
                            }
                            catch
                            {
                                Wizard.TryInvoke((Action)(() => ConnectedAt = null));
                            }
                        } while (!ct.Token.WaitHandle.WaitOne(5 * 1000));
                        Wizard.TryInvoke((Action)(() =>
                        {
                            Wizard.TaskCount++;
                            State = SessionStatusType.Disconnecting;
                        }));
                        managerSession.Deactivate();
                        if (DeactivateInProgress.IsCancellationRequested || Window.Abort.IsCancellationRequested)
                            return;
                    }

                    // Reapply for a profile config.
                    ConnectingProfile.Server.ResetKeypair();
                    var config = ConnectingProfile.Connect(
                        Wizard.GetAuthenticatingServer(ConnectingProfile.Server),
                        true,
                        ProfileConfig.ContentType,
                        Window.Abort.Token);
                    Wizard.TryInvoke((Action)(() => ProfileConfig = config));
                    goto retry;
                }
                finally
                {
                    Wizard.TryInvoke((Action)(() =>
                    {
                        // Cleanup status properties.
                        TunnelAddress = null;
                        IPv6TunnelAddress = null;
                        ConnectedAt = null;
                        BytesIn = null;
                        BytesOut = null;
                    }));

                    propertyUpdater.Stop();
                }
            }
            finally { Wizard.TryInvoke((Action)(() => Wizard.TaskCount--)); }
        }

        /// <summary>
        /// Purges stale log files
        /// </summary>
        public static void PurgeOldLogs()
        {
            var timestamp = DateTime.UtcNow.Subtract(new TimeSpan(7, 0, 0, 0));
            foreach (var f in Directory.EnumerateFiles(WorkingFolder, "*.txt", SearchOption.TopDirectoryOnly))
            {
                Window.Abort.Token.ThrowIfCancellationRequested();
                if (File.GetLastWriteTimeUtc(f) <= timestamp)
                {
                    Trace.TraceInformation("Purging {0}", f);
                    try { File.Delete(f); }
                    catch { }
                }
            }
        }

        #endregion
    }
}
