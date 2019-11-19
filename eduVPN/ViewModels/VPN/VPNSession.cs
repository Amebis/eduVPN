/*
    eduVPN - VPN for education and research

    Copyright: 2017-2019 The Commons Conservancy eduVPN Programme
    SPDX-License-Identifier: GPL-3.0+
*/

using eduVPN.JSON;
using eduVPN.Models;
using eduVPN.ViewModels.Windows;
using Prism.Commands;
using Prism.Mvvm;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Threading;

namespace eduVPN.ViewModels.VPN
{
    /// <summary>
    /// VPN session base class
    /// </summary>
    public class VPNSession : BindableBase, IDisposable
    {
        #region Fields

        /// <summary>
        /// Blank session
        /// </summary>
        public static readonly VPNSession Blank = new VPNSession();

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

        /// <summary>
        /// Connected time update timer
        /// </summary>
        protected DispatcherTimer _connected_time_updater;

        #endregion

        #region Properties

        /// <summary>
        /// The connecting wizard
        /// </summary>
        public ConnectWizard Wizard { get; }

        /// <summary>
        /// Authenticating eduVPN instance
        /// </summary>
        public Instance AuthenticatingInstance { get; }

        /// <summary>
        /// Connecting eduVPN instance profile
        /// </summary>
        public Profile ConnectingProfile { get; }

        /// <summary>
        /// Event to signal VPN session finished
        /// </summary>
        public EventWaitHandle Finished { get => _finished; }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private EventWaitHandle _finished;

        /// <summary>
        /// Merged list of user and system messages
        /// </summary>
        public MessageList MessageList
        {
            get { return _message_list; }
            set { SetProperty(ref _message_list, value); }
        }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private MessageList _message_list;

        /// <summary>
        /// Client connection state
        /// </summary>
        public VPNSessionStatusType State
        {
            get { return _state; }
            set { SetProperty(ref _state, value); }
        }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private VPNSessionStatusType _state;

        /// <summary>
        /// Descriptive string (used mostly on <see cref="eduOpenVPN.OpenVPNStateType.Reconnecting"/> and <see cref="eduOpenVPN.OpenVPNStateType.Exiting"/> to show the reason for the disconnect)
        /// </summary>
        public string StateDescription
        {
            get { return _state_description; }
            set { SetProperty(ref _state_description, value); }
        }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
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

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
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

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
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

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private DateTimeOffset? _connected_since;

        /// <summary>
        /// Running time connected
        /// </summary>
        /// <remarks><c>null</c> when not connected</remarks>
        public TimeSpan? ConnectedTime
        {
            get { return _connected_since != null ? DateTimeOffset.UtcNow - _connected_since : null; }
        }

        /// <summary>
        /// Number of bytes that have been received from the server
        /// </summary>
        /// <remarks><c>null</c> when not connected</remarks>
        public ulong? BytesIn
        {
            get { return _bytes_in; }
            set { SetProperty(ref _bytes_in, value); }
        }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
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

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
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

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
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
                            Wizard.ChangeTaskCount(+1);
                            try { DoShowLog(); }
                            catch (Exception ex) { Wizard.Error = ex; }
                            finally { Wizard.ChangeTaskCount(-1); }
                        },

