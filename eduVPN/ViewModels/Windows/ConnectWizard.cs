/*
    eduVPN - End-user friendly VPN

    Copyright: 2017, The Commons Conservancy eduVPN Programme
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
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using System.Windows.Threading;
using System.Xml;

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
        private static readonly float _popularity_alpha = 0.1f;

        #endregion

        #region Properties

        /// <summary>
        /// Available instance sources
        /// </summary>
        public InstanceSource[] InstanceSources
        {
            get { return _instance_sources; }
        }
        private InstanceSource[] _instance_sources;

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
                            Properties.Settings.Default[Global.InstanceDirectoryId[source_index] + "InstanceSourceSettings"] = new Xml.InstanceSourceSettings() { InstanceSource = InstanceSources[source_index].ToSettings() };
                        },

                        // canExecute
                        param =>
                            param is StartSessionParams &&
                            param.ConnectingProfile != null &&
                            param.ConnectingProfile.Instance != null);

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
            public InstanceSourceType InstanceSourceType { get; }

            /// <summary>
            /// Connecting eduVPN instance profile
            /// </summary>
            public Profile ConnectingProfile { get; }

            #endregion

            #region Constructors

            /// <summary>
            /// Constructs a StartSession command parameter set
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
        /// Instance request authorization event
        /// </summary>
        public event EventHandler<RequestInstanceAuthorizationEventArgs> RequestInstanceAuthorization;

        /// <summary>
        /// 2-Factor Authentication enrollment requested
        /// </summary>
        public event EventHandler<RequestTwoFactorEnrollmentEventArgs> RequestTwoFactorEnrollment;

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
                for (var source_index = (int)InstanceSourceType._start; source_index < (int)InstanceSourceType._end; source_index++)
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
        /// <remarks>Available only when authenticating and connecting instances can be different. I.e. <c>InstanceSource</c> is <c>eduVPN.LocalInstanceSource</c> or <c>eduVPN.DistributedInstanceSource</c>.</remarks>
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
            // Create session queue.
            _sessions = new ObservableCollection<VPNSession>();
            _sessions.CollectionChanged += (object sender, NotifyCollectionChangedEventArgs e) => RaisePropertyChanged(nameof(ActiveSession));

            // Show initializing wizard page.
            _current_page = InitializingPage;

            // Setup initialization.
            var worker = new BackgroundWorker() { WorkerReportsProgress = true };
            worker.DoWork += (object sender, DoWorkEventArgs e) =>
            {
                var source_type_length = (int)InstanceSourceType._end;
                _instance_sources = new InstanceSource[source_type_length];

                // Setup progress feedback.
                Dispatcher.Invoke(DispatcherPriority.Normal, (Action)(() => InitializingPage.Progress = new Range<int>(0, (source_type_length - (int)InstanceSourceType._start) * 2, 0)));

                // Spawn instance source loading threads.
                Parallel.For((int)InstanceSourceType._start, source_type_length, source_index =>
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
                                var pub_key = (string)Properties.Settings.Default[Global.InstanceDirectoryId[source_index] + "DiscoveryPubKey"];
                                var obj_web = Properties.Settings.Default.ResponseCache.GetSeq(
                                    new Uri((string)Properties.Settings.Default[Global.InstanceDirectoryId[source_index] + "Discovery"]),
                                    !string.IsNullOrWhiteSpace(pub_key) ? Convert.FromBase64String(pub_key) : null,
                                    Abort.Token);

                                // Add a tick.
                                Dispatcher.Invoke(DispatcherPriority.Normal, (Action)(() => InitializingPage.Progress.Value++));
                                ticks++;

                                // Load instance source.
                                _instance_sources[source_index] = InstanceSource.FromJSON(obj_web);

                                {
                                    // Attach to instance events.
                                    if (_instance_sources[source_index] is FederatedInstanceSource instance_source_federated)
                                    {
                                        instance_source_federated.AuthenticatingInstance.RequestAuthorization += Instance_RequestAuthorization;
                                        instance_source_federated.AuthenticatingInstance.ForgetAuthorization += Instance_ForgetAuthorization;
                                    }

                                    foreach (var instance in _instance_sources[source_index].InstanceList)
                                    {
                                        instance.RequestAuthorization += Instance_RequestAuthorization;
                                        instance.ForgetAuthorization += Instance_ForgetAuthorization;
                                    }
                                }

                                // Import settings.
                                Xml.InstanceSourceSettingsBase h = (Properties.Settings.Default[Global.InstanceDirectoryId[source_index] + "InstanceSourceSettings"] as Xml.InstanceSourceSettings)?.InstanceSource;
                                InstanceSources[source_index].FromSettings(this, h);

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
                                    Error = new AggregateException(String.Format(Resources.Strings.ErrorInstanceSourceInfoLoad, Global.InstanceDirectoryId[source_index]), ex);

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
                        Version repo_version = null, product_version = null;

                        try
                        {
                            Parallel.ForEach(new List<Action>()
                            {
                                () =>
                                {
                                    if (File.Exists(updater_filename))
                                    {
                                        // Clean stale updater WSF file. If possible.
                                        Trace.TraceInformation("Deleting file {0}...", updater_filename);
                                        try { File.Delete(updater_filename); }
                                        catch (Exception ex) { Trace.TraceWarning("Deleting {0} file failed: {1}", updater_filename, ex.ToString()); }
                                    }
                                },

                                () =>
                                {
                                    // Get self-update.
                                    var pub_key = Properties.Settings.Default.SelfUpdatePubKey;
                                    var uri = new Uri(Properties.Settings.Default.SelfUpdate);
                                    Trace.TraceInformation("Downloading self-update JSON discovery from {0}...", uri.AbsoluteUri);
                                    obj_web = Properties.Settings.Default.ResponseCache.GetSeq(
                                        uri,
                                        !string.IsNullOrWhiteSpace(pub_key) ? Convert.FromBase64String(pub_key) : null,
                                        Abort.Token);

                                    repo_version = new Version((string)obj_web["version"]);
                                    Trace.TraceInformation("Online version: {0}", repo_version.ToString());
                                },

                                () =>
                                {
                                    // Evaluate installed products.
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
                                                if ((bundle_upgrade_code is string   bundle_upgrade_code_str   && bundle_upgrade_code_str.ToUpperInvariant() == "{EF5D5806-B90B-4AA3-800A-2D7EA1592BA0}" ||
                                                        bundle_upgrade_code is string[] bundle_upgrade_code_array && bundle_upgrade_code_array.FirstOrDefault(code => code.ToUpperInvariant() == "{EF5D5806-B90B-4AA3-800A-2D7EA1592BA0}") != null) &&
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
                                return;
                            }
                        }
                        catch { }

                        if (product_version == null)
                        {
                            // Nothing to update.
                            Trace.TraceInformation("Product not installed or version could not be determined.");
                            return;
                        }

                        if (repo_version <= product_version)
                        {
                            // Product already up-to-date.
                            Trace.TraceInformation("Update not required.");
                            return;
                        }

                        var installer_ready = false;
                        var repo_hash = ((string)obj_web["hash-sha256"]).FromHexToBin();
                        if (File.Exists(installer_filename))
                        {
                            // File already exists. Verify its integrity.
                            Trace.TraceInformation("Verifying installer file {0} integrity...", installer_filename);
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

                                Trace.TraceInformation("{0} integrity OK.", installer_filename);
                                installer_ready = true;
                            }
                            catch (OperationCanceledException) { throw; }
                            catch (Exception ex)
                            {
                                Trace.TraceWarning("Error: {0}", ex.ToString());

                                // Delete file. If possible.
                                Trace.TraceInformation("Deleting file {0}...", installer_filename);
                                try { File.Delete(installer_filename); }
                                catch (Exception ex2) { Trace.TraceWarning("Deleting {0} file failed: {1}", installer_filename, ex2.ToString()); }
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
                                    Trace.TraceInformation("Downloading installer file from {0}...", uri.AbsoluteUri);
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
                                                    throw new DownloadedFileCorruptException(string.Format(Resources.Strings.ErrorDownloadedFileCorrupt, uri.AbsoluteUri));
                                            }

                                            installer_ready = true;
                                            break;
                                        }
                                        catch
                                        {
                                            // Delete file. If possible.
                                            Trace.TraceInformation("Deleting file {0}...", installer_filename);
                                            try { File.Delete(installer_filename); }
                                            catch (Exception ex2) { Trace.TraceWarning("Deleting {0} file failed: {1}", installer_filename, ex2.ToString()); }

                                            throw;
                                        }
                                    }
                                }
                                catch (OperationCanceledException) { throw; }
                                catch (Exception ex)
                                {
                                    Trace.TraceWarning("Error: {0}", ex.ToString());
                                    uris.RemoveAt(uri_idx);
                                }
                            }
                        }

                        if (!installer_ready)
                        {
                            // The installer file is not ready.
                            Trace.TraceWarning("Installer file not available. Aborting...");
                            return;
                        }

                        // We're in the background thread - raise the prompt event via dispatcher.
                        Trace.TraceInformation("Prompting user to update...");
                        var e_prompt = new PromptSelfUpdateEventArgs(product_version, repo_version);
                        Dispatcher.Invoke(DispatcherPriority.Normal, (Action)(() => PromptSelfUpdate?.Invoke(this, e_prompt)));
                        bool quit = false;

                        switch (e_prompt.Action)
                        {
                            case PromptSelfUpdateActionType.Update:
                                {
                                    // Prepare WSF file.
                                    Trace.TraceInformation("Creating update script file {0}...", updater_filename);
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
                                        var installer_arguments_esc = eduJSON.Parser.GetValue(obj_web, "arguments", out string installer_arguments) ? " " + HttpUtility.JavaScriptStringEncode(installer_arguments) : "";
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
                                        script.AppendLine("if (wsh.Run(\"\\\"" + HttpUtility.JavaScriptStringEncode(installer_filename.Replace("\"", "\"\"")) + "\\\"" + installer_arguments_esc + "\", 0, true) == 0) {");
                                        script.AppendLine("  // Installer succeeded. Relaunch the application.");
                                        script.AppendLine("  var shl = WScript.CreateObject(\"Shell.Application\");");
                                        script.AppendLine("  shl.ShellExecute(\"" + HttpUtility.JavaScriptStringEncode(argv[0]) + "\", \"" + HttpUtility.JavaScriptStringEncode(arguments.ToString()) + "\", \"" + HttpUtility.JavaScriptStringEncode(Environment.CurrentDirectory) + "\");");
                                        script.AppendLine("  // Delete the installer file.");
                                        script.AppendLine("  var fso = WScript.CreateObject(\"Scripting.FileSystemObject\");");
                                        script.AppendLine("  try { fso.DeleteFile(\"" + HttpUtility.JavaScriptStringEncode(installer_filename) + "\", true); } catch (err) {}");
                                        script.AppendLine("}");
                                        writer.WriteCData(script.ToString());
                                        writer.WriteEndElement(); // script

                                        writer.WriteEndElement(); // job
                                        writer.WriteEndElement(); // package
                                        writer.WriteEndDocument();
                                    }

                                    // Launch wscript.exe with WSF file.
                                    Trace.TraceInformation("Launching update script file {0}...", updater_filename);
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
                                Trace.TraceInformation("User will be reminded after three days again. (Should an update is still required.)");
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

                        if (quit)
                        {
                            // Ask the view to quit.
                            Trace.TraceInformation("Quitting client...");
                            Dispatcher.Invoke(DispatcherPriority.Normal, (Action)(() => QuitApplication?.Invoke(this, EventArgs.Empty)));
                        }
                    }
                    catch (OperationCanceledException) { }
                    catch (Exception ex) { Trace.TraceError("Error: {0}", ex.ToString()); }
                    finally { Dispatcher.Invoke(DispatcherPriority.Normal, (Action)(() => ChangeTaskCount(-1))); }
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
        /// <param name="sender">Instance of type <c>eduVPN.Instance</c> requiring authorization</param>
        /// <param name="e">Authorization request event arguments</param>
        public void Instance_RequestAuthorization(object sender, RequestAuthorizationEventArgs e)
        {
            if (sender is Instance authenticating_instance)
            {
                lock (Properties.Settings.Default.AccessTokenCache)
                {
                    if (e.SourcePolicy != RequestAuthorizationEventArgs.SourcePolicyType.ForceAuthorization)
                    {
                        var key = authenticating_instance.Base.AbsoluteUri;
                        if (Properties.Settings.Default.AccessTokenCache.TryGetValue(key, out var access_token))
                        {
                            if (access_token.Expires.HasValue && access_token.Expires.Value <= DateTime.Now)
                            {
                                // Token expired. Refresh it.

                                // Get API endpoints. (Not called from the UI thread or already cached by now. Otherwise it would need to be spawned as a background task to avoid deadlock.)
                                var api = authenticating_instance.GetEndpoints(Abort.Token);

                                access_token = access_token.RefreshToken(api.TokenEndpoint, null, Abort.Token);
                                if (access_token != null)
                                {
                                    // Update access token cache.
                                    Properties.Settings.Default.AccessTokenCache[key] = access_token;

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

                    if (e.SourcePolicy != RequestAuthorizationEventArgs.SourcePolicyType.SavedOnly)
                    {
                        // Get API endpoints. (Not called from the UI thread or already cached by now. Otherwise it would need to be spawned as a background task to avoid deadlock.)
                        var api = authenticating_instance.GetEndpoints(Abort.Token);

                        // Prepare new authorization grant.
                        var authorization_grant = new AuthorizationGrant()
                        {
                            AuthorizationEndpoint = api.AuthorizationEndpoint,
                            ClientID = "org.eduvpn.app",
                            Scope = new List<string>() { e.Scope },
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
                            e.AccessToken = authorization_grant.ProcessResponse(
                                HttpUtility.ParseQueryString(e_instance.CallbackURI.Query),
                                api.TokenEndpoint,
                                null,
                                Abort.Token);

                        if (e.AccessToken != null)
                        {
                            // Save access token to the cache.
                            Properties.Settings.Default.AccessTokenCache[authenticating_instance.Base.AbsoluteUri] = e.AccessToken;
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Called when an instance requests authorization delete
        /// </summary>
        /// <param name="sender">Instance of type <c>eduVPN.Instance</c> requiring authorization</param>
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

        public void Profile_RequestTwoFactorEnrollment(object sender, RequestTwoFactorEnrollmentEventArgs e)
        {
            // Re-raise this event as ConnectWizard event, to simplify view.
            // This way the view can listen ConnectWizard for profile events only.
            if (Dispatcher.CurrentDispatcher == Dispatcher)
            {
                // We're in the GUI thread.
                RequestTwoFactorEnrollment?.Invoke(sender, e);
            }
            else
            {
                // We're in the background thread - raise event via dispatcher.
                Dispatcher.Invoke(DispatcherPriority.Normal, (Action)(() => RequestTwoFactorEnrollment?.Invoke(sender, e)));
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
