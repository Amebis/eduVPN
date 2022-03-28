/*
    eduVPN - VPN for education and research

    Copyright: 2017-2022 The Commons Conservancy
    SPDX-License-Identifier: GPL-3.0+
*/

using eduVPN.Models;
using eduVPN.ViewModels.Windows;
using Prism.Commands;
using Prism.Mvvm;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Net;
using System.Threading;
using System.Windows.Threading;

namespace eduVPN.ViewModels.VPN
{
    /// <summary>
    /// VPN session base class
    /// </summary>
    public class Session : BindableBase
    {
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
        protected Xml.Response ProfileConfig
        {
            get => _ProfileConfig;
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
        private Xml.Response _ProfileConfig;

        /// <summary>
        /// VPN session worker
        /// </summary>
        public Thread Thread { get; private set; }

        /// <summary>
        /// Client connection state
        /// </summary>
        public SessionStatusType State
        {
            get => _State;
            protected set => SetProperty(ref _State, value);
        }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private SessionStatusType _State;

        /// <summary>
        /// Descriptive string (used mostly on <see cref="eduOpenVPN.OpenVPNStateType.Reconnecting"/> and <see cref="eduOpenVPN.OpenVPNStateType.Exiting"/> to show the reason for the disconnect)
        /// </summary>
        public string StateDescription
        {
            get => _StateDescription;
            protected set => SetProperty(ref _StateDescription, value);
        }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private string _StateDescription = "";

        /// <summary>
        /// TUN/TAP local IPv4 address
        /// </summary>
        /// <remarks><c>null</c> when not connected</remarks>
        public IPAddress TunnelAddress
        {
            get => _TunnelAddress;
            protected set => SetProperty(ref _TunnelAddress, value);
        }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private IPAddress _TunnelAddress;

        /// <summary>
        /// TUN/TAP local IPv6 address
        /// </summary>
        /// <remarks><c>null</c> when not connected</remarks>
        public IPAddress IPv6TunnelAddress
        {
            get => _IPv6TunnelAddress;
            protected set => SetProperty(ref _IPv6TunnelAddress, value);
        }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private IPAddress _IPv6TunnelAddress;

        /// <summary>
        /// Time when connected state recorded
        /// </summary>
        /// <remarks><c>null</c> when not connected</remarks>
        public DateTimeOffset? ConnectedAt
        {
            get => _ConnectedAt;
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
        public TimeSpan? ConnectedTime => ConnectedAt != null ? DateTimeOffset.UtcNow - ConnectedAt : null;

        /// <summary>
        /// Number of bytes that have been received from the server
        /// </summary>
        /// <remarks><c>null</c> when not connected</remarks>
        public ulong? BytesIn
        {
            get => _BytesIn;
            protected set => SetProperty(ref _BytesIn, value);
        }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private ulong? _BytesIn;

        /// <summary>
        /// Number of bytes that have been sent to the server
        /// </summary>
        /// <remarks><c>null</c> when not connected</remarks>
        public ulong? BytesOut
        {
            get => _BytesOut;
            protected set => SetProperty(ref _BytesOut, value);
        }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private ulong? _BytesOut;

        /// <summary>
        /// Session valid from date
        /// </summary>
        /// <remarks><c>DateTimeOffset.MinValue</c> when unknown or not available</remarks>
        public DateTimeOffset ValidFrom => ProfileConfig != null ? ProfileConfig.Authorized : DateTimeOffset.MinValue;

        /// <summary>
        /// Session expiration date
        /// </summary>
        /// <remarks><c>DateTimeOffset.MaxValue</c> when unknown or not available</remarks>
        public DateTimeOffset ValidTo => ProfileConfig != null ? ProfileConfig.Expires : DateTimeOffset.MaxValue;

        /// <summary>
        /// Is the session expired?
        /// </summary>
        public bool Expired => ValidTo <= DateTimeOffset.Now;

        /// <summary>
        /// Remaining time before the session expires; or <see cref="TimeSpan.MaxValue"/> when session does not expire
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
        public virtual DelegateCommand Disconnect { get; } = new DelegateCommand(() => { }, () => false);

        /// <summary>
        /// Show log command
        /// </summary>
        public virtual DelegateCommand ShowLog { get; } = new DelegateCommand(() => { }, () => false);

        #endregion

        #region Constructors

        /// <summary>
        /// Creates a VPN session
        /// </summary>
        /// <param name="wizard">The connecting wizard</param>
        /// <param name="connectingProfile">Connecting eduVPN profile</param>
        /// <param name="profileConfig">Initial profile configuration</param>
        public Session(ConnectWizard wizard, Profile connectingProfile, Xml.Response profileConfig)
        {
            Wizard = wizard;
            ConnectingProfile = connectingProfile;
            ProfileConfig = profileConfig;

            PropertyChanged += (object sender, PropertyChangedEventArgs e) =>
            {
                // Disconnect session when expired.
                if ((e.PropertyName == nameof(State) || e.PropertyName == nameof(Expired)) &&
                    (State == SessionStatusType.Initializing || State == SessionStatusType.Connecting || State == SessionStatusType.Connected) &&
                    Expired &&
                    Disconnect.CanExecute())
                    Disconnect.Execute();
            };
        }

        #endregion

        #region Methods

        /// <summary>
        /// Executes the session
        /// </summary>
        public void Execute()
        {
            Wizard.TryInvoke((Action)(() =>
            {
                State = SessionStatusType.Initializing;
            }));
            try
            {
                var propertyUpdater = new DispatcherTimer(
                    new TimeSpan(0, 0, 0, 1),
                    DispatcherPriority.Normal,
                    (object senderTimer, EventArgs eTimer) =>
                    {
                        RaisePropertyChanged(nameof(ConnectedTime));
                        RaisePropertyChanged(nameof(Expired));
                        RaisePropertyChanged(nameof(ExpiresTime));
                        RaisePropertyChanged(nameof(OfferRenewal));
                        RaisePropertyChanged(nameof(SuggestRenewal));
                    },
                    Wizard.Dispatcher);
                propertyUpdater.Start();
                try { Run(); }
                finally { propertyUpdater.Stop(); }
            }
            finally
            {
                Wizard.TryInvoke((Action)(() =>
                {
                    // Cleanup status properties.
                    State = SessionStatusType.Disconnected;
                    StateDescription = "";
                }));
            }
        }

        /// <summary>
        /// Run the session
        /// </summary>
        protected virtual void Run()
        {
            throw new NotImplementedException();
        }

        #endregion
    }
}
