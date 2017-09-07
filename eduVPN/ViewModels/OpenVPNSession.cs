/*
    eduVPN - End-user friendly VPN

    Copyright: 2017, The Commons Conservancy eduVPN Programme
    SPDX-License-Identifier: GPL-3.0+
*/

using eduOpenVPN;
using eduOpenVPN.Management;
using System;
using System.Collections.Generic;
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
using System.Threading;
using System.Web.Security;
using System.Windows.Threading;

namespace eduVPN.ViewModels
{
    /// <summary>
    /// OpenVPN session
    /// </summary>
    public class OpenVPNSession : VPNSession, eduOpenVPN.Management.ISessionNotifications
    {
        #region Fields

        /// <summary>
        /// OpenVPN connection ID
        /// </summary>
        /// <remarks>Connection ID determines .ovpn and .log filenames.</remarks>
        private string _connection_id;

        /// <summary>
        /// OpenVPN working folder
        /// </summary>
        private string _working_folder;

        /// <summary>
        /// Client certificate
        /// </summary>
        private X509Certificate2 _client_certificate;

        #endregion

        #region Properties

        /// <summary>
        /// OpenVPN profile configuration file path
        /// </summary>
        string ConfigurationPath { get => _working_folder + _connection_id + ".ovpn"; }

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
            _connection_id = "eduVPN-" + Guid.NewGuid().ToString();
            _working_folder = Path.GetTempPath();

            // Create dispatcher timer to refresh ShowLog command "can execute" status every second.
            new DispatcherTimer(
                new TimeSpan(0, 0, 0, 1),
                DispatcherPriority.Normal, (object sender, EventArgs e) => ShowLog.RaiseCanExecuteChanged(),
                Parent.Dispatcher).Start();
        }

        #endregion

        #region Methods

        [SuppressMessage("Microsoft.Usage", "CA2202:Do not dispose objects multiple times", Justification = "FileStream tolerates multiple disposes.")]
        public override void Run()
        {
            Parent.Dispatcher.Invoke(DispatcherPriority.Normal, (Action)(() => Parent.ChangeTaskCount(+1)));
            try
            {
                var ct_quit = CancellationTokenSource.CreateLinkedTokenSource(_disconnect.Token, Window.Abort.Token);

                // Check if the Interactive Service is started.
                // In case we hit "Access Denied" (or another error) give up on SCM.
                ServiceController openvpn_interactive_service = new ServiceController("OpenVPNServiceInteractive");
                try
                {
                    if (openvpn_interactive_service.Status == ServiceControllerStatus.Stopped ||
                        openvpn_interactive_service.Status == ServiceControllerStatus.Paused)
                        openvpn_interactive_service.Start();
                }
                catch { openvpn_interactive_service = null; }

                // Get profile's OpenVPN configuration and instance client certificate (in parallel).
                Exception error = null;
                string profile_config = null;
                new List<Action>()
                            {
                                () => { profile_config = Configuration.ConnectingProfile.GetOpenVPNConfig(Configuration.ConnectingInstance, Configuration.AuthenticatingInstance, ct_quit.Token); },
                                () => { _client_certificate = Configuration.ConnectingInstance.GetClientCertificate(Configuration.AuthenticatingInstance, ct_quit.Token); }
                            }.Select(
                    action =>
                    {
                        var t = new Thread(new ThreadStart(
                            () =>
                            {
                                try { action(); }
                                catch (Exception ex) { error = ex; }
                            }));
                        t.Start();
                        return t;
                    }).ToList().ForEach(t => t.Join());
                if (error != null)
                    throw error;

                if (openvpn_interactive_service != null)
                {
                    // Wait for OpenVPN Interactive Service to report "running" status.
                    // Maximum 30 times 100ms = 3s.
                    var refresh_interval = TimeSpan.FromMilliseconds(100);
                    for (var i = 0; i < 30 && !ct_quit.Token.IsCancellationRequested; i++)
                    {
                        try { openvpn_interactive_service.WaitForStatus(ServiceControllerStatus.Running, refresh_interval); }
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
                                sw.Write(profile_config);

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

                                // Configure client certificate.
                                //sw.WriteLine("cryptoapicert " + eduOpenVPN.Configuration.EscapeParamValue("THUMB:" + BitConverter.ToString(_client_certificate.GetCertHash()).Replace("-", " ")));
                                sw.WriteLine("management-external-cert " + eduOpenVPN.Configuration.EscapeParamValue(_connection_id));
                                sw.WriteLine("management-external-key");

                                // Ask when username/password is denied.
                                sw.WriteLine("auth-retry interact");

                                // Set TAP interface to be used.
                                if (Properties.Settings.Default.OpenVPNInterface != null && Properties.Settings.Default.OpenVPNInterface != "")
                                    sw.Write("dev-node " + eduOpenVPN.Configuration.EscapeParamValue(Properties.Settings.Default.OpenVPNInterface) + "\n");
                            }
                        }
                        catch (OperationCanceledException) { throw; }
                        catch (Exception ex) { throw new AggregateException(string.Format(Resources.Strings.ErrorSavingProfileConfiguration, ConfigurationPath), ex); }

                        // Connect to OpenVPN Interactive Service to launch the openvpn.exe.
                        using (var openvpn_interactive_service_connection = new eduOpenVPN.InteractiveService.Session())
                        {
                            var mgmt_password = Membership.GeneratePassword(16, 6);
                            openvpn_interactive_service_connection.Connect(
                                    _working_folder,
                                    new string[] { "--config", _connection_id + ".ovpn", },
                                    mgmt_password + "\n",
                                    3000,
                                    ct_quit.Token);
                            try
                            {
                                // Wait and accept the openvpn.exe on our management interface (--management-client parameter).
                                var mgmt_client_task = mgmt_server.AcceptTcpClientAsync();
                                try { mgmt_client_task.Wait(ct_quit.Token); }
                                catch (AggregateException ex) { throw ex.InnerException; }
                                var mgmt_client = mgmt_client_task.Result;
                                try
                                {
                                    // Start the management session.
                                    var mgmt_session = new eduOpenVPN.Management.Session();
                                    mgmt_session.Start(mgmt_client.GetStream(), mgmt_password, this, ct_quit.Token);

                                    // Initialize session and release openvpn.exe to get started.
                                    mgmt_session.ReplayAndEnableState(ct_quit.Token);
                                    mgmt_session.SetByteCount(5, ct_quit.Token);
                                    mgmt_session.EnableHold(false, ct_quit.Token);
                                    mgmt_session.ReleaseHold(ct_quit.Token);

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
                                        StateDescription = null;
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
                        StateDescription = null;

                        Parent.ChangeTaskCount(-1);
                    }));

                // Signal session finished.
                Finished.Set();
            }
        }

