/*
    eduVPN - VPN for education and research

    Copyright: 2017-2019 The Commons Conservancy eduVPN Programme
    SPDX-License-Identifier: GPL-3.0+
*/

using eduOAuth;
using eduVPN.Models;
using eduVPN.ViewModels.Pages;
using eduVPN.ViewModels.VPN;
using Microsoft.Win32;
using Prism.Commands;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using System.Windows.Threading;

namespace eduVPN.ViewModels.Windows
{
    /// <summary>
    /// Connect wizard
    /// </summary>
    public class ConnectWizard : Window
    {
        #region Fields

        /// <summary>
        /// The alpha factor to increase/decrease popularity
        /// </summary>
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private static readonly float _popularity_alpha = 0.1f;

        #endregion

        #region Properties

        /// <summary>
        /// Available instance sources
        /// </summary>
        public InstanceSource[] InstanceSources { get; }

        /// <summary>
        /// Are instance sources available?
        /// </summary>
        public bool HasInstanceSources
        {
            get
            {
                for (var source_index = (int)InstanceSourceType._start; source_index < (int)InstanceSourceType._end; source_index++)
                    if (InstanceSources[source_index] != null && InstanceSources[source_index].InstanceList.Count > 0)
                        return true;

                return false;
            }
        }

        /// <summary>
        /// Selected instance source
        /// </summary>
        /// <remarks>This property is used in a process of adding new instance/profile.</remarks>
        public InstanceSourceType InstanceSourceType
        {
            get { return _instance_source_type; }
            set
            {
                if (SetProperty(ref _instance_source_type, value))
                {
                    RaisePropertyChanged(nameof(InstanceSource));
                    RaisePropertyChanged(nameof(AuthenticatingInstanceSelectPage));
                }
            }
        }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private InstanceSourceType _instance_source_type;

        /// <summary>
        /// Selected instance source
        /// </summary>
        /// <remarks>This property is used in a process of adding new instance/profile.</remarks>
        public InstanceSource InstanceSource
        {
            get { return InstanceSources[(int)_instance_source_type]; }
        }

        /// <summary>
        /// VPN session queue - session 0 is the active session
        /// </summary>
        public ObservableCollection<VPNSession> Sessions { get; }

        /// <summary>
        /// Active VPN session
        /// </summary>
        public VPNSession ActiveSession
        {
            get { return Sessions.Count > 0 ? Sessions[0] : VPNSession.Blank; }
        }

        /// <summary>
        /// Connection info command
        /// </summary>
        public DelegateCommand SessionInfo
        {
            get
            {
                if (_session_info == null)
                {
                    _session_info = new DelegateCommand(
                        // execute
                        () => CurrentPage = StatusPage,

                        // canExecute
                        () => Sessions.Count > 0);

                    // Setup canExecute refreshing.
                    Sessions.CollectionChanged += (object sender, NotifyCollectionChangedEventArgs e) => _session_info.RaiseCanExecuteChanged();
                }

                return _session_info;
            }
        }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private DelegateCommand _session_info;

        /// <summary>
        /// Navigate to a pop-up page command
        /// </summary>
        public DelegateCommand<ConnectWizardPopupPage> NavigateTo
        {
            get
            {
                if (_navigate_to == null)
                    _navigate_to = new DelegateCommand<ConnectWizardPopupPage>(
                        // execute
                        page =>
                        {
                            ChangeTaskCount(+1);
                            try { CurrentPopupPage = page; }
                            catch (Exception ex) { Error = ex; }
                            finally { ChangeTaskCount(-1); }
                        });

                return _navigate_to;
            }
        }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private DelegateCommand<ConnectWizardPopupPage> _navigate_to;

