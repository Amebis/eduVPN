/*
    eduVPN - VPN for education and research

    Copyright: 2017-2023 The Commons Conservancy
    SPDX-License-Identifier: GPL-3.0+
*/

using eduVPN.Models;
using eduVPN.ViewModels.VPN;
using eduVPN.ViewModels.Windows;
using Prism.Commands;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Threading;

namespace eduVPN.ViewModels.Pages
{
    /// <summary>
    /// Connection status wizard page
    /// </summary>
    public class ConnectionPage : ConnectWizardStandardPage
    {
        #region Data Types

        /// <summary>
        /// The state connection page is in
        /// </summary>
        public enum StateType
        {
            /// <summary>
            /// Waiting for user to (select profile and) start connection
            /// </summary>
            Inactive = 0,

            /// <summary>
            /// Getting profile configuration
            /// </summary>
            Initializing,

            /// <summary>
            /// Session is active
            /// </summary>
            Active,

            /// <summary>
            /// Session expired
            /// </summary>
            Expired,
        }

        #endregion

        #region Properties

        /// <summary>
        /// The state connection page is in
        /// </summary>
        public StateType State
        {
            get => _State;
            private set
            {
                if (SetProperty(ref _State, value))
                {
                    RaisePropertyChanged(nameof(CanSessionToggle));
                    _StartSession?.RaiseCanExecuteChanged();
                    _SessionInfo?.RaiseCanExecuteChanged();
                }
            }
        }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private StateType _State;

        /// <summary>
        /// Connecting server
        /// </summary>
        /// <remarks><c>null</c> if none selected.</remarks>
        public Server Server
        {
            get => _Server;
            set
            {
                // Always overwrite server value: in the process of profile selection and connecting, we
                // set server using discovery/cached info first, and again, once we get the server profile
                // selection. Since the both Server objects represent the "same" server (base URI are the
                // same, i.e. _Server.Equals(value)), using SetProperty() would not make a required change.
                _Server = value;
                RaisePropertyChanged(nameof(Server));
                SetProfiles(_Server?.Profiles);
            }
        }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private Server _Server;

        /// <summary>
        /// List of available profiles
        /// </summary>
        public ObservableCollectionEx<Profile> Profiles { get; } = new ObservableCollectionEx<Profile>();

        /// <summary>
        /// Selected profile
        /// </summary>
        /// <remarks><c>null</c> if none selected.</remarks>
        public Profile SelectedProfile
        {
            get => _SelectedProfile;
            set
            {
                if (SetProperty(ref _SelectedProfile, value))
                {
                    RaisePropertyChanged(nameof(CanSessionToggle));
                    _StartSession?.RaiseCanExecuteChanged();
                }
            }
        }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private Profile _SelectedProfile;

        /// <summary>
        /// Active VPN session
        /// </summary>
        public Session ActiveSession
        {
            get => _ActiveSession;
            private set
            {
                if (SetProperty(ref _ActiveSession, value))
                {
                    RaisePropertyChanged(nameof(IsSessionActive));
                    RaisePropertyChanged(nameof(CanSessionToggle));
                    _NavigateBack?.RaiseCanExecuteChanged();
                }
            }
        }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private Session _ActiveSession;

        /// <summary>
        /// Is VPN session active?
        /// </summary>
        public bool IsSessionActive
        {
            get => ActiveSession != null;
            set
            {
                if (value && StartSession.CanExecute())
                    StartSession.Execute();
                else if (!value && ActiveSession != null && ActiveSession.Disconnect.CanExecute())
                {
                    ActiveSession.Disconnect.Execute();

                    // Clear server/profile to auto-start on next launch.
                    Properties.Settings.Default.LastSelectedServer = null;
                }
            }
        }

        /// <summary>
        /// Can session be activated or deactivated?
        /// </summary>
        public bool CanSessionToggle
        {
            get =>
                StartSession.CanExecute() ||
                ActiveSession != null && ActiveSession.Disconnect.CanExecute();
        }

        /// <summary>
        /// Starts VPN session
        /// </summary>
        public DelegateCommand StartSession
        {
            get
            {
                if (_StartSession == null)
                    _StartSession = new DelegateCommand(
                        () =>
                        {
                            if (SelectedProfile == null || State != StateType.Inactive && State != StateType.Expired)
                                return;
                            if (Wizard.OperationInProgress != null)
                            {
                                // Profile selection is in process.
                                State = StateType.Initializing;
                                try { Wizard.OperationInProgress.Reply(SelectedProfile.Id); }
                                catch (Exception ex)
                                {
                                    State = StateType.Inactive;
                                    throw ex;
                                }
                            }
                            else
                            {
                                // User attempted to reconnect.
                                Engine.SetProfileId(SelectedProfile.Id);
                                Wizard.Connect(Server);
                            }
                        },
                        () => SelectedProfile != null && (State == StateType.Inactive || State == StateType.Expired));
                return _StartSession;
            }
        }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private DelegateCommand _StartSession;

