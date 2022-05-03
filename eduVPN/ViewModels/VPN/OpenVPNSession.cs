/*
    eduVPN - VPN for education and research

    Copyright: 2017-2022 The Commons Conservancy
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
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
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
        /// <remarks>Connection identifier determines .conf and .log filenames.</remarks>
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

        /// <summary>
        /// Session renewal in progress
        /// </summary>
        private volatile bool RenewInProgress;

        /// <summary>
        /// Session disconnect in progress
        /// </summary>
        private volatile bool DisconnectInProgress;

        #endregion

        #region Properties

        /// <summary>
        /// OpenVPN profile configuration file path
        /// </summary>
        private string ConfigurationPath => Path.Combine(WorkingFolder, ConnectionId + ".conf");

        /// <summary>
        /// OpenVPN connection log
        /// </summary>
        private string LogPath => Path.Combine(WorkingFolder, ConnectionId + ".txt");

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
                            RenewInProgress = true;
                            _Renew.RaiseCanExecuteChanged();
                            new Thread(new ThreadStart(
                                () =>
                                {
                                    try
                                    {
                                        var config = ConnectingProfile.Connect(
                                            Wizard.GetAuthenticatingServer(ConnectingProfile.Server),
                                            true,
                                            ProfileConfig.ContentType,
                                            Window.Abort.Token);
                                        Wizard.TryInvoke((Action)(() => ProfileConfig = config));
                                        ManagementSession.SendSignal(SignalType.SIGHUP, Window.Abort.Token);
                                    }
                                    catch (OperationCanceledException) { }
                                    catch (Exception ex) { Wizard.TryInvoke((Action)(() => throw ex)); }
                                    finally
                                    {
                                        RenewInProgress = false;
                                        Wizard.TryInvoke((Action)(() => _Renew.RaiseCanExecuteChanged()));
                                    }
                                })).Start();
                        },
                        () => !RenewInProgress && State == SessionStatusType.Connected && ManagementSession != null);
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
                            DisconnectInProgress = true;
                            _Disconnect.RaiseCanExecuteChanged();

                            // Terminate connection.
                            State = SessionStatusType.Disconnecting;
                            ManagementSession.QueueSendSignal(SignalType.SIGTERM);
                        },
                        () => !DisconnectInProgress && ManagementSession != null);
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
                    try
                    {
                        // Get a random TCP port for openvpn.exe management interface.
                        var mgmtServer = new TcpListener(IPAddress.Loopback, 0);
                        mgmtServer.Start();
                        try
                        {
                            var mgmtEndpoint = mgmtServer.LocalEndpoint as IPEndPoint;
                            try
                            {
                                // Save OpenVPN configuration file.
                                using (var fs = new FileStream(
                                    ConfigurationPath,
                                    FileMode.Create,
                                    FileAccess.Write,
                                    FileShare.Read,
                                    1048576,
                                    FileOptions.SequentialScan))
                                using (var sw = new StreamWriter(fs))
                                {
                                    // Save profile's configuration to file.

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
                                                            var option = Configuration.ParseParams(trimmedLine);
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
                                    sw.WriteLine("setenv IV_GUI_VER " + Configuration.EscapeParamValue(assemblyTitleAttribute?.Title + " " + assemblyVersion?.ToString()));

                                    // Configure log file (relative to WorkingFolder).
                                    sw.WriteLine("log-append " + Configuration.EscapeParamValue(ConnectionId + ".txt"));

                                    // Configure interaction between us and openvpn.exe.
                                    sw.WriteLine("management " + Configuration.EscapeParamValue(mgmtEndpoint.Address.ToString()) + " " + Configuration.EscapeParamValue(mgmtEndpoint.Port.ToString()) + " stdin");
                                    sw.WriteLine("management-hold"); // Wait for our signal to start connecting.
                                    sw.WriteLine("management-signal"); // Raise SIGUSR1 if our client dies/closes management interface.
                                    sw.WriteLine("remap-usr1 SIGTERM"); // SIGUSR1 (reconnect) => SIGTERM (disconnect)
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

                                    var openVPNAddOptions = Properties.SettingsEx.Default.OpenVPNAddOptions;
                                    if (!string.IsNullOrWhiteSpace(openVPNAddOptions))
                                    {
                                        sw.WriteLine();
                                        sw.WriteLine();
                                        sw.WriteLine("# Added by OpenVPNAddOptions setting:");
                                        sw.WriteLine(openVPNAddOptions);
                                    }
                                }
                            }
                            catch (OperationCanceledException) { throw; }
                            catch (Exception ex) { throw new AggregateException(string.Format(Resources.Strings.ErrorSavingProfileConfiguration, ConfigurationPath), ex); }

                            retry:
                            // Connect to OpenVPN Interactive Service to launch the openvpn.exe.
                            using (var openvpnInteractiveServiceConnection = new eduOpenVPN.InteractiveService.Session())
                            {
                                var mgmtPassword = Membership.GeneratePassword(16, 6);

                                // Release TCP port for openvpn.exe management interface.
                                mgmtServer.Stop();

                                try
                                {
                                    openvpnInteractiveServiceConnection.Connect(
                                        string.Format("openvpn{0}\\service", Properties.SettingsEx.Default.OpenVPNInteractiveServiceInstance),
                                        WorkingFolder,
                                        new string[] { "--config", ConnectionId + ".conf", },
                                        mgmtPassword + "\n",
                                        3000,
                                        Window.Abort.Token);
                                }
                                catch (OperationCanceledException) { throw; }
                                catch (Exception ex) { throw new AggregateException(Resources.Strings.ErrorInteractiveService, ex); }

                                try
                                {
                                    // Connect to the openvpn.exe management interface.
                                    var mgmtClient = new TcpClient();
                                    var reconnectCount = 0;
                                    reconnect:
                                    var mgmtClientTask = mgmtClient.ConnectAsync(mgmtEndpoint.Address, mgmtEndpoint.Port);
                                    try { mgmtClientTask.Wait(30000, Window.Abort.Token); }
                                    catch (AggregateException ex)
                                    {
                                        if (ex.InnerException is SocketException ex2 && ex2.SocketErrorCode == SocketError.ConnectionRefused &&
                                            ++reconnectCount < 30 && !Window.Abort.Token.WaitHandle.WaitOne(1000))
                                        {
                                            Trace.TraceWarning("Failed to connect to openvpn.exe");
                                            goto reconnect;
                                        }
                                        throw ex.InnerException;
                                    }
                                    try
                                    {
                                        // Create and start the management session.
                                        ManagementSession = new eduOpenVPN.Management.Session();
                                        ManagementSession.ByteCountReported += ManagementSession_ByteCountReported;
                                        ManagementSession.FatalErrorReported += ManagementSession_FatalErrorReported;
                                        ManagementSession.HoldReported += ManagementSession_HoldReported;
                                        ManagementSession.StateReported += ManagementSession_StateReported;
                                        ManagementSession.Start(mgmtClient.GetStream(), mgmtPassword, Window.Abort.Token);

                                        // Initialize session and release openvpn.exe to get started.
                                        ManagementSession.SetVersion(3, Window.Abort.Token);
                                        ManagementSession.ReplayAndEnableState(Window.Abort.Token);
                                        ManagementSession.ReplayAndEnableEcho(Window.Abort.Token);
                                        ManagementSession.SetByteCount(5, Window.Abort.Token);
                                        ManagementSession.ReleaseHold(Window.Abort.Token);

                                        Wizard.TryInvoke((Action)(() =>
                                        {
                                            _Renew?.RaiseCanExecuteChanged();
                                            _Disconnect?.RaiseCanExecuteChanged();
                                            Wizard.TaskCount--;
                                        }));
                                        try
                                        {
                                            // Wait for the session to end gracefully.
                                            ManagementSession.Monitor.Join();
                                            if (ManagementSession.Error != null && // Session terminated in an error.
                                                !(ManagementSession.Error is OperationCanceledException)) // Session was not cancelled.
                                                goto retry;
                                        }
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
                                        BytesIn = null;
                                        BytesOut = null;
                                    }));

                                    // Wait for openvpn.exe to finish. Maximum 30s.
                                    try { Process.GetProcessById(openvpnInteractiveServiceConnection.ProcessId)?.WaitForExit(30000); }
                                    catch (ArgumentException) { }
                                }
                            }
                        }
                        finally { mgmtServer.Stop(); }
                    }
                    finally
                    {
                        try { File.Delete(ConfigurationPath); }
                        catch (Exception ex) { Trace.TraceWarning("Deleting {0} file failed: {1}", ConfigurationPath, ex.ToString()); }
                    }
                }
                finally { propertyUpdater.Stop(); }
            }
            finally { Wizard.TryInvoke((Action)(() => Wizard.TaskCount--)); }
        }

        private void ManagementSession_ByteCountReported(object sender, ByteCountReportedEventArgs e)
        {
            Wizard.TryInvoke((Action)(() =>
            {
                BytesIn = e.BytesIn;
                BytesOut = e.BytesOut;
            }));
        }

        private void ManagementSession_FatalErrorReported(object sender, MessageReportedEventArgs e)
        {
            Wizard.TryInvoke((Action)(() =>
            {
                State = SessionStatusType.Error;
                var ex = new OpenVPNException(e.Message);
                StateDescription = ex.ToString();
                throw ex;
            }));
        }

        private void ManagementSession_HoldReported(object sender, HoldReportedEventArgs e)
        {
            if (!IgnoreHoldHint && e.WaitHint > 0)
                Window.Abort.Token.WaitHandle.WaitOne(e.WaitHint * 1000);
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
                switch (e.Message)
                {
                    case "connection-reset": // Connection was reset.
                        if (ValidTo <= DateTimeOffset.Now)
                        {
                            // Client certificate expired. Try with a new client certificate then.
                            goto case "tls-error";
                        }
                        goto default;

                    case "auth-failure": // Client certificate was deleted/revoked on the server side, or the user is disabled.
                    case "tls-error": // Client certificate is not compliant with this eduVPN server. Was eduVPN server reinstalled?
                                      // Refresh configuration.
                        var config = ConnectingProfile.Connect(
                            Wizard.GetAuthenticatingServer(ConnectingProfile.Server),
                            true,
                            ProfileConfig.ContentType,
                            Window.Abort.Token);
                        Wizard.TryInvoke((Action)(() => ProfileConfig = config));
                        IgnoreHoldHint = true;
                        break;

                    default:
                        IgnoreHoldHint = false;
                        break;
                }

                ManagementSession.QueueReleaseHold(Window.Abort.Token);
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