        /// <summary>
        /// Starts VPN session
        /// </summary>
        public DelegateCommand<StartSessionParams> StartSession
        {
            get
            {
                if (_start_session == null)
                    _start_session = new DelegateCommand<StartSessionParams>(
                        // execute
                        param =>
                        {
                            // Switch to the status page, for user to see the progress.
                            CurrentPage = StatusPage;

                            // Note: Sessions locking is not required, since all queue manipulation is done exclusively in the UI thread.

                            var source_index = (int)param.InstanceSourceType;
                            var authenticating_instance = InstanceSources[source_index].GetAuthenticatingInstance(param.ConnectingProfile.Instance);

                            if (Sessions.Count > 0)
                            {
                                var s = Sessions[Sessions.Count - 1];
                                if (s.ConnectingProfile.Equals(param.ConnectingProfile))
                                {
                                    // Wizard is already running (or scheduled to run) a VPN session of the same configuration as specified.
                                    return;
                                }
                            }

                            // Launch the VPN session in the background.
                            new Thread(new ThreadStart(
                                () =>
                                {
                                    Dispatcher.Invoke(DispatcherPriority.Normal, (Action)(() => ChangeTaskCount(+1)));
                                    try
                                    {
                                        // Create our new session.
                                        using (var session = new OpenVPNSession(
                                            Properties.Settings.Default.OpenVPNInteractiveServiceInstance,
                                            this,
                                            authenticating_instance,
                                            param.ConnectingProfile))
                                        {
                                            VPNSession previous_session = null;
                                            Dispatcher.Invoke(DispatcherPriority.Normal, (Action)(
                                                () =>
                                                {
                                                    if (Sessions.Count > 0)
                                                    {
                                                        // Trigger disconnection of the previous session.
                                                        previous_session = Sessions[Sessions.Count - 1];
                                                        if (previous_session.Disconnect.CanExecute())
                                                            previous_session.Disconnect.Execute();
                                                    }

                                                    // Add our session to the queue.
                                                    Sessions.Add(session);
                                                }));
                                            try
                                            {
                                                if (previous_session != null)
                                                {
                                                    // Await for the previous session to finish.
                                                    if (WaitHandle.WaitAny(new WaitHandle[] { Abort.Token.WaitHandle, previous_session.Finished }) == 0)
                                                        throw new OperationCanceledException();
                                                }

                                                // Run our session.
                                                Dispatcher.Invoke(DispatcherPriority.Normal, (Action)(() => ChangeTaskCount(-1)));
                                                try { session.Run(); }
                                                finally { Dispatcher.Invoke(DispatcherPriority.Normal, (Action)(() => ChangeTaskCount(+1))); }
                                            }
                                            finally
                                            {
                                                // Remove our session from the queue.
                                                Dispatcher.Invoke(DispatcherPriority.Normal, (Action)(
                                                    () =>
                                                    {
                                                        Sessions.Remove(session);

                                                        if (Sessions.Count <= 0 && CurrentPage == StatusPage)
                                                        {
                                                            // No more sessions and user is still on the status page. Redirect the wizard back.
                                                            CurrentPage = RecentConfigurationSelectPage;
                                                        }
                                                    }));
                                            }
                                        }
                                    }
                                    catch (OperationCanceledException) { }
                                    catch (Exception ex) { Dispatcher.Invoke(DispatcherPriority.Normal, (Action)(() => Error = ex)); }
                                    finally { Dispatcher.Invoke(DispatcherPriority.Normal, (Action)(() => ChangeTaskCount(-1))); }
                                })).Start();

                            // Do the instance source book-keeping.
                            if (InstanceSources[source_index] is LocalInstanceSource instance_source_local)
                            {
                                // Local authenticating instance source
                                var profile_found = false;
                                foreach (var profile in instance_source_local.ConnectingProfileList)
                                {
                                    if (profile.Equals(param.ConnectingProfile))
                                    {
                                        // Upvote profile popularity.
                                        profile.Popularity = profile.Popularity * (1.0f - _popularity_alpha) + 1.0f * _popularity_alpha;
                                        profile_found = true;
                                    }
                                    else
                                    {
                                        // Downvote profile popularity.
                                        profile.Popularity = profile.Popularity * (1.0f - _popularity_alpha) /*+ 0.0f * _popularity_alpha*/;
                                    }
                                }
                                if (!profile_found)
                                {
                                    // Add connecting profile to the list.
                                    instance_source_local.ConnectingProfileList.Add(param.ConnectingProfile);
                                }
                                if (Properties.Settings.Default.ConnectingProfileSelectMode == 2)
                                {
                                    // Add all profiles of connecting instance to the list. (Profile list is already cached by now. Otherwise it would need to be spawned as a background task to avoid deadlock.)
                                    foreach (var profile in param.ConnectingProfile.Instance.GetProfileList(authenticating_instance, Abort.Token))
                                        if (instance_source_local.ConnectingProfileList.FirstOrDefault(prof => prof.Equals(profile)) == null)
                                        {
                                            // Downvote profile popularity.
                                            profile.Popularity = profile.Popularity * (1.0f - _popularity_alpha) /*+ 0.0f * _popularity_alpha*/;

                                            // Add sibling profile to the list.
                                            instance_source_local.ConnectingProfileList.Add(profile);
                                        }
                                }

                                var instance_found = false;
                                foreach (var instance in instance_source_local.ConnectingInstanceList)
                                {
                                    if (instance.Equals(param.ConnectingProfile.Instance))
                                    {
                                        // Upvote instance popularity.
                                        instance.Popularity = instance.Popularity * (1.0f - _popularity_alpha) + 1.0f * _popularity_alpha;
                                        instance_found = true;
                                    }
                                    else
                                    {
                                        // Downvote instance popularity.
                                        instance.Popularity = instance.Popularity * (1.0f - _popularity_alpha) /*+ 0.0f * _popularity_alpha*/;
                                    }
                                }
                                if (!instance_found)
                                {
                                    // Add connecting instance to the list.
                                    instance_source_local.ConnectingInstanceList.Add(param.ConnectingProfile.Instance);
                                }
                            }
                            else if (InstanceSources[source_index] is DistributedInstanceSource instance_source_distributed)
                            {
                                // Distributed authenticating instance source
                                instance_source_distributed.AuthenticatingInstance = authenticating_instance;
                            }
                            else if (InstanceSources[source_index] is FederatedInstanceSource instance_source_federated)
                            {
                                // Federated authenticating instance source
                            }
                            else
                                throw new InvalidOperationException();

                            // Update settings.
                            Properties.Settings.Default[Properties.Settings.InstanceDirectoryId[source_index] + "InstanceSourceSettings"] = new Xml.InstanceSourceSettings() { InstanceSource = InstanceSources[source_index].ToSettings() };
                        },

                        // canExecute
                        param =>
                            param is StartSessionParams &&
                            param.ConnectingProfile != null &&
                            param.ConnectingProfile.Instance != null);

                return _start_session;
            }
        }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private DelegateCommand<StartSessionParams> _start_session;

