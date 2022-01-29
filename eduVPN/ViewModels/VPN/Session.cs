﻿/*
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
    public class Session : BindableBase
    {
        #region Fields

        /// <summary>
        /// List of actions to run prior running the session
        /// </summary>
        /// <remarks>Actions will be run in parallel and session run will wait for all to finish.</remarks>
        protected List<Action> PreRun = new List<Action>();

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
        /// VPN session worker
        /// </summary>
        public Thread Thread { get; private set; }

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
        public virtual DateTimeOffset ValidFrom { get; } = DateTimeOffset.MinValue;

        /// <summary>
        /// Session expiration date
        /// </summary>
        /// <remarks><c>DateTimeOffset.MaxValue</c> when unknown or not available</remarks>
        public virtual DateTimeOffset ValidTo { get; } = DateTimeOffset.MaxValue;

        /// <summary>
        /// Is the session expired?
        /// </summary>
        public virtual bool Expired { get; } = false;

        /// <summary>
        /// Remaining time before the session expires; or <see cref="TimeSpan.MaxValue"/> when certificate does not expire
        /// </summary>
        public virtual TimeSpan ExpiresTime { get; } = TimeSpan.MaxValue;

        /// <summary>
        /// Should UI offer session renewal?
        /// </summary>
        public virtual bool OfferRenewal { get; } = false;

        /// <summary>
        /// Should UI suggest session renewal?
        /// </summary>
        public virtual bool SuggestRenewal { get; } = false;

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
        public Session(ConnectWizard wizard, Profile connectingProfile)
        {
            Wizard = wizard;
            ConnectingProfile = connectingProfile;
        }

        #endregion

        #region Methods

        /// <summary>
        /// Starts the session
        /// </summary>
        public void Start()
        {
            State = SessionStatusType.Initializing;
            Thread = new Thread(new ThreadStart(() =>
            {
                try
                {
                    var connectedTimeUpdater = new DispatcherTimer(
                        new TimeSpan(0, 0, 0, 1),
                        DispatcherPriority.Normal,
                        (object senderTimer, EventArgs eTimer) => RaisePropertyChanged(nameof(ConnectedTime)),
                        Wizard.Dispatcher);
                    connectedTimeUpdater.Start();
                    try
                    {
                        try
                        {
                            Parallel.ForEach(PreRun,
                                action =>
                                {
                                    TryInvoke((Action)(() => Wizard.TaskCount++));
                                    try { action(); }
                                    finally { TryInvoke((Action)(() => Wizard.TaskCount--)); }
                                });
                        }
                        catch (AggregateException ex)
                        {
                            var nonCancelledException = ex.InnerExceptions.Where(exInner => !(exInner is OperationCanceledException));
                            if (nonCancelledException.Any())
                                throw new AggregateException("", nonCancelledException.ToArray());
                            throw new OperationCanceledException();
                        }

                        Run();
                    }
                    finally { connectedTimeUpdater.Stop(); }

                    TryInvoke((Action)(() =>
                    {
                        // Cleanup status properties.
                        State = SessionStatusType.Disconnected;
                        StateDescription = "";
                    }));
                }
                catch (Exception ex)
                {
                    TryInvoke((Action)(() => {
                        State = SessionStatusType.Error;
                        StateDescription = ex.ToString();
                        throw ex;
                    }));
                }
            }));
            Thread.IsBackground = false;
            Thread.Start();
        }

        /// <summary>
        /// Run the session
        /// </summary>
        protected virtual void Run()
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Invoke method on GUI thread if it's not terminating.
        /// </summary>
        /// <param name="method">Method to execute</param>
        /// <returns>The return value from the delegate being invoked or <c>null</c> if the delegate has no return value or dispatcher is shutting down.</returns>
        protected object TryInvoke(Delegate method)
        {
            if (Wizard.Dispatcher.HasShutdownStarted)
                return null;
            return Wizard.Dispatcher.Invoke(DispatcherPriority.Normal, method);
        }

        #endregion
    }
}
