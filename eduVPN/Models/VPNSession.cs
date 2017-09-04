/*
    eduVPN - End-user friendly VPN

    Copyright: 2017, The Commons Conservancy eduVPN Programme
    SPDX-License-Identifier: GPL-3.0+
*/

using Prism.Commands;
using Prism.Mvvm;
using System;
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
        /// Parent thread dispatcher
        /// </summary>
        protected Dispatcher _dispatcher;

        /// <summary>
        /// Terminate connection token
        /// </summary>
        protected CancellationTokenSource _disconnect;

        /// <summary>
        /// VPN configuration
        /// </summary>
        protected VPNConfiguration _configuration;

        #endregion

        #region Properties

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
                        });

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
                    _show_log_command = new DelegateCommand(DoShowLog, CanShowLog);

                return _show_log_command;
            }
        }
        private DelegateCommand _show_log_command;

        #endregion

        #region Constructors

        /// <summary>
        /// Creates a VPN session
        /// </summary>
        public VPNSession(VPNConfiguration configuration)
        {
            _dispatcher = Dispatcher.CurrentDispatcher;
            _disconnect = new CancellationTokenSource();

            _configuration = configuration;

            // Create dispatcher timer.
            _connected_time_updater = new DispatcherTimer(
                new TimeSpan(0, 0, 0, 1),
                DispatcherPriority.Normal, (object sender, EventArgs e) => RaisePropertyChanged("ConnectedTime"),
                Dispatcher.CurrentDispatcher);
        }

        #endregion

        #region Methods

        /// <summary>
        /// Run the session
        /// </summary>
        /// <param name="ct">The token to monitor for cancellation requests</param>
        public virtual void Run(CancellationToken ct = default(CancellationToken))
        {
            // Do nothing but wait.
            CancellationTokenSource.CreateLinkedTokenSource(_disconnect.Token, ct).Token.WaitHandle.WaitOne();
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
                    _disconnect.Dispose();

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
