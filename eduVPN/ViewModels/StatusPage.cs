/*
    eduVPN - End-user friendly VPN

    Copyright: 2017, The Commons Conservancy eduVPN Programme
    SPDX-License-Identifier: GPL-3.0+
*/

using eduOpenVPN;
using eduOpenVPN.Management;
using eduVPN.JSON;
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
    public class StatusPage : ConnectWizardPage, ISessionNotifications, IDisposable
    {
        #region Fields

        /// <summary>
        /// Disconnect cancellation token
        /// </summary>
        private CancellationTokenSource _disconnect;

        /// <summary>
        /// Client certificate
        /// </summary>
        private X509Certificate2 _client_certificate;

        #endregion

        #region Properties

        /// <summary>
        /// Merged list of user and system messages
        /// </summary>
        public Models.MessageList MessageList
        {
            get { return _message_list; }
            set { _message_list = value; RaisePropertyChanged(); }
        }
        private Models.MessageList _message_list;

        /// <summary>
        /// Client connection state
        /// </summary>
        public OpenVPNStateType State
        {
            get { return _state; }
            set { if (value != _state) { _state = value; RaisePropertyChanged(); } }
        }
        private OpenVPNStateType _state;

        /// <summary>
        /// Descriptive string (used mostly on <c>StateType.Reconnecting</c> and <c>StateType.Exiting</c> to show the reason for the disconnect)
        /// </summary>
        public string StateDescription
        {
            get { return _state_description; }
            set { if (value != _state_description) { _state_description = value; RaisePropertyChanged(); } }
        }
        private string _state_description;

        /// <summary>
        /// TUN/TAP local IPv4 address
        /// </summary>
        public IPAddress TunnelAddress
        {
            get { return _tunnel_address; }
            set { if (value != _tunnel_address) { _tunnel_address = value; RaisePropertyChanged(); } }
        }
        private IPAddress _tunnel_address;

        /// <summary>
        /// TUN/TAP local IPv6 address
        /// </summary>
        private IPAddress IPv6TunnelAddress
        {
            get { return _ipv6_tunnel_address; }
            set { if (value != _ipv6_tunnel_address) { _ipv6_tunnel_address = value; RaisePropertyChanged(); } }
        }
        private IPAddress _ipv6_tunnel_address;

        /// <summary>
        /// Time when connected state recorded
        /// </summary>
        public DateTimeOffset? ConnectedSince
        {
            get { return _connected_since; }
            set { if (value != _connected_since) { _connected_since = value; RaisePropertyChanged(); } }
        }
        private DateTimeOffset? _connected_since;

        /// <summary>
        /// Number of bytes that have been received from the server
        /// </summary>
        public ulong BytesIn
        {
            get { return _bytes_in; }
            set { if (value != _bytes_in) { _bytes_in = value; RaisePropertyChanged(); } }
        }
        private ulong _bytes_in;

        /// <summary>
        /// Number of bytes that have been sent to the server
        /// </summary>
        public ulong BytesOut
        {
            get { return _bytes_out; }
            set { if (value != _bytes_out) { _bytes_out = value; RaisePropertyChanged(); } }
        }
        private ulong _bytes_out;

        #endregion

        #region Constructors

        /// <summary>
        /// Constructs a status wizard page
        /// </summary>
        /// <param name="parent"></param>
        public StatusPage(ConnectWizard parent) :
            base(parent)
        {
        }

        #endregion

        #region Methods

        [SuppressMessage("Microsoft.Usage", "CA2202:Do not dispose objects multiple times", Justification = "FileStream tolerates multiple disposes.")]
        public override void OnActivate()
        {
            base.OnActivate();

            _disconnect = new CancellationTokenSource();
            var ct_quit = CancellationTokenSource.CreateLinkedTokenSource(_disconnect.Token, ConnectWizard.Abort.Token);

            MessageList = new Models.MessageList();

            // Load messages from all possible sources: authenticating/connecting instance, user/system list.
            // Any errors shall be ignored.
            var api_authenticating = Parent.AuthenticatingInstance.GetEndpoints(ct_quit.Token);
            var api_connecting = Parent.ConnectingInstance.GetEndpoints(ct_quit.Token);
            foreach (
                var list in new List<KeyValuePair<Uri, string>>() {
                    new KeyValuePair<Uri, string>(api_authenticating.UserMessages, "user_messages"),
                    new KeyValuePair<Uri, string>(api_connecting.UserMessages, "user_messages"),
                    new KeyValuePair<Uri, string>(api_authenticating.SystemMessages, "system_messages"),
                    new KeyValuePair<Uri, string>(api_connecting.SystemMessages, "system_messages"),
                }
                .Where(list => list.Key != null)
                .Distinct(new EqualityComparer<KeyValuePair<Uri, string>>((x, y) => x.Key.AbsoluteUri == y.Key.AbsoluteUri && x.Value == y.Value)))
            {
                new Thread(new ThreadStart(
                    () =>
                    {
                        Parent.Dispatcher.Invoke(DispatcherPriority.Normal, (Action)(() => TaskCount++));
                        try
                        {
                            // Get and load user messages.
                            var message_list = new Models.MessageList();
                            message_list.LoadJSONAPIResponse(
                                JSON.Response.Get(
                                    uri: list.Key,
                                    token: Parent.AccessToken,
                                    ct: ct_quit.Token).Value,
                                list.Value,
                                ct_quit.Token);

                            if (message_list.Count > 0)
                            {
                                // Add user messages.
                                Parent.Dispatcher.Invoke(DispatcherPriority.Normal, (Action)(() =>
                                {
                                    foreach (var msg in message_list)
                                        MessageList.Add(msg);
                                }));
                            }
                        }
                        catch (Exception) { }
                        finally { Parent.Dispatcher.Invoke(DispatcherPriority.Normal, (Action)(() => TaskCount--)); }
                    })).Start();
            }

            //// Add test messages.
            //Parent.Dispatcher.Invoke(DispatcherPriority.Normal, (Action)(() =>
            //{
            //    MessageList.Add(new Models.MessageMaintenance()
            //    {
            //        Text = "This is a test maintenance message.",
            //        Date = DateTime.Now,
            //        Begin = new DateTime(2017, 7, 31, 22, 00, 00),
            //        End = new DateTime(2017, 7, 31, 23, 59, 00)
            //    });
            //}));

            // Launch VPN connecting task in the background.
            new Thread(new ThreadStart(
                () =>
                {
                    Parent.Dispatcher.Invoke(DispatcherPriority.Normal, (Action)(() => Error = null));
                    Parent.Dispatcher.Invoke(DispatcherPriority.Normal, (Action)(() => TaskCount++));
                    try
                    {
                        // Check if the Interactive Service is started.
                        // In case we hit "Access Denied" (or another error) give up on SCM.
                        ServiceController openvpn_interactive_service = new ServiceController("OpenVPNServiceInteractive");
                        try
                        {
                            if (openvpn_interactive_service.Status == ServiceControllerStatus.Stopped ||
                                openvpn_interactive_service.Status == ServiceControllerStatus.Paused)
                                openvpn_interactive_service.Start();
                        }
                        catch (Exception) { openvpn_interactive_service = null; }

                        // Get profile's OpenVPN configuration and instance client certificate (in parallel).
                        string profile_config = null;
                        new List<Action>()
                        {
                            () => { profile_config = Parent.ConnectingProfile.GetOpenVPNConfig(Parent.ConnectingInstance, Parent.AccessToken, ct_quit.Token); },
                            () => { _client_certificate = Parent.ConnectingInstance.GetClientCertificate(Parent.AccessToken, ct_quit.Token); }
                        }.Select(
                            action =>
                            {
                                var t = new Thread(new ThreadStart(
                                    () =>
                                    {
                                        try { action(); }
                                        catch (OperationCanceledException) { }
                                        catch (Exception ex) { Parent.Dispatcher.Invoke(DispatcherPriority.Normal, (Action)(() => Error = ex)); }
                                    }));
                                t.Start();
                                return t;
                            }).ToList().ForEach(t => t.Join());
                        if (Error != null)
                            return;

                        if (openvpn_interactive_service != null) {
                            // Wait for OpenVPN Interactive Service to report "running" status.
                            // Maximum 30 times 100ms = 3s.
                            var refresh_interval = TimeSpan.FromMilliseconds(100);
                            for (var i = 0; i < 30 && !ct_quit.Token.IsCancellationRequested; i++)
                            {
                                try { openvpn_interactive_service.WaitForStatus(ServiceControllerStatus.Running, refresh_interval); }
                                catch (System.ServiceProcess.TimeoutException) { }
                                catch (Exception) { break; }
                            }
                        }

                        var working_folder = Path.GetTempPath();
                        var connection_id = "eduVPN-" + Guid.NewGuid().ToString();
                        var profile_config_path = working_folder + connection_id + ".ovpn";
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
                                        profile_config_path,
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

                                        // Configure log file (relative to working_folder).
                                        sw.WriteLine("log " + eduOpenVPN.Configuration.EscapeParamValue(connection_id + ".txt"));

                                        // Configure interaction between us and openvpn.exe.
                                        sw.WriteLine("management " + eduOpenVPN.Configuration.EscapeParamValue(((IPEndPoint)mgmt_server.LocalEndpoint).Address.ToString()) + " " + eduOpenVPN.Configuration.EscapeParamValue(((IPEndPoint)mgmt_server.LocalEndpoint).Port.ToString()) + " stdin");
                                        sw.WriteLine("management-client");
                                        sw.WriteLine("management-hold");
                                        sw.WriteLine("management-query-passwords");

                                        // Configure client certificate.
                                        //sw.WriteLine("cryptoapicert " + eduOpenVPN.Configuration.EscapeParamValue("THUMB:" + BitConverter.ToString(_client_certificate.GetCertHash()).Replace("-", " ")));
                                        sw.WriteLine("management-external-cert " + eduOpenVPN.Configuration.EscapeParamValue(connection_id));
                                        sw.WriteLine("management-external-key");

                                        // Ask when username/password is denied.
                                        sw.WriteLine("auth-retry interact");

                                        // Set TAP interface to be used.
                                        if (Properties.Settings.Default.OpenVPNInterface != null && Properties.Settings.Default.OpenVPNInterface != "")
                                            sw.Write("dev-node " + eduOpenVPN.Configuration.EscapeParamValue(Properties.Settings.Default.OpenVPNInterface) + "\n");
                                    }
                                }
                                catch (OperationCanceledException) { throw; }
                                catch (Exception ex) { throw new AggregateException(string.Format(Resources.Strings.ErrorSavingProfileConfiguration, profile_config_path), ex); }

                                // Connect to OpenVPN Interactive Service to launch the openvpn.exe.
                                using (var openvpn_interactive_service_connection = new eduOpenVPN.InteractiveService.Session())
                                {
                                    var mgmt_password = Membership.GeneratePassword(16, 6);
                                    openvpn_interactive_service_connection.Connect(
                                            working_folder,
                                            new string[] { "--config", connection_id + ".ovpn", },
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

                                            // Wait for the session to end gracefully.
                                            Parent.Dispatcher.Invoke(DispatcherPriority.Normal, (Action)(() => TaskCount--));
                                            try { mgmt_session.Monitor.Join(); }
                                            finally { Parent.Dispatcher.Invoke(DispatcherPriority.Normal, (Action)(() => TaskCount++)); }
                                        }
                                        finally { mgmt_client.Close(); }
                                    }
                                    finally
                                    {
                                        // Disconnect from OpenVPN interactive service.
                                        openvpn_interactive_service_connection.Disconnect();

                                        // Wait for openvpn.exe to finish. Maximum 5s.
                                        Process.GetProcessById(openvpn_interactive_service_connection.ProcessID)?.WaitForExit(5000);

                                        // Delete OpenVPN log file. If possible.
                                        try { File.Delete(working_folder + connection_id + ".txt"); }
                                        catch (Exception) { }
                                    }
                                }
                            }
                            finally { mgmt_server.Stop(); }
                        }
                        finally
                        {
                            // Delete profile configuration file. If possible.
                            try { File.Delete(profile_config_path); }
                            catch (Exception) { }
                        }
                    }
                    catch (OperationCanceledException) { }
                    catch (Exception ex) { Parent.Dispatcher.Invoke(DispatcherPriority.Normal, (Action)(() => { Error = ex; })); }
                    finally { Parent.Dispatcher.Invoke(DispatcherPriority.Normal, (Action)(() => TaskCount--)); }
                })).Start();
        }

        protected override void DoNavigateBack()
        {
            // Terminate connection.
            _disconnect.Cancel();

            if (Parent.InstanceList is Models.InstanceInfoFederatedList)
                Parent.CurrentPage = Parent.InstanceAndProfileSelectPage;
            else if (Parent.InstanceList is Models.InstanceInfoDistributedList)
                Parent.CurrentPage = Parent.InstanceAndProfileSelectPage;
            else
                Parent.CurrentPage = Parent.ProfileSelectPage;
        }

        protected override bool CanNavigateBack()
        {
            return true;
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
                    State = OpenVPNStateType.FatalError;
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
                    State = state;
                    StateDescription = message;
                    TunnelAddress = tunnel;
                    IPv6TunnelAddress = ipv6_tunnel;
                    ConnectedSince = state == OpenVPNStateType.Connected ? timestamp : (DateTimeOffset?)null;
                }));
        }

        #endregion

        #region IDisposable Support
        private bool disposedValue = false; // To detect redundant calls

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                    _disconnect.Dispose();

                disposedValue = true;
            }
        }

        // This code added to correctly implement the disposable pattern.
        public void Dispose()
        {
            // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
            Dispose(true);
        }
        #endregion
    }
}
