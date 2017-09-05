/*
    eduVPN - End-user friendly VPN

    Copyright: 2017, The Commons Conservancy eduVPN Programme
    SPDX-License-Identifier: GPL-3.0+
*/

using eduVPN.JSON;
using Prism.Commands;
using Prism.Mvvm;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading;
using System.Windows.Threading;

namespace eduVPN.Models
{
    /// <summary>
    /// VPN session base class
    /// </summary>
    public class VPNSession : BindableBase, IDisposable
    {
        #region Fields

        /// <summary>
        /// Terminate connection token
        /// </summary>
        protected CancellationTokenSource _disconnect;

        #endregion

        #region Properties

        /// <summary>
        /// The session parent
        /// </summary>
        public ViewModels.ConnectWizard Parent { get; }

        /// <summary>
        /// VPN configuration
        /// </summary>
        public VPNConfiguration Configuration { get; }

        /// <summary>
        /// Event to signal VPN session finished
        /// </summary>
        public EventWaitHandle Finished { get => _finished; }
        private EventWaitHandle _finished;

        /// <summary>
        /// User info
        /// </summary>
        public Models.UserInfo UserInfo
        {
            get { return _user_info; }
            set { if (value != _user_info) { _user_info = value; RaisePropertyChanged(); } }
        }
        private Models.UserInfo _user_info;

        /// <summary>
        /// Merged list of user and system messages
        /// </summary>
        public Models.MessageList MessageList
        {
            get { return _message_list; }
            set { if (value != _message_list) { _message_list = value; RaisePropertyChanged(); } }
        }
        private Models.MessageList _message_list;

        /// <summary>
        /// Client connection state
        /// </summary>
        public VPNSessionStatusType State
        {
            get { return _state; }
            set { if (value != _state) { _state = value; RaisePropertyChanged(); } }
        }
        private VPNSessionStatusType _state;

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
        /// <remarks><c>null</c> when not connected</remarks>
        public IPAddress TunnelAddress
        {
            get { return _tunnel_address; }
            set { if (value != _tunnel_address) { _tunnel_address = value; RaisePropertyChanged(); } }
        }
        private IPAddress _tunnel_address;

        /// <summary>
        /// TUN/TAP local IPv6 address
        /// </summary>
        /// <remarks><c>null</c> when not connected</remarks>
        public IPAddress IPv6TunnelAddress
        {
            get { return _ipv6_tunnel_address; }
            set { if (value != _ipv6_tunnel_address) { _ipv6_tunnel_address = value; RaisePropertyChanged(); } }
        }
        private IPAddress _ipv6_tunnel_address;

        /// <summary>
        /// Time when connected state recorded
        /// </summary>
        /// <remarks><c>null</c> when not connected</remarks>
        public DateTimeOffset? ConnectedSince
        {
            get { return _connected_since; }
            set
            {
                if (value != _connected_since)
                {
                    _connected_since = value;
                    RaisePropertyChanged();
                    RaisePropertyChanged("ConnectedTime");
                }
            }
        }
        private DateTimeOffset? _connected_since;

        /// <summary>
        /// Running time connected
        /// </summary>
        /// <remarks><c>null</c> when not connected</remarks>
        public TimeSpan? ConnectedTime
        {
            get { return _connected_since != null ? DateTimeOffset.UtcNow - _connected_since : null; }
        }
        protected DispatcherTimer _connected_time_updater;

        /// <summary>
        /// Number of bytes that have been received from the server
        /// </summary>
        /// <remarks><c>null</c> when not connected</remarks>
        public ulong? BytesIn
        {
            get { return _bytes_in; }
            set { if (value != _bytes_in) { _bytes_in = value; RaisePropertyChanged(); } }
        }
        private ulong? _bytes_in;

        /// <summary>
        /// Number of bytes that have been sent to the server
        /// </summary>
        /// <remarks><c>null</c> when not connected</remarks>
        public ulong? BytesOut
        {
            get { return _bytes_out; }
            set { if (value != _bytes_out) { _bytes_out = value; RaisePropertyChanged(); } }
        }
        private ulong? _bytes_out;

        /// <summary>
        /// Disconnect command
        /// </summary>
        public DelegateCommand Disconnect
        {
            get
            {
                if (_disconnect_command == null)
                    _disconnect_command = new DelegateCommand(
                        // execute
                        () =>
                        {
                            // Terminate connection.
                            _disconnect.Cancel();
                            Disconnect.RaiseCanExecuteChanged();
                        },

                        // canExecute
                        () => !_disconnect.IsCancellationRequested);

                return _disconnect_command;
            }
        }
        private DelegateCommand _disconnect_command;

