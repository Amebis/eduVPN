/*
    eduVPN - VPN for education and research

    Copyright: 2017-2019 The Commons Conservancy eduVPN Programme
    SPDX-License-Identifier: GPL-3.0+
*/

using eduOpenVPN;
using eduOpenVPN.Management;
using eduVPN.Models;
using eduVPN.ViewModels.Windows;
using Microsoft.Win32;
using System;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Web.Security;
using System.Windows.Threading;

namespace eduVPN.ViewModels.VPN
{
    /// <summary>
    /// OpenVPN session
    /// </summary>
    public class OpenVPNSession : VPNSession
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
            new DispatcherTimer(
                new TimeSpan(0, 0, 0, 1),
                DispatcherPriority.Normal, (object sender, EventArgs e) => ShowLog.RaiseCanExecuteChanged(),
                Wizard.Dispatcher).Start();

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
                                sw.WriteLine("log " + eduOpenVPN.Configuration.EscapeParamValue(_connection_id + ".txt"));

                                // Configure interaction between us and openvpn.exe.
                                sw.WriteLine("management " + eduOpenVPN.Configuration.EscapeParamValue(((IPEndPoint)mgmt_server.LocalEndpoint).Address.ToString()) + " " + eduOpenVPN.Configuration.EscapeParamValue(((IPEndPoint)mgmt_server.LocalEndpoint).Port.ToString()) + " stdin");
                                sw.WriteLine("management-client");
                                sw.WriteLine("management-hold");
                                sw.WriteLine("management-query-passwords");
                                sw.WriteLine("management-query-remote");

                                // Configure client certificate.
                                //sw.WriteLine("cryptoapicert " + eduOpenVPN.Configuration.EscapeParamValue("THUMB:" + BitConverter.ToString(_client_certificate.GetCertHash()).Replace("-", " ")));
                                sw.WriteLine("management-external-cert " + eduOpenVPN.Configuration.EscapeParamValue(_connection_id));
                                sw.WriteLine("management-external-key");

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

