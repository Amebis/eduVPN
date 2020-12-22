/*
    eduVPN - VPN for education and research

    Copyright: 2017-2020 The Commons Conservancy eduVPN Programme
    SPDX-License-Identifier: GPL-3.0+
*/

using eduVPN.Models;
using eduVPN.ViewModels.VPN;
using eduVPN.ViewModels.Windows;
using Prism.Commands;
using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Threading;
using System.Windows.Threading;

namespace eduVPN.ViewModels.Pages
{
    /// <summary>
    /// Connection status wizard page
    /// </summary>
    public class ConnectionPage : ConnectWizardPage
    {
        #region Fields

        /// <summary>
        /// Profiles refresh cancellation token
        /// </summary>
        private CancellationTokenSource ProfilesRefreshInProgress;

        #endregion

        #region Properties

        /// <summary>
        /// Authenticating server
        /// </summary>
        /// <remarks><c>null</c> if none selected.</remarks>
        public Server AuthenticatingServer
        {
            get { return _AuthenticatingServer; }
            set { SetProperty(ref _AuthenticatingServer, value); }
        }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private Server _AuthenticatingServer;

        /// <summary>
        /// Connecting server
        /// </summary>
        /// <remarks><c>null</c> if none selected.</remarks>
        public Server ConnectingServer
        {
            get { return _ConnectingServer; }
            set
            {
                if (SetProperty(ref _ConnectingServer, value))
                {
                    ProfilesRefreshInProgress?.Cancel();
                    ProfilesRefreshInProgress = new CancellationTokenSource();
                    Profiles = null;
                    SelectedProfile = null;
                    new Thread(new ParameterizedThreadStart(
                        (object ctObj) =>
                        {
                            var ct = CancellationTokenSource.CreateLinkedTokenSource((CancellationToken)ctObj, Window.Abort.Token).Token;
                            Wizard.Dispatcher.Invoke(DispatcherPriority.Normal, (Action)(() => Wizard.TaskCount++));
                            try
                            {
                                if (ConnectingServer != null)
                                {
                                    var list = ConnectingServer.GetProfileList(AuthenticatingServer, ct);
                                    Wizard.Dispatcher.Invoke(DispatcherPriority.Normal, (Action)(() =>
                                    {
                                        //ct.WaitHandle.WaitOne(10000); // Mock a slow link for testing.
                                        Profiles = list;

                                        // Auto-connect when there is exactly one profile.
                                        if (list.Count == 1)
                                        {
                                            SelectedProfile = list[0];
                                            if (StartSession.CanExecute(list[0]))
                                                StartSession.Execute(list[0]);
                                        }
                                    }));
                                }
                                else
                                    Wizard.Dispatcher.Invoke(DispatcherPriority.Normal, (Action)(() => Profiles = new ObservableCollection<Profile>()));
                            }
                            catch (OperationCanceledException) { }
                            catch (Exception ex) { Wizard.Dispatcher.Invoke(DispatcherPriority.Normal, (Action)(() => Wizard.Error = ex)); }
                            finally { Wizard.Dispatcher.Invoke(DispatcherPriority.Normal, (Action)(() => Wizard.TaskCount--)); }
                        })).Start(ProfilesRefreshInProgress.Token);
                }
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
                    ConfirmProfileSelection.RaiseCanExecuteChanged();
            }
        }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private Profile _SelectedProfile;

        /// <summary>
        /// Confirms profile selection
        /// </summary>
        public DelegateCommand ConfirmProfileSelection { get; }


        /// <summary>
        /// VPN session queue - session 0 is the active session
        /// </summary>
        public ObservableCollection<VPNSession> Sessions { get; } = new ObservableCollection<VPNSession>();

        /// <summary>
        /// Active VPN session
        /// </summary>
        public VPNSession ActiveSession
        {
            get { return Sessions.Count > 0 ? Sessions[0] : VPNSession.Blank; }
        }

        /// <summary>
        /// Starts VPN session
        /// </summary>
        public DelegateCommand<Profile> StartSession { get; }

        /// <summary>
        /// Connection info command
        /// </summary>
        public DelegateCommand SessionInfo { get; }

        /// <inheritdoc/>
        public override DelegateCommand NavigateBack { get; }

        #endregion

        #region Constructors