        /// <summary>
        /// Show log command
        /// </summary>
        public DelegateCommand ShowLog
        {
            get
            {
                if (_show_log_command == null)
                    _show_log_command = new DelegateCommand(
                        // execute
                        () =>
                        {
                            Parent.ChangeTaskCount(+1);
                            try { DoShowLog(); }
                            catch (Exception ex) { Parent.Error = ex; }
                            finally { Parent.ChangeTaskCount(-1); }
                        },

                        // canExecute
                        CanShowLog);

                return _show_log_command;
            }
        }
        private DelegateCommand _show_log_command;

        #endregion

        #region Constructors

        /// <summary>
        /// Creates a VPN session
        /// </summary>
        public VPNSession(ViewModels.ConnectWizard parent, VPNConfiguration configuration)
        {
            _disconnect = new CancellationTokenSource();
            _finished = new EventWaitHandle(false, EventResetMode.ManualReset);

            Parent = parent;

            // Clone configuration and keep an own copy.
            // This prevents dependency on Parent.Configuration.
            Configuration = (VPNConfiguration)configuration.Clone();

            // Create dispatcher timer.
            _connected_time_updater = new DispatcherTimer(
                new TimeSpan(0, 0, 0, 1),
                DispatcherPriority.Normal, (object sender, EventArgs e) => RaisePropertyChanged("ConnectedTime"),
                Dispatcher.CurrentDispatcher);

            // Launch user info load in the background.
            UserInfo = new Models.UserInfo();
            new Thread(new ThreadStart(
                () =>
                {
                    Parent.Dispatcher.Invoke(DispatcherPriority.Normal, (Action)(() => Parent.ChangeTaskCount(+1)));
                    try
                    {
                        var user_info = Configuration.AuthenticatingInstance.GetUserInfo(Configuration.AuthenticatingInstance, ViewModels.Window.Abort.Token);
                        Parent.Dispatcher.Invoke(DispatcherPriority.Normal, (Action)(() => UserInfo = user_info));
                    }
                    catch (OperationCanceledException) { }
                    catch (Exception ex) { Parent.Dispatcher.Invoke(DispatcherPriority.Normal, (Action)(() => Parent.Error = ex)); }
                    finally { Parent.Dispatcher.Invoke(DispatcherPriority.Normal, (Action)(() => Parent.ChangeTaskCount(-1))); }
                })).Start();

            // Load messages from all possible sources: authenticating/connecting instance, user/system list.
            // Any errors shall be ignored.
            MessageList = new Models.MessageList();
            new Thread(new ThreadStart(
                () =>
                {
                    var api_authenticating = Configuration.AuthenticatingInstance.GetEndpoints(ViewModels.Window.Abort.Token);
                    var api_connecting = Configuration.ConnectingInstance.GetEndpoints(ViewModels.Window.Abort.Token);
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
                                Parent.Dispatcher.Invoke(DispatcherPriority.Normal, (Action)(() => Parent.ChangeTaskCount(+1)));
                                try
                                {
                                    // Get and load user messages.
                                    var message_list = new Models.MessageList();
                                    message_list.LoadJSONAPIResponse(
                                        JSON.Response.Get(
                                            uri: list.Key,
                                            token: Configuration.AuthenticatingInstance.PeekAccessToken(ViewModels.Window.Abort.Token),
                                            ct: ViewModels.Window.Abort.Token).Value,
                                        list.Value,
                                        ViewModels.Window.Abort.Token);

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
                                finally { Parent.Dispatcher.Invoke(DispatcherPriority.Normal, (Action)(() => Parent.ChangeTaskCount(-1))); }
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
                })).Start();
        }

        #endregion

        #region Methods

        /// <summary>
        /// Run the session
        /// </summary>
        public virtual void Run()
        {
            // Do nothing but wait.
            CancellationTokenSource.CreateLinkedTokenSource(_disconnect.Token, ViewModels.Window.Abort.Token).Token.WaitHandle.WaitOne();

            // Signal session finished.
            Finished.Set();
        }

        /// <summary>
        /// Called when ShowLog command is invoked.
        /// </summary>
        protected virtual void DoShowLog()
        {
        }

        /// <summary>
        /// Called to test if ShowLog command is enabled.
        /// </summary>
        /// <returns><c>true</c> if enabled; <c>false</c> otherwise</returns>
        protected virtual bool CanShowLog()
        {
            return false;
        }

        #endregion

        #region IDisposable Support
        private bool disposedValue = false; // To detect redundant calls

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    _disconnect.Dispose();
                    _finished.Dispose();
                }

                _disconnect.Cancel();

                disposedValue = true;
            }
        }

        ~VPNSession()
        {
            // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
            Dispose(false);
        }

        // This code added to correctly implement the disposable pattern.
        public void Dispose()
        {
            // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        #endregion
    }
}
