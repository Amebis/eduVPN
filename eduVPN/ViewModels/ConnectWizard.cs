/*
    eduVPN - End-user friendly VPN

    Copyright: 2017, The Commons Conservancy eduVPN Programme
    SPDX-License-Identifier: GPL-3.0+
*/

using eduOAuth;
using Prism.Commands;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Threading;

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
        public Models.InstanceSourceInfo[] InstanceSources
        {
            get { return _instance_sources; }
        }
        private Models.InstanceSourceInfo[] _instance_sources;

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
        public Models.InstanceSourceInfo InstanceSource
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
                                    s.ConnectingInstance.Equals(param.ConnectingInstance) &&
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
                                            param.ConnectingInstance,
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
                            if (InstanceSources[(int)param.InstanceSourceType] is Models.LocalInstanceSourceInfo instance_source_local)
                            {
                                for (var i = instance_source_local.ConnectingInstanceList.Count; ;)
                                {
                                    if (i-- > 0)
                                    {
                                        if (instance_source_local.ConnectingInstanceList[i].Equals(param.ConnectingInstance))
                                        {
                                            // Upvote popularity.
                                            instance_source_local.ConnectingInstanceList[i].Popularity = instance_source_local.ConnectingInstanceList[i].Popularity * (1.0f - _popularity_alpha) + 1.0f * _popularity_alpha;
                                            break;
                                        }
                                        else
                                        {
                                            // Downvote popularity.
                                            instance_source_local.ConnectingInstanceList[i].Popularity = instance_source_local.ConnectingInstanceList[i].Popularity * (1.0f - _popularity_alpha) /*+ 0.0f * _popularity_alpha*/;
                                        }
                                    }
                                    else
                                    {
                                        // Add connecting instance to the list.
                                        instance_source_local.ConnectingInstanceList.Add(param.ConnectingInstance);
                                        break;
                                    }
                                }
                            }
                            else if (InstanceSources[(int)param.InstanceSourceType] is Models.DistributedInstanceSourceInfo instance_source_distributed)
                            {
                                instance_source_distributed.AuthenticatingInstance = param.AuthenticatingInstance;
                            }
                            else if (InstanceSources[(int)param.InstanceSourceType] is Models.FederatedInstanceSourceInfo instance_source_federated)
                            {
                            }
                            else
                                throw new InvalidOperationException();

                            InstanceSources[(int)param.InstanceSourceType].ConnectingInstance = param.ConnectingInstance;
                        },

                        // canExecute
                        param =>
                            param is StartSessionParams &&
                            param.AuthenticatingInstance != null &&
                            param.ConnectingInstance != null &&
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
            public Models.InstanceInfo AuthenticatingInstance { get; }

            /// <summary>
            /// Connecting eduVPN instance
            /// </summary>
            public Models.InstanceInfo ConnectingInstance { get; }

            /// <summary>
            /// Connecting eduVPN instance profile
            /// </summary>
            public Models.ProfileInfo ConnectingProfile { get; }

            #endregion

            #region Constructors

            /// <summary>
            /// Constructs a StartSession command parameter set
            /// </summary>
            /// <param name="instance_source_type">Instance source type</param>
            /// <param name="authenticating_instance">Authenticating eduVPN instance</param>
            /// <param name="connecting_instance">Connecting eduVPN instance</param>
            /// <param name="connecting_profile">Connecting eduVPN instance profile</param>
            public StartSessionParams(Models.InstanceSourceType instance_source_type, Models.InstanceInfo authenticating_instance, Models.InstanceInfo connecting_instance, Models.ProfileInfo connecting_profile)
            {
                InstanceSourceType = instance_source_type;
                AuthenticatingInstance = authenticating_instance;
                ConnectingInstance = connecting_instance;
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
                for (var instance_source_type = Models.InstanceSourceType._start; instance_source_type < Models.InstanceSourceType._end; instance_source_type++)
                {
                    if (HasConnectingInstances(instance_source_type))
                        return RecentConfigurationSelectPage;
                }

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
        /// <remarks>Available only when authenticating and connecting instances can be different. I.e. <c>InstanceSource</c> is <c>eduVPN.Models.LocalInstanceSourceInfo</c> or <c>eduVPN.Models.DistributedInstanceSourceInfo</c>.</remarks>
        public AuthenticatingInstanceSelectPage AuthenticatingInstanceSelectPage
        {
            get
            {
                if (InstanceSource is Models.LocalInstanceSourceInfo ||
                    InstanceSource is Models.DistributedInstanceSourceInfo)
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
                    Models.InterfaceInfo.TryFromName(iface_name, out var iface))
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
                _instance_sources = new Models.InstanceSourceInfo[source_type_length];

                // Setup progress feedback. Each instance will add two ticks of progress, plus as many ticks as there are configuration entries in its history.
                Dispatcher.Invoke(DispatcherPriority.Normal, (Action)(() => InitializingPage.Progress = new Range<int>(0, (Properties.Settings.Default.AccessTokens.Count + source_type_length - (int)Models.InstanceSourceType._start) * 2, 0)));

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
                                var response_cache = (JSON.Response)Properties.Settings.Default[_instance_directory_id[source_index] + "DiscoveryCache"];

                                // Get instance source.
                                var response_web = JSON.Response.Get(
                                    uri: new Uri((string)Properties.Settings.Default[_instance_directory_id[source_index] + "Discovery"]),
                                    pub_key: Convert.FromBase64String((string)Properties.Settings.Default[_instance_directory_id[source_index] + "DiscoveryPubKey"]),
                                    ct: Abort.Token,
                                    previous: response_cache);

                                // Add a tick.
                                Dispatcher.Invoke(DispatcherPriority.Normal, (Action)(() => InitializingPage.Progress.Value++));
                                ticks++;

                                // Parse instance source JSON.
                                var obj_web = (Dictionary<string, object>)eduJSON.Parser.Parse(
                                    response_web.Value,
                                    Abort.Token);

                                if (response_web.IsFresh)
                                {
                                    if (response_cache != null)
                                    {
                                        try
                                        {
                                            // Verify sequence.
                                            var obj_cache = (Dictionary<string, object>)eduJSON.Parser.Parse(
                                                response_cache.Value,
                                                Abort.Token);

                                            bool rollback = false;
                                            try { rollback = (uint)eduJSON.Parser.GetValue<int>(obj_cache, "seq") > (uint)eduJSON.Parser.GetValue<int>(obj_web, "seq"); }
                                            catch { rollback = true; }
                                            if (rollback)
                                            {
                                                // Sequence rollback detected. Revert to cached version.
                                                obj_web = obj_cache;
                                                response_web = response_cache;
                                            }
                                        }
                                        catch { }
                                    }

                                    // Save response to cache.
                                    Properties.Settings.Default[_instance_directory_id[source_index] + "DiscoveryCache"] = response_web;
                                }

                                // Add a tick.
                                Dispatcher.Invoke(DispatcherPriority.Normal, (Action)(() => InitializingPage.Progress.Value++));
                                ticks++;

                                // Load instance source.
                                _instance_sources[source_index] = Models.InstanceSourceInfo.FromJSON(obj_web);

                                {
                                    // Attach to RequestAuthorization instance events.
                                    if (_instance_sources[source_index] is Models.FederatedInstanceSourceInfo instance_source_federated)
                                        instance_source_federated.AuthenticatingInstance.RequestAuthorization += Instance_RequestAuthorization;

                                    foreach (var instance in _instance_sources[source_index].InstanceList)
                                        instance.RequestAuthorization += Instance_RequestAuthorization;
                                }

                                // Load instance source info settings.
                                Models.InstanceSourceInfo h = null;
                                #pragma warning disable 0612 // This section contains legacy settings conversion.
                                if (is_migrating_settings &&
                                    Properties.Settings.Default.GetPreviousVersion(_instance_directory_id[source_index] + "ConfigHistory") is Xml.VPNConfigurationSettingsList settings_list)
                                {
                                    // Versions before 1.0.9 used different instance source settings. Convert them.
                                    if (_instance_sources[source_index] is Models.LocalInstanceSourceInfo instance_source_local)
                                    {
                                        // Local authenticating instance source:
                                        // - Convert instance list.
                                        // - Set connecting instance by maximum popularity.
                                        var h_local = new Models.LocalInstanceSourceInfo();
                                        foreach (var h_cfg in settings_list)
                                        {
                                            if (h_cfg is Xml.LocalVPNConfigurationSettings h_cfg_local)
                                            {
                                                var instance = h_local.ConnectingInstanceList.FirstOrDefault(inst => inst.Base.AbsoluteUri == h_cfg_local.Instance.Base.AbsoluteUri);
                                                if (instance == null)
                                                {
                                                    h_cfg_local.Instance.Popularity = h_cfg_local.Popularity;
                                                    h_local.ConnectingInstanceList.Add(h_cfg_local.Instance);
                                                }
                                                else
                                                    instance.Popularity = Math.Max(instance.Popularity, h_cfg_local.Popularity);
                                            }
                                        }
                                        h_local.ConnectingInstance = h_local.ConnectingInstanceList.Aggregate((curMin, x) => (curMin == null || x.Popularity > curMin.Popularity ? x : curMin));
                                        h = h_local;
                                    }
                                    else if (_instance_sources[source_index] is Models.DistributedInstanceSourceInfo instance_source_distributed)
                                    {
                                        // Distributed authenticating instance source:
                                        // - Convert authenticating instance.
                                        // - Convert connecting instance.
                                        var h_distributed = new Models.DistributedInstanceSourceInfo();
                                        if (settings_list.Count > 0 && settings_list[0] is Xml.DistributedVPNConfigurationSettings h_cfg_distributed)
                                        {
                                            h_distributed.AuthenticatingInstance = new Models.InstanceInfo() { Base = new Uri(h_cfg_distributed.AuthenticatingInstance) };
                                            h_distributed.ConnectingInstance = new Models.InstanceInfo() { Base = new Uri(h_cfg_distributed.LastInstance) };
                                        }
                                        h = h_distributed;
                                    }
                                    else if (_instance_sources[source_index] is Models.FederatedInstanceSourceInfo instance_source_federated)
                                    {
                                        // Federated authenticating instance source:
                                        // - Convert connecting instance.
                                        var h_federated = new Models.FederatedInstanceSourceInfo();
                                        if (settings_list.Count > 0 && settings_list[0] is Xml.FederatedVPNConfigurationSettings h_cfg_federated)
                                        {
                                            h_federated.ConnectingInstance = new Models.InstanceInfo() { Base = new Uri(h_cfg_federated.LastInstance) };
                                        }
                                        h = h_federated;
                                    }
                                }
                                else
                                #pragma warning restore 0612
                                {
                                    h = Properties.Settings.Default[_instance_directory_id[source_index] + "InstanceSourceInfo"] is Xml.InstanceSourceInfo h_xml &&
                                        h_xml.InstanceSource != null ? h_xml.InstanceSource : null;
                                }

                                // Import instance source from settings.
                                {
                                    if (InstanceSources[source_index] is Models.LocalInstanceSourceInfo instance_source_local &&
                                        h is Models.LocalInstanceSourceInfo h_local)
                                    {
                                        // Local authenticating instance source:
                                        // - Restore instance list.
                                        // - Restore connecting instance (optional).
                                        foreach (var h_instance in h_local.ConnectingInstanceList)
                                        {
                                            var connecting_instance = instance_source_local.InstanceList.FirstOrDefault(inst => inst.Base.AbsoluteUri == h_instance.Base.AbsoluteUri);
                                            if (connecting_instance == null)
                                            {
                                                // The connecting instance was not found. Could be user entered or removed from discovery file.
                                                connecting_instance = h_instance;
                                                connecting_instance.RequestAuthorization += Instance_RequestAuthorization;
                                            } else
                                                connecting_instance.Popularity = h_instance.Popularity;

                                            var instance = instance_source_local.ConnectingInstanceList.FirstOrDefault(inst => inst.Base.AbsoluteUri == connecting_instance.Base.AbsoluteUri);
                                            if (instance == null)
                                                instance_source_local.ConnectingInstanceList.Add(connecting_instance);
                                            else
                                                instance.Popularity = Math.Max(instance.Popularity, h_instance.Popularity);
                                        }
                                        instance_source_local.ConnectingInstance = h_local.ConnectingInstance != null ? instance_source_local.ConnectingInstanceList.FirstOrDefault(inst => inst.Base.AbsoluteUri == h_local.ConnectingInstance.Base.AbsoluteUri) : null;
                                    }
                                    else if (InstanceSources[source_index] is Models.DistributedInstanceSourceInfo instance_source_distributed &&
                                        h is Models.DistributedInstanceSourceInfo h_distributed)
                                    {
                                        // Distributed authenticating instance source:
                                        // - Restore authenticating instance.
                                        // - Restore connecting instance (optional).
                                        instance_source_distributed.AuthenticatingInstance = h_distributed.AuthenticatingInstance != null ? instance_source_distributed.InstanceList.FirstOrDefault(inst => inst.Base.AbsoluteUri == h_distributed.AuthenticatingInstance.Base.AbsoluteUri) : null;
                                        if (instance_source_distributed.AuthenticatingInstance != null)
                                        {
                                            instance_source_distributed.AuthenticatingInstance.RequestAuthorization += Instance_RequestAuthorization;
                                            instance_source_distributed.ConnectingInstance = h_distributed.ConnectingInstance != null ? instance_source_distributed.ConnectingInstanceList.FirstOrDefault(inst => inst.Base.AbsoluteUri == h_distributed.ConnectingInstance.Base.AbsoluteUri) : null;
                                        }
                                    }
                                    else if (InstanceSources[source_index] is Models.FederatedInstanceSourceInfo instance_source_federated &&
                                        h is Models.FederatedInstanceSourceInfo h_federated)
                                    {
                                        // Federated authenticating instance source:
                                        // - Restore connecting instance (optional).
                                        instance_source_federated.ConnectingInstance = h_federated.ConnectingInstance != null ? instance_source_federated.ConnectingInstanceList.FirstOrDefault(inst => inst.Base.AbsoluteUri == h_federated.ConnectingInstance.Base.AbsoluteUri) : null;
                                    }
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
                        Properties.Settings.Default.AccessTokens = new Xml.StringDictionary();
                        lock (_access_token_cache_lock)
                            foreach (var access_token in _access_token_cache)
                                Properties.Settings.Default.AccessTokens[access_token.Key] = access_token.Value.ToBase64String();

                        // Update settings.
                        Parallel.For((int)Models.InstanceSourceType._start, (int)Models.InstanceSourceType._end, source_index =>
                            Properties.Settings.Default[_instance_directory_id[source_index] + "InstanceSourceInfo"] = new Xml.InstanceSourceInfo() { InstanceSource = InstanceSources[source_index] });

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
        }

        #endregion

        #region Methods

        /// <summary>
        /// Triggers authorization for selected instance asynchronously
        /// </summary>
        /// <param name="instance">Instance</param>
        /// <returns>Asynchronous operation</returns>
        /// <exception cref="AccessTokenNullException">Authorization failed</exception>
        public async Task TriggerAuthorizationAsync(Models.InstanceInfo instance)
        {
            var e = new Models.RequestAuthorizationEventArgs("config");
            var authorization_task = new Task(() => Instance_RequestAuthorization(instance, e), Abort.Token, TaskCreationOptions.LongRunning);
            authorization_task.Start();
            await authorization_task;

            if (e.AccessToken == null)
                throw new Models.AccessTokenNullException();
        }

        /// <summary>
        /// Does the instance source have any previously connected instances?
        /// </summary>
        /// <param name="instance_source_type">Instance source type</param>
        /// <returns><c>true</c> when user has previously connected to any instances of this source; <c>false</c> otherwise</returns>
        public bool HasConnectingInstances(Models.InstanceSourceType instance_source_type)
        {
            int source_index = (int)instance_source_type;

            if (InstanceSources[source_index] is Models.LocalInstanceSourceInfo instance_source_local)
            {
                // Local authenticating instance source:
                // We need at least one connecting instance on the list.
                if (instance_source_local.ConnectingInstanceList.Count > 0)
                    return true;
            }
            else if (InstanceSources[source_index] is Models.DistributedInstanceSourceInfo instance_source_distributed)
            {
                // Distributed authenticating instance source:
                // The authenticating instance must be selected and we need its access token cached.
                if (instance_source_distributed.AuthenticatingInstance != null)
                {
                    var e = new Models.RequestAuthorizationEventArgs("config") { SourcePolicy = Models.RequestAuthorizationEventArgs.SourcePolicyType.SavedOnly };
                    Instance_RequestAuthorization(instance_source_distributed.AuthenticatingInstance, e);
                    if (e.AccessToken != null)
                        return true;
                }
            }
            else if (InstanceSources[source_index] is Models.FederatedInstanceSourceInfo instance_source_federated)
            {
                // Federated authenticating instance source:
                // At least one of the instances need its access token cached.
                var e = new Models.RequestAuthorizationEventArgs("config") { SourcePolicy = Models.RequestAuthorizationEventArgs.SourcePolicyType.SavedOnly };
                foreach (var instance in instance_source_federated.ConnectingInstanceList)
                {
                    Instance_RequestAuthorization(instance, e);
                    if (e.AccessToken != null)
                        return true;
                }
            }
            else
                throw new InvalidOperationException();

            return false;
        }

        /// <summary>
        /// Called when an instance requests user authorization
        /// </summary>
        /// <param name="sender">Instance of type <c>eduVPN.Models.InstanceInfo</c> requiring authorization</param>
        /// <param name="e">Authorization request event arguments</param>
        public void Instance_RequestAuthorization(object sender, Models.RequestAuthorizationEventArgs e)
        {
            if (sender is Models.InstanceInfo authenticating_instance)
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
                            var e_instance = new RequestInstanceAuthorizationEventArgs((Models.InstanceInfo)sender, e.Scope);
                            RequestInstanceAuthorization?.Invoke(this, e_instance);
                            e.AccessToken = e_instance.AccessToken;
                        }
                        else
                        {
                            // We're in the background thread - raise event via dispatcher.
                            Dispatcher.Invoke(DispatcherPriority.Normal,
                                (Action)(() =>
                                {
                                    var e_instance = new RequestInstanceAuthorizationEventArgs((Models.InstanceInfo)sender, e.Scope);
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