        /// <summary>
        /// <see cref="StartSession"/> command parameter set
        /// </summary>
        public class StartSessionParams
        {
            #region Properties

            /// <summary>
            /// Instance source
            /// </summary>
            public InstanceSourceType InstanceSourceType { get; }

            /// <summary>
            /// Connecting eduVPN instance profile
            /// </summary>
            public Profile ConnectingProfile { get; }

            #endregion

            #region Constructors

            /// <summary>
            /// Constructs a <see cref="StartSession"/> command parameter set
            /// </summary>
            /// <param name="instance_source_type">Instance source type</param>
            /// <param name="connecting_profile">Connecting eduVPN instance profile</param>
            public StartSessionParams(InstanceSourceType instance_source_type, Profile connecting_profile)
            {
                InstanceSourceType = instance_source_type;
                ConnectingProfile = connecting_profile;
            }

            #endregion
        }

        /// <summary>
        /// Occurs when instance requests authorization.
        /// </summary>
        /// <remarks>Sender is the connection wizard <see cref="ConnectWizard"/>.</remarks>
        public event EventHandler<RequestInstanceAuthorizationEventArgs> RequestInstanceAuthorization;

        /// <summary>
        /// Occurs when OpenVPN requests a password.
        /// </summary>
        /// <remarks>Sender is the OpenVPN session <see cref="OpenVPNSession"/>.</remarks>
        public event EventHandler<eduOpenVPN.Management.PasswordAuthenticationRequestedEventArgs> RequestOpenVPNPasswordAuthentication;

        /// <summary>
        /// Occurs when OpenVPN requests a username and password.
        /// </summary>
        /// <remarks>Sender is the OpenVPN session <see cref="OpenVPNSession"/>.</remarks>
        public event EventHandler<eduOpenVPN.Management.UsernamePasswordAuthenticationRequestedEventArgs> RequestOpenVPNUsernamePasswordAuthentication;

        /// <summary>
        /// Occurs when 2-Factor Authentication is requested.
        /// </summary>
        /// <remarks>Sender is the OpenVPN session <see cref="OpenVPNSession"/>.</remarks>
        public event EventHandler<eduOpenVPN.Management.UsernamePasswordAuthenticationRequestedEventArgs> RequestTwoFactorAuthentication;

        /// <summary>
        /// Occurs when product update is available.
        /// </summary>
        /// <remarks>Sender is the connection wizard <see cref="ConnectWizard"/>.</remarks>
        public event EventHandler<PromptSelfUpdateEventArgs> PromptSelfUpdate;

        /// <summary>
        /// Occurs when application should quit.
        /// </summary>
        /// <remarks>Sender is the connection wizard <see cref="ConnectWizard"/>.</remarks>
        public event EventHandler QuitApplication;

        #region Pages

        /// <summary>
        /// The page the wizard is currently displaying (if no pop-up page)
        /// </summary>
        public ConnectWizardPage CurrentPage
        {
            get { return _current_popup_page ?? _current_page; }
            set
            {
                if (value != _current_page)
                {
                    _current_page = value;
                    if (_current_page != null)
                        _current_page.OnActivate();

                    if (_current_popup_page == null)
                        RaisePropertyChanged();
                }
            }
        }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private ConnectWizardPage _current_page;

        /// <summary>
        /// The pop-up page the wizard is currently displaying
        /// </summary>
        public ConnectWizardPopupPage CurrentPopupPage
        {
            get { return _current_popup_page; }
            set
            {
                if (value != _current_popup_page)
                {
                    SetProperty(ref _current_popup_page, value);
                    if (_current_popup_page != null)
                        _current_popup_page.OnActivate();

                    RaisePropertyChanged(nameof(CurrentPage));
                }
            }
        }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private ConnectWizardPopupPage _current_popup_page;

        /// <summary>
        /// The first page of the wizard
        /// </summary>
        public ConnectWizardPage StartingPage
        {
            get
            {
                for (var source_index = (int)InstanceSourceType._start; source_index < (int)InstanceSourceType._end; source_index++)
                    if (InstanceSources[source_index]?.ConnectingInstance != null)
                        return RecentConfigurationSelectPage;

                if (HasInstanceSources)
                    return InstanceSourceSelectPage;
                else
                    return CustomInstancePage;
            }
        }

        /// <summary>
        /// Initializing wizard page
        /// </summary>
        public InitializingPage InitializingPage
        {
            get
            {
                if (_initializing_page == null)
                    _initializing_page = new InitializingPage(this);
                return _initializing_page;
            }
        }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private InitializingPage _initializing_page;

        /// <summary>
        /// Instance source page
        /// </summary>
        public InstanceSourceSelectPage InstanceSourceSelectPage
        {
            get
            {
                if (_instance_source_page == null)
                    _instance_source_page = new InstanceSourceSelectPage(this);
                return _instance_source_page;
            }
        }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private InstanceSourceSelectPage _instance_source_page;

