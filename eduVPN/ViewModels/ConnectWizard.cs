/*
    eduVPN - End-user friendly VPN

    Copyright: 2017, The Commons Conservancy eduVPN Programme
    SPDX-License-Identifier: GPL-3.0+
*/

using eduOAuth;
using Microsoft.Win32;
using Prism.Commands;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using System.Windows.Threading;
using System.Xml;

namespace eduVPN.ViewModels
{
    /// <summary>
    /// Connect wizard
    /// </summary>
    public class ConnectWizard : Window
    {
        #region Fields

        /// <summary>
        /// Instance directory URI IDs as used in <c>Properties.Settings.Default</c> collection
        /// </summary>
        private static readonly string[] _instance_directory_id = new string[]
        {
            null,
            "SecureInternet",
            "InstituteAccess",
        };

        /// <summary>
        /// The alpha factor to increase/decrease popularity
        /// </summary>
        private static readonly float _popularity_alpha = 0.1f;

        /// <summary>
        /// Access token cache
        /// </summary>
        private Dictionary<string, AccessToken> _access_token_cache;
        private object _access_token_cache_lock;

        #endregion

        #region Properties

        /// <summary>
        /// Available instance sources
        /// </summary>
        public Models.InstanceSource[] InstanceSources
        {
            get { return _instance_sources; }
        }
        private Models.InstanceSource[] _instance_sources;

        /// <summary>
        /// Selected instance source
        /// </summary>
        /// <remarks>This property is used in a process of adding new instance/profile.</remarks>
        public Models.InstanceSourceType InstanceSourceType
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
        private Models.InstanceSourceType _instance_source_type;

        /// <summary>
        /// Selected instance source
        /// </summary>
        /// <remarks>This property is used in a process of adding new instance/profile.</remarks>
        public Models.InstanceSource InstanceSource
        {
            get { return InstanceSources[(int)_instance_source_type]; }
        }

        /// <summary>
        /// VPN session queue - session 0 is the active session
        /// </summary>
        public ObservableCollection<VPNSession> Sessions
        {
            get { return _sessions; }
        }
        private ObservableCollection<VPNSession> _sessions;

        /// <summary>
        /// Active VPN session
        /// </summary>
        public VPNSession ActiveSession
        {
            get { return _sessions.Count > 0 ? _sessions[0] : VPNSession.Blank; }
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

                            if (Sessions.Count > 0)
                            {
                                var s = Sessions[Sessions.Count - 1];
                                if (s.AuthenticatingInstance.Equals(param.AuthenticatingInstance) &&
                                    s.ConnectingProfile.Equals(param.ConnectingProfile))
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
                                            this,
                                            param.AuthenticatingInstance,
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
                            if (InstanceSources[(int)param.InstanceSourceType] is Models.LocalInstanceSource instance_source_local)
                            {
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
                                    // Add all profiles of connecting instance to the list.
                                    foreach (var profile in param.ConnectingProfile.Instance.GetProfileList(param.AuthenticatingInstance, Abort.Token))
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
                            else if (InstanceSources[(int)param.InstanceSourceType] is Models.DistributedInstanceSource instance_source_distributed)
                            {
                                instance_source_distributed.AuthenticatingInstance = param.AuthenticatingInstance;
                            }
                            else if (InstanceSources[(int)param.InstanceSourceType] is Models.FederatedInstanceSource instance_source_federated)
                            {
                            }
                            else
                                throw new InvalidOperationException();
                        },

                        // canExecute
                        param =>
                            param is StartSessionParams &&
                            param.AuthenticatingInstance != null &&
                            param.ConnectingProfile.Instance != null &&
                            param.ConnectingProfile != null);

                return _start_session;
            }
        }
        private DelegateCommand<StartSessionParams> _start_session;

        /// <summary>
        /// StartSession command parameter set
        /// </summary>
        public class StartSessionParams
        {
            #region Properties

            /// <summary>
            /// Instance source
            /// </summary>
            public Models.InstanceSourceType InstanceSourceType { get; }

            /// <summary>
            /// Authenticating eduVPN instance
            /// </summary>
            public Models.Instance AuthenticatingInstance { get; }

            /// <summary>
            /// Connecting eduVPN instance profile
            /// </summary>
            public Models.Profile ConnectingProfile { get; }

            #endregion

            #region Constructors

            /// <summary>
            /// Constructs a StartSession command parameter set
            /// </summary>
            /// <param name="instance_source_type">Instance source type</param>
            /// <param name="authenticating_instance">Authenticating eduVPN instance</param>
            /// <param name="connecting_profile">Connecting eduVPN instance profile</param>
            public StartSessionParams(Models.InstanceSourceType instance_source_type, Models.Instance authenticating_instance, Models.Profile connecting_profile)
            {
                InstanceSourceType = instance_source_type;
                AuthenticatingInstance = authenticating_instance;
                ConnectingProfile = connecting_profile;
            }

            #endregion
        }

