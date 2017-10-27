/*
    eduVPN - End-user friendly VPN

    Copyright: 2017, The Commons Conservancy eduVPN Programme
    SPDX-License-Identifier: GPL-3.0+
*/

using eduOpenVPN;
using eduOpenVPN.Management;
using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.ServiceProcess;
using System.Web.Security;
using System.Windows.Threading;

namespace eduVPN.ViewModels
{
    /// <summary>
    /// OpenVPN session
    /// </summary>
    public class OpenVPNSession : VPNSession
    {
        #region Fields

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
        /// OpenVPN Interactive Service Controller
        /// </summary>
        private ServiceController _openvpn_interactive_service;

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
        public OpenVPNSession(ConnectWizard parent, Models.VPNConfiguration configuration) :
            base(parent, configuration)
        {
            _working_folder = Path.GetTempPath();
            _connection_id = "eduVPN-" + Guid.NewGuid().ToString();

            // Create dispatcher timer to refresh ShowLog command "can execute" status every second.
            new DispatcherTimer(
                new TimeSpan(0, 0, 0, 1),
                DispatcherPriority.Normal, (object sender, EventArgs e) => ShowLog.RaiseCanExecuteChanged(),
                Parent.Dispatcher).Start();

            _pre_run_actions.Add(() =>
            {
                // Get profile's OpenVPN configuration.
                _profile_config = Configuration.ConnectingProfile.GetOpenVPNConfig(Configuration.ConnectingInstance, _quit.Token);
            });

            _pre_run_actions.Add(() =>
            {
                // Get instance client certificate.
                _client_certificate = Configuration.ConnectingInstance.GetClientCertificate(Configuration.AuthenticatingInstance, _quit.Token);
            });

            _openvpn_interactive_service = new ServiceController("eduVPNServiceInteractive");
            _pre_run_actions.Add(() =>
            {
                try
                {
                    // Check if the Interactive Service is started.
                    // In case we hit "Access Denied" (or another error) give up on SCM.
                    if (_openvpn_interactive_service.Status == ServiceControllerStatus.Stopped ||
                        _openvpn_interactive_service.Status == ServiceControllerStatus.Paused)
                    {
                        _openvpn_interactive_service.Start();
                    }
                }
                catch { _openvpn_interactive_service = null; }
            });
        }

        #endregion

        #region Methods

        /// <inheritdoc/>
        [SuppressMessage("Microsoft.Usage", "CA2202:Do not dispose objects multiple times", Justification = "FileStream tolerates multiple disposes.")]
        protected override void DoRun()
        {
            Parent.Dispatcher.Invoke(DispatcherPriority.Normal, (Action)(() => Parent.ChangeTaskCount(+1)));
            try
            {
                if (_openvpn_interactive_service != null)
                {
                    // Wait for OpenVPN Interactive Service to report "running" status.
                    // Maximum 30 times 100ms = 3s.
                    var refresh_interval = TimeSpan.FromMilliseconds(100);
                    for (var i = 0; i < 30 && !_quit.Token.IsCancellationRequested; i++)
                    {
                        try
                        {
                            _openvpn_interactive_service.WaitForStatus(ServiceControllerStatus.Running, refresh_interval);
                            break;
                        }
                        catch (System.ServiceProcess.TimeoutException) { }
                        catch { break; }
                    }
                }

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
                                sw.Write(_profile_config);

                                // Append eduVPN Client specific configuration directives.
                                sw.WriteLine();
                                sw.WriteLine();
                                sw.WriteLine("# eduVPN Client for Windows");

                                // Introduce ourself (to OpenVPN server).
                                var assembly = Assembly.GetExecutingAssembly();
                                var assembly_title_attribute = Attribute.GetCustomAttributes(assembly, typeof(AssemblyTitleAttribute)).SingleOrDefault() as AssemblyTitleAttribute;
                                var assembly_informational_version = Attribute.GetCustomAttributes(assembly, typeof(AssemblyInformationalVersionAttribute)).SingleOrDefault() as AssemblyInformationalVersionAttribute;
                                sw.WriteLine("setenv IV_GUI_VER " + eduOpenVPN.Configuration.EscapeParamValue(assembly_title_attribute?.Title + " " + assembly_informational_version?.InformationalVersion));

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

                                // Set TAP interface to be used.
                                if (Models.InterfaceInfo.TryFromID(Properties.Settings.Default.OpenVPNInterfaceID, out var iface))
                                    sw.Write("dev-node " + eduOpenVPN.Configuration.EscapeParamValue(iface.Name) + "\n");

#if DEBUG
                                // Renegotiate data channel every 5 minutes in debug versions.
                                sw.WriteLine("reneg-sec 300");
#endif
                            }
                        }
                        catch (OperationCanceledException) { throw; }
                        catch (Exception ex) { throw new AggregateException(string.Format(Resources.Strings.ErrorSavingProfileConfiguration, ConfigurationPath), ex); }