        /// <summary>
        /// Authenticating instance selection page
        /// </summary>
        /// <remarks>Available only when authenticating and connecting instances can be different. I.e. <see cref="InstanceSource"/> is <see cref="LocalInstanceSource"/> or <see cref="DistributedInstanceSource"/>.</remarks>
        public AuthenticatingInstanceSelectPage AuthenticatingInstanceSelectPage
        {
            get
            {
                if (InstanceSource is LocalInstanceSource ||
                    InstanceSource is DistributedInstanceSource)
                {
                    // Only local and distrubuted authentication sources have this page.
                    // However, this page varies between Secure Internet and Institute Access.
                    switch (InstanceSourceType)
                    {
                        case InstanceSourceType.SecureInternet:
                            if (_authenticating_country_select_page == null)
                                _authenticating_country_select_page = new AuthenticatingCountrySelectPage(this);
                            return _authenticating_country_select_page;

                        case InstanceSourceType.InstituteAccess:
                            if (_authenticating_institute_select_page == null)
                                _authenticating_institute_select_page = new AuthenticatingInstituteSelectPage(this);
                            return _authenticating_institute_select_page;

                        default:
                            return null;
                    }
                }
                else
                    return null;
            }
        }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private AuthenticatingCountrySelectPage _authenticating_country_select_page;

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private AuthenticatingInstituteSelectPage _authenticating_institute_select_page;

        /// <summary>
        /// Custom instance source page
        /// </summary>
        public CustomInstancePage CustomInstancePage
        {
            get
            {
                if (_custom_instance_page == null)
                    _custom_instance_page = new CustomInstancePage(this);
                return _custom_instance_page;
            }
        }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private CustomInstancePage _custom_instance_page;

        /// <summary>
        /// Recent profile selection wizard page
        /// </summary>
        public RecentConfigurationSelectPage RecentConfigurationSelectPage
        {
            get
            {
                if (_recent_configuration_select_page == null)
                    _recent_configuration_select_page = new RecentConfigurationSelectPage(this);
                return _recent_configuration_select_page;
            }
        }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private RecentConfigurationSelectPage _recent_configuration_select_page;

        /// <summary>
        /// Profile selection wizard page
        /// </summary>
        public ConnectingProfileSelectPage ConnectingProfileSelectPage
        {
            get
            {
                if (_profile_select_page == null)
                    _profile_select_page = new ConnectingProfileSelectPage(this);
                return _profile_select_page;
            }
        }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private ConnectingProfileSelectPage _profile_select_page;

        /// <summary>
        /// Status wizard page
        /// </summary>
        public StatusPage StatusPage
        {
            get
            {
                if (_status_page == null)
                    _status_page = new StatusPage(this);
                return _status_page;
            }
        }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private StatusPage _status_page;

        /// <summary>
        /// Settings wizard page
        /// </summary>
        public SettingsPage SettingsPage
        {
            get
            {
                if (_settings_page == null)
                    _settings_page = new SettingsPage(this);
                return _settings_page;
            }
        }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private SettingsPage _settings_page;

        /// <summary>
        /// About wizard page
        /// </summary>
        public AboutPage AboutPage
        {
            get
            {
                if (_about_page == null)
                    _about_page = new AboutPage(this);
                return _about_page;
            }
        }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private AboutPage _about_page;

        /// <summary>
        /// Self-update wizard page
        /// </summary>
        public SelfUpdatingPage SelfUpdatingPage
        {
            get
            {
                if (_self_updating_page == null)
                    _self_updating_page = new SelfUpdatingPage(this);
                return _self_updating_page;
            }
        }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private SelfUpdatingPage _self_updating_page;

        #endregion

        #endregion

        #region Constructors