        /// <summary>
        /// Instance request authorization event
        /// </summary>
        public event EventHandler<RequestInstanceAuthorizationEventArgs> RequestInstanceAuthorization;

        /// <summary>
        /// OpenVPN requested a password
        /// </summary>
        public event EventHandler<eduOpenVPN.Management.PasswordAuthenticationRequestedEventArgs> RequestOpenVPNPasswordAuthentication;

        /// <summary>
        /// OpenVPN requested a username and password
        /// </summary>
        public event EventHandler<eduOpenVPN.Management.UsernamePasswordAuthenticationRequestedEventArgs> RequestOpenVPNUsernamePasswordAuthentication;

        /// <summary>
        /// 2-Factor Authentication requested
        /// </summary>
        public event EventHandler<eduOpenVPN.Management.UsernamePasswordAuthenticationRequestedEventArgs> RequestTwoFactorAuthentication;

        /// <summary>
        /// Product update is available
        /// </summary>
        public event EventHandler<PromptSelfUpdateEventArgs> PromptSelfUpdate;

        /// <summary>
        /// Application should quit
        /// </summary>
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
        private ConnectWizardPopupPage _current_popup_page;

        /// <summary>
        /// The first page of the wizard
        /// </summary>
        public ConnectWizardPage StartingPage
        {
            get
            {
                for (var source_index = (int)Models.InstanceSourceType._start; source_index < (int)Models.InstanceSourceType._end; source_index++)
                    if (InstanceSources[source_index].ConnectingInstance != null)
                        return RecentConfigurationSelectPage;

                return InstanceSourceSelectPage;
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
        private InstanceSourceSelectPage _instance_source_page;

        /// <summary>
        /// Authenticating instance selection page
        /// </summary>
        /// <remarks>Available only when authenticating and connecting instances can be different. I.e. <c>InstanceSource</c> is <c>eduVPN.Models.LocalInstanceSource</c> or <c>eduVPN.Models.DistributedInstanceSource</c>.</remarks>
        public AuthenticatingInstanceSelectPage AuthenticatingInstanceSelectPage
        {
            get
            {
                if (InstanceSource is Models.LocalInstanceSource ||
                    InstanceSource is Models.DistributedInstanceSource)
                {
                    // Only local and distrubuted authentication sources have this page.
                    // However, this page varies between Secure Internet and Institute Access.
                    switch (InstanceSourceType)
                    {
                        case Models.InstanceSourceType.SecureInternet:
                            if (_authenticating_country_select_page == null)
                                _authenticating_country_select_page = new AuthenticatingCountrySelectPage(this);
                            return _authenticating_country_select_page;

                        case Models.InstanceSourceType.InstituteAccess:
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
        private AuthenticatingCountrySelectPage _authenticating_country_select_page;
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
        private AboutPage _about_page;

        #endregion

        #endregion

        #region Constructors

        /// <summary>
        /// Constructs the wizard
        /// </summary>
        public ConnectWizard()
        {
            bool is_migrating_settings = false;
            if (Properties.Settings.Default.SettingsVersion == 0)
            {
                // Migrate settings from previous version.
                Properties.Settings.Default.Upgrade();
                Properties.Settings.Default.SettingsVersion = 1;
                is_migrating_settings = true;

                // Versions before 1.0.4 used interface name, instead of ID.
                if (Properties.Settings.Default.GetPreviousVersion("OpenVPNInterface") is string iface_name &&
                    Models.NetworkInterface.TryFromName(iface_name, out var iface))
                    Properties.Settings.Default.OpenVPNInterfaceID = iface.ID;
            }

            // Create access token cache.
            _access_token_cache = new Dictionary<string, AccessToken>();
            _access_token_cache_lock = new object();

            // Create session queue.
            _sessions = new ObservableCollection<VPNSession>();
            _sessions.CollectionChanged += (object sender, NotifyCollectionChangedEventArgs e) => RaisePropertyChanged(nameof(ActiveSession));

            // Show initializing wizard page.
            _current_page = InitializingPage;

            // Setup initialization.
            var worker = new BackgroundWorker() { WorkerReportsProgress = true };
            worker.DoWork += (object sender, DoWorkEventArgs e) =>
            {
                var source_type_length = (int)Models.InstanceSourceType._end;
                _instance_sources = new Models.InstanceSource[source_type_length];

                // Setup progress feedback.
                Dispatcher.Invoke(DispatcherPriority.Normal, (Action)(() => InitializingPage.Progress = new Range<int>(0, Properties.Settings.Default.AccessTokens.Count + (source_type_length - (int)Models.InstanceSourceType._start) * 2, 0)));

                // Load access tokens from settings.
                Parallel.ForEach(Properties.Settings.Default.AccessTokens,
                    token =>
                    {
                        try
                        {
                            // Try to load the access token from the settings.
                            var access_token = AccessToken.FromBase64String(token.Value);
                            lock (_access_token_cache_lock)
                                _access_token_cache.Add(token.Key, access_token);
                        }
                        catch { }
                        finally
                        {
                            // Add a tick.
                            Dispatcher.Invoke(DispatcherPriority.Normal, (Action)(() => InitializingPage.Progress.Value++));
                        }
                    });

                // Spawn instance source loading threads.
                Parallel.For((int)Models.InstanceSourceType._start, source_type_length, source_index =>
                {
                    Dispatcher.Invoke(DispatcherPriority.Normal, (Action)(() => ChangeTaskCount(+1)));
                    try
                    {
                        for (;;)
                        {
                            int ticks = 0;
                            object ticks_lock = new object();
                            try
                            {
                                // Get instance source.
                                var response_cache = (JSON.Response)Properties.Settings.Default[_instance_directory_id[source_index] + "DiscoveryCache"];
                                var pub_key = (string)Properties.Settings.Default[_instance_directory_id[source_index] + "DiscoveryPubKey"];
                                var obj_web = JSON.Response.GetSeq(
                                    uri: new Uri((string)Properties.Settings.Default[_instance_directory_id[source_index] + "Discovery"]),
                                    pub_key: !string.IsNullOrWhiteSpace(pub_key) ? Convert.FromBase64String(pub_key) : null,
                                    ct: Abort.Token,
                                    response_cache: ref response_cache);
                                Properties.Settings.Default[_instance_directory_id[source_index] + "DiscoveryCache"] = response_cache;

                                // Add a tick.
                                Dispatcher.Invoke(DispatcherPriority.Normal, (Action)(() => InitializingPage.Progress.Value++));
                                ticks++;

                                // Load instance source.
                                _instance_sources[source_index] = Models.InstanceSource.FromJSON(obj_web);

                                {
                                    // Attach to RequestAuthorization instance events.
                                    if (_instance_sources[source_index] is Models.FederatedInstanceSource instance_source_federated)
                                        instance_source_federated.AuthenticatingInstance.RequestAuthorization += Instance_RequestAuthorization;

                                    foreach (var instance in _instance_sources[source_index].InstanceList)
                                        instance.RequestAuthorization += Instance_RequestAuthorization;
                                }

                                // Load instance source info settings.
                                Xml.InstanceSourceSettingsBase h = null;
                                #pragma warning disable 0612 // This section contains legacy settings conversion.
                                if (is_migrating_settings &&
                                    Properties.Settings.Default.GetPreviousVersion(_instance_directory_id[source_index] + "ConfigHistory") is Xml.VPNConfigurationSettingsList settings_list)
                                {
                                    // Versions before 1.0.9 used different instance source settings. Convert them.
                                    if (_instance_sources[source_index] is Models.LocalInstanceSource instance_source_local)
                                    {
                                        // Local authenticating instance source:
                                        // - Convert instance list.
                                        // - Set connecting instance by maximum popularity.
                                        var h_local = new Xml.LocalInstanceSourceSettings();
                                        foreach (var h_cfg in settings_list)
                                        {
                                            if (h_cfg is Xml.LocalVPNConfigurationSettings h_cfg_local)
                                            {
                                                var instance = h_local.ConnectingInstanceList.FirstOrDefault(inst => inst.Base.AbsoluteUri == h_cfg_local.Instance.AbsoluteUri);
                                                if (instance == null)
                                                {
                                                    // Add (instance, profile) pair.
                                                    h_local.ConnectingInstanceList.Add(new Xml.InstanceRef()
                                                    {
                                                        Base = h_cfg_local.Instance,
                                                        Popularity = h_cfg_local.Popularity,
                                                        ProfileList = new Xml.ProfileRefList()
                                                        {
                                                            new Xml.ProfileRef()
                                                            {
                                                                ID = h_cfg_local.Profile,
                                                                Popularity = h_cfg_local.Popularity
                                                            }
                                                        }
                                                    });
                                                }
                                                else
                                                {
                                                    // Instance already on the list. Update it.
                                                    instance.Popularity = Math.Max(instance.Popularity, h_cfg_local.Popularity);
                                                    if (instance.ProfileList.FirstOrDefault(prof => prof.ID == h_cfg_local.Profile) == null)
                                                    {
                                                        // Add profile to the instance.
                                                        instance.ProfileList.Add(new Xml.ProfileRef()
                                                        {
                                                            ID = h_cfg_local.Profile,
                                                            Popularity = h_cfg_local.Popularity
                                                        });
                                                    }
                                                }
                                            }
                                        }
                                        h_local.ConnectingInstance = h_local.ConnectingInstanceList.Count > 0 ? h_local.ConnectingInstanceList.Aggregate((most_popular_instance, inst) => (most_popular_instance == null || inst.Popularity > most_popular_instance.Popularity ? inst : most_popular_instance))?.Base : null;
                                        h = h_local;
                                    }
                                    else if (_instance_sources[source_index] is Models.DistributedInstanceSource instance_source_distributed)
                                    {
                                        // Distributed authenticating instance source:
                                        // - Convert authenticating instance.
                                        // - Convert connecting instance.
                                        var h_distributed = new Xml.DistributedInstanceSourceSettings();
                                        if (settings_list.Count > 0 && settings_list[0] is Xml.DistributedVPNConfigurationSettings h_cfg_distributed)
                                        {
                                            h_distributed.AuthenticatingInstance = new Uri(h_cfg_distributed.AuthenticatingInstance);
                                            h_distributed.ConnectingInstance = new Uri(h_cfg_distributed.LastInstance);
                                        }
                                        h = h_distributed;
                                    }
                                    else if (_instance_sources[source_index] is Models.FederatedInstanceSource instance_source_federated)
                                    {
                                        // Federated authenticating instance source:
                                        // - Convert connecting instance.
                                        var h_federated = new Xml.FederatedInstanceSourceSettings();
                                        if (settings_list.Count > 0 && settings_list[0] is Xml.FederatedVPNConfigurationSettings h_cfg_federated)
                                        {
                                            h_federated.ConnectingInstance = new Uri(h_cfg_federated.LastInstance);
                                        }
                                        h = h_federated;
                                    }
                                }
                                else
                                #pragma warning restore 0612
                                    h = (Properties.Settings.Default[_instance_directory_id[source_index] + "InstanceSourceSettings"] as Xml.InstanceSourceSettings)?.InstanceSource;

                                // Import instance source from settings.
                                {
                                    if (InstanceSources[source_index] is Models.LocalInstanceSource instance_source_local &&
                                        h is Xml.LocalInstanceSourceSettings h_local)
                                    {
                                        // Local authenticating instance source:
                                        // - Restore instance list.
                                        // - Restore connecting instance (optional).
                                        foreach (var h_instance in h_local.ConnectingInstanceList)
                                        {
                                            var connecting_instance = instance_source_local.InstanceList.FirstOrDefault(inst => inst.Base.AbsoluteUri == h_instance.Base.AbsoluteUri);
                                            if (connecting_instance == null)
                                            {
                                                // The connecting instance was not found. Could be user entered, or removed from discovery file.
                                                connecting_instance = new Models.Instance(h_instance.Base);
                                                connecting_instance.RequestAuthorization += Instance_RequestAuthorization;
                                            } else
                                                connecting_instance.Popularity = h_instance.Popularity;

                                            var instance = instance_source_local.ConnectingInstanceList.FirstOrDefault(inst => inst.Base.AbsoluteUri == connecting_instance.Base.AbsoluteUri);
                                            if (instance == null)
                                            {
                                                instance_source_local.ConnectingInstanceList.Add(connecting_instance);
                                                instance = connecting_instance;
                                            }
                                            else
                                                instance.Popularity = Math.Max(instance.Popularity, h_instance.Popularity);

                                            // Restore connecting profiles (optionally).
                                            // Matching profile with existing profiles might trigger OAuth in GetProfileList().
                                            switch (Properties.Settings.Default.ConnectingProfileSelectMode)
                                            {
                                                case 0:
                                                    {
                                                        // Restore only profiles user connected to before.
                                                        var profile_list = instance.GetProfileList(instance, Abort.Token);
                                                        foreach (var h_profile in h_instance.ProfileList)
                                                        {
                                                            var profile = profile_list.FirstOrDefault(prof => prof.ID == h_profile.ID);
                                                            if (profile != null)
                                                            {
                                                                profile.Popularity = h_profile.Popularity;
                                                                if (instance_source_local.ConnectingProfileList.FirstOrDefault(prof => prof.Equals(profile)) == null)
                                                                    instance_source_local.ConnectingProfileList.Add(profile);
                                                            }
                                                        }
                                                    }

                                                    break;

                                                case 2:
                                                    {
                                                        // Add all available profiles to the connecting profile list.
                                                        // Restore popularity on the fly (or leave default to promote newly discovered profiles).
                                                        var profile_list = instance.GetProfileList(instance, Abort.Token);
                                                        foreach (var profile in profile_list)
                                                        {
                                                            var h_profile = h_instance.ProfileList.FirstOrDefault(prof => prof.ID == profile.ID);
                                                            if (h_profile != null)
                                                                profile.Popularity = h_profile.Popularity;

                                                            instance_source_local.ConnectingProfileList.Add(profile);
                                                        }
                                                    }

                                                    break;
                                            }
                                        }
                                        instance_source_local.ConnectingInstance = h_local.ConnectingInstance != null ? instance_source_local.ConnectingInstanceList.FirstOrDefault(inst => inst.Base.AbsoluteUri == h_local.ConnectingInstance.AbsoluteUri) : null;
                                    }
                                    else if (InstanceSources[source_index] is Models.DistributedInstanceSource instance_source_distributed &&
                                        h is Xml.DistributedInstanceSourceSettings h_distributed)
                                    {
                                        // Distributed authenticating instance source:
                                        // - Restore authenticating instance.
                                        // - Restore connecting instance (optional).
                                        instance_source_distributed.AuthenticatingInstance = h_distributed.AuthenticatingInstance != null ? instance_source_distributed.InstanceList.FirstOrDefault(inst => inst.Base.AbsoluteUri == h_distributed.AuthenticatingInstance.AbsoluteUri) : null;
                                        if (instance_source_distributed.AuthenticatingInstance != null)
                                        {
                                            instance_source_distributed.AuthenticatingInstance.RequestAuthorization += Instance_RequestAuthorization;
                                            instance_source_distributed.ConnectingInstance = h_distributed.ConnectingInstance != null ? instance_source_distributed.ConnectingInstanceList.FirstOrDefault(inst => inst.Base.AbsoluteUri == h_distributed.ConnectingInstance.AbsoluteUri) : null;
                                        }
                                    }
                                    else if (InstanceSources[source_index] is Models.FederatedInstanceSource instance_source_federated &&
                                        h is Xml.FederatedInstanceSourceSettings h_federated)
                                    {
                                        // Federated authenticating instance source:
                                        // - Restore connecting instance (optional).
                                        instance_source_federated.ConnectingInstance = h_federated.ConnectingInstance != null ? instance_source_federated.ConnectingInstanceList.FirstOrDefault(inst => inst.Base.AbsoluteUri == h_federated.ConnectingInstance.AbsoluteUri) : null;
                                    }
                                }

                                // Add a tick.
                                Dispatcher.Invoke(DispatcherPriority.Normal, (Action)(() => InitializingPage.Progress.Value++));
                                ticks++;

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
                                    Error = new AggregateException(String.Format(Resources.Strings.ErrorInstanceSourceInfoLoad, _instance_directory_id[source_index]), ex);

                                    // Revert progress indicator value.
                                    InitializingPage.Progress.Value -= ticks;
                                }));
                            }

                            // Sleep for 3s, then retry.
                            if (Abort.Token.WaitHandle.WaitOne(TimeSpan.FromSeconds(3)))
                                break;
                        }
                    }
                    finally { Dispatcher.Invoke(DispatcherPriority.Normal, (Action)(() => ChangeTaskCount(-1))); }
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

                    Dispatcher.ShutdownStarted += (object sender2, EventArgs e2) =>
                    {
                        // Update access token settings.
                        Properties.Settings.Default.AccessTokens = new Xml.SerializableStringDictionary();
                        lock (_access_token_cache_lock)
                            foreach (var access_token in _access_token_cache)
                                Properties.Settings.Default.AccessTokens[access_token.Key] = access_token.Value.ToBase64String();

                        // Update settings.
                        Parallel.For((int)Models.InstanceSourceType._start, (int)Models.InstanceSourceType._end, source_index =>
                        {
                            Xml.InstanceSourceSettingsBase h = null;
                            if (InstanceSources[source_index] is Models.LocalInstanceSource instance_source_local)
                            {
                                // Local authenticating instance source
                                h = new Xml.LocalInstanceSourceSettings()
                                {
                                    ConnectingInstance = instance_source_local.ConnectingInstance?.Base,
                                    ConnectingInstanceList = new Xml.InstanceRefList(
                                        instance_source_local.ConnectingInstanceList
                                        .Select(inst =>
                                            new Xml.InstanceRef()
                                            {
                                                Base = inst.Base,
                                                Popularity = inst.Popularity,
                                                ProfileList = new Xml.ProfileRefList(
                                                    instance_source_local.ConnectingProfileList
                                                    .Where(prof => prof.Instance.Equals(inst))
                                                    .Select(prof => new Xml.ProfileRef()
                                                    {
                                                        ID = prof.ID,
                                                        Popularity = prof.Popularity
                                                    }))
                                            }
                                        ))
                                };
                            }
                            else if (InstanceSources[source_index] is Models.DistributedInstanceSource instance_source_distributed)
                            {
                                // Distributed authenticating instance source
                                h = new Xml.DistributedInstanceSourceSettings()
                                {
                                    AuthenticatingInstance = instance_source_distributed.AuthenticatingInstance?.Base,
                                    ConnectingInstance = instance_source_distributed.ConnectingInstance?.Base
                                };
                            }
                            else if (InstanceSources[source_index] is Models.FederatedInstanceSource instance_source_federated)
                            {
                                // Federated authenticating instance source
                                h = new Xml.FederatedInstanceSourceSettings()
                                {
                                    ConnectingInstance = instance_source_federated.ConnectingInstance?.Base
                                };
                            }

                            Properties.Settings.Default[_instance_directory_id[source_index] + "InstanceSourceSettings"] = new Xml.InstanceSourceSettings() { InstanceSource = h };
                        });

                        // Persist settings to disk.
                        Properties.Settings.Default.Save();
                    };

                    // Proceed to the "first" page.
                    RaisePropertyChanged(nameof(StartingPage));
                    CurrentPage = StartingPage;
                }
                finally { ChangeTaskCount(-1); }

                // Self-dispose.
                (sender as BackgroundWorker)?.Dispose();
            };

            worker.RunWorkerAsync();

            if (Properties.Settings.Default.SelfUpdate is string)
            {
                // Setup self-update.
                var self_update = new BackgroundWorker() { WorkerReportsProgress = true };
                self_update.DoWork += (object sender, DoWorkEventArgs e) =>
                {
                    Dispatcher.Invoke(DispatcherPriority.Normal, (Action)(() => ChangeTaskCount(+1)));
                    try
                    {
                        var working_folder = Path.GetTempPath();
                        var installer_filename = working_folder + "eduVPNClient.exe";
                        var updater_filename = working_folder + "eduVPNClient.wsf";
                        Dictionary<string, object> obj_web = null;
                        Version product_version = null;

                        try
                        {
                            Parallel.ForEach(new List<Action>()
                                {
                                    () =>
                                    {
                                        if (File.Exists(updater_filename))
                                        {
                                            // Clean stale updater WSF file. If possible.
                                            try { File.Delete(updater_filename); }
                                            catch { }
                                        }
                                    },

                                    () =>
                                    {
                                        // Get self-update.
                                        var response_cache = (JSON.Response)Properties.Settings.Default.SelfUpdateCache;
                                        var pub_key = (string)Properties.Settings.Default.SelfUpdatePubKey;
                                        obj_web = JSON.Response.GetSeq(
                                            uri: new Uri((string)Properties.Settings.Default.SelfUpdate),
                                            pub_key: !string.IsNullOrWhiteSpace(pub_key) ? Convert.FromBase64String(pub_key) : null,
                                            ct: Abort.Token,
                                            response_cache: ref response_cache);
                                        Properties.Settings.Default.SelfUpdateCache = response_cache;
                                    },

                                    () =>
                                    {
                                        // Evaluate installed products.
                                        using (var hklm_key = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry32))
                                        using (var uninstall_key = hklm_key.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Uninstall", false))
                                        {
                                            foreach (var product_key_name in uninstall_key.GetSubKeyNames())
                                            {
                                                Abort.Token.ThrowIfCancellationRequested();
                                                using (var product_key = uninstall_key.OpenSubKey(product_key_name))
                                                {
                                                    var bundle_upgrade_code = product_key.GetValue("BundleUpgradeCode");
                                                    if ((bundle_upgrade_code is string   bundle_upgrade_code_str   && bundle_upgrade_code_str.ToUpperInvariant() == "{EF5D5806-B90B-4AA3-800A-2D7EA1592BA0}" ||
                                                         bundle_upgrade_code is string[] bundle_upgrade_code_array && bundle_upgrade_code_array.FirstOrDefault(code => code.ToUpperInvariant() == "{EF5D5806-B90B-4AA3-800A-2D7EA1592BA0}") != null) &&
                                                        product_key.GetValue("BundleVersion") is string bundle_version_str)
                                                    {
                                                        // Our product entry found.
                                                        product_version = new Version(product_key.GetValue("DisplayVersion") is string display_version_str ? display_version_str : bundle_version_str);
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

                        var repo_version = new Version((string)obj_web["version"]);

                        try
                        {
                            if (new Version(Properties.Settings.Default.SelfUpdateLastVersion) == repo_version &&
                                (Properties.Settings.Default.SelfUpdateLastReminder == DateTime.MaxValue ||
                                (DateTime.UtcNow - Properties.Settings.Default.SelfUpdateLastReminder).TotalDays < 3))
                            {
                                // We already prompted user for this version.
                                // Either user opted not to be reminded of this version update again,
                                // or it has been less than three days since the last prompt.
                                return;
                            }
                        }
                        catch { }

                        if (product_version == null || repo_version <= product_version)
                        {
                            // Nothing to update.
                            return;
                        }

                        // Download installer.
                        var installer_ready = false;
                        var repo_hash = ((string)obj_web["hash-sha256"]).FromHexToBin();
                        if (File.Exists(installer_filename))
                        {
                            // File already exists. Verify its integrity.
                            try
                            {
                                using (BinaryReader reader = new BinaryReader(File.Open(installer_filename, FileMode.Open)))
                                {
                                    var hash = new eduEd25519.SHA256();
                                    var buffer = new byte[1048576];

                                    for (; ; )
                                    {
                                        // Read data and hash it.
                                        Abort.Token.ThrowIfCancellationRequested();
                                        var buffer_length = reader.Read(buffer, 0, buffer.Length);
                                        if (buffer_length == 0)
                                            break;
                                        hash.TransformBlock(buffer, 0, buffer_length, buffer, 0);
                                    }

                                    hash.TransformFinalBlock(buffer, 0, 0);
                                    if (!hash.Hash.SequenceEqual(repo_hash))
                                        throw new DownloadedFileCorruptException(string.Format(Resources.Strings.ErrorDownloadedFileCorrupt, installer_filename));
                                }

                                installer_ready = true;
                            }
                            catch (OperationCanceledException) { throw; }
                            catch
                            {
                                // Delete file. If possible.
                                try { File.Delete(installer_filename); }
                                catch { }
                            }
                        }

                        if (!installer_ready)
                        {
                            // Download installer.
                            var uris = (List<object>)obj_web["uri"];
                            var random = new Random();
                            while (uris.Count > 0)
                            {
                                Abort.Token.ThrowIfCancellationRequested();
                                var uri_idx = random.Next(uris.Count);
                                try
                                {
                                    var uri = new Uri((string)uris[uri_idx]);
                                    var request = WebRequest.Create(uri);
                                    using (var response = request.GetResponse())
                                    using (var stream = response.GetResponseStream())
                                    {
                                        try
                                        {
                                            // Read to file.
                                            using (BinaryWriter writer = new BinaryWriter(File.Open(installer_filename, FileMode.Create)))
                                            {
                                                var hash = new eduEd25519.SHA256();
                                                var buffer = new byte[1048576];

                                                for (; ; )
                                                {
                                                    // Wait for the data to arrive.
                                                    Abort.Token.ThrowIfCancellationRequested();
                                                    var buffer_length = stream.Read(buffer, 0, buffer.Length);
                                                    if (buffer_length == 0)
                                                        break;

                                                    // Append it to the file and hash it.
                                                    Abort.Token.ThrowIfCancellationRequested();
                                                    writer.Write(buffer, 0, buffer_length);
                                                    hash.TransformBlock(buffer, 0, buffer_length, buffer, 0);
                                                }

                                                hash.TransformFinalBlock(buffer, 0, 0);
                                                if (!hash.Hash.SequenceEqual(repo_hash))
                                                    throw new DownloadedFileCorruptException(string.Format(Resources.Strings.ErrorDownloadedFileCorrupt, uri.AbsolutePath));
                                            }

                                            installer_ready = true;
                                            break;
                                        }
                                        catch
                                        {
                                            // Delete file. If possible.
                                            try { File.Delete(installer_filename); }
                                            catch { }
                                            throw;
                                        }
                                    }
                                }
                                catch (OperationCanceledException) { throw; }
                                catch { uris.RemoveAt(uri_idx); }
                            }
                        }

                        if (!installer_ready)
                        {
                            // The installer file is not ready.
                            return;
                        }

                        // We're in the background thread - raise the prompt event via dispatcher.
                        var e_prompt = new PromptSelfUpdateEventArgs(product_version, repo_version);
                        Dispatcher.Invoke(DispatcherPriority.Normal, (Action)(() => PromptSelfUpdate?.Invoke(this, e_prompt)));
                        bool quit = false;

                        switch (e_prompt.Action)
                        {
                            case PromptSelfUpdateActionType.Update:
                                {
                                    // Prepare WSF file.
                                    using (XmlTextWriter writer = new XmlTextWriter(updater_filename, null))
                                    {
                                        writer.WriteStartDocument();
                                        writer.WriteStartElement("package");
                                        writer.WriteStartElement("job");

                                        writer.WriteStartElement("reference");
                                        writer.WriteAttributeString("object", "WScript.Shell");
                                        writer.WriteEndElement(); // reference

                                        writer.WriteStartElement("reference");
                                        writer.WriteAttributeString("object", "Scripting.FileSystemObject");
                                        writer.WriteEndElement(); // reference

                                        writer.WriteStartElement("script");
                                        writer.WriteAttributeString("language", "JScript");
                                        var installer_filename_esc = HttpUtility.JavaScriptStringEncode(installer_filename);
                                        var argv = Environment.GetCommandLineArgs();
                                        var arguments = new StringBuilder();
                                        for (long i = 1, n = argv.LongLength; i < n; i++)
                                        {
                                            if (i > 1) arguments.Append(" ");
                                            arguments.Append("\"");
                                            arguments.Append(argv[i].Replace("\"", "\"\""));
                                            arguments.Append("\"");
                                        }
                                        var script = new StringBuilder();
                                        script.AppendLine("// This script was auto-generated.");
                                        script.AppendLine("// Launch installer file and wait for the update to finish.");
                                        script.AppendLine("var wsh = WScript.CreateObject(\"WScript.Shell\");");
                                        script.AppendLine("if (wsh.Run(\"" + installer_filename_esc + "\", 0, true) == 0) {");
                                        script.AppendLine("  // Installer succeeded. Relaunch the application.");
                                        script.AppendLine("  var shl = WScript.CreateObject(\"Shell.Application\");");
                                        script.AppendLine("  shl.ShellExecute(\"" + HttpUtility.JavaScriptStringEncode(argv[0]) + "\", \"" + HttpUtility.JavaScriptStringEncode(arguments.ToString()) + "\", \"" + HttpUtility.JavaScriptStringEncode(Environment.CurrentDirectory) + "\");");
                                        script.AppendLine("  // Delete the installer file.");
                                        script.AppendLine("  var fso = WScript.CreateObject(\"Scripting.FileSystemObject\");");
                                        script.AppendLine("  try { fso.DeleteFile(\"" + installer_filename_esc + "\", true); } catch (err) {}");
                                        script.AppendLine("}");
                                        writer.WriteCData(script.ToString());
                                        writer.WriteEndElement(); // script

                                        writer.WriteEndElement(); // job
                                        writer.WriteEndElement(); // package
                                        writer.WriteEndDocument();
                                    }

                                    // Launch wscript.exe with WSF file.
                                    var process = new Process();
                                    process.StartInfo.FileName = "wscript.exe";
                                    process.StartInfo.Arguments = "\"" + updater_filename + "\"";
                                    process.StartInfo.WorkingDirectory = working_folder;
                                    process.Start();

                                    // Quit the client.
                                    quit = true;
                                }
                                goto case PromptSelfUpdateActionType.AskLater;

                            case PromptSelfUpdateActionType.AskLater:
                                // Mark the timestamp of the prompt.
                                Properties.Settings.Default.SelfUpdateLastReminder = DateTime.UtcNow;
                                break;

                            case PromptSelfUpdateActionType.Skip:
                                // Mark not to re-prompt again.
                                Properties.Settings.Default.SelfUpdateLastReminder = DateTime.MaxValue;
                                break;
                        }

                        // Mark the version of this prompt.
                        Properties.Settings.Default.SelfUpdateLastVersion = repo_version.ToString();

                        if (quit)
                        {
                            // Ask the view to quit.
                            Dispatcher.Invoke(DispatcherPriority.Normal, (Action)(() => QuitApplication?.Invoke(this, new EventArgs())));
                        }
                    }
                    catch { }
                    finally { Dispatcher.Invoke(DispatcherPriority.Normal, (Action)(() => ChangeTaskCount(-1))); }
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
        public async Task TriggerAuthorizationAsync(Models.Instance instance)
        {
            var e = new Models.RequestAuthorizationEventArgs("config");
            var authorization_task = new Task(() => Instance_RequestAuthorization(instance, e), Abort.Token, TaskCreationOptions.LongRunning);
            authorization_task.Start();
            await authorization_task;

            if (e.AccessToken == null)
                throw new Models.AccessTokenNullException();
        }

        /// <summary>
        /// Called when an instance requests user authorization
        /// </summary>
        /// <param name="sender">Instance of type <c>eduVPN.Models.Instance</c> requiring authorization</param>
        /// <param name="e">Authorization request event arguments</param>
        public void Instance_RequestAuthorization(object sender, Models.RequestAuthorizationEventArgs e)
        {
            if (sender is Models.Instance authenticating_instance)
            {
                lock (_access_token_cache_lock)
                {
                    // Get API endpoints.
                    var api = authenticating_instance.GetEndpoints(Abort.Token);

                    if (e.SourcePolicy != Models.RequestAuthorizationEventArgs.SourcePolicyType.ForceAuthorization)
                    {
                        var key = api.AuthorizationEndpoint.AbsoluteUri;
                        if (_access_token_cache.TryGetValue(key, out var access_token))
                        {
                            if (access_token.Expires.HasValue && access_token.Expires.Value <= DateTime.Now)
                            {
                                // Token expired. Refresh it.
                                access_token = access_token.RefreshToken(api.TokenEndpoint, null, Abort.Token);
                                if (access_token != null)
                                {
                                    // Update access token cache.
                                    _access_token_cache[key] = access_token;

                                    // If we got here, return the token.
                                    e.AccessToken = access_token;
                                    return;
                                }
                            }
                            else
                            {
                                // If we got here, return the token.
                                e.AccessToken = access_token;
                                return;
                            }
                        }
                    }

                    if (e.SourcePolicy != Models.RequestAuthorizationEventArgs.SourcePolicyType.SavedOnly)
                    {
                        // Re-raise this event as ConnectWizard event, to simplify view.
                        // This way the view can listen ConnectWizard for authentication events only.
                        if (Dispatcher.CurrentDispatcher == Dispatcher)
                        {
                            // We're in the GUI thread.
                            var e_instance = new RequestInstanceAuthorizationEventArgs((Models.Instance)sender, e.Scope);
                            RequestInstanceAuthorization?.Invoke(this, e_instance);
                            e.AccessToken = e_instance.AccessToken;
                        }
                        else
                        {
                            // We're in the background thread - raise event via dispatcher.
                            Dispatcher.Invoke(DispatcherPriority.Normal,
                                (Action)(() =>
                                {
                                    var e_instance = new RequestInstanceAuthorizationEventArgs((Models.Instance)sender, e.Scope);
                                    RequestInstanceAuthorization?.Invoke(this, e_instance);
                                    e.AccessToken = e_instance.AccessToken;
                                }));
                        }

                        if (e.AccessToken != null)
                        {
                            // Save access token to the cache.
                            _access_token_cache[api.AuthorizationEndpoint.AbsoluteUri] = e.AccessToken;
                        }
                    }
                }
            }
        }

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

        #endregion
    }
}
