/*
    eduVPN - VPN for education and research

    Copyright: 2017-2024 The Commons Conservancy
    SPDX-License-Identifier: GPL-3.0+
*/

using eduOpenVPN;
using eduOpenVPN.Management;
using eduVPN.Models;
using eduVPN.ViewModels.Windows;
using Microsoft.Win32;
using Prism.Commands;
using System;
using System.Collections.Specialized;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Web.Security;
using System.Windows.Threading;

namespace eduVPN.ViewModels.VPN
{
    /// <summary>
    /// OpenVPN session
    /// </summary>
    public class OpenVPNSession : Session
    {
        #region Fields

        /// <summary>
        /// OpenVPN connection identifier
        /// </summary>
        /// <remarks>Connection identifier determines .ovpn and .log filenames.</remarks>
        private readonly string ConnectionId;

        /// <summary>
        /// OpenVPN working folder
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
                            using (var key = hklmKey.OpenSubKey("SOFTWARE\\OpenVPN" + Properties.SettingsEx.Default.OpenVPNInteractiveServiceInstance, false))
                            {
                                var path = key.GetValue("config_dir").ToString().TrimEnd();
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
        /// Should HOLD hint on reconnect be ignored?
        /// </summary>
        private bool IgnoreHoldHint;

        /// <summary>
        /// Management Session
        /// </summary>
        private eduOpenVPN.Management.Session ManagementSession;

        #endregion

        #region Properties

        /// <summary>
        /// OpenVPN connection log
        /// </summary>
        private string LogPath => Path.Combine(WorkingFolder, ConnectionId + ".txt");

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
        /// Creates an OpenVPN session
        /// </summary>
        /// <param name="wizard">The connecting wizard</param>
        /// <param name="connectingProfile">Connecting eduVPN profile</param>
        /// <param name="profileConfig">Initial profile configuration</param>
        public OpenVPNSession(ConnectWizard wizard, Profile connectingProfile, Xml.Response profileConfig) :
            base(wizard, connectingProfile, profileConfig)
        {
            ConnectionId = Guid.NewGuid().ToString();
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
                    // Start OpenVPN management interface on IPv4 loopack interface (any TCP free port).
                    var mgmtServer = new TcpListener(IPAddress.Loopback, 0);
                    mgmtServer.Start();
                    try
                    {
                        byte[] ovpn;
                        var mgmtEndpoint = mgmtServer.LocalEndpoint as IPEndPoint;
                        var mgmtPassword = Membership.GeneratePassword(16, 6);

                        // Prepare OpenVPN configuration.
                        var fs = new MemoryStream();
                        using (fs)
                        using (var sw = new StreamWriter(fs))
                        {
                            if (Properties.SettingsEx.Default.OpenVPNRemoveOptions is StringCollection openVPNRemoveOptions)
                            {
                                // Remove options on the OpenVPNRemoveOptions list on the fly.
                                using (var sr = new StringReader(ProfileConfig.Value))
                                {
                                    string inlineTerm = null;
                                    var inlineRemove = false;
                                    for (; ; )
                                    {
                                        var line = sr.ReadLine();
                                        if (line == null)
                                            break;

                                        var trimmedLine = line.Trim();
                                        if (!string.IsNullOrEmpty(trimmedLine))
                                        {
                                            // Not an empty line.
                                            if (inlineTerm == null)
                                            {
                                                // Not inside an inline option block = Regular parsing mode.
                                                if (!trimmedLine.StartsWith("#") &&
                                                    !trimmedLine.StartsWith(";"))
                                                {
                                                    // Not a comment.
                                                    var option = eduOpenVPN.Configuration.ParseParams(trimmedLine);
                                                    if (option.Count > 0)
                                                    {
                                                        if (option[0].StartsWith("<") && !option[0].StartsWith("</") && option[0].EndsWith(">"))
                                                        {
                                                            // Start of an inline option.
                                                            var o = option[0].Substring(1, option[0].Length - 2);
                                                            inlineTerm = "</" + o + ">";
                                                            inlineRemove = openVPNRemoveOptions.Contains(o);
                                                            if (inlineRemove)
                                                            {
                                                                sw.WriteLine("# Commented by OpenVPNRemoveOptions setting:");
                                                                line = "# " + line;
                                                            }
                                                        }
                                                        else if (openVPNRemoveOptions.Contains(option[0]))
                                                        {
                                                            sw.WriteLine("# Commented by OpenVPNRemoveOptions setting:");
                                                            line = "# " + line;
                                                        }
                                                    }
                                                }
                                            }
                                            else
                                            {
                                                // Inside an inline option block.
                                                if (inlineRemove)
                                                {
                                                    // Remove the inline option content.
                                                    line = "# " + line;
                                                }

                                                if (trimmedLine == inlineTerm)
                                                {
                                                    // Inline option terminator found. Returning to regular parsing mode.
                                                    inlineTerm = null;
                                                }
                                            }
                                        }

                                        sw.WriteLine(line);
                                    }
                                }
                            }
                            else
                                sw.Write(ProfileConfig.Value);

                            // Append eduVPN Client specific configuration directives.
                            sw.WriteLine();
                            sw.WriteLine();
                            sw.WriteLine("# eduVPN Client for Windows");

                            // Introduce ourself (to OpenVPN server).
                            var assembly = Assembly.GetExecutingAssembly();
                            var assemblyTitleAttribute = Attribute.GetCustomAttributes(assembly, typeof(AssemblyTitleAttribute)).SingleOrDefault() as AssemblyTitleAttribute;
                            var assemblyVersion = assembly?.GetName()?.Version;
                            sw.WriteLine("setenv IV_GUI_VER " + eduOpenVPN.Configuration.EscapeParamValue(assemblyTitleAttribute?.Title + " " + assemblyVersion?.ToString()));

                            // Configure log file (relative to WorkingFolder).
                            sw.WriteLine("log-append " + eduOpenVPN.Configuration.EscapeParamValue(ConnectionId + ".txt"));

                            // Configure interaction between us and openvpn.exe.
                            sw.WriteLine("management " + eduOpenVPN.Configuration.EscapeParamValue(mgmtEndpoint.Address.ToString()) + " " + eduOpenVPN.Configuration.EscapeParamValue(mgmtEndpoint.Port.ToString()));
                            sw.WriteLine("<management-client-pass>");
                            sw.WriteLine(mgmtPassword);
                            sw.WriteLine("</management-client-pass>");
                            sw.WriteLine("management-client"); // Instruct openvpn.exe to contact us.
                            sw.WriteLine("management-hold"); // Wait for our signal to start connecting.
                            sw.WriteLine("management-query-passwords");

                            // Ask when username/password is denied.
                            sw.WriteLine("auth-retry interact");
                            sw.WriteLine("auth-nocache");

                            // Set Wintun interface to be used.
                            sw.Write("windows-driver wintun\n");
                            var hash = new SHA1CryptoServiceProvider(); // https://datatracker.ietf.org/doc/html/rfc4122#section-4.3
                            byte[] bufferPrefix = { 0x6b, 0xa7, 0xb8, 0x11, 0x9d, 0xad, 0x11, 0xd1, 0x80, 0xb4, 0x00, 0xc0, 0x4f, 0xd4, 0x30, 0xc8 }; // https://datatracker.ietf.org/doc/html/rfc4122#appendix-C in network byte order
                            hash.TransformBlock(bufferPrefix, 0, bufferPrefix.Length, bufferPrefix, 0);
                            var bufferUri = Encoding.UTF8.GetBytes(new Uri(ConnectingProfile.Server.Base, ConnectingProfile.Id).AbsoluteUri);
                            hash.TransformFinalBlock(bufferUri, 0, bufferUri.Length);
                            var guid = new Guid(
                                ((uint)hash.Hash[0] << 24) | ((uint)hash.Hash[1] << 16) | ((uint)hash.Hash[2] << 8) | hash.Hash[3], // time_low
                                (ushort)(((uint)hash.Hash[4] << 8) | hash.Hash[5]), // time_mid
                                (ushort)(((((uint)hash.Hash[6] << 8) | hash.Hash[7]) & 0x0fff) | 0x5000), // time_hi_and_version
                                (byte)(((uint)hash.Hash[8] & 0x3f) | 0x80), // clock_seq_hi_and_reserved
                                hash.Hash[9], // clock_seq_low
                                hash.Hash[10], hash.Hash[11], hash.Hash[12], hash.Hash[13], hash.Hash[14], hash.Hash[15]); // node[0-5]
                            sw.Write("dev-node {" + guid + "}\n");

#if DEBUG
                            // Renegotiate data channel every 5 minutes in debug versions.
                            sw.WriteLine("reneg-sec 300");
#endif

                            if (Environment.OSVersion.Version < new Version(6, 2))
                            {
                                // Windows 7 is using tiny 8kB send/receive socket buffers by default.
                                // Increase to 64kB which is default from Windows 8 on.
                                sw.WriteLine("sndbuf 65536");
                                sw.WriteLine("rcvbuf 65536");
                            }

                            sw.WriteLine("script-security 1");

                            var openVPNAddOptions = Properties.SettingsEx.Default.OpenVPNAddOptions;
                            if (!string.IsNullOrWhiteSpace(openVPNAddOptions))
                            {
                                sw.WriteLine();
                                sw.WriteLine();
                                sw.WriteLine("# Added by OpenVPNAddOptions setting:");
                                sw.WriteLine(openVPNAddOptions);
                            }
                        }
                        ovpn = fs.ToArray();

                        // Connect to OpenVPN Interactive Service to launch the openvpn.exe.
                        using (var openvpnInteractiveServiceConnection = new eduOpenVPN.InteractiveService.Session())
                        {
                            try
                            {
                                openvpnInteractiveServiceConnection.Connect(
                                    string.Format("openvpn{0}\\service", Properties.SettingsEx.Default.OpenVPNInteractiveServiceInstance),
                                    WorkingFolder,
                                    new string[] { "--config", "stdin", },
                                    Encoding.UTF8.GetString(ovpn),
                                    3000,
                                    SessionAndWindowInProgress.Token);
                            }
                            catch (OperationCanceledException) { throw; }
                            catch (Exception ex) { throw new AggregateException(Resources.Strings.ErrorInteractiveService, ex); }

                            try
                            {
                                // Wait and accept the openvpn.exe on our management interface (--management-client parameter).
                                var mgmtClientTask = mgmtServer.AcceptTcpClientAsync();
                                try { mgmtClientTask.Wait(30000, SessionAndWindowInProgress.Token); }
                                catch (AggregateException ex) { throw ex.InnerException; }
                                var mgmtClient = mgmtClientTask.Result;
                                try
                                {
                                    // Create and start the management session.
                                    ManagementSession = new eduOpenVPN.Management.Session();
                                    ManagementSession.ByteCountReported += ManagementSession_ByteCountReported;
                                    ManagementSession.FatalErrorReported += ManagementSession_FatalErrorReported;
                                    ManagementSession.HoldReported += ManagementSession_HoldReported;
                                    ManagementSession.StateReported += ManagementSession_StateReported;
                                    ManagementSession.Start(mgmtClient.GetStream(), mgmtPassword, SessionAndWindowInProgress.Token);

                                    // Initialize session and release openvpn.exe to get started.
                                    ManagementSession.SetVersion(3, SessionAndWindowInProgress.Token);
                                    ManagementSession.ReplayAndEnableState(SessionAndWindowInProgress.Token);
                                    ManagementSession.ReplayAndEnableEcho(SessionAndWindowInProgress.Token);
                                    ManagementSession.SetByteCount(5, SessionAndWindowInProgress.Token);
                                    ManagementSession.ReleaseHold(SessionAndWindowInProgress.Token);

                                    Wizard.TryInvoke((Action)(() =>
                                    {
                                        Renew?.RaiseCanExecuteChanged();
                                        Wizard.TaskCount--;
                                    }));
                                    try { ManagementSession.Monitor.Join(); }
                                    finally { Wizard.TryInvoke((Action)(() => Wizard.TaskCount++)); }
                                }
                                finally { mgmtClient.Close(); }
                            }
                            finally
                            {
                                Wizard.TryInvoke((Action)(() =>
                                {
                                    // Cleanup status properties.
                                    State = SessionStatusType.Disconnecting;
                                    StateDescription = Resources.Strings.OpenVPNStateTypeExiting;
                                    TunnelAddress = null;
                                    IPv6TunnelAddress = null;
                                    ConnectedAt = null;
                                    RxBytes = null;
                                    TxBytes = null;
                                }));

                                // Wait for openvpn.exe to finish. Maximum 30s.
                                try { Process.GetProcessById(openvpnInteractiveServiceConnection.ProcessId)?.WaitForExit(30000); }
                                catch (ArgumentException) { }
                            }
                        }
                    }
                    finally { mgmtServer.Stop(); }
                }
                finally { propertyUpdater.Stop(); }
            }
            finally { Wizard.TryInvoke((Action)(() => Wizard.TaskCount--)); }
        }