        /// <summary>
        /// Constructs a status wizard page
        /// </summary>
        /// <param name="wizard">The connecting wizard</param>
        public ConnectionPage(ConnectWizard wizard) :
            base(wizard)
        {
            ConfirmProfileSelection = new DelegateCommand(
                () =>
                {
                    try
                    {
                        if (ActiveSession.ConnectingProfile != null && ActiveSession.ConnectingProfile.Equals(SelectedProfile))
                        {
                            if (ActiveSession.Disconnect.CanExecute())
                                ActiveSession.Disconnect.Execute();
                        }
                        else
                        {
                            if (StartSession.CanExecute(SelectedProfile))
                                StartSession.Execute(SelectedProfile);
                        }
                    }
                    catch (Exception ex) { Wizard.Error = ex; }
                },
                () => SelectedProfile != null);

            StartSession = new DelegateCommand<Profile>(
                profile =>
                {
                    try
                    {
                        // Switch to this page, for user to see the progress.
                        Wizard.CurrentPage = this;

                        // Note: Sessions locking is not required, since all queue manipulation is done exclusively in the UI thread.

                        if (Sessions.Count > 0)
                        {
                            var s = Sessions[Sessions.Count - 1];
                            if (s.ConnectingProfile.Equals(profile))
                            {
                                // Wizard is already running (or scheduled to run) a VPN session of the same configuration as specified.
                                return;
                            }
                        }

                        Server authenticatingServer = profile.Server;
                        if (authenticatingServer is SecureInternetServer)
                        {
                            var org = Wizard.GetDiscoveredOrganization(Properties.Settings.Default.SecureInternetOrganization);
                            authenticatingServer = Wizard.GetDiscoveredServer<SecureInternetServer>(org.SecureInternetBase);
                        }

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
                                        authenticatingServer,
                                        profile))
                                    {
                                        VPNSession previousSession = null;
                                        Wizard.Dispatcher.Invoke(DispatcherPriority.Normal, (Action)(
                                            () =>
                                            {
                                                if (Sessions.Count > 0)
                                                {
                                                    // Trigger disconnection of the previous session.
                                                    previousSession = Sessions[Sessions.Count - 1];
                                                    if (previousSession.Disconnect.CanExecute())
                                                        previousSession.Disconnect.Execute();
                                                }

                                                // Add our session to the queue.
                                                Sessions.Add(session);
                                                RaisePropertyChanged(nameof(ActiveSession));
                                                SessionInfo.RaiseCanExecuteChanged();
                                            }));
                                        try
                                        {
                                            if (previousSession != null)
                                            {
                                                // Await for the previous session to finish.
                                                if (WaitHandle.WaitAny(new WaitHandle[] { Window.Abort.Token.WaitHandle, previousSession.Finished }) == 0)
                                                    throw new OperationCanceledException();
                                            }

                                            // Set profile to auto-start on next launch.
                                            Properties.Settings.Default.AutoStartProfile = new eduVPN.Xml.StartSessionParams
                                            {
                                                ConnectingServer = profile.Server.Base,
                                                ProfileId = profile.Id
                                            };

                                            // Run our session.
                                            Wizard.Dispatcher.Invoke(DispatcherPriority.Normal, (Action)(() => Wizard.TaskCount--));
                                            try { session.Run(); }
                                            finally { Wizard.Dispatcher.Invoke(DispatcherPriority.Normal, (Action)(() => Wizard.TaskCount++)); }
                                        }
                                        finally
                                        {
                                            // Remove our session from the queue.
                                            Wizard.Dispatcher.Invoke(DispatcherPriority.Normal, (Action)(
                                                () =>
                                                {
                                                    Sessions.Remove(session);
                                                    RaisePropertyChanged(nameof(ActiveSession));
                                                    SessionInfo.RaiseCanExecuteChanged();
                                                    if (Sessions.Count <= 0 && Wizard.CurrentPage == this)
                                                    {
                                                        // No more sessions and user is still on the status page. Redirect the wizard back.
                                                        Wizard.CurrentPage = Wizard.HomePage;
                                                    }
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
                profile => profile != null && profile.Server != null);

            SessionInfo = new DelegateCommand(
                () =>
                {
                    try { Wizard.CurrentPage = this; }
                    catch (Exception ex) { Wizard.Error = ex; }
                },
                () => Sessions.Count > 0);

            NavigateBack = new DelegateCommand(
                () =>
                {
                    try
                    {
                        if (ActiveSession.Disconnect.CanExecute())
                            ActiveSession.Disconnect.Execute();
                        Wizard.CurrentPage = Wizard.HomePage;
                    }
                    catch (Exception ex) { Wizard.Error = ex; }
                });
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
                if (StartSession.CanExecute(Profiles[0]))
                    StartSession.Execute(Profiles[0]);
            }
        }

        #endregion
    }
}