                                if (Properties.Settings.Default.OpenVPNAllowLocal)
                                {
                                    // Don't accept block-outside-dns and block-local.
                                    sw.WriteLine("pull-filter ignore \"block-outside-dns\"");
                                    sw.WriteLine("pull-filter ignore \"block-local\"");
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
                                    var mgmt_session = new eduOpenVPN.Management.Session();

                                    // Set event handlers.
                                    mgmt_session.ByteCountReported += (object sender, ByteCountReportedEventArgs e) =>
                                        Wizard.Dispatcher.Invoke(DispatcherPriority.Normal, (Action)(
                                            () =>
                                            {
                                                BytesIn = e.BytesIn;
                                                BytesOut = e.BytesOut;
                                            }));

                                    mgmt_session.FatalErrorReported += (object sender, MessageReportedEventArgs e) =>
                                        Wizard.Dispatcher.Invoke(DispatcherPriority.Normal, (Action)(
                                            () =>
                                            {
                                                State = VPNSessionStatusType.Error;
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

                                    mgmt_session.HoldReported += (object sender, HoldReportedEventArgs e) =>
                                    {
                                        if (!_ignore_hold_hint && e.WaitHint > 0)
                                            _quit.Token.WaitHandle.WaitOne(e.WaitHint * 1000);
                                    };

                                    mgmt_session.CertificateRequested += (object sender, CertificateRequestedEventArgs e) => e.Certificate = _client_certificate;

                                    mgmt_session.PasswordAuthenticationRequested += (object sender, PasswordAuthenticationRequestedEventArgs e) => Wizard.OpenVPNSession_RequestPasswordAuthentication(this, e);

                                    // OpenVPN username/password prompts are actually 2FA for eduVPN use-case. Relay them as such.
                                    mgmt_session.UsernamePasswordAuthenticationRequested += (object sender, UsernamePasswordAuthenticationRequestedEventArgs e) => Wizard.OpenVPNSession_RequestTwoFactorAuthentication(this, e);

                                    mgmt_session.RemoteReported += (object sender, RemoteReportedEventArgs e) =>
                                    {
                                        if (e.Protocol == ProtoType.UDP && Properties.Settings.Default.OpenVPNForceTCP)
                                            e.Action = new RemoteSkipAction();
                                        else
                                            e.Action = new RemoteAcceptAction();
                                    };

                                    mgmt_session.RSASignRequested += (object sender, RSASignRequestedEventArgs e) =>
                                    {
                                        var rsa = (RSACryptoServiceProvider)_client_certificate.PrivateKey;
                                        var RSAFormatter = new RSAPKCS1SignatureFormatter(rsa);

                                        // Parse message.
                                        var stream = new MemoryStream(e.Data);
                                        using (var reader = new BinaryReader(stream))
                                        {
                                            // SEQUENCE(DigestInfo)
                                            if (reader.ReadByte() != 0x30)
                                                throw new InvalidDataException();
                                            long dgi_end = reader.ReadASN1Length() + reader.BaseStream.Position;

                                            // SEQUENCE(AlgorithmIdentifier)
                                            if (reader.ReadByte() != 0x30)
                                                throw new InvalidDataException();
                                            long alg_id_end = reader.ReadASN1Length() + reader.BaseStream.Position;

                                            // OBJECT IDENTIFIER
                                            switch (reader.ReadASN1ObjectID().Value)
                                            {
                                                case "2.16.840.1.101.3.4.2.1": RSAFormatter.SetHashAlgorithm("SHA256"); break;
                                                case "2.16.840.1.101.3.4.2.2": RSAFormatter.SetHashAlgorithm("SHA384"); break;
                                                case "2.16.840.1.101.3.4.2.3": RSAFormatter.SetHashAlgorithm("SHA512"); break;
                                                case "2.16.840.1.101.3.4.2.4": RSAFormatter.SetHashAlgorithm("SHA224"); break;
                                                default: throw new InvalidDataException();
                                            }

                                            reader.BaseStream.Seek(alg_id_end, SeekOrigin.Begin);

                                            // OCTET STRING(Digest)
                                            if (reader.ReadByte() != 0x04)
                                                throw new InvalidDataException();

                                            // Read, sign hash, and return.
                                            e.Signature = RSAFormatter.CreateSignature(reader.ReadBytes(reader.ReadASN1Length()));
                                        }
                                    };

                                    mgmt_session.StateReported += (object sender, StateReportedEventArgs e) =>
                                    {
                                        Wizard.Dispatcher.Invoke(DispatcherPriority.Normal, (Action)(
                                            () =>
                                            {
                                                string msg = null;

                                                switch (e.State)
                                                {
                                                    case OpenVPNStateType.Connecting:
                                                        State = VPNSessionStatusType.Connecting;
                                                        msg = Resources.Strings.OpenVPNStateTypeConnecting;
                                                        break;

                                                    case OpenVPNStateType.Resolving:
                                                        State = VPNSessionStatusType.Connecting;
                                                        msg = Resources.Strings.OpenVPNStateTypeResolving;
                                                        break;

                                                    case OpenVPNStateType.TcpConnecting:
                                                        State = VPNSessionStatusType.Connecting;
                                                        msg = Resources.Strings.OpenVPNStateTypeTcpConnecting;
                                                        break;

                                                    case OpenVPNStateType.Waiting:
                                                        State = VPNSessionStatusType.Connecting;
                                                        msg = Resources.Strings.OpenVPNStateTypeWaiting;
                                                        break;

                                                    case OpenVPNStateType.Authenticating:
                                                        State = VPNSessionStatusType.Connecting;
                                                        msg = Resources.Strings.OpenVPNStateTypeAuthenticating;
                                                        break;

                                                    case OpenVPNStateType.GettingConfiguration:
                                                        State = VPNSessionStatusType.Connecting;
                                                        msg = Resources.Strings.OpenVPNStateTypeGettingConfiguration;
                                                        break;

                                                    case OpenVPNStateType.AssigningIP:
                                                        State = VPNSessionStatusType.Connecting;
                                                        msg = Resources.Strings.OpenVPNStateTypeAssigningIP;
                                                        break;

                                                    case OpenVPNStateType.AddingRoutes:
                                                        State = VPNSessionStatusType.Connecting;
                                                        msg = Resources.Strings.OpenVPNStateTypeAddingRoutes;
                                                        break;

                                                    case OpenVPNStateType.Connected:
                                                        State = VPNSessionStatusType.Connected;
                                                        msg = Resources.Strings.OpenVPNStateTypeConnected;
                                                        break;

                                                    case OpenVPNStateType.Reconnecting:
                                                        State = VPNSessionStatusType.Connecting;
                                                        msg = Resources.Strings.OpenVPNStateTypeReconnecting;
                                                        break;

                                                    case OpenVPNStateType.Exiting:
                                                        State = VPNSessionStatusType.Disconnecting;
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

                                            mgmt_session.QueueReleaseHold(_quit.Token);
                                        }
                                    };

                                    mgmt_session.Start(mgmt_client.GetStream(), mgmt_password, _quit.Token);

                                    // Initialize session and release openvpn.exe to get started.
                                    mgmt_session.ReplayAndEnableState(_quit.Token);
                                    mgmt_session.ReplayAndEnableEcho(_quit.Token);
                                    mgmt_session.SetByteCount(5, _quit.Token);
                                    mgmt_session.ReleaseHold(_quit.Token);

                                    Wizard.Dispatcher.Invoke(DispatcherPriority.Normal, (Action)(() => Wizard.ChangeTaskCount(-1)));
                                    try
                                    {
                                        // Wait for the session to end gracefully.
                                        mgmt_session.Monitor.Join();
                                        if (mgmt_session.Error != null && !(mgmt_session.Error is OperationCanceledException))
                                        {
                                            // Session reported an error. Rethrow it.
                                            throw mgmt_session.Error;
                                        }
                                    } finally { Wizard.Dispatcher.Invoke(DispatcherPriority.Normal, (Action)(() => Wizard.ChangeTaskCount(+1))); }
                                }
                                finally { mgmt_client.Close(); }
                            }
                            finally
                            {
                                Wizard.Dispatcher.Invoke(DispatcherPriority.Normal, (Action)(
                                    () =>
                                    {
                                        // Cleanup status properties.
                                        State = VPNSessionStatusType.Disconnecting;
                                        StateDescription = Resources.Strings.OpenVPNStateTypeExiting;
                                        TunnelAddress = null;
                                        IPv6TunnelAddress = null;
                                        _connected_time_updater.Stop();
                                        ConnectedSince = null;
                                        BytesIn = null;
                                        BytesOut = null;
                                    }));

                                // Wait for openvpn.exe to finish. Maximum 30s.
                                try { Process.GetProcessById(openvpn_interactive_service_connection.ProcessID)?.WaitForExit(30000); }
                                catch (ArgumentException) { }

                                // Delete OpenVPN log file. If possible.
                                try { File.Delete(LogPath); }
                                catch { }
                            }
                        }
                    }
                    finally { mgmt_server.Stop(); }
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
                        State = VPNSessionStatusType.Initializing;
                        StateDescription = "";

                        Wizard.ChangeTaskCount(-1);
                    }));
            }
        }

        /// <inheritdoc/>
        protected override void DoShowLog()
        {
            // Open log file in registered application.
            Process.Start(LogPath);
        }

        /// <inheritdoc/>
        protected override bool CanShowLog()
        {
            return File.Exists(LogPath);
        }

        #endregion
    }
}
