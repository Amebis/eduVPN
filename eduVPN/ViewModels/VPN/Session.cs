/*
    eduVPN - VPN for education and research

    Copyright: 2017-2022 The Commons Conservancy eduVPN Programme
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
    public class Session : BindableBase, IDisposable
    {
        #region Fields

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
        /// Connecting eduVPN server profile
        /// </summary>
        public Profile ConnectingProfile { get; }

        /// <summary>
        /// Profile configuration
        /// </summary>
        protected eduVPN.Xml.Response ProfileConfig
        {
            get { return _ProfileConfig; }
            set
            {
                if (SetProperty(ref _ProfileConfig, value))
                {
                    RaisePropertyChanged(nameof(ValidFrom));
                    RaisePropertyChanged(nameof(ValidTo));
                    RaisePropertyChanged(nameof(Expired));
                    RaisePropertyChanged(nameof(ExpiresTime));
                    RaisePropertyChanged(nameof(OfferRenewal));
                    RaisePropertyChanged(nameof(SuggestRenewal));
                }
            }
        }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private eduVPN.Xml.Response _ProfileConfig;

        /// <summary>
        /// Client connection state
        /// </summary>
        public SessionStatusType State
        {
            get { return _State; }
            protected set { SetProperty(ref _State, value); }
        }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private SessionStatusType _State;

        /// <summary>
        /// Descriptive string (used mostly on <see cref="eduOpenVPN.OpenVPNStateType.Reconnecting"/> and <see cref="eduOpenVPN.OpenVPNStateType.Exiting"/> to show the reason for the disconnect)
        /// </summary>
        public string StateDescription
        {
            get { return _StateDescription; }
            protected set { SetProperty(ref _StateDescription, value); }
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
            protected set { SetProperty(ref _TunnelAddress, value); }
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
            protected set { SetProperty(ref _IPv6TunnelAddress, value); }
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
            protected set
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
            protected set { SetProperty(ref _BytesIn, value); }
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
            protected set { SetProperty(ref _BytesOut, value); }
        }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private ulong? _BytesOut;

        /// <summary>
        /// Session valid from date
        /// </summary>
        /// <remarks><c>DateTimeOffset.MinValue</c> when unknown or not available</remarks>
        public DateTimeOffset ValidFrom { get => ProfileConfig != null ? ProfileConfig.Authorized : DateTimeOffset.MinValue; }

        /// <summary>
        /// Session expiration date
        /// </summary>
        /// <remarks><c>DateTimeOffset.MaxValue</c> when unknown or not available</remarks>
        public DateTimeOffset ValidTo { get => ProfileConfig != null ? ProfileConfig.Expires : DateTimeOffset.MaxValue; }

        /// <summary>
        /// Is the session expired?
        /// </summary>
        public bool Expired { get => ValidTo <= DateTimeOffset.Now; }

        /// <summary>
        /// Remaining time before the session expires; or <see cref="TimeSpan.MaxValue"/> when certificate does not expire
        /// </summary>
        public TimeSpan ExpiresTime
        {
            get
            {
                var v = ValidTo;
                return v != DateTimeOffset.MaxValue ?
                    v - DateTimeOffset.Now :
                    TimeSpan.MaxValue;
            }
        }

        /// <summary>
        /// Should UI offer session renewal?
        /// </summary>
        public bool OfferRenewal
        {
            get
            {
                DateTimeOffset from = ValidFrom, now = DateTimeOffset.Now, to = ValidTo;
                return
#if DEBUG
                    (now - from).TotalMinutes >= 1;
#else
                    (now - from).TotalMinutes >= 30 &&
                    (to - now).TotalHours <= 24;
#endif
            }
        }

        /// <summary>
        /// Should UI suggest session renewal?
        /// </summary>
        public bool SuggestRenewal
        {
            get
            {
                DateTimeOffset from = ValidFrom, now = DateTimeOffset.Now, to = ValidTo;
                return
                    from != DateTimeOffset.MinValue && to != DateTimeOffset.MaxValue &&
                    (now - from).Ticks >= 0.75 * (to - from).Ticks &&
                    (to - now).TotalHours <= 24;
            }
        }

        /// <summary>
        /// Renews and restarts the session
        /// </summary>
        public virtual DelegateCommand Renew { get; } = new DelegateCommand(() => { }, () => false);

        /// <summary>
        /// Disconnect command
        /// </summary>
        public DelegateCommand Disconnect
        {
            get
            {
                if (_Disconnect == null)
                    _Disconnect = new DelegateCommand(
                        () =>
                        {
                            try
                            {
                                // Terminate connection.
                                SessionInProgress.Cancel();
                                _Disconnect.RaiseCanExecuteChanged();

                                // Clear server/profile to auto-start on next launch.
                                Properties.Settings.Default.LastSelectedServer = null;
                            }
                            catch (Exception ex) { Wizard.Error = ex; }
                        },
                        () => !SessionInProgress.IsCancellationRequested);
                return _Disconnect;
            }
        }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private DelegateCommand _Disconnect;

        /// <summary>
        /// Show log command
        /// </summary>
        public virtual DelegateCommand ShowLog { get; } = new DelegateCommand(() => { }, () => false);

        #endregion

        #region Constructors

        /// <summary>
        /// Creates a VPN session
        /// </summary>
        public Session()
        {
        }

        /// <summary>
        /// Creates a VPN session
        /// </summary>
        /// <param name="wizard">The connecting wizard</param>
        /// <param name="connectingProfile">Connecting eduVPN profile</param>
        public Session(ConnectWizard wizard, Profile connectingProfile) :
            this()
        {
            SessionAndWindowInProgress = CancellationTokenSource.CreateLinkedTokenSource(SessionInProgress.Token, Window.Abort.Token);

            Wizard = wizard;
            ConnectingProfile = connectingProfile;
            State = SessionStatusType.Initializing;

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
            ConnectedTimeUpdater.Stop();
        }

        /// <summary>
        /// Run the session
        /// </summary>
        protected virtual void DoRun()
        {
            throw new NotImplementedException();
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
