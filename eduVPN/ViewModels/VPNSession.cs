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
using System.Threading.Tasks;
using System.Windows.Threading;

namespace eduVPN.ViewModels
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

        /// <summary>
        /// Quit token
        /// </summary>
        protected CancellationTokenSource _quit;

        /// <summary>
        /// List of actions to run prior running the session
        /// </summary>
        /// <remarks>Actions will be run in parallel and session run will wait for all to finish.</remarks>
        protected List<Action> _pre_run_actions;

        #endregion

        #region Properties

        /// <summary>
        /// The session parent
        /// </summary>
        public ConnectWizard Parent { get; }

        /// <summary>
        /// VPN configuration
        /// </summary>
        public Models.VPNConfiguration Configuration { get; }

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
            set { SetProperty(ref _user_info, value); }
        }
        private Models.UserInfo _user_info;

        /// <summary>
        /// Merged list of user and system messages
        /// </summary>
        public Models.MessageList MessageList
        {
            get { return _message_list; }
            set { SetProperty(ref _message_list, value); }
        }
        private Models.MessageList _message_list;

        /// <summary>
        /// Client connection state
        /// </summary>
        public Models.VPNSessionStatusType State
        {
            get { return _state; }
            set { SetProperty(ref _state, value); }
        }
        private Models.VPNSessionStatusType _state;

        /// <summary>
        /// Descriptive string (used mostly on <c>StateType.Reconnecting</c> and <c>StateType.Exiting</c> to show the reason for the disconnect)
        /// </summary>
        public string StateDescription
        {
            get { return _state_description; }
            set { SetProperty(ref _state_description, value); }
        }
        private string _state_description;

        /// <summary>
        /// TUN/TAP local IPv4 address
        /// </summary>
        /// <remarks><c>null</c> when not connected</remarks>
        public IPAddress TunnelAddress
        {
            get { return _tunnel_address; }
            set { SetProperty(ref _tunnel_address, value); }
        }
        private IPAddress _tunnel_address;

        /// <summary>
        /// TUN/TAP local IPv6 address
        /// </summary>
        /// <remarks><c>null</c> when not connected</remarks>
        public IPAddress IPv6TunnelAddress
        {
            get { return _ipv6_tunnel_address; }
            set { SetProperty(ref _ipv6_tunnel_address, value); }
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
                if (SetProperty(ref _connected_since, value))
                    RaisePropertyChanged(nameof(ConnectedTime));
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
            set { SetProperty(ref _bytes_in, value); }
        }
        private ulong? _bytes_in;

        /// <summary>
        /// Number of bytes that have been sent to the server
        /// </summary>
        /// <remarks><c>null</c> when not connected</remarks>
        public ulong? BytesOut
        {
            get { return _bytes_out; }
            set { SetProperty(ref _bytes_out, value); }
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
        public VPNSession(ConnectWizard parent, Models.VPNConfiguration configuration)
        {
            _disconnect = new CancellationTokenSource();
            _quit = CancellationTokenSource.CreateLinkedTokenSource(_disconnect.Token, Window.Abort.Token);
            _finished = new EventWaitHandle(false, EventResetMode.ManualReset);

            Parent = parent;
            Configuration = configuration;
            _message_list = new Models.MessageList();

            // Create dispatcher timer.
            _connected_time_updater = new DispatcherTimer(
                new TimeSpan(0, 0, 0, 1),
                DispatcherPriority.Normal, (object sender, EventArgs e) => RaisePropertyChanged(nameof(ConnectedTime)),
                Parent.Dispatcher);

            _pre_run_actions = new List<Action>()
            {
                // Launch user info load in the background.
                () =>
                {
                    var user_info = Configuration.AuthenticatingInstance.GetUserInfo(Configuration.AuthenticatingInstance, _quit.Token);
                    Parent.Dispatcher.Invoke(DispatcherPriority.Normal, (Action)(() => UserInfo = user_info));
                },

                // Load messages from all possible sources: authenticating/connecting instance, user/system list.
                // Any errors shall be ignored.
                () =>
                {
                    var api_authenticating = Configuration.AuthenticatingInstance.GetEndpoints(_quit.Token);
                    var api_connecting = Configuration.ConnectingInstance.GetEndpoints(_quit.Token);
                    Parallel.ForEach(new List<KeyValuePair<Uri, string>>() {
                            new KeyValuePair<Uri, string>(api_authenticating.UserMessages, "user_messages"),
                            new KeyValuePair<Uri, string>(api_connecting.UserMessages, "user_messages"),
                            new KeyValuePair<Uri, string>(api_authenticating.SystemMessages, "system_messages"),
                            new KeyValuePair<Uri, string>(api_connecting.SystemMessages, "system_messages"),
                        }
                        .Where(list => list.Key != null)
                        .Distinct(new EqualityComparer<KeyValuePair<Uri, string>>((x, y) => x.Key.AbsoluteUri == y.Key.AbsoluteUri && x.Value == y.Value)),
                        list =>
                        {
                            try
                            {
                                // Get and load messages.
                                var message_list = new Models.MessageList();
                                message_list.LoadJSONAPIResponse(
                                    JSON.Response.Get(
                                        uri: list.Key,
                                        token: Configuration.AuthenticatingInstance.PeekAccessToken(_quit.Token),
                                        ct: _quit.Token).Value,
                                    list.Value,
                                    _quit.Token);

                                if (message_list.Count > 0)
                                {
                                    // Add messages.
                                    Parent.Dispatcher.Invoke(DispatcherPriority.Normal, (Action)(() =>
                                    {
                                        foreach (var msg in message_list)
                                            MessageList.Add(msg);
                                    }));
                                }
                            }
                            catch { }
                        });

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
                },
            };
        }

        #endregion

        #region Methods

        /// <summary>
        /// Run the session
        /// </summary>
        public void Run()
        {
            try
            {
                try
                {
                    Parallel.ForEach(_pre_run_actions,
                        action =>
                        {
                            Parent.Dispatcher.Invoke(DispatcherPriority.Normal, (Action)(() => Parent.ChangeTaskCount(+1)));
                            try { action(); }
                            finally { Parent.Dispatcher.Invoke(DispatcherPriority.Normal, (Action)(() => Parent.ChangeTaskCount(-1))); }
                        });
                }
                catch (AggregateException ex)
                {
                    var ex_non_cancelled = ex.InnerExceptions.Where(ex_inner => !(ex_inner is OperationCanceledException));
                    if (ex_non_cancelled.Any())
                    {
                        // Some exceptions were issues beyond OperationCanceledException.
                        throw new AggregateException(ex.Message, ex_non_cancelled.ToArray());
                    }
                    else
                    {
                        // All exceptions were OperationCanceledException.
                        throw new OperationCanceledException();
                    }
                }

                DoRun();
            }
            finally
            {
                // Signal session finished.
                Finished.Set();
            }
        }

        /// <summary>
        /// Run the session
        /// </summary>
        protected virtual void DoRun()
        {
            // Do nothing but wait.
            _quit.Token.WaitHandle.WaitOne();
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
                    if (_disconnect != null)
                        _disconnect.Dispose();

                    if (_quit != null)
                        _quit.Dispose();

                    if (_finished != null)
                        _finished.Dispose();
                }

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