                        // Connect to OpenVPN Interactive Service to launch the openvpn.exe.
                        using (var openvpn_interactive_service_connection = new eduOpenVPN.InteractiveService.Session())
                        {
                            var mgmt_password = Membership.GeneratePassword(16, 6);
                            try
                            {
                                openvpn_interactive_service_connection.Connect(
                                    "eduvpn\\service",
                                    _working_folder,
                                    new string[] { "--config", _connection_id + ".conf", },
                                    mgmt_password + "\n",
                                    3000,
                                    _quit.Token);
                            }
                            catch (Exception ex) { throw new AggregateException(Resources.Strings.ErrorInteractiveService, ex); }

                            try
                            {
                                // Wait and accept the openvpn.exe on our management interface (--management-client parameter).
                                var mgmt_client_task = mgmt_server.AcceptTcpClientAsync();
                                try { mgmt_client_task.Wait(_quit.Token); }
                                catch (AggregateException ex) { throw ex.InnerException; }
                                var mgmt_client = mgmt_client_task.Result;
                                try
                                {
                                    // Start the management session.
                                    var mgmt_session = new eduOpenVPN.Management.Session();

                                    // Set event handlers.
                                    mgmt_session.ByteCountReported += (object sender, ByteCountReportedEventArgs e) =>
                                        Parent.Dispatcher.Invoke(DispatcherPriority.Normal, (Action)(
                                            () =>
                                            {
                                                BytesIn = e.BytesIn;
                                                BytesOut = e.BytesOut;
                                            }));

                                    mgmt_session.FatalErrorReported += (object sender, MessageReportedEventArgs e) =>
                                        Parent.Dispatcher.Invoke(DispatcherPriority.Normal, (Action)(
                                            () =>
                                            {
                                                State = Models.VPNSessionStatusType.Error;
                                                var msg = Resources.Strings.OpenVPNStateTypeFatalError;
                                                if (e.Message != null && e.Message.Length > 0)
                                                {
                                                    // Append OpenVPN message.
                                                    msg += "\r\n" + e.Message;
                                                }
                                                StateDescription = msg;
                                            }));

                                    mgmt_session.HoldReported += (object sender, HoldReportedEventArgs e) =>
                                    {
                                        if (!_ignore_hold_hint && e.WaitHint > 0)
                                            _quit.Token.WaitHandle.WaitOne(e.WaitHint * 1000);
                                    };

                                    mgmt_session.CertificateRequested += (object sender, CertificateRequestedEventArgs e) => e.Certificate = _client_certificate;

                                    mgmt_session.PasswordAuthenticationRequested += (object sender, PasswordAuthenticationRequestedEventArgs e) => Parent.OpenVPNSession_RequestPasswordAuthentication(this, e);

                                    // OpenVPN username/password prompts are actually 2FA for eduVPN use-case. Relay them as such.
                                    mgmt_session.UsernamePasswordAuthenticationRequested += (object sender, UsernamePasswordAuthenticationRequestedEventArgs e) => Parent.OpenVPNSession_RequestTwoFactorAuthentication(this, e);

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
                                        Parent.Dispatcher.Invoke(DispatcherPriority.Normal, (Action)(
                                            () =>
                                            {
                                                string msg = null;

                                                switch (e.State)
                                                {
                                                    case OpenVPNStateType.Connecting:
                                                        State = Models.VPNSessionStatusType.Connecting;
                                                        msg = Resources.Strings.OpenVPNStateTypeConnecting;
                                                        break;

                                                    case OpenVPNStateType.Waiting:
                                                        State = Models.VPNSessionStatusType.Connecting;
                                                        msg = Resources.Strings.OpenVPNStateTypeWaiting;
                                                        break;

                                                    case OpenVPNStateType.Authenticating:
                                                        State = Models.VPNSessionStatusType.Connecting;
                                                        msg = Resources.Strings.OpenVPNStateTypeAuthenticating;
                                                        break;

                                                    case OpenVPNStateType.GettingConfiguration:
                                                        State = Models.VPNSessionStatusType.Connecting;
                                                        msg = Resources.Strings.OpenVPNStateTypeGettingConfiguration;
                                                        break;

                                                    case OpenVPNStateType.AssigningIP:
                                                        State = Models.VPNSessionStatusType.Connecting;
                                                        msg = Resources.Strings.OpenVPNStateTypeAssigningIP;
                                                        break;

                                                    case OpenVPNStateType.AddingRoutes:
                                                        State = Models.VPNSessionStatusType.Connecting;
                                                        msg = Resources.Strings.OpenVPNStateTypeAddingRoutes;
                                                        break;

                                                    case OpenVPNStateType.Connected:
                                                        State = Models.VPNSessionStatusType.Connected;
                                                        msg = Resources.Strings.OpenVPNStateTypeConnected;
                                                        break;

                                                    case OpenVPNStateType.Reconnecting:
                                                        State = Models.VPNSessionStatusType.Connecting;
                                                        msg = Resources.Strings.OpenVPNStateTypeReconnecting;
                                                        break;

                                                    case OpenVPNStateType.Exiting:
                                                        State = Models.VPNSessionStatusType.Disconnecting;
                                                        msg = Resources.Strings.OpenVPNStateTypeExiting;
                                                        break;

                                                    case OpenVPNStateType.FatalError:
                                                        State = Models.VPNSessionStatusType.Error;
                                                        msg = Resources.Strings.OpenVPNStateTypeFatalError;
                                                        break;
                                                }

                                                if (!string.IsNullOrEmpty(e.Message))
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
                                            if (e.Message == "tls-error")
                                            {
                                                // TLS negotiation failed. Perhaps the cause was our cached password. Refresh it.
                                                _client_certificate = Configuration.ConnectingInstance.RefreshClientCertificate(Configuration.AuthenticatingInstance, _quit.Token);
                                                _ignore_hold_hint = true;
                                            }
                                            else
                                                _ignore_hold_hint = false;

                                            mgmt_session.QueueReleaseHold(_quit.Token);
                                        }
                                    };

                                    mgmt_session.Start(mgmt_client.GetStream(), mgmt_password, _quit.Token);

                                    // Initialize session and release openvpn.exe to get started.
                                    mgmt_session.ReplayAndEnableState(_quit.Token);
                                    mgmt_session.ReplayAndEnableEcho(_quit.Token);
                                    mgmt_session.SetByteCount(5, _quit.Token);
                                    mgmt_session.ReleaseHold(_quit.Token);

                                    Parent.Dispatcher.Invoke(DispatcherPriority.Normal, (Action)(() => Parent.ChangeTaskCount(-1)));
                                    try
                                    {
                                        // Wait for the session to end gracefully.
                                        mgmt_session.Monitor.Join();
                                    } finally { Parent.Dispatcher.Invoke(DispatcherPriority.Normal, (Action)(() => Parent.ChangeTaskCount(+1))); }
                                }
                                finally { mgmt_client.Close(); }
                            }
                            finally
                            {
                                Parent.Dispatcher.Invoke(DispatcherPriority.Normal, (Action)(
                                    () =>
                                    {
                                        // Cleanup status properties.
                                        State = Models.VPNSessionStatusType.Disconnecting;
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
                Parent.Dispatcher.Invoke(DispatcherPriority.Normal, (Action)(
                    () =>
                    {
                        // Cleanup status properties.
                        State = Models.VPNSessionStatusType.Initializing;
                        StateDescription = "";

                        Parent.ChangeTaskCount(-1);
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
