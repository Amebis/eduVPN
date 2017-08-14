/*
    eduVPN - End-user friendly VPN

    Copyright: 2017, The Commons Conservancy eduVPN Programme
    SPDX-License-Identifier: GPL-3.0+
*/

using eduVPN.JSON;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.IO.Pipes;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.ServiceProcess;
using System.Text;
using System.Threading;
using System.Web.Security;
using System.Windows.Threading;

namespace eduVPN.ViewModels
{
    public class StatusPage : ConnectWizardPage
    {
        #region Properties

        /// <summary>
        /// Client connection state
        /// </summary>
        public Models.StatusType State
        {
            get { return _state; }
            set { if (value != _state) { _state = value; RaisePropertyChanged(); } }
        }
        private Models.StatusType _state;

        /// <summary>
        /// Merged list of user and system messages
        /// </summary>
        public Models.MessageList MessageList
        {
            get { return _message_list; }
            set { _message_list = value; RaisePropertyChanged(); }
        }
        private Models.MessageList _message_list;

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

            // State >> Initializing...
            State = Models.StatusType.Initializing;
            MessageList = new Models.MessageList();

            // Load messages from all possible sources: authenticating/connecting instance, user/system list.
            // Any errors shall be ignored.
            var api_authenticating = Parent.AuthenticatingInstance.GetEndpoints(ConnectWizard.Abort.Token);
            var api_connecting = Parent.ConnectingInstance.GetEndpoints(ConnectWizard.Abort.Token);
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
                                    ct: ConnectWizard.Abort.Token).Value,
                                list.Value,
                                ConnectWizard.Abort.Token);

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
                        byte[] client_certificate_hash = null;
                        new List<Action>()
                        {
                            () => { profile_config = Parent.ConnectingProfile.GetOpenVPNConfig(Parent.ConnectingInstance, Parent.AccessToken, ConnectWizard.Abort.Token); },
                            () => { client_certificate_hash = Parent.ConnectingInstance.GetClientCertificate(Parent.AccessToken, ConnectWizard.Abort.Token); }
                        }.Select(
                            action =>
                            {
                                var t = new Thread(new ThreadStart(
                                    () =>
                                    {
                                        try
                                        {
                                            action();
                                        }
                                        catch (OperationCanceledException) { }
                                        catch (Exception ex)
                                        {
                                            Parent.Dispatcher.Invoke(DispatcherPriority.Normal, (Action)(() =>
                                            {
                                                Error = ex;
                                                State = Models.StatusType.Error;
                                            }));
                                        }
                                    }));
                                t.Start();
                                return t;
                            }).ToList().ForEach(t => t.Join());
                        if (Error != null)
                            return;

                        if (openvpn_interactive_service != null) {
                            // Wait for OpenVPN Interactive Service to report "running" status.
                            // Maximum 30 times 100µs = 3s.
                            var refresh_interval = new TimeSpan(1000);
                            for (var i = 0; i < 30 && !ConnectWizard.Abort.Token.IsCancellationRequested; i++)
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
                            try
                            {
                                // Create OpenVPN configuration file.
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
                                    sw.Write(
                                        "\n\n# eduVPN Client for Windows\n" +
                                        "cryptoapicert \"THUMB: " + BitConverter.ToString(client_certificate_hash).Replace("-", " ") + "\"\n");

                                    if (Properties.Settings.Default.OpenVPNInterface != null && Properties.Settings.Default.OpenVPNInterface != "")
                                        sw.Write("dev-node " + eduOpenVPN.Configuration.EscapeParamValue(Properties.Settings.Default.OpenVPNInterface) + "\n");
                                }
                            }
                            catch (OperationCanceledException) { throw; }
                            catch (Exception ex) { throw new AggregateException(string.Format(Resources.Strings.ErrorSavingProfileConfiguration, profile_config_path), ex); }

                            var openvpn_quit_event = new EventWaitHandle(false, EventResetMode.ManualReset, connection_id);
                            try
                            {
                                // Get management interface and TCP port.
                                var mgmt_interface = IPAddress.Loopback;
                                var mgmt_port = 0;
                                {
                                    var listener = new TcpListener(mgmt_interface, mgmt_port);
                                    listener.Start();
                                    mgmt_port = ((IPEndPoint)listener.LocalEndpoint).Port;
                                    listener.Stop();
                                }

                                // Generate management password.
                                var mgmt_password = Membership.GeneratePassword(16, 6);

                                using (var openvpn_interactive_service_connection = new eduOpenVPN.InteractiveService.Session())
                                {
                                    // Connect to OpenVPN Interactive Service.
                                    openvpn_interactive_service_connection.Connect(3000);

                                    try {
                                        // Ask OpenVPN Interactive Service to launch openvpn.exe.
                                        var assembly = Assembly.GetExecutingAssembly();
                                        var assembly_title_attribute = Attribute.GetCustomAttributes(assembly, typeof(AssemblyTitleAttribute)).SingleOrDefault() as AssemblyTitleAttribute;
                                        var assembly_informational_version = Attribute.GetCustomAttributes(assembly, typeof(AssemblyInformationalVersionAttribute)).SingleOrDefault() as AssemblyInformationalVersionAttribute;
                                        var pid = openvpn_interactive_service_connection.RunOpenVPN(
                                            working_folder,
                                            new string[]
                                            {
                                                "--log", connection_id + ".txt",
                                                "--config", connection_id + ".ovpn",
                                                "--setenv", "IV_GUI_VER", assembly_title_attribute?.Title + " " + assembly_informational_version?.InformationalVersion,
                                                "--service", connection_id, "0",
                                                "--auth-retry", "interact",
                                                "--management", mgmt_interface.ToString(), mgmt_port.ToString(), "stdin",
                                                "--management-query-passwords",
                                                "--management-hold",
                                            },
                                            mgmt_password + "\n");
                                    }
                                    finally
                                    {
                                        // Delete OpenVPN log file. If possible.
                                        try { File.Delete(working_folder + connection_id + ".txt"); }
                                        catch (Exception) { }
                                    }
                                }
                            }
                            finally { openvpn_quit_event.Set(); }
                        }
                        finally
                        {
                            // Delete profile configuration file. If possible.
                            try { File.Delete(profile_config_path); }
                            catch (Exception) { }
                        }

                        // State >> Connecting...
                        Parent.Dispatcher.Invoke(DispatcherPriority.Normal, (Action)(() => State = Models.StatusType.Connecting));

                        // Wait for three seconds, then switch to connected state.
                        if (ConnectWizard.Abort.Token.WaitHandle.WaitOne(1000 * 3)) return;
                        Parent.Dispatcher.Invoke(DispatcherPriority.Normal, (Action)(() => State = Models.StatusType.Connected));
                    }
                    catch (OperationCanceledException) { }
                    catch (Exception ex) {
                        Parent.Dispatcher.Invoke(DispatcherPriority.Normal, (Action)(() =>
                        {
                            Error = ex;
                            State = Models.StatusType.Error;
                        }));
                    }
                    finally { Parent.Dispatcher.Invoke(DispatcherPriority.Normal, (Action)(() => TaskCount--)); }
                })).Start();
        }

        protected override void DoNavigateBack()
        {
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
    }
}
