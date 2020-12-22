/*
    eduVPN - VPN for education and research

    Copyright: 2017-2020 The Commons Conservancy eduVPN Programme
    SPDX-License-Identifier: GPL-3.0+
*/

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
        /// Session termination token
        /// </summary>
        private readonly CancellationTokenSource SessionInProgress = new CancellationTokenSource();

        /// <summary>
        /// Quit token
        /// </summary>
        protected CancellationTokenSource SessionAndWindowInProgress;

        /// <summary>
        /// List of actions to run prior running the session
        /// </summary>
        /// <remarks>Actions will be run in parallel and session run will wait for all to finish.</remarks>
        protected List<Action> PreRun = new List<Action>();

        /// <summary>
        /// Connected time update timer
        /// </summary>
        protected DispatcherTimer ConnectedTimeUpdater;

        #endregion

        #region Properties

        /// <summary>
        /// The connecting wizard
        /// </summary>
        public ConnectWizard Wizard { get; }

        /// <summary>
        /// Authenticating eduVPN server
        /// </summary>
        public Server AuthenticatingServer { get; }

        /// <summary>
        /// Connecting eduVPN server profile
        /// </summary>
        public Profile ConnectingProfile { get; }

        /// <summary>
        /// Event to signal VPN session finished
        /// </summary>
        public EventWaitHandle Finished { get; }

        /// <summary>
        /// Client connection state
        /// </summary>
        public VPNSessionStatusType State
        {
            get { return _State; }
            set { SetProperty(ref _State, value); }
        }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private VPNSessionStatusType _State;

        /// <summary>
        /// Descriptive string (used mostly on <see cref="eduOpenVPN.OpenVPNStateType.Reconnecting"/> and <see cref="eduOpenVPN.OpenVPNStateType.Exiting"/> to show the reason for the disconnect)
        /// </summary>
        public string StateDescription
        {
            get { return _StateDescription; }
            set { SetProperty(ref _StateDescription, value); }
        }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private string _StateDescription = "";

        /// <summary>
        /// TUN/TAP local IPv4 address
        /// </summary>
        /// <remarks><c>null</c> when not connected</remarks>
        public IPAddress TunnelAddress
        {
            get { return _TunnelAddress; }
            set { SetProperty(ref _TunnelAddress, value); }
        }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private IPAddress _TunnelAddress;

        /// <summary>
        /// TUN/TAP local IPv6 address
        /// </summary>
        /// <remarks><c>null</c> when not connected</remarks>
        public IPAddress IPv6TunnelAddress
        {
            get { return _IPv6TunnelAddress; }
            set { SetProperty(ref _IPv6TunnelAddress, value); }
        }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private IPAddress _IPv6TunnelAddress;

        /// <summary>
        /// Time when connected state recorded
        /// </summary>
        /// <remarks><c>null</c> when not connected</remarks>
        public DateTimeOffset? ConnectedAt
        {
            get { return _ConnectedAt; }
            set
            {
                if (SetProperty(ref _ConnectedAt, value))
                    RaisePropertyChanged(nameof(ConnectedTime));
            }
        }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private DateTimeOffset? _ConnectedAt;

        /// <summary>
        /// Running time connected
        /// </summary>
        /// <remarks><c>null</c> when not connected</remarks>
        public TimeSpan? ConnectedTime
        {
            get { return ConnectedAt != null ? DateTimeOffset.UtcNow - ConnectedAt : null; }
        }

        /// <summary>
        /// Number of bytes that have been received from the server
        /// </summary>
        /// <remarks><c>null</c> when not connected</remarks>
        public ulong? BytesIn
        {
            get { return _BytesIn; }
            set { SetProperty(ref _BytesIn, value); }
        }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private ulong? _BytesIn;

        /// <summary>
        /// Number of bytes that have been sent to the server
        /// </summary>
        /// <remarks><c>null</c> when not connected</remarks>
        public ulong? BytesOut
        {
            get { return _BytesOut; }
            set { SetProperty(ref _BytesOut, value); }
        }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private ulong? _BytesOut;

        /// <summary>
        /// Disconnect command
        /// </summary>
        public DelegateCommand Disconnect { get; }

        /// <summary>
        /// Show log command
        /// </summary>
        public DelegateCommand ShowLog { get; }

        #endregion

        #region Constructors

        /// <summary>
        /// Creates a VPN session
        /// </summary>
        public VPNSession()
        {
            Disconnect = new DelegateCommand(
                () =>
                {
                    try
                    {
                        // Terminate connection.
                        SessionInProgress.Cancel();
                        Disconnect.RaiseCanExecuteChanged();

                        // Clear profile to auto-start on next launch.
                        Properties.Settings.Default.AutoStartProfile = null;
                    }
                    catch (Exception ex) { Wizard.Error = ex; }
                },
                () => !SessionInProgress.IsCancellationRequested);

            ShowLog = new DelegateCommand(
                () =>
                {
                    try { DoShowLog(); }
                    catch (Exception ex) { Wizard.Error = ex; }
                },
                CanShowLog);
        }

        /// <summary>
        /// Creates a VPN session
        /// </summary>
        /// <param name="wizard">The connecting wizard</param>
        /// <param name="authenticatingServer">Authenticating eduVPN server</param>
        /// <param name="connectingProfile">Connecting eduVPN profile</param>
        public VPNSession(ConnectWizard wizard, Server authenticatingServer, Profile connectingProfile) :
            this()
        {
            SessionAndWindowInProgress = CancellationTokenSource.CreateLinkedTokenSource(SessionInProgress.Token, Window.Abort.Token);
            Finished = new EventWaitHandle(false, EventResetMode.ManualReset);

            Wizard = wizard;
            AuthenticatingServer = authenticatingServer;
            ConnectingProfile = connectingProfile;

            // Create dispatcher timer.
            ConnectedTimeUpdater = new DispatcherTimer(
                new TimeSpan(0, 0, 0, 1),
                DispatcherPriority.Normal,
                (object sender, EventArgs e) => RaisePropertyChanged(nameof(ConnectedTime)),
                Wizard.Dispatcher);
            ConnectedTimeUpdater.Start();
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
                    Parallel.ForEach(PreRun,
                        action =>
                        {
                            Wizard.Dispatcher.Invoke(DispatcherPriority.Normal, (Action)(() => Wizard.TaskCount++));
                            try { action(); }
                            finally { Wizard.Dispatcher.Invoke(DispatcherPriority.Normal, (Action)(() => Wizard.TaskCount--)); }
                        });
                }
                catch (AggregateException ex)
                {
                    var nonCancelledException = ex.InnerExceptions.Where(exInner => !(exInner is OperationCanceledException));
                    if (nonCancelledException.Any())
                        throw new AggregateException("", nonCancelledException.ToArray());
                    throw new OperationCanceledException();
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
            SessionAndWindowInProgress.Token.WaitHandle.WaitOne();
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
                    if (SessionAndWindowInProgress != null)
                        SessionAndWindowInProgress.Dispose();

                    if (SessionInProgress != null)
                        SessionInProgress.Dispose();

                    if (Finished != null)
                        Finished.Dispose();
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