        /// <summary>
        /// Constructs the wizard
        /// </summary>
        public ConnectWizard()
        {
            // Create session queue.
            Sessions = new ObservableCollection<VPNSession>();
            Sessions.CollectionChanged += (object sender, NotifyCollectionChangedEventArgs e) => RaisePropertyChanged(nameof(ActiveSession));

            // Show initializing wizard page.
            _current_page = InitializingPage;

            int
                source_type_start = (int)InstanceSourceType._start,
                source_type_end = (int)InstanceSourceType._end;
            InstanceSources = new InstanceSource[source_type_end];

            // Setup initialization.
            var worker = new BackgroundWorker() { WorkerReportsProgress = true };
            worker.DoWork += (object sender, DoWorkEventArgs e) =>
            {
                // Setup progress feedback.
                Dispatcher.Invoke(DispatcherPriority.Normal, (Action)(() => InitializingPage.Progress = new Range<int>(0, (source_type_end - source_type_start) * 2, 0)));

                // Spawn instance source loading threads.
                Parallel.For(source_type_start, source_type_end, source_index =>
                {
                    do
                    {
                        int ticks = 0;
                        object ticks_lock = new object();

                        Dispatcher.Invoke(DispatcherPriority.Normal, (Action)(() => ChangeTaskCount(+1)));
                        try
                        {
                            // Get instance source.
                            var source = Properties.Settings.Default.GetResourceRef(Properties.Settings.InstanceDirectoryId[source_index] + "Discovery");
                            if (source.Uri != null)
                            {
                                var obj_web = Properties.Settings.Default.ResponseCache.GetSeq(
                                    source.Uri,
                                    source.PublicKey,
                                    Abort.Token);

                                // Add a tick.
                                Dispatcher.Invoke(DispatcherPriority.Normal, (Action)(() => InitializingPage.Progress.Value++));
                                ticks++;

                                // Load instance source.
                                InstanceSources[source_index] = InstanceSource.FromJSON(obj_web);

                                {
                                    // Attach to instance events.
                                    if (InstanceSources[source_index] is FederatedInstanceSource instance_source_federated)
                                    {
                                        instance_source_federated.AuthenticatingInstance.RequestAuthorization += Instance_RequestAuthorization;
                                        instance_source_federated.AuthenticatingInstance.ForgetAuthorization += Instance_ForgetAuthorization;
                                    }

                                    foreach (var instance in InstanceSources[source_index].InstanceList)
                                    {
                                        instance.RequestAuthorization += Instance_RequestAuthorization;
                                        instance.ForgetAuthorization += Instance_ForgetAuthorization;
                                    }
                                }

                                // Import settings.
                                Xml.InstanceSourceSettingsBase h = (Properties.Settings.Default[Properties.Settings.InstanceDirectoryId[source_index] + "InstanceSourceSettings"] as Xml.InstanceSourceSettings)?.InstanceSource;
                                InstanceSources[source_index].FromSettings(this, h);

                                // Add a tick.
                                Dispatcher.Invoke(DispatcherPriority.Normal, (Action)(() => InitializingPage.Progress.Value++));
                                ticks++;
                            }
                            else
                            {
                                // Add a tick.
                                Dispatcher.Invoke(DispatcherPriority.Normal, (Action)(() => InitializingPage.Progress.Value++));
                                ticks++;

                                if (source_index == (int)InstanceSourceType.InstituteAccess)
                                {
                                    // Institute access is required. When it is missing the discovery JSON, use a blank one.
                                    InstanceSources[source_index] = new LocalInstanceSource();

                                    // Import settings.
                                    Xml.InstanceSourceSettingsBase h = (Properties.Settings.Default[Properties.Settings.InstanceDirectoryId[source_index] + "InstanceSourceSettings"] as Xml.InstanceSourceSettings)?.InstanceSource;
                                    InstanceSources[source_index].FromSettings(this, h);

                                    // Preset source type.
                                    Dispatcher.Invoke(DispatcherPriority.Normal, (Action)(() => InstanceSourceType = InstanceSourceType.InstituteAccess));
                                }

                                // Add a tick.
                                Dispatcher.Invoke(DispatcherPriority.Normal, (Action)(() => InitializingPage.Progress.Value++));
                                ticks++;
                            }

                            break;
                        }
                        catch (OperationCanceledException) { break; }
                        catch (Exception ex)
                        {
                            // Do not re-throw the exception.
                            Dispatcher.Invoke(DispatcherPriority.Normal, (Action)(() =>
                            {
                                // Notify the sender the instance source loading failed.
                                // This will overwrite all previous error messages.
                                Error = new AggregateException(String.Format(Resources.Strings.ErrorInstanceSourceInfoLoad, Properties.Settings.InstanceDirectoryId[source_index]), ex);

                                // Revert progress indicator value.
                                InitializingPage.Progress.Value -= ticks;
                            }));
                        }
                        finally { Dispatcher.Invoke(DispatcherPriority.Normal, (Action)(() => ChangeTaskCount(-1))); }
                    }
                    // Sleep for 3s, then retry.
                    while (!Abort.Token.WaitHandle.WaitOne(TimeSpan.FromSeconds(3)));
                });
            };

            // Setup initialization completition.
            worker.RunWorkerCompleted += (object sender, RunWorkerCompletedEventArgs e) =>
            {
                ChangeTaskCount(+1);
                try
                {
                    if (Abort.Token.IsCancellationRequested)
                        return;

                    // Proceed to the "first" page.
                    RaisePropertyChanged(nameof(HasInstanceSources));
                    RaisePropertyChanged(nameof(StartingPage));
                    CurrentPage = StartingPage;
                }
                finally { ChangeTaskCount(-1); }

                // Self-dispose.
                (sender as BackgroundWorker)?.Dispose();
            };

            worker.RunWorkerAsync();

            if (Properties.Settings.Default.SelfUpdateDescr?.Uri != null)
            {
                // Setup self-update.
                var self_update = new BackgroundWorker() { WorkerReportsProgress = true };
                self_update.DoWork += (object sender, DoWorkEventArgs e) =>
                {
                    var random = new Random();
                    do
                    {
                        Dispatcher.Invoke(DispatcherPriority.Normal, (Action)(() => ChangeTaskCount(+1)));
                        try
                        {
                            Dictionary<string, object> obj_web = null;
                            Version repo_version = null, product_version = null;

                            try
                            {
                                Parallel.ForEach(new List<Action>()
                                {
                                    () =>
                                    {
                                        // Get self-update.
                                        var discovery_uri = Properties.Settings.Default.SelfUpdateDescr.Uri;
                                        Trace.TraceInformation("Downloading self-update JSON discovery from {0}...", discovery_uri.AbsoluteUri);
                                        obj_web = Properties.Settings.Default.ResponseCache.GetSeq(
                                            discovery_uri,
                                            Properties.Settings.Default.SelfUpdateDescr.PublicKey,
                                            Abort.Token);

                                        repo_version = new Version((string)obj_web["version"]);
                                        Trace.TraceInformation("Online version: {0}", repo_version.ToString());
                                    },

                                    () =>
                                    {
                                        // Evaluate installed products.
                                        var product_id = Properties.Settings.Default.SelfUpdateBundleID.ToUpperInvariant();
                                        Trace.TraceInformation("Evaluating installed products...");
                                        using (var hklm_key = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry32))
                                        using (var uninstall_key = hklm_key.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Uninstall", false))
                                        {
                                            foreach (var product_key_name in uninstall_key.GetSubKeyNames())
                                            {
                                                Abort.Token.ThrowIfCancellationRequested();
                                                using (var product_key = uninstall_key.OpenSubKey(product_key_name))
                                                {
                                                    var bundle_upgrade_code = product_key.GetValue("BundleUpgradeCode");
                                                    if ((bundle_upgrade_code is string   bundle_upgrade_code_str   && bundle_upgrade_code_str.ToUpperInvariant() == product_id ||
                                                         bundle_upgrade_code is string[] bundle_upgrade_code_array && bundle_upgrade_code_array.FirstOrDefault(code => code.ToUpperInvariant() == product_id) != null) &&
                                                        product_key.GetValue("BundleVersion") is string bundle_version_str)
                                                    {
                                                        // Our product entry found.
                                                        product_version = new Version(product_key.GetValue("DisplayVersion") is string display_version_str ? display_version_str : bundle_version_str);
                                                        Trace.TraceInformation("Installed version: {0}", product_version.ToString());
                                                        break;
                                                    }
                                                }
                                            }
                                        }
                                    },
                                },
                                    action =>
                                    {
                                        Dispatcher.Invoke(DispatcherPriority.Normal, (Action)(() => ChangeTaskCount(+1)));
                                        try { action(); }
                                        finally { Dispatcher.Invoke(DispatcherPriority.Normal, (Action)(() => ChangeTaskCount(-1))); }
                                    });
                            }
                            catch (AggregateException ex)
                            {
                                var ex_non_cancelled = ex.InnerExceptions.Where(ex_inner => !(ex_inner is OperationCanceledException));
                                if (ex_non_cancelled.Any())
                                {
                                    // Some exceptions were issues beyond OperationCanceledException.
                                    throw new AggregateException(ex.Message, ex_non_cancelled.ToArray());
                                }
                                else
                                {
                                    // All exceptions were OperationCanceledException.
                                    throw new OperationCanceledException();
                                }
                            }

                            try
                            {
                                if (new Version(Properties.Settings.Default.SelfUpdateLastVersion) == repo_version &&
                                    (Properties.Settings.Default.SelfUpdateLastReminder == DateTime.MaxValue ||
                                    (DateTime.UtcNow - Properties.Settings.Default.SelfUpdateLastReminder).TotalDays < 3))
                                {
                                    // We already prompted user for this version.
                                    // Either user opted not to be reminded of this version update again,
                                    // or it has been less than three days since the last prompt.
                                    Trace.TraceInformation("Update deferred by user choice.");
                                    continue;
                                }
                            }
                            catch { }

                            if (product_version == null)
                            {
                                // Nothing to update.
                                Trace.TraceInformation("Product not installed or version could not be determined.");
                                return; // Quit self-updating.
                            }

                            if (repo_version <= product_version)
                            {
                                // Product already up-to-date.
                                Trace.TraceInformation("Update not required.");
                                continue;
                            }

                            // We're in the background thread - raise the prompt event via dispatcher.
                            Trace.TraceInformation("Prompting user to update...");
                            var e_prompt = new PromptSelfUpdateEventArgs(
                                product_version,
                                repo_version,
                                eduJSON.Parser.GetValue(obj_web, "changelog_uri", out string changelog) ? new Uri(changelog) : null);
                            Dispatcher.Invoke(DispatcherPriority.Normal, (Action)(() => PromptSelfUpdate?.Invoke(this, e_prompt)));

                            switch (e_prompt.Action)
                            {
                                case PromptSelfUpdateActionType.Update:
                                    // Pass control to the self-update page.
                                    SelfUpdatingPage.ObjWeb = obj_web;
                                    Dispatcher.Invoke(DispatcherPriority.Normal, (Action)(() =>
                                    {
                                        if (NavigateTo.CanExecute(SelfUpdatingPage))
                                            NavigateTo.Execute(SelfUpdatingPage);
                                    }));
                                    goto case PromptSelfUpdateActionType.AskLater;

                                case PromptSelfUpdateActionType.AskLater:
                                    // Mark the timestamp of the prompt.
                                    Trace.TraceInformation("User will be reminded after three days again. (Should the update be still required.)");
                                    Properties.Settings.Default.SelfUpdateLastReminder = DateTime.UtcNow;
                                    break;

                                case PromptSelfUpdateActionType.Skip:
                                    // Mark not to re-prompt again.
                                    Trace.TraceInformation("User choose to skip this update.");
                                    Properties.Settings.Default.SelfUpdateLastReminder = DateTime.MaxValue;
                                    break;
                            }

                            // Mark the version of this prompt.
                            Properties.Settings.Default.SelfUpdateLastVersion = repo_version.ToString();
                        }
                        catch (OperationCanceledException) { }
                        catch (Exception ex) { Dispatcher.Invoke(DispatcherPriority.Normal, (Action)(() => Error = ex)); }
                        finally { Dispatcher.Invoke(DispatcherPriority.Normal, (Action)(() => ChangeTaskCount(-1))); }
                    }
                    // Sleep for 23-24h, then retry.
                    while (!Abort.Token.WaitHandle.WaitOne(random.Next(23 * 60 * 60 * 1000, 24 * 60 * 60 * 1000)));
                };

                // Self-update completition.
                self_update.RunWorkerCompleted += (object sender, RunWorkerCompletedEventArgs e) =>
                {
                    // Self-dispose.
                    (sender as BackgroundWorker)?.Dispose();
                };

                self_update.RunWorkerAsync();
            }
        }

