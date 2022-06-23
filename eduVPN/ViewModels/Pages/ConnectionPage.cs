/*
    eduVPN - VPN for education and research

    Copyright: 2017-2022 The Commons Conservancy
    SPDX-License-Identifier: GPL-3.0+
*/

using eduVPN.Models;
using eduVPN.ViewModels.VPN;
using eduVPN.ViewModels.Windows;
using Prism.Commands;
using System;
using System.Collections.ObjectModel;
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

        #region Fields

        /// <summary>
        /// Profiles refresh cancellation token
        /// </summary>
        private CancellationTokenSource ProfilesRefreshInProgress;

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
                    _AuthenticateAndStartSession?.RaiseCanExecuteChanged();
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
        public Server ConnectingServer
        {
            get => _ConnectingServer;
            set
            {
                ProfilesRefreshInProgress?.Cancel();

                SetProperty(ref _ConnectingServer, value);
                SelectedProfile = null;
                Profiles = null;
                if (ConnectingServer == null)
                    return;

                ProfilesRefreshInProgress = new CancellationTokenSource();
                var ct = CancellationTokenSource.CreateLinkedTokenSource(ProfilesRefreshInProgress.Token, Window.Abort.Token).Token;
                new Thread(new ThreadStart(
                    () =>
                    {
                        Wizard.TryInvoke((Action)(() => Wizard.TaskCount++));
                        try
                        {
                            var list = ConnectingServer.GetProfileList(Wizard.GetAuthenticatingServer(ConnectingServer), ct);
                            //ct.WaitHandle.WaitOne(10000); // Mock a slow link for testing.
                            //list = new ObservableCollection<Profile>(); // Mock an empty list of profiles for testing.
                            ct.ThrowIfCancellationRequested();
                            Wizard.TryInvoke((Action)(() =>
                            {
                                if (ct.IsCancellationRequested) return;
                                Profiles = list;

                                Profile profile = null;
                                if (Properties.Settings.Default.LastSelectedProfile.TryGetValue(ConnectingServer.Base.AbsoluteUri, out var profileId))
                                    profile = list.FirstOrDefault(p => p.Id == profileId);
                                if (profile == null && list.Count > 0)
                                    profile = list[0];
                                SelectedProfile = profile;

                                // Auto-connect when connected previously or there is exactly one profile.
                                if ((Properties.Settings.Default.LastSelectedServer == ConnectingServer.Base || list.Count == 1) && StartSession.CanExecute())
                                    StartSession.Execute();
                            }));
                        }
                        catch (OperationCanceledException) { }
                        catch (Exception ex) { Wizard.TryInvoke((Action)(() => throw ex)); }
                        finally { Wizard.TryInvoke((Action)(() => Wizard.TaskCount--)); }
                    })).Start();
            }
        }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private Server _ConnectingServer;

        /// <summary>
        /// List of available profiles
        /// </summary>
        public ObservableCollection<Profile> Profiles
        {
            get => _Profiles;
            private set => SetProperty(ref _Profiles, value);
        }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private ObservableCollection<Profile> _Profiles;

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
                    if (ConnectingServer != null && SelectedProfile != null)
                        Properties.Settings.Default.LastSelectedProfile[ConnectingServer.Base.AbsoluteUri] = SelectedProfile.Id;
                    _StartSession?.RaiseCanExecuteChanged();
                    _AuthenticateAndStartSession?.RaiseCanExecuteChanged();
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
                if (value && AuthenticateAndStartSession.CanExecute())
                    AuthenticateAndStartSession.Execute();
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
                AuthenticateAndStartSession.CanExecute() ||
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
                            State = StateType.Initializing;
                            try
                            {
                                var connectingProfile = SelectedProfile;
                                new Thread(new ThreadStart(
                                    () =>
                                    {
                                        Wizard.TryInvoke((Action)(() => Wizard.TaskCount++));
                                        try
                                        {
                                            // Get session profile.
                                            var authenticatingServer = Wizard.GetAuthenticatingServer(connectingProfile.Server);
                                            var profileConfig = connectingProfile.Connect(authenticatingServer: authenticatingServer, ct: Window.Abort.Token);
                                            Session session;
                                            try
                                            {
                                                // Initialize session.
                                                switch (profileConfig.ContentType.ToLowerInvariant())
                                                {
                                                    case "application/x-openvpn-profile":
                                                        session = new OpenVPNSession(Wizard, connectingProfile, profileConfig);
                                                        break;
                                                    case "application/x-wireguard-profile":
                                                        session = new WireGuardSession(Wizard, connectingProfile, profileConfig);
                                                        break;
                                                    default:
                                                        throw new ArgumentOutOfRangeException(nameof(profileConfig.ContentType), profileConfig.ContentType, null);
                                                }
                                                session.PropertyChanged += (object sender, PropertyChangedEventArgs e) =>
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
                                                };
                                                session.Disconnect.CanExecuteChanged += (object sender, EventArgs e) => RaisePropertyChanged(nameof(CanSessionToggle));

                                                // Activate session.
                                                Wizard.TryInvoke((Action)(() =>
                                                {
                                                    ActiveSession = session;
                                                    State = StateType.Active;

                                                    // Set server/profile to auto-start on next launch.
                                                    Properties.Settings.Default.LastSelectedServer = connectingProfile.Server.Base;

                                                    Wizard.TaskCount--;
                                                }));
                                                try { session.Execute(); }
                                                finally { Wizard.TryInvoke((Action)(() => Wizard.TaskCount++)); }
                                            }
                                            finally
                                            {
                                                try { connectingProfile.Server.Disconnect(authenticatingServer); } catch { }
                                            }
                                        }
                                        catch (OperationCanceledException) { }
                                        catch (Exception ex)
                                        {
                                            Wizard.TryInvoke((Action)(() =>
                                            {
                                                // Clear failing server/profile to auto-start on next launch.
                                                Properties.Settings.Default.LastSelectedServer = null;
                                                throw ex;
                                            }));
                                        }
                                        finally
                                        {
                                            Wizard.TryInvoke((Action)(() =>
                                            {
                                                ActiveSession = null;
                                                if (State != StateType.Expired)
                                                    State = StateType.Inactive;
                                                Wizard.TaskCount--;
                                            }));
                                        }
                                    })).Start();
                            }
                            catch (Exception ex)
                            {
                                State = StateType.Inactive;
                                throw ex;
                            }
                        },
                        () => SelectedProfile != null && (State == StateType.Inactive || State == StateType.Expired));
                return _StartSession;
            }
        }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private DelegateCommand _StartSession;

        /// <summary>
        /// Starts VPN session
        /// </summary>
        public DelegateCommand AuthenticateAndStartSession
        {
            get
            {
                if (_AuthenticateAndStartSession == null)
                    _AuthenticateAndStartSession = new DelegateCommand(
                        async () =>
                        {
                            await Wizard.AuthorizationPage.TriggerAuthorizationAsync(Wizard.GetAuthenticatingServer(ConnectingServer));
                            StartSession.Execute();
                        },
                        () => StartSession.CanExecute());
                return _AuthenticateAndStartSession;
            }
        }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private DelegateCommand _AuthenticateAndStartSession;

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
                        () => Wizard.CurrentPage = Wizard.HomePage,
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
    }
}