        private void ManagementSession_ByteCountReported(object sender, ByteCountReportedEventArgs e)
        {
            Wizard.TryInvoke((Action)(() =>
            {
                RxBytes = e.RxBytes;
                TxBytes = e.TxBytes;
            }));
        }

        private void ManagementSession_FatalErrorReported(object sender, MessageReportedEventArgs e)
        {
            Wizard.TryInvoke((Action)(() =>
            {
                State = SessionStatusType.Error;
                var ex = new OpenVPNException(e.Message);
                StateDescription = ex.ToString();
                Wizard.Error = ex;
            }));
        }

        private void ManagementSession_HoldReported(object sender, HoldReportedEventArgs e)
        {
            if (!IgnoreHoldHint && e.WaitHint > 0)
                SessionAndWindowInProgress.Token.WaitHandle.WaitOne(e.WaitHint * 1000);
        }

        private void ManagementSession_StateReported(object sender, StateReportedEventArgs e)
        {
            var state = SessionStatusType.Error;
            string msg = null;
            switch (e.State)
            {
                case OpenVPNStateType.Connecting:
                    state = SessionStatusType.Connecting;
                    msg = Resources.Strings.OpenVPNStateTypeConnecting;
                    break;

                case OpenVPNStateType.Resolving:
                    state = SessionStatusType.Connecting;
                    msg = Resources.Strings.OpenVPNStateTypeResolving;
                    break;

                case OpenVPNStateType.TcpConnecting:
                    state = SessionStatusType.Connecting;
                    msg = Resources.Strings.OpenVPNStateTypeTcpConnecting;
                    break;

                case OpenVPNStateType.Waiting:
                    state = SessionStatusType.Connecting;
                    msg = Resources.Strings.OpenVPNStateTypeWaiting;
                    break;

                case OpenVPNStateType.Authenticating:
                    state = SessionStatusType.Connecting;
                    msg = Resources.Strings.OpenVPNStateTypeAuthenticating;
                    break;

                case OpenVPNStateType.GettingConfiguration:
                    state = SessionStatusType.Connecting;
                    msg = Resources.Strings.OpenVPNStateTypeGettingConfiguration;
                    break;

                case OpenVPNStateType.AssigningIP:
                    state = SessionStatusType.Connecting;
                    msg = Resources.Strings.OpenVPNStateTypeAssigningIP;
                    break;

                case OpenVPNStateType.AddingRoutes:
                    state = SessionStatusType.Connecting;
                    msg = Resources.Strings.OpenVPNStateTypeAddingRoutes;
                    break;

                case OpenVPNStateType.Connected:
                    state = SessionStatusType.Connected;
                    msg = Resources.Strings.OpenVPNStateTypeConnected;
                    break;

                case OpenVPNStateType.Reconnecting:
                    state = SessionStatusType.Connecting;
                    msg = Resources.Strings.OpenVPNStateTypeReconnecting;
                    break;

                case OpenVPNStateType.Exiting:
                    state = SessionStatusType.Disconnecting;
                    msg = Resources.Strings.OpenVPNStateTypeExiting;
                    break;
            }
            if (!string.IsNullOrEmpty(e.Message))
            {
                if (msg != null)
                    msg += "\r\n" + e.Message;
                else
                    msg = e.Message;
            }
            else if (msg == null)
                msg = "";
            Wizard.TryInvoke((Action)(() =>
            {
                StateDescription = msg;
                TunnelAddress = e.Tunnel;
                IPv6TunnelAddress = e.IPv6Tunnel;
                ConnectedAt = e.State == OpenVPNStateType.Connected ? (DateTimeOffset?)e.TimeStamp : null;

                // Set State property last, as the whole world is listening on this property to monitor connectivity changes.
                // It is important that we have IP addresses and other info already set before rising PropertyChanged event for State.
                State = state;
            }));

            if (e.State == OpenVPNStateType.Reconnecting)
            {
                IgnoreHoldHint = false;
                ManagementSession.QueueReleaseHold(SessionAndWindowInProgress.Token);
            }
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