        #endregion

        #region Methods

        /// <summary>
        /// Triggers authorization for selected instance asynchronously
        /// </summary>
        /// <param name="instance">Instance</param>
        /// <returns>Asynchronous operation</returns>
        /// <exception cref="AccessTokenNullException">Authorization failed</exception>
        public async Task TriggerAuthorizationAsync(Instance instance)
        {
            var e = new RequestAuthorizationEventArgs("config");
            var authorization_task = new Task(() => Instance_RequestAuthorization(instance, e), Abort.Token, TaskCreationOptions.LongRunning);
            authorization_task.Start();
            await authorization_task;

            if (e.AccessToken == null)
                throw new AccessTokenNullException();
        }

        /// <summary>
        /// Called when an instance requests user authorization
        /// </summary>
        /// <param name="sender">Instance of type <see cref="Instance"/> requiring authorization</param>
        /// <param name="e">Authorization request event arguments</param>
        public void Instance_RequestAuthorization(object sender, RequestAuthorizationEventArgs e)
        {
            if (sender is Instance authenticating_instance)
            {
                e.TokenOrigin = RequestAuthorizationEventArgs.TokenOriginType.None;
                e.AccessToken = null;

                lock (Properties.Settings.Default.AccessTokenCache)
                {
                    if (e.SourcePolicy != RequestAuthorizationEventArgs.SourcePolicyType.ForceAuthorization)
                    {
                        var key = authenticating_instance.Base.AbsoluteUri;
                        if (Properties.Settings.Default.AccessTokenCache.TryGetValue(key, out var access_token))
                        {
                            if (e.ForceRefresh || access_token.Expires <= DateTime.Now)
                            {
                                // Token refresh was explicitly requested or the token expired. Refresh it.

                                // Get API endpoints. (Not called from the UI thread or already cached by now. Otherwise it would need to be spawned as a background task to avoid deadlock.)
                                var api = authenticating_instance.GetEndpoints(Abort.Token);

                                // Prepare web request.
                                var request = WebRequest.Create(api.TokenEndpoint);
                                request.CachePolicy = Xml.Response.CachePolicy;
                                request.Proxy = null;
                                if (request is HttpWebRequest request_http)
                                    request_http.UserAgent = Xml.Response.UserAgent;

                                try
                                {
                                    access_token = access_token.RefreshToken(request, null, Abort.Token);

                                    // Update access token cache.
                                    Properties.Settings.Default.AccessTokenCache[key] = access_token;

                                    // If we got here, return the token.
                                    e.TokenOrigin = RequestAuthorizationEventArgs.TokenOriginType.Refreshed;
                                    e.AccessToken = access_token;
                                    return;
                                }
                                catch (AccessTokenException ex)
                                {
                                    if (ex.ErrorCode == AccessTokenException.ErrorCodeType.InvalidGrant)
                                    {
                                        // The grant has been revoked. Drop the access token.
                                        Properties.Settings.Default.AccessTokenCache.Remove(key);
                                    }
                                    else
                                        throw;
                                }
                            }
                            else
                            {
                                // If we got here, return the token.
                                e.TokenOrigin = RequestAuthorizationEventArgs.TokenOriginType.Saved;
                                e.AccessToken = access_token;
                                return;
                            }
                        }
                    }

                    if (e.SourcePolicy != RequestAuthorizationEventArgs.SourcePolicyType.SavedOnly)
                    {
                        // Get API endpoints. (Not called from the UI thread or already cached by now. Otherwise it would need to be spawned as a background task to avoid deadlock.)
                        var api = authenticating_instance.GetEndpoints(Abort.Token);

                        // Prepare new authorization grant.
                        var authorization_grant = new AuthorizationGrant()
                        {
                            AuthorizationEndpoint = api.AuthorizationEndpoint,
                            ClientID = Properties.Settings.Default.ClientID + ".windows",
                            Scope = new HashSet<string>() { e.Scope },
                            CodeChallengeAlgorithm = AuthorizationGrant.CodeChallengeAlgorithmType.S256
                        };

                        // Re-raise this event as ConnectWizard event, to simplify view.
                        // This way the view can listen ConnectWizard for authorization events only.
                        var e_instance = new RequestInstanceAuthorizationEventArgs(authenticating_instance, authorization_grant);
                        if (Dispatcher.CurrentDispatcher == Dispatcher)
                        {
                            // We're in the GUI thread.
                            RequestInstanceAuthorization?.Invoke(this, e_instance);
                        }
                        else
                        {
                            // We're in the background thread - raise event via dispatcher.
                            Dispatcher.Invoke(DispatcherPriority.Normal, (Action)(() => RequestInstanceAuthorization?.Invoke(this, e_instance)));
                        }

                        // Get access token from authorization grant.
                        if (e_instance.CallbackURI != null)
                        {
                            // Prepare web request.
                            var request = WebRequest.Create(api.TokenEndpoint);
                            request.CachePolicy = Xml.Response.CachePolicy;
                            request.Proxy = null;
                            if (request is HttpWebRequest request_http)
                                request_http.UserAgent = Xml.Response.UserAgent;

                            e.AccessToken = authorization_grant.ProcessResponse(
                                HttpUtility.ParseQueryString(e_instance.CallbackURI.Query),
                                request,
                                null,
                                Abort.Token);
                        }

                        if (e.AccessToken != null)
                        {
                            // Save access token to the cache.
                            e.TokenOrigin = RequestAuthorizationEventArgs.TokenOriginType.Authorized;
                            Properties.Settings.Default.AccessTokenCache[authenticating_instance.Base.AbsoluteUri] = e.AccessToken;
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Called when an instance requests authorization delete
        /// </summary>
        /// <param name="sender">Instance of type <see cref="Instance"/> requiring authorization</param>
        /// <param name="e">Authorization forget event arguments</param>
        public void Instance_ForgetAuthorization(object sender, ForgetAuthorizationEventArgs e)
        {
            if (sender is Instance authenticating_instance)
            {
                // Remove access token from cache.
                lock (Properties.Settings.Default.AccessTokenCache)
                    Properties.Settings.Default.AccessTokenCache.Remove(authenticating_instance.Base.AbsoluteUri);
            }
        }

        /// <summary>
        /// Invokes <see cref="RequestOpenVPNPasswordAuthentication"/> event in GUI thread.
        /// </summary>
        /// <param name="sender"><see cref="RequestOpenVPNPasswordAuthentication"/> event sender</param>
        /// <param name="e"><see cref="RequestOpenVPNPasswordAuthentication"/> event arguments</param>
        public void OpenVPNSession_RequestPasswordAuthentication(object sender, eduOpenVPN.Management.PasswordAuthenticationRequestedEventArgs e)
        {
            // Re-raise this event as ConnectWizard event, to simplify view.
            // This way the view can listen ConnectWizard for authentication events only.
            if (Dispatcher.CurrentDispatcher == Dispatcher)
            {
                // We're in the GUI thread.
                RequestOpenVPNPasswordAuthentication?.Invoke(sender, e);
            }
            else
            {
                // We're in the background thread - raise event via dispatcher.
                Dispatcher.Invoke(DispatcherPriority.Normal, (Action)(() => RequestOpenVPNPasswordAuthentication?.Invoke(sender, e)));
            }
        }

        /// <summary>
        /// Invokes <see cref="RequestOpenVPNUsernamePasswordAuthentication"/> event in GUI thread.
        /// </summary>
        /// <param name="sender"><see cref="RequestOpenVPNUsernamePasswordAuthentication"/> event sender</param>
        /// <param name="e"><see cref="RequestOpenVPNUsernamePasswordAuthentication"/> event arguments</param>
        public void OpenVPNSession_RequestUsernamePasswordAuthentication(object sender, eduOpenVPN.Management.UsernamePasswordAuthenticationRequestedEventArgs e)
        {
            // Re-raise this event as ConnectWizard event, to simplify view.
            // This way the view can listen ConnectWizard for authentication events only.
            if (Dispatcher.CurrentDispatcher == Dispatcher)
            {
                // We're in the GUI thread.
                RequestOpenVPNUsernamePasswordAuthentication?.Invoke(sender, e);
            }
            else
            {
                // We're in the background thread - raise event via dispatcher.
                Dispatcher.Invoke(DispatcherPriority.Normal, (Action)(() => RequestOpenVPNUsernamePasswordAuthentication?.Invoke(sender, e)));
            }
        }

        /// <summary>
        /// Invokes <see cref="RequestTwoFactorAuthentication"/> event in GUI thread.
        /// </summary>
        /// <param name="sender"><see cref="RequestTwoFactorAuthentication"/> event sender</param>
        /// <param name="e"><see cref="RequestTwoFactorAuthentication"/> event arguments</param>
        public void OpenVPNSession_RequestTwoFactorAuthentication(object sender, eduOpenVPN.Management.UsernamePasswordAuthenticationRequestedEventArgs e)
        {
            // Re-raise this event as ConnectWizard event, to simplify view.
            // This way the view can listen ConnectWizard for authentication events only.
            if (Dispatcher.CurrentDispatcher == Dispatcher)
            {
                // We're in the GUI thread.
                RequestTwoFactorAuthentication?.Invoke(sender, e);
            }
            else
            {
                // We're in the background thread - raise event via dispatcher.
                Dispatcher.Invoke(DispatcherPriority.Normal, (Action)(() => RequestTwoFactorAuthentication?.Invoke(sender, e)));
            }
        }

        /// <summary>
        /// Ask view to quit.
        /// </summary>
        /// <param name="sender">Event sender</param>
        public void OnQuitApplication(object sender)
        {
            Trace.TraceInformation("Quitting client...");
            QuitApplication?.Invoke(sender, EventArgs.Empty);
        }

        #endregion
    }
}
