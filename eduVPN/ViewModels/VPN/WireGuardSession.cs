/*
    eduVPN - VPN for education and research

    Copyright: 2017-2024 The Commons Conservancy
    SPDX-License-Identifier: GPL-3.0+
*/

using eduVPN.Models;
using eduVPN.ViewModels.Windows;
using eduWireGuard;
using Microsoft.Win32;
using Prism.Commands;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
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

        #endregion

        #region Properties

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
        /// WireGuard tunnel log
        /// </summary>
        private string LogPath => Path.Combine(WorkingFolder, TunnelName + ".txt");

        /// <inheritdoc/>
        public override DelegateCommand ShowLog
        {
            get
            {
                if (_ShowLog == null)
                    _ShowLog = new DelegateCommand(
                        () => {
                            if (Process.Start(LogPath) == null)
                                throw new Exception(string.Format("Failed to open {0}", LogPath));
                        },
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
        /// <param name="server">Connecting eduVPN server</param>
        /// <param name="config">Initial profile configuration</param>
        /// <param name="expiration">VPN expiry times</param>
        public WireGuardSession(ConnectWizard wizard, Server server, Configuration config, Expiration expiration) :
            base(wizard, server, config, expiration)
        {
            TunnelName =
                Uri.TryCreate(server.Id, UriKind.Absolute, out var uri) ? uri.Host :
                new string(server.Id.Where(c => c == '_' || c == '=' || c == '+' || c == '.' || c == '-' || char.IsLetter(c) || char.IsNumber(c)).ToArray());
            if (TunnelName.Length > 32)
                TunnelName = TunnelName.Substring(0, 32);
        }

        #endregion

        #region Methods

        /// <inheritdoc/>
        public override string ToString()
        {
            switch (Config.Protocol)
            {
                case VPNProtocol.WireGuard: return "WireGuard";
                case VPNProtocol.WireGuardProxy: return "WireGuard (TCP)";
                default: throw new ArgumentOutOfRangeException(nameof(Config.Protocol));
            }
        }

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
                    Interface iface;
                    using (var reader = new StringReader(Config.VPNConfig))
                        iface = new Interface(reader);
                    switch (Properties.SettingsEx.Default.WireGuardKillSwitch)
                    {
                        case WireGuardKillSwitchMode.Enforce:
                            foreach (var peer in iface.Peers)
                            {
                                IEnumerable<IPPrefix> allowedIPs = peer.AllowedIPs;
                                if (allowedIPs.Any(addr => addr == IPPrefix.LowerHalf) &&
                                    allowedIPs.Any(addr => addr == IPPrefix.UpperHalf))
                                {
                                    allowedIPs = allowedIPs
                                        .Where(addr => addr != IPPrefix.LowerHalf && addr != IPPrefix.UpperHalf)
                                        .Union(new IPPrefix[] { IPPrefix.All });
                                }
                                if (allowedIPs.Any(addr => addr == IPPrefix.IPv6LowerHalf) &&
                                    allowedIPs.Any(addr => addr == IPPrefix.IPv6UpperHalf))
                                {
                                    allowedIPs = allowedIPs
                                        .Where(addr => addr != IPPrefix.IPv6LowerHalf && addr != IPPrefix.IPv6UpperHalf)
                                        .Union(new IPPrefix[] { IPPrefix.IPv6All });
                                }
                                peer.AllowedIPs = allowedIPs.ToList();
                            }
                            break;

                        case WireGuardKillSwitchMode.Remove:
                            foreach (var peer in iface.Peers)
                            {
                                var addr2 = new List<IPPrefix>();
                                foreach (var addr in peer.AllowedIPs)
                                {
                                    if (addr == IPPrefix.All)
                                    {
                                        addr2.Add(IPPrefix.LowerHalf);
                                        addr2.Add(IPPrefix.UpperHalf);
                                    }
                                    else if (addr == IPPrefix.IPv6All)
                                    {
                                        addr2.Add(IPPrefix.IPv6LowerHalf);
                                        addr2.Add(IPPrefix.IPv6UpperHalf);
                                    }
                                    else
                                        addr2.Add(addr);
                                }
                                peer.AllowedIPs = addr2;
                            }
                            break;
                    }

                    // Connect to WireGuard Tunnel Manager Service to activate the tunnel.
                    using (var managerSession = new eduWireGuard.ManagerService.Session())
                    {
                        Wizard.TryInvoke((Action)(() => State = SessionStatusType.Connecting));
                        try
                        {
                            managerSession.Activate(
                                "eduWGManager" + Properties.Settings.Default.WireGuardTunnelManagerServiceInstance,
                                TunnelName,
                                iface.ToWgQuick(),
                                3000,
                                SessionAndWindowInProgress.Token);
                        }
                        catch (OperationCanceledException) { throw; }
                        catch (Exception ex) { throw new AggregateException(Resources.Strings.ErrorWireGuardTunnelManagerService, ex); }
                        try
                        {
                            IPAddress tunnelAddress = null, ipv6TunnelAddress = null;
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

                            Wizard.TryInvoke((Action)(() =>
                            {
                                TunnelAddress = tunnelAddress;
                                IPv6TunnelAddress = ipv6TunnelAddress;
                                State = SessionStatusType.Testing;
                                Wizard.TaskCount--;
                            }));
                            Engine.SetState(Engine.State.Connected);
                            try
                            {
                                // Wait for a change and update stats.
                                int millisecondTimeout = 100;
                                do
                                {
                                    //throw new Exception("Test exception");
                                    try
                                    {
                                        var cfg = managerSession.GetTunnelConfig(SessionAndWindowInProgress.Token);
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
                                            RxBytes = rxBytes;
                                            TxBytes = txBytes;
                                        }));
                                    }
                                    catch (OperationCanceledException) { throw; }
                                    catch {
                                        // Ignore tunnel status update failures. Immediately after resume from sleep,
                                        // GetTunnelConfig() is sometimes throwing with ERROR_FILE_NOT_FOUND (2).
                                    }
                                    millisecondTimeout = Math.Min(millisecondTimeout + 100, 5 * 1000);
                                } while (!SessionAndWindowInProgress.Token.WaitHandle.WaitOne(millisecondTimeout));
                            }
                            finally {
                                Engine.SetState(Engine.State.Disconnecting);
                                Wizard.TryInvoke((Action)(() =>
                                {
                                    Wizard.TaskCount++;
                                    State = SessionStatusType.Disconnecting;
                                }));
                            }
                        }
                        finally { managerSession.Deactivate(); }
                    }
                }
                finally
                {
                    propertyUpdater.Stop();
                    Wizard.TryInvoke((Action)(() =>
                    {
                        // Cleanup status properties.
                        TunnelAddress = null;
                        IPv6TunnelAddress = null;
                        RxBytes = null;
                        TxBytes = null;
                    }));
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
