/*
    eduVPN - VPN for education and research

    Copyright: 2017-2021 The Commons Conservancy eduVPN Programme
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
            Initial = 0,

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

        /// <summary>
        /// Profiles refresh thread
        /// </summary>
        private Thread ProfilesRefreshThread;

        #endregion

        #region Properties

        /// <summary>
        /// The state connection page is in
        /// </summary>
        public StateType State
        {
            get { return _State; }
            private set { SetProperty(ref _State, value); }
        }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private StateType _State;

        /// <summary>
        /// Connecting server
        /// </summary>
        /// <remarks><c>null</c> if none selected.</remarks>
        public Server ConnectingServer
        {
            get { return _ConnectingServer; }
            set
            {
                if (_ConnectingServer == value)
                    return;

                ProfilesRefreshInProgress?.Cancel();
                ProfilesRefreshThread?.Join();

                SetProperty(ref _ConnectingServer, value);
                SelectedProfile = null;
                Profiles = null;
                if (ConnectingServer == null)
                    return;

                ProfilesRefreshInProgress = new CancellationTokenSource();
                ProfilesRefreshThread = new Thread(new ThreadStart(
                    () =>
                    {
                        var ct = CancellationTokenSource.CreateLinkedTokenSource(ProfilesRefreshInProgress.Token, Window.Abort.Token).Token;
                        Wizard.Dispatcher.Invoke(DispatcherPriority.Normal, (Action)(() => Wizard.TaskCount++));
                        try
                        {
                            var list = ConnectingServer.GetProfileList(Wizard.GetAuthenticatingServer(ConnectingServer), ct);
                            //ct.WaitHandle.WaitOne(10000); // Mock a slow link for testing.
                            Wizard.Dispatcher.Invoke(DispatcherPriority.Normal, (Action)(() =>
                            {
                                Profiles = list;

                                Profile profile = null;
                                if (Properties.Settings.Default.LastSelectedProfile.TryGetValue(ConnectingServer.Base.AbsoluteUri, out var profileId))
                                    profile = list.FirstOrDefault(p => p.Id == profileId);
                                if (profile == null && list.Count > 0)
                                    profile = list[0];
                                SelectedProfile = profile;

                                // Auto-connect when there is exactly one profile.
                                if (list.Count == 1 && StartSession.CanExecute())
                                    StartSession.Execute();
                            }));
                        }
                        catch (OperationCanceledException) { }
                        catch (Exception ex) { Wizard.Dispatcher.Invoke(DispatcherPriority.Normal, (Action)(() => Wizard.Error = ex)); }
                        finally { Wizard.Dispatcher.Invoke(DispatcherPriority.Normal, (Action)(() => Wizard.TaskCount--)); }
                    }));
                ProfilesRefreshThread.Start();
            }
        }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private Server _ConnectingServer;

        /// <summary>
        /// List of available profiles
        /// </summary>
        public ObservableCollection<Profile> Profiles
        {
            get { return _Profiles; }
            private set { SetProperty(ref _Profiles, value); }
        }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private ObservableCollection<Profile> _Profiles;

        /// <summary>
        /// Selected profile
        /// </summary>
        /// <remarks><c>null</c> if none selected.</remarks>
        public Profile SelectedProfile
        {
            get { return _SelectedProfile; }
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
        public VPNSession ActiveSession
        {
            get { return _ActiveSession; }
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
        private VPNSession _ActiveSession;

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
                            try
                            {
                                // Launch the VPN session in the background.
                                new Thread(new ThreadStart(
                                    () =>
                                    {
                                        Wizard.Dispatcher.Invoke(DispatcherPriority.Normal, (Action)(() => Wizard.TaskCount++));
                                        try
                                        {
                                            // Create our new session.
                                            using (var session = new OpenVPNSession(
                                                    Properties.Settings.Default.OpenVPNInteractiveServiceInstance,
                                                    Wizard,
                                                    SelectedProfile))
                                            {
                                                var finalState = StateType.Initial;
                                                Wizard.Dispatcher.Invoke(DispatcherPriority.Normal, (Action)(
                                                    () =>
                                                    {
                                                        session.Disconnect.CanExecuteChanged += (object sender, EventArgs e) => _ToggleSession?.RaiseCanExecuteChanged();
                                                        session.PropertyChanged += (object sender, PropertyChangedEventArgs e) =>
                                                        {
                                                            if ((e.PropertyName == nameof(session.State) || e.PropertyName == nameof(session.Expired)) &&
                                                                (session.State == VPNSessionStatusType.Initializing || session.State == VPNSessionStatusType.Connecting || session.State == VPNSessionStatusType.Connected || session.State == VPNSessionStatusType.Disconnecting) &&
                                                                session.Expired)
                                                            {
                                                                finalState = StateType.SessionExpired;
                                                                if (session.Disconnect.CanExecute())
                                                                    session.Disconnect.Execute();
                                                            }
                                                        };
                                                        ActiveSession = session;
                                                        State = StateType.SessionActive;
                                                    }));
                                                try
                                                {
                                                    // Set profile to auto-start on next launch.
                                                    Properties.Settings.Default.AutoStartProfile = new eduVPN.Xml.StartSessionParams
                                                    {
                                                        ConnectingServer = SelectedProfile.Server.Base,
                                                        ProfileId = SelectedProfile.Id
                                                    };

                                                    // Run our session.
                                                    Wizard.Dispatcher.Invoke(DispatcherPriority.Normal, (Action)(() => Wizard.TaskCount--));
                                                    try { session.Run(); }
                                                    finally { Wizard.Dispatcher.Invoke(DispatcherPriority.Normal, (Action)(() => Wizard.TaskCount++)); }
                                                }
                                                finally
                                                {
                                                    Wizard.Dispatcher.Invoke(DispatcherPriority.Normal, (Action)(() =>
                                                        {
                                                            ActiveSession = null;
                                                            State = finalState;
                                                        }));
                                                }
                                            }
                                        }
                                        catch (OperationCanceledException) { }
                                        catch (Exception ex) { Wizard.Dispatcher.Invoke(DispatcherPriority.Normal, (Action)(() => Wizard.Error = ex)); }
                                        finally { Wizard.Dispatcher.Invoke(DispatcherPriority.Normal, (Action)(() => Wizard.TaskCount--)); }
                                    })).Start();
                            }
                            catch (Exception ex) { Wizard.Error = ex; }
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
                        () =>
                        {
                            try
                            {
                                if (ActiveSession != null && ActiveSession.Disconnect.CanExecute())
                                    ActiveSession.Disconnect.Execute();
                                else if (ActiveSession == null && StartSession.CanExecute())
                                    StartSession.Execute();
                            }
                            catch (Exception ex) { Wizard.Error = ex; }
                        },
                        () =>
                            ActiveSession != null && ActiveSession.Disconnect.CanExecute() ||
                            ActiveSession == null && StartSession.CanExecute());
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
                        () =>
                        {
                            try { Wizard.CurrentPage = this; }
                            catch (Exception ex) { Wizard.Error = ex; }
                        },
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
                        () =>
                        {
                            try { Wizard.CurrentPage = Wizard.HomePage; }
                            catch (Exception ex) { Wizard.Error = ex; }
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

        /// <inheritdoc/>
        public override void OnActivate()
        {
            base.OnActivate();

            // When there are profiles available from before (connecting to the same server again),
            // and there is exactly one profile available, auto-connect.
            if (Profiles != null && Profiles.Count == 1)
            {
                SelectedProfile = Profiles[0];
                if (StartSession.CanExecute())
                    StartSession.Execute();
            }
        }

        #endregion
    }
}