                        // canExecute
                        CanShowLog);

                return _show_log_command;
            }
        }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private DelegateCommand _show_log_command;

        #endregion

        #region Constructors

        /// <summary>
        /// Creates a VPN session
        /// </summary>
        public VPNSession()
        {
            _disconnect = new CancellationTokenSource();
            _state_description = "";
            _message_list = new MessageList();
        }

        /// <summary>
        /// Creates a VPN session
        /// </summary>
        /// <param name="wizard">The connecting wizard</param>
        /// <param name="authenticating_instance">Authenticating eduVPN instance</param>
        /// <param name="connecting_profile">Connecting eduVPN profile</param>
        public VPNSession(ConnectWizard wizard, Instance authenticating_instance, Profile connecting_profile) :
            this()
        {
            _quit = CancellationTokenSource.CreateLinkedTokenSource(_disconnect.Token, Window.Abort.Token);
            _finished = new EventWaitHandle(false, EventResetMode.ManualReset);

            Wizard = wizard;

            AuthenticatingInstance = authenticating_instance;
            ConnectingProfile = connecting_profile;

            // Create dispatcher timer.
            _connected_time_updater = new DispatcherTimer(
                new TimeSpan(0, 0, 0, 1),
                DispatcherPriority.Normal, (object sender, EventArgs e) => RaisePropertyChanged(nameof(ConnectedTime)),
                Wizard.Dispatcher);

            _pre_run_actions = new List<Action>()
            {
                // Load messages from multiple sources: connecting instance, user/system list.
                // Any errors shall be ignored.
                () =>
                {
                    var api_authenticating = AuthenticatingInstance.GetEndpoints(_quit.Token);
                    var api_connecting = ConnectingProfile.Instance.GetEndpoints(_quit.Token);
                    var e = new RequestAuthorizationEventArgs("config");
                    Wizard.Instance_RequestAuthorization(AuthenticatingInstance, e);
                    if (e.AccessToken != null)
                    {
                        Parallel.ForEach(new List<KeyValuePair<Uri, string>>() {
                                new KeyValuePair<Uri, string>(api_connecting.UserMessages, "user_messages"),
                                new KeyValuePair<Uri, string>(api_connecting.SystemMessages, "system_messages"),
                            }
                            .Where(list => list.Key != null)
                            .Distinct(new EqualityComparer<KeyValuePair<Uri, string>>((x, y) => x.Key.AbsoluteUri == y.Key.AbsoluteUri && x.Value == y.Value)),
                            list =>
                            {
                                try
                                {
                                    // Get and load messages.
                                    var message_list = new MessageList();
                                    message_list.LoadJSONAPIResponse(
                                        Xml.Response.Get(
                                            uri: list.Key,
                                            token: e.AccessToken,
                                            ct: _quit.Token).Value,
                                        list.Value,
                                        _quit.Token);

                                    if (message_list.Count > 0)
                                    {
                                        // Add messages.
                                        Wizard.Dispatcher.Invoke(DispatcherPriority.Normal, (Action)(() =>
                                        {
                                            foreach (var msg in message_list)
                                                MessageList.Add(msg);
                                        }));
                                    }
                                }
                                catch { }
                            });
                    }

                    //// Add test messages.
                    //Wizard.Dispatcher.Invoke(DispatcherPriority.Normal, (Action)(() =>
                    //{
                    //    MessageList.Add(new MessageMaintenance()
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
                            Wizard.Dispatcher.Invoke(DispatcherPriority.Normal, (Action)(() => Wizard.ChangeTaskCount(+1)));
                            try { action(); }
                            finally { Wizard.Dispatcher.Invoke(DispatcherPriority.Normal, (Action)(() => Wizard.ChangeTaskCount(-1))); }
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
        /// <summary>
        /// Flag to detect redundant <see cref="Dispose(bool)"/> calls.
        /// </summary>
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private bool disposedValue = false;

        /// <summary>
        /// Called to dispose the object.
        /// </summary>
        /// <param name="disposing">Dispose managed objects</param>
        /// <remarks>
        /// To release resources for inherited classes, override this method.
        /// Call <c>base.Dispose(disposing)</c> within it to release parent class resources, and release child class resources if <paramref name="disposing"/> parameter is <c>true</c>.
        /// This method can get called multiple times for the same object instance. When the child specific resources should be released only once, introduce a flag to detect redundant calls.
        /// </remarks>
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

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting resources.
        /// </summary>
        /// <remarks>
        /// This method calls <see cref="Dispose(bool)"/> with <c>disposing</c> parameter set to <c>true</c>.
        /// To implement resource releasing override the <see cref="Dispose(bool)"/> method.
        /// </remarks>
        public void Dispose()
        {
            // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
            Dispose(true);
        }
        #endregion
    }
}