        protected override void DoShowLog()
        {
            // Open log file in registered application.
            Process.Start(LogPath);
        }

        protected override bool CanShowLog()
        {
            return File.Exists(LogPath);
        }

        #endregion

        #region ISessionNotifications Support

        public void OnByteCount(ulong bytes_in, ulong bytes_out)
        {
            Parent.Dispatcher.Invoke(DispatcherPriority.Normal, (Action)(
                () =>
                {
                    BytesIn = bytes_in;
                    BytesOut = bytes_out;
                }));
        }

        public void OnByteCountClient(uint cid, ulong bytes_in, ulong bytes_out)
        {
        }

        public void OnEcho(DateTimeOffset timestamp, string command)
        {
        }

        public void OnFatal(string message)
        {
            Parent.Dispatcher.Invoke(DispatcherPriority.Normal, (Action)(
                () =>
                {
                    State = Models.VPNSessionStatusType.Error;
                    StateDescription = message;
                }));
        }

        public void OnHold(string message, int wait_hint)
        {
        }

        public void OnInfo(string message)
        {
        }

        public void OnLog(DateTimeOffset timestamp, LogMessageFlags flags, string message)
        {
        }

        public X509Certificate2 OnNeedCertificate(string hint)
        {
            return _client_certificate;
        }

        public void OnNeedAuthentication(string realm, out string password)
        {
            // TODO: Implement.
            throw new NotImplementedException();
        }

        public void OnNeedAuthentication(string realm, out string username, out string password)
        {
            // TODO: Implement.
            throw new NotImplementedException();
        }

        public void OnAuthenticationFailed(string realm)
        {
            // TODO: Implement.
            throw new NotImplementedException();
        }

        public byte[] OnRSASign(byte[] data)
        {
            var rsa = (RSACryptoServiceProvider)_client_certificate.PrivateKey;
            var RSAFormatter = new RSAPKCS1SignatureFormatter(rsa);

            // Parse message.
            var stream = new MemoryStream(data);
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
                return RSAFormatter.CreateSignature(reader.ReadBytes(reader.ReadASN1Length()));
            }
        }

        public void OnState(DateTimeOffset timestamp, OpenVPNStateType state, string message, IPAddress tunnel, IPAddress ipv6_tunnel, IPEndPoint remote, IPEndPoint local)
        {
            Parent.Dispatcher.Invoke(DispatcherPriority.Normal, (Action)(
                () =>
                {
                    switch (state)
                    {
                        case OpenVPNStateType.Connecting:
                        case OpenVPNStateType.Waiting:
                        case OpenVPNStateType.Authenticating:
                        case OpenVPNStateType.GettingConfiguration:
                        case OpenVPNStateType.AssigningIP:
                        case OpenVPNStateType.AddingRoutes:
                        case OpenVPNStateType.Reconnecting:
                            State = Models.VPNSessionStatusType.Connecting;
                            break;

                        case OpenVPNStateType.Connected:
                            State = Models.VPNSessionStatusType.Connected;
                            break;

                        case OpenVPNStateType.Exiting:
                            State = Models.VPNSessionStatusType.Disconnecting;
                            break;

                        case OpenVPNStateType.FatalError:
                            State = Models.VPNSessionStatusType.Error;
                            break;
                    }
                    StateDescription = message;
                    TunnelAddress = tunnel;
                    IPv6TunnelAddress = ipv6_tunnel;

                    // Update connected time.
                    if (state == OpenVPNStateType.Connected)
                    {
                        ConnectedSince = timestamp;
                        _connected_time_updater.Start();
                    }
                    else
                    {
                        _connected_time_updater.Stop();
                        ConnectedSince = null;
                    }
                }));
        }

        #endregion
    }
}