        /// <summary>
        /// Connection info command
        /// </summary>
        public DelegateCommand SessionInfo
        {
            get
            {
                if (_SessionInfo == null)
                    _SessionInfo = new DelegateCommand(
                        () => Wizard.CurrentPage = this,
                        () => State != StateType.Inactive);
                return _SessionInfo;
            }
        }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private DelegateCommand _SessionInfo;

        /// <inheritdoc/>
        public override DelegateCommand NavigateBack
        {
            get
            {
                if (_NavigateBack == null)
                    _NavigateBack = new DelegateCommand(
                        () =>
                        {
                            Wizard.OperationInProgress?.Cancel();
                            Wizard.CurrentPage = Wizard.HomePage;
                        },
                        () => ActiveSession == null);
                return _NavigateBack;
            }
        }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private DelegateCommand _NavigateBack;

        #endregion

        #region Constructors

        /// <summary>
        /// Constructs a status wizard page
        /// </summary>
        /// <param name="wizard">The connecting wizard</param>
        public ConnectionPage(ConnectWizard wizard) :
            base(wizard)
        {
        }

        #endregion

        #region Methods

        /// <summary>
        /// Populates profile list
        /// </summary>
        /// <param name="profiles">eduvpn-common provided profile list</param>
        public void SetProfiles(ProfileDictionary profiles)
        {
            var list = Profiles.BeginUpdate();
            try
            {
                list.Clear();
                if (profiles != null)
                    foreach (var item in profiles)
                        list.Add(item.Value);
            }
            finally { Profiles.EndUpdate(); }
            SelectedProfile = profiles != null ? profiles.Current ?? profiles.FirstOrDefault().Value : null;
        }

        /// <summary>
        /// Activates VPN session
        /// </summary>
        /// <param name="config">VPN configuration</param>
        /// <param name="expiration">VPN expiry times</param>
        /// <exception cref="ArgumentOutOfRangeException">Unsupported VPN configuration</exception>
        public void ActivateSession(Configuration config, Expiration expiration)
        {
            var server = Server;
            new Thread(() =>
            {
                Wizard.TryInvoke((Action)(() => Wizard.TaskCount++));
                try
                {
                    using (var session =
                        config.Protocol == VPNProtocol.WireGuard ? (Session)new WireGuardSession(Wizard, server, config.VPNConfig, expiration) :
                        config.Protocol == VPNProtocol.OpenVPN ? new OpenVPNSession(Wizard, server, config.VPNConfig, expiration) :
                            throw new ArgumentOutOfRangeException(nameof(config.Protocol), config.Protocol, null))
                    {
                        void OnPropertyChanged(object sender, PropertyChangedEventArgs e)
                        {
                            // Update connection page when session disconnects.
                            if (e.PropertyName == nameof(session.State))
                                switch (session.State)
                                {
                                    case SessionStatusType.Disconnected:
                                        ActiveSession = null;
                                        State = session.Expired ? StateType.Expired : StateType.Inactive;
                                        break;
                                }
                        }
                        session.PropertyChanged += OnPropertyChanged;

                        void OnDisconnectCanExecuteChanged(object sender, EventArgs e) => RaisePropertyChanged(nameof(CanSessionToggle));
                        session.Disconnect.CanExecuteChanged += OnDisconnectCanExecuteChanged;

                        // Activate session.
                        try
                        {
                            Wizard.TryInvoke((Action)(() =>
                            {
                                ActiveSession = session;
                                State = StateType.Active;

                                // Set server/profile to auto-start on next launch.
                                Properties.Settings.Default.LastSelectedServer = server.Id;

                                Wizard.TaskCount--;
                            }));
                            try { session.Execute(); }
                            finally { Wizard.TryInvoke((Action)(() => Wizard.TaskCount++)); }
                        }
                        finally
                        {
                            Wizard.TryInvoke((Action)(() => ActiveSession = null));
                            session.PropertyChanged -= OnPropertyChanged;
                            session.Disconnect.CanExecuteChanged -= OnDisconnectCanExecuteChanged;
                        }
                    }
                }
                catch (OperationCanceledException) { }
                catch (Exception ex) { Wizard.TryInvoke((Action)(() => Wizard.Error = ex)); }
                finally
                {
                    Wizard.TryInvoke((Action)(() =>
                    {
                        // Do not use Wizard.OperationInProgress, not to interfere with session renewal.
                        // The cleanup should not raise any callbacks, therefore its cookie is not important
                        // to other pages.
                        using (var operationInProgress = new Engine.CancellationTokenCookie(Window.Abort.Token))
                            try { Engine.Cleanup(operationInProgress); } catch { }
                        if (State != StateType.Expired)
                            State = StateType.Inactive;
                        Wizard.TaskCount--;
                    }));
                }
            }).Start();
        }

        #endregion
    }
}
