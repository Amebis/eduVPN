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
using System.Windows.Threading;

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
            /// Waiting for user to (select profile and) connect
            /// </summary>
            SessionInactive = 0,

            /// <summary>
            /// A session is activating
            /// </summary>
            SessionActivating,

            /// <summary>
            /// A session is active
            /// </summary>
            SessionActive,

            /// <summary>
            /// A session is deactivating
            /// </summary>
            SessionDeactivating,

            /// <summary>
            /// The active session expired
            /// </summary>
            SessionExpired,
        }

        #endregion

        #region Fields

        /// <summary>
        /// Profiles refresh cancellation token
        /// </summary>
        private CancellationTokenSource ProfilesRefreshInProgress;

        /// <summary>
        /// Profile connect cancellation token
        /// </summary>
        private CancellationTokenSource ProfileConnectInProgress;

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
                    RaisePropertyChanged(nameof(IsSessionActive));
                    RaisePropertyChanged(nameof(CanSessionToggle));
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
                        Wizard.Dispatcher.Invoke(DispatcherPriority.Normal, (Action)(() => Wizard.TaskCount++));
                        try
                        {
                            var list = ConnectingServer.GetProfileList(Wizard.GetAuthenticatingServer(ConnectingServer), ct);
                            //ct.WaitHandle.WaitOne(10000); // Mock a slow link for testing.
                            //list = new ObservableCollection<Profile>(); // Mock an empty list of profiles for testing.
                            ct.ThrowIfCancellationRequested();
                            Wizard.Dispatcher.Invoke(DispatcherPriority.Normal, (Action)(() =>
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
                        catch (Exception ex) { Wizard.Dispatcher.Invoke(DispatcherPriority.Normal, (Action)(() => throw ex)); }
                        finally { Wizard.Dispatcher.Invoke(DispatcherPriority.Normal, (Action)(() => Wizard.TaskCount--)); }
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
                    _StartSession?.RaiseCanExecuteChanged();
                    _AuthenticateAndStartSession?.RaiseCanExecuteChanged();
                    _SessionInfo?.RaiseCanExecuteChanged();
                    _NavigateBack?.RaiseCanExecuteChanged();
                }
            }
        }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private Session _ActiveSession;

        /// <summary>
        /// Is VPN session active
        /// </summary>
        public bool IsSessionActive
        {
            get => StateType.SessionActivating <= State && State <= StateType.SessionActive;
            set
            {
                if (!CanSessionToggle)
                    return;
                if (ActiveSession != null && !value)
                {
                    if (ActiveSession.Disconnect.CanExecute())
                    {
                        ActiveSession.Disconnect.Execute();

                        // Clear server/profile to auto-start on next launch.
                        Properties.Settings.Default.LastSelectedServer = null;
                    }
                }
                else if (ActiveSession == null && value)
                {
                    if (AuthenticateAndStartSession.CanExecute())
                        AuthenticateAndStartSession.Execute();
                }
            }
        }

        /// <summary>
        /// Can session be activated or deactivated?
        /// </summary>
        public bool CanSessionToggle
        {
            get =>
                State == StateType.SessionInactive && SelectedProfile != null ||
                State == StateType.SessionActive;
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
                            State = StateType.SessionActivating;
                            ProfileConnectInProgress?.Cancel();

                            var connectingProfile = SelectedProfile;

                            ProfileConnectInProgress = new CancellationTokenSource();
                            var ct = CancellationTokenSource.CreateLinkedTokenSource(ProfileConnectInProgress.Token, Window.Abort.Token).Token;
                            new Thread(new ThreadStart(
                                () =>
                                {
                                    Wizard.TryInvoke((Action)(() => Wizard.TaskCount++));
                                    try
                                    {
                                        // Get session profile.
                                        var authenticatingServer = Wizard.GetAuthenticatingServer(connectingProfile.Server);
                                        var profileConfig = connectingProfile.Connect(authenticatingServer: authenticatingServer, ct: ct);
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
                                                        case SessionStatusType.Connecting:
                                                            State = StateType.SessionActivating;
                                                            break;

                                                        case SessionStatusType.Connected:
                                                            State = StateType.SessionActive;
                                                            break;

                                                        case SessionStatusType.Disconnecting:
                                                            State = StateType.SessionDeactivating;
                                                            break;

                                                        case SessionStatusType.Disconnected:
                                                            ActiveSession = null;
                                                            State = session.Expired ? StateType.SessionExpired : StateType.SessionInactive;
                                                            break;
                                                    }
                                            };

                                            // Activate session.
                                            Wizard.TryInvoke((Action)(() =>
                                            {
                                                ActiveSession = session;

                                                // Set server/profile to auto-start on next launch.
                                                Properties.Settings.Default.LastSelectedServer = SelectedProfile.Server.Base;

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
                                    finally { Wizard.TryInvoke((Action)(() => Wizard.TaskCount--)); }
                                })).Start();
                        },
                        () => ActiveSession == null && SelectedProfile != null);
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
                        () => ActiveSession != null);
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
