/*
    eduVPN - VPN for education and research

    Copyright: 2017-2021 The Commons Conservancy eduVPN Programme
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
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Security.Cryptography.X509Certificates;
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
        /// OpenVPN Interactive Service instance name to connect to (e.g. "$eduVPN", "", etc.)
        /// </summary>
        private string _instance_name;

        /// <summary>
        /// OpenVPN connection ID
        /// </summary>
        /// <remarks>Connection ID determines .conf and .log filenames.</remarks>
        private string _connection_id;

        /// <summary>
        /// OpenVPN working folder
        /// </summary>
        private string _working_folder;

        /// <summary>
        /// Client certificate
        /// </summary>
        private X509Certificate2 _client_certificate;

        /// <summary>
        /// Profile configuration
        /// </summary>
        private string _profile_config;

        /// <summary>
        /// Should HOLD hint on reconnect be ignored?
        /// </summary>
        private bool _ignore_hold_hint;

        /// <summary>
        /// Management Session
        /// </summary>
        private eduOpenVPN.Management.Session _mgmt_session = new eduOpenVPN.Management.Session();

        /// <summary>
        /// Property update timer
        /// </summary>
        protected DispatcherTimer _property_updater;

        #endregion

        #region Properties

        /// <summary>
        /// OpenVPN profile configuration file path
        /// </summary>
        string ConfigurationPath { get => _working_folder + _connection_id + ".conf"; }

        /// <summary>
        /// OpenVPN connection log
        /// </summary>
        string LogPath { get => _working_folder + _connection_id + ".txt"; }

        /// <inheritdoc/>
        override public DelegateCommand ShowLog
        {
            get
            {
                if (_ShowLog == null)
                    _ShowLog = new DelegateCommand(
                        () =>
                        {
                            try
                            {
                                // Open log file in registered application.
                                Process.Start(LogPath);
                            }
                            catch (Exception ex) { Wizard.Error = ex; }
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
        /// Creates an OpenVPN session
        /// </summary>
        /// <param name="instance_name">OpenVPN Interactive Service instance name to connect to (e.g. "$eduVPN", "", etc.)</param>
        /// <param name="wizard">The connecting wizard</param>
        /// <param name="authenticating_instance">Authenticating eduVPN instance</param>
        /// <param name="connecting_profile">Connecting eduVPN profile</param>
        public OpenVPNSession(string instance_name, ConnectWizard wizard, Instance authenticating_instance, Profile connecting_profile) :
            base(wizard, authenticating_instance, connecting_profile)
        {
            _instance_name = instance_name;
            _connection_id = Guid.NewGuid().ToString();
            try
            {
                // Use OpenVPN configuration folder.
                using (var hklm_key = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry64))
                {
                    using (var key = hklm_key.OpenSubKey("SOFTWARE\\OpenVPN" + _instance_name, false))
                    {
                        _working_folder = key.GetValue("config_dir").ToString().TrimEnd();
                        string path_separator = Path.DirectorySeparatorChar.ToString();
                        if (!_working_folder.EndsWith(path_separator))
                            _working_folder += path_separator;
                        if (!Directory.Exists(_working_folder))
                            throw new FileNotFoundException();
                    }
                }
            }
            catch
            {
                // Use temporary folder.
                _working_folder = Path.GetTempPath();
            }

            // Create dispatcher timer to refresh ShowLog command "can execute" status every second.
            _property_updater = new DispatcherTimer(
                new TimeSpan(0, 0, 0, 1),
                DispatcherPriority.Normal, (object sender, EventArgs e) => ShowLog.RaiseCanExecuteChanged(),
                Wizard.Dispatcher);
            _property_updater.Start();

            _pre_run_actions.Add(() =>
            {
                // Get profile's OpenVPN configuration.
                _profile_config = ConnectingProfile.GetOpenVPNConfig(_quit.Token);
            });

            _pre_run_actions.Add(() =>
            {
                // Get instance client certificate.
                _client_certificate = ConnectingProfile.Instance.GetClientCertificate(AuthenticatingInstance, _quit.Token);
            });

            // Set event handlers.
            _mgmt_session.ByteCountReported += (object sender, ByteCountReportedEventArgs e) =>
                Wizard.Dispatcher.Invoke(DispatcherPriority.Normal, (Action)(
                    () =>
                    {
                        BytesIn = e.BytesIn;
                        BytesOut = e.BytesOut;
                    }));

            _mgmt_session.FatalErrorReported += (object sender, MessageReportedEventArgs e) =>
                Wizard.Dispatcher.Invoke(DispatcherPriority.Normal, (Action)(
                    () =>
                    {
                        State = SessionStatusType.Error;
                        var msg = Resources.Strings.OpenVPNStateTypeFatalError;
                        if (e.Message != null && e.Message.Length > 0)
                        {
                            // Append OpenVPN message.
                            msg += "\r\n" + e.Message;
                        }
                        StateDescription = msg;

                        // Also, display the error message in the connect wizard.
                        Wizard.Error = new Exception(msg);
                    }));

            _mgmt_session.HoldReported += (object sender, HoldReportedEventArgs e) =>
            {
                if (!_ignore_hold_hint && e.WaitHint > 0)
                    _quit.Token.WaitHandle.WaitOne(e.WaitHint * 1000);
            };

            _mgmt_session.PasswordAuthenticationRequested += (object sender, PasswordAuthenticationRequestedEventArgs e) => Wizard.OpenVPNSession_RequestPasswordAuthentication(this, e);

            _mgmt_session.RemoteReported += (object sender, RemoteReportedEventArgs e) =>
            {
                if (e.Protocol == ProtoType.UDP && Properties.Settings.Default.OpenVPNForceTCP)
                    e.Action = new RemoteSkipAction();
                else
                    e.Action = new RemoteAcceptAction();
            };

            _mgmt_session.StateReported += (object sender, StateReportedEventArgs e) =>
            {
                Wizard.Dispatcher.Invoke(DispatcherPriority.Normal, (Action)(
                    () =>
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

                        if (!String.IsNullOrEmpty(e.Message))
                        {
                            if (msg != null)
                            {
                                // Append OpenVPN message.
                                msg += "\r\n" + e.Message;
                            }
                            else
                            {
                                // Replace with OpenVPN message.
                                msg = e.Message;
                            }
                        }
                        else if (msg == null)
                            msg = "";

                        StateDescription = msg;
                        TunnelAddress = e.Tunnel;
                        IPv6TunnelAddress = e.IPv6Tunnel;

                        // Update connected time.
                        if (e.State == OpenVPNStateType.Connected)
                        {
                            ConnectedSince = e.TimeStamp;
                            _connected_time_updater.Start();
                        }
                        else
                        {
                            _connected_time_updater.Stop();
                            ConnectedSince = null;
                        }

                        // Set State property last, as the whole world is listening on this property to monitor connectivity changes.
                        // It is important that we have IP addresses and other info already set before rising PropertyChanged event for State.
                        State = state;
                    }));

                if (e.State == OpenVPNStateType.Reconnecting)
                {
                    switch (e.Message)
                    {
                        case "connection-reset": // Connection was reset.
                            if (_client_certificate.NotAfter <= DateTime.Now)
                            {
                                // Client certificate expired. Try with a new client certificate then.
                                goto case "tls-error";
                            }
                            goto default;

                        case "auth-failure": // Client certificate was deleted/revoked on the server side, or the user is disabled.
                        case "tls-error": // Client certificate is not compliant with this eduVPN instance. Was eduVPN instance reinstalled?
                                          // Refresh client certificate.
                            _client_certificate = ConnectingProfile.Instance.RefreshClientCertificate(AuthenticatingInstance, _quit.Token);
                            _ignore_hold_hint = true;
                            break;

                        default:
                            _ignore_hold_hint = false;
                            break;
                    }

                    _mgmt_session.QueueReleaseHold(_quit.Token);
                }
            };
        }

        #endregion

        #region Methods

        /// <inheritdoc/>
        [SuppressMessage("Microsoft.Usage", "CA2202:Do not dispose objects multiple times", Justification = "FileStream tolerates multiple disposes.")]
        protected override void DoRun()
        {
            Wizard.Dispatcher.Invoke(DispatcherPriority.Normal, (Action)(() => Wizard.ChangeTaskCount(+1)));
            try
            {
                try
                {
                    // Start OpenVPN management interface on IPv4 loopack interface (any TCP free port).
                    var mgmt_server = new TcpListener(IPAddress.Loopback, 0);
                    mgmt_server.Start();
                    try
                    {
                        try
                        {
                            // Purge stale log files.
                            var timestamp = DateTime.UtcNow.Subtract(new TimeSpan(30, 0, 0, 0));
                            foreach (var f in Directory.EnumerateFiles(_working_folder, "*.txt", SearchOption.TopDirectoryOnly))
                            {
                                _quit.Token.ThrowIfCancellationRequested();
                                if (File.GetLastWriteTimeUtc(f) <= timestamp)
                                {
                                    try { File.Delete(LogPath); }
                                    catch { }
                                }
                            }
                        }
                        catch (OperationCanceledException) { throw; }
                        catch (Exception) { /* Failure to remove stale log files is not fatal. */ }

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

                                if (Properties.Settings.Default.OpenVPNRemoveOptions is StringCollection)
                                {
                                    // Remove options on the OpenVPNRemoveOptions list on the fly.
                                    using (var sr = new StringReader(_profile_config))
                                    {
                                        string inline_term = null;
                                        bool inline_remove = false;
                                        for (; ; )
                                        {
                                            var line = sr.ReadLine();
                                            if (line == null)
                                                break;

                                            var line_t = line.Trim();
                                            if (!String.IsNullOrEmpty(line_t))
                                            {
                                                // Not an empty line.
                                                if (inline_term == null)
                                                {
                                                    // Not inside an inline option block = Regular parsing mode.
                                                    if (!line_t.StartsWith("#") &&
                                                        !line_t.StartsWith(";"))
                                                    {
                                                        // Not a comment.
                                                        var option = eduOpenVPN.Configuration.ParseParams(line_t);
                                                        if (option.Count > 0)
                                                        {
                                                            if (option[0].StartsWith("<") && !option[0].StartsWith("</") && option[0].EndsWith(">"))
                                                            {
                                                                // Start of an inline option.
                                                                var o = option[0].Substring(1, option[0].Length - 2);
                                                                inline_term = "</" + o + ">";
                                                                inline_remove = Properties.Settings.Default.OpenVPNRemoveOptions.Contains(o);
                                                                if (inline_remove)
                                                                {
                                                                    sw.WriteLine("# Commented by OpenVPNRemoveOptions setting:");
                                                                    line = "# " + line;
                                                                }
                                                            }
                                                            else if (Properties.Settings.Default.OpenVPNRemoveOptions.Contains(option[0]))
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
                                                    if (inline_remove)
                                                    {
                                                        // Remove the inline option content.
                                                        line = "# " + line;
                                                    }

                                                    if (line_t == inline_term)
                                                    {
                                                        // Inline option terminator found. Returning to regular parsing mode.
                                                        inline_term = null;
                                                    }
                                                }
                                            }

                                            sw.WriteLine(line);
                                        }
                                    }
                                }
                                else
                                    sw.Write(_profile_config);

                                // Append eduVPN Client specific configuration directives.
                                sw.WriteLine();
                                sw.WriteLine();
                                sw.WriteLine("# eduVPN Client for Windows");

                                // Introduce ourself (to OpenVPN server).
                                var assembly = Assembly.GetExecutingAssembly();
                                var assembly_title_attribute = Attribute.GetCustomAttributes(assembly, typeof(AssemblyTitleAttribute)).SingleOrDefault() as AssemblyTitleAttribute;
                                var assembly_version = assembly?.GetName()?.Version;
                                sw.WriteLine("setenv IV_GUI_VER " + eduOpenVPN.Configuration.EscapeParamValue(assembly_title_attribute?.Title + " " + assembly_version?.ToString()));

                                // Configure log file (relative to _working_folder).
                                sw.WriteLine("log-append " + eduOpenVPN.Configuration.EscapeParamValue(_connection_id + ".txt"));

                                // Configure interaction between us and openvpn.exe.
                                sw.WriteLine("management " + eduOpenVPN.Configuration.EscapeParamValue(((IPEndPoint)mgmt_server.LocalEndpoint).Address.ToString()) + " " + eduOpenVPN.Configuration.EscapeParamValue(((IPEndPoint)mgmt_server.LocalEndpoint).Port.ToString()) + " stdin");
                                sw.WriteLine("management-client");
                                sw.WriteLine("management-hold");
                                sw.WriteLine("management-query-passwords");
                                sw.WriteLine("management-query-remote");

                                // Configure client certificate.
                                sw.WriteLine("cert " + eduOpenVPN.Configuration.EscapeParamValue(ConnectingProfile.Instance.ClientCertificatePath));
                                sw.WriteLine("key " + eduOpenVPN.Configuration.EscapeParamValue(ConnectingProfile.Instance.ClientCertificatePath));

                                // Ask when username/password is denied.
                                sw.WriteLine("auth-retry interact");
                                sw.WriteLine("auth-nocache");

                                // Set TAP interface to be used.
                                if (NetworkInterface.TryFromID(Properties.Settings.Default.OpenVPNInterfaceID, out var iface))
                                    sw.Write("dev-node " + eduOpenVPN.Configuration.EscapeParamValue(iface.Name) + "\n");

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

                                if (!String.IsNullOrWhiteSpace(Properties.Settings.Default.OpenVPNAddOptions))
                                {
                                    sw.WriteLine();
                                    sw.WriteLine();
                                    sw.WriteLine("# Added by OpenVPNAddOptions setting:");
                                    sw.WriteLine(Properties.Settings.Default.OpenVPNAddOptions);
                                }
                            }
                        }
                        catch (OperationCanceledException) { throw; }
                        catch (Exception ex) { throw new AggregateException(String.Format(Resources.Strings.ErrorSavingProfileConfiguration, ConfigurationPath), ex); }

                        bool retry;
                        do
                        {
                            retry = false;

                            // Connect to OpenVPN Interactive Service to launch the openvpn.exe.
                            using (var openvpn_interactive_service_connection = new eduOpenVPN.InteractiveService.Session())
                            {
                                var mgmt_password = Membership.GeneratePassword(16, 6);
                                try
                                {
                                    openvpn_interactive_service_connection.Connect(
                                        String.Format("openvpn{0}\\service", _instance_name),
                                        _working_folder,
                                        new string[] { "--config", _connection_id + ".conf", },
                                        mgmt_password + "\n",
                                        3000,
                                        _quit.Token);
                                }
                                catch (OperationCanceledException) { throw; }
                                catch (Exception ex) { throw new AggregateException(Resources.Strings.ErrorInteractiveService, ex); }

                                try
                                {
                                    // Wait and accept the openvpn.exe on our management interface (--management-client parameter).
                                    var mgmt_client_task = mgmt_server.AcceptTcpClientAsync();
                                    try { mgmt_client_task.Wait(30000, _quit.Token); }
                                    catch (AggregateException ex) { throw ex.InnerException; }
                                    var mgmt_client = mgmt_client_task.Result;
                                    try
                                    {
                                        // Start the management session.
                                        _mgmt_session.Start(mgmt_client.GetStream(), mgmt_password, _quit.Token);

                                        // Initialize session and release openvpn.exe to get started.
                                        _mgmt_session.SetVersion(3, _quit.Token);
                                        _mgmt_session.ReplayAndEnableState(_quit.Token);
                                        _mgmt_session.ReplayAndEnableEcho(_quit.Token);
                                        _mgmt_session.SetByteCount(5, _quit.Token);
                                        _mgmt_session.ReleaseHold(_quit.Token);

                                        Wizard.Dispatcher.Invoke(DispatcherPriority.Normal, (Action)(() => Wizard.ChangeTaskCount(-1)));
                                        try
                                        {
                                            // Wait for the session to end gracefully.
                                            _mgmt_session.Monitor.Join();
                                            if (_mgmt_session.Error != null && !(_mgmt_session.Error is OperationCanceledException))
                                            {
                                                // Session reported an error. Retry.
                                                retry = true;
                                            }
                                        }
                                        finally { Wizard.Dispatcher.Invoke(DispatcherPriority.Normal, (Action)(() => Wizard.ChangeTaskCount(+1))); }
                                    }
                                    finally { mgmt_client.Close(); }
                                }
                                finally
                                {
                                    Wizard.Dispatcher.Invoke(DispatcherPriority.Normal, (Action)(
                                        () =>
                                        {
                                            // Cleanup status properties.
                                            State = SessionStatusType.Disconnecting;
                                            StateDescription = Resources.Strings.OpenVPNStateTypeExiting;
                                            TunnelAddress = null;
                                            IPv6TunnelAddress = null;
                                            _connected_time_updater.Stop();
                                            ConnectedSince = null;
                                            BytesIn = null;
                                            BytesOut = null;
                                        }));

                                    // Wait for openvpn.exe to finish. Maximum 30s.
                                    try { Process.GetProcessById(openvpn_interactive_service_connection.ProcessId)?.WaitForExit(30000); }
                                    catch (ArgumentException) { }
                                }
                            }
                        } while (retry);
                    }
                    finally
                    {
                        mgmt_server.Stop();
                    }
                }
                finally
                {
                    // Delete profile configuration file. If possible.
                    try { File.Delete(ConfigurationPath); }
                    catch { }
                }
            }
            finally
            {
                Wizard.Dispatcher.Invoke(DispatcherPriority.Normal, (Action)(
                    () =>
                    {
                        // Cleanup status properties.
                        State = SessionStatusType.Initializing;
                        StateDescription = "";

                        Wizard.ChangeTaskCount(-1);
                    }));
                _property_updater.Stop();
            }
        }

        #endregion
    }
}
