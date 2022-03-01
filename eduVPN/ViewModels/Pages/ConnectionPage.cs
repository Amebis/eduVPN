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
            /// A session is active
            /// </summary>
            SessionActive,

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

        #endregion

        #region Properties

        /// <summary>
        /// The state connection page is in
        /// </summary>
        public StateType State
        {
            get => _State;
            private set => SetProperty(ref _State, value);
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
                    if (ConnectingServer != null && SelectedProfile != null)
                        Properties.Settings.Default.LastSelectedProfile[ConnectingServer.Base.AbsoluteUri] = SelectedProfile.Id;
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
                    _StartSession?.RaiseCanExecuteChanged();
                    _ToggleSession?.RaiseCanExecuteChanged();
                    _SessionInfo?.RaiseCanExecuteChanged();
                    _NavigateBack?.RaiseCanExecuteChanged();
                }
            }
        }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private Session _ActiveSession;

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
                            var session = new OpenVPNSession(Wizard, SelectedProfile);
                            var finalState = StateType.SessionInactive;
                            session.Disconnect.CanExecuteChanged += (object sender, EventArgs e) => _ToggleSession?.RaiseCanExecuteChanged();
                            session.PropertyChanged += (object sender, PropertyChangedEventArgs e) =>
                            {
                                if ((e.PropertyName == nameof(session.State) || e.PropertyName == nameof(session.Expired)) &&
                                    (session.State == SessionStatusType.Initializing || session.State == SessionStatusType.Connecting || session.State == SessionStatusType.Connected || session.State == SessionStatusType.Disconnecting) &&
                                    session.Expired)
                                {
                                    finalState = StateType.SessionExpired;
                                    if (session.Disconnect.CanExecute())
                                        session.Disconnect.Execute();
                                }

                                if (e.PropertyName == nameof(session.State) && session.State == SessionStatusType.Disconnected)
                                {
                                    ActiveSession = null;
                                    State = finalState;
                                }
                            };

                            ActiveSession = session;
                            State = StateType.SessionActive;

                            // Set server/profile to auto-start on next launch.
                            Properties.Settings.Default.LastSelectedServer = SelectedProfile.Server.Base;

                            session.Start();
                        },
                        () => SelectedProfile != null && ActiveSession == null);
                return _StartSession;
            }
        }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private DelegateCommand _StartSession;

        /// <summary>
        /// Activates a VPN session for selected profile or deactivates the active VPN session
        /// </summary>
        public DelegateCommand ToggleSession
        {
            get
            {
                if (_ToggleSession == null)
                {
                    _ToggleSession = new DelegateCommand(
                        async () =>
                        {
                            if (ActiveSession != null && ActiveSession.Disconnect.CanExecute())
                                ActiveSession.Disconnect.Execute();
                            else if (StartSession.CanExecute())
                            {
                                await Wizard.AuthorizationPage.TriggerAuthorizationAsync(Wizard.GetAuthenticatingServer(ConnectingServer));
                                StartSession.Execute();
                            }
                        },
                        () =>
                            ActiveSession != null && ActiveSession.Disconnect.CanExecute() ||
                            StartSession.CanExecute());
                    StartSession.CanExecuteChanged += (object sender, EventArgs e) => _ToggleSession.RaiseCanExecuteChanged();
                }
                return _ToggleSession;
            }
        }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private DelegateCommand _ToggleSession;

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
