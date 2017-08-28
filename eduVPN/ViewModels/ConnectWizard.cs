/*
    eduVPN - End-user friendly VPN

    Copyright: 2017, The Commons Conservancy eduVPN Programme
    SPDX-License-Identifier: GPL-3.0+
*/

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Threading;

namespace eduVPN.ViewModels
{
    /// <summary>
    /// Connect wizard
    /// </summary>
    public class ConnectWizard : Window, IDisposable
    {
        #region Fields

        /// <summary>
        /// Instance directory URI IDs as used in <c>Properties.Settings.Default</c> collection
        /// </summary>
        private static readonly string[] _instance_directory_id = new string[]
        {
            "SecureInternetDiscovery",
            "InstituteAccessDiscovery",
        };

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
        /// VPN configuration histories
        /// </summary>
        public ObservableCollection<Models.VPNConfiguration>[] ConfigurationHistories
        {
            get { return _configuration_histories; }
        }
        private ObservableCollection<Models.VPNConfiguration>[] _configuration_histories;

        /// <summary>
        /// Selected instance source
        /// </summary>
        public Models.InstanceSourceInfo InstanceSource
        {
            get { return _instance_source; }
            set { _instance_source = value; RaisePropertyChanged(); }
        }
        private Models.InstanceSourceInfo _instance_source;

        /// <summary>
        /// VPN configuration
        /// </summary>
        public Models.VPNConfiguration Configuration
        {
            get { return _configuration; }
            set { _configuration = value; RaisePropertyChanged(); }
        }
        private Models.VPNConfiguration _configuration;

        /// <summary>
        /// VPN session
        /// </summary>
        public Models.VPNSession Session
        {
            get { return _session; }
            set { _session = value; RaisePropertyChanged(); }
        }
        private Models.VPNSession _session;

        /// <summary>
        /// Instance request authorization event
        /// </summary>
        public event EventHandler<RequestInstanceAuthorizationEventArgs> RequestInstanceAuthorization;

        #region Pages

        /// <summary>
        /// The page the wizard is currently displaying
        /// </summary>
        public ConnectWizardPage CurrentPage
        {
            get { return _current_page; }
            set
            {
                _current_page = value;
                RaisePropertyChanged();
                _current_page.OnActivate();
            }
        }
        private ConnectWizardPage _current_page;

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
        /// Instance selection page
        /// </summary>
        public AuthenticatingInstanceSelectPage AuthenticatingInstanceSelectPage
        {
            get
            {
                if (InstanceSource is Models.DistributedInstanceSourceInfo)
                {
                    if (_authenticating_country_select_page == null)
                        _authenticating_country_select_page = new AuthenticatingCountrySelectPage(this);
                    return _authenticating_country_select_page;
                }
                else
                {
                    if (_authenticating_institute_select_page == null)
                        _authenticating_institute_select_page = new AuthenticatingInstituteSelectPage(this);
                    return _authenticating_institute_select_page;
                }
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
        /// (Instance and) profile selection wizard page
        /// </summary>
        public ProfileSelectBasePage ProfileSelectPage
        {
            get
            {
                if (InstanceSource is Models.FederatedInstanceSourceInfo ||
                    InstanceSource is Models.DistributedInstanceSourceInfo)
                {
                    if (_connecting_instance_and_profile_select_page == null)
                        _connecting_instance_and_profile_select_page = new ConnectingInstanceAndProfileSelectPage(this);
                    return _connecting_instance_and_profile_select_page;
                }
                else
                {
                    if (_profile_select_page == null)
                        _profile_select_page = new ProfileSelectPage(this);
                    return _profile_select_page;
                }
            }
        }
        private ProfileSelectPage _profile_select_page;
        private ConnectingInstanceAndProfileSelectPage _connecting_instance_and_profile_select_page;

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

        #endregion

        #endregion

        #region Constructors

        /// <summary>
        /// Constructs the wizard
        /// </summary>
        public ConnectWizard()
        {
            if (Properties.Settings.Default.SettingsVersion == 0)
            {
                // Migrate settings from previous version.
                Properties.Settings.Default.Upgrade();
                Properties.Settings.Default.SettingsVersion = 1;
            }

            Dispatcher.ShutdownStarted += (object sender, EventArgs e) => {
                // Persist settings to disk.
                Properties.Settings.Default.Save();
            };

            Configuration = new Models.VPNConfiguration();

            // Show initializing wizard page.
            CurrentPage = InitializingPage;

            _instance_sources = new Models.InstanceSourceInfo[_instance_directory_id.Length];
            _configuration_histories = new ObservableCollection<Models.VPNConfiguration>[_instance_directory_id.Length];

            // Spawn instance source loading threads.
            var threads = new Thread[_instance_directory_id.Length];
            for (var i = 0; i < _instance_directory_id.Length; i++)
            {
                // Launch instance source load in the background.
                threads[i] = new Thread(new ParameterizedThreadStart(
                    param =>
                    {
                        var source_index = (int)param;
                        for (;;)
                        {
                            try
                            {
                                var response_cache = (JSON.Response)Properties.Settings.Default[_instance_directory_id[source_index] + "Cache"];

                                // Get instance source.
                                var response_web = JSON.Response.Get(
                                    uri: new Uri((string)Properties.Settings.Default[_instance_directory_id[source_index]]),
                                    pub_key: Convert.FromBase64String((string)Properties.Settings.Default[_instance_directory_id[source_index] + "PubKey"]),
                                    ct: Abort.Token,
                                    previous: response_cache);

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
                                            catch (Exception) { rollback = true; }
                                            if (rollback)
                                            {
                                                // Sequence rollback detected. Revert to cached version.
                                                obj_web = obj_cache;
                                                response_web = response_cache;
                                            }
                                        }
                                        catch (Exception) { }
                                    }

                                    // Save response to cache.
                                    Properties.Settings.Default[_instance_directory_id[source_index] + "Cache"] = response_web;
                                }

                                // Load instance source.
                                _instance_sources[source_index] = Models.InstanceSourceInfo.FromJSON(obj_web);

                                // Attach to RequestAuthorization instance events.
                                foreach (var instance in _instance_sources[source_index])
                                    instance.RequestAuthorization += Instance_RequestAuthorization;

                                _configuration_histories[source_index] = new ObservableCollection<Models.VPNConfiguration>();

                                // Load configuration histories from settings.
                                var hist = (Models.VPNConfigurationSettingsList)Properties.Settings.Default[_instance_directory_id[source_index] + "ConfigHistory"];
                                Parallel.For(0, hist.Count,
                                    idx_hist =>
                                    {
                                        var cfg = new Models.VPNConfiguration();
                                        var h = hist[idx_hist];

                                        try
                                        {
                                            // Restore configuration.
                                            if (_instance_sources[source_index] is Models.LocalInstanceSourceInfo instance_source_local &&
                                                h is Models.LocalVPNConfigurationSettings h_local)
                                            {
                                                // Local authenticating instance source:
                                                // - Restore instance, which is both: authenticating and connecting.
                                                // - Restore profile.
                                                cfg.AuthenticatingInstance = instance_source_local.Where(inst => inst.Base.AbsoluteUri == h_local.Instance.Base.AbsoluteUri).FirstOrDefault();
                                                if (cfg.AuthenticatingInstance == null)
                                                {
                                                    cfg.AuthenticatingInstance = h_local.Instance;
                                                    cfg.AuthenticatingInstance.RequestAuthorization += Instance_RequestAuthorization;
                                                }
                                                cfg.ConnectingInstance = cfg.AuthenticatingInstance;

                                                // Don't try to match profile with existing profiles, or risk triggering OAuth in GetProfileList(). Use stored version from settings directly.
                                                //cfg.ConnectingProfile = cfg.ConnectingInstance.GetProfileList(cfg.AuthenticatingInstance, Abort.Token).Where(p => p.ID == h_local.Profile.ID).FirstOrDefault();
                                                cfg.ConnectingProfile = h_local.Profile;
                                                if (cfg.ConnectingProfile == null) return;
                                            }
                                            else if (_instance_sources[source_index] is Models.DistributedInstanceSourceInfo instance_source_distributed &&
                                                h is Models.DistributedVPNConfigurationSettings h_distributed)
                                            {
                                                // Distributed authenticating instance source:
                                                // - Restore authenticating instance.
                                                // - Restore last connected instance (optional).
                                                cfg.AuthenticatingInstance = instance_source_distributed.Where(inst => inst.Base.AbsoluteUri == h_distributed.AuthenticatingInstance).FirstOrDefault();
                                                if (cfg.AuthenticatingInstance == null) return;
                                                cfg.ConnectingInstance = instance_source_distributed.Where(inst => inst.Base.AbsoluteUri == h_distributed.LastInstance).FirstOrDefault();
                                            }
                                            else if (_instance_sources[source_index] is Models.FederatedInstanceSourceInfo instance_source_federated &&
                                                h is Models.FederatedVPNConfigurationSettings h_federated)
                                            {
                                                // Federated authenticating instance source:
                                                // - Get authenticating instance from federated instance source settings.
                                                // - Restore last connected instance (optional).
                                                cfg.AuthenticatingInstance = new Models.InstanceInfo(instance_source_federated);
                                                cfg.AuthenticatingInstance.RequestAuthorization += Instance_RequestAuthorization;
                                                cfg.ConnectingInstance = instance_source_federated.Where(inst => inst.Base.AbsoluteUri == h_federated.LastInstance).FirstOrDefault();
                                            }
                                            else
                                                return;
                                        }
                                        catch (Exception) { return; }

                                        // Configuration successfuly restored. Add it.
                                        lock (_configuration_histories[source_index])
                                            _configuration_histories[source_index].Add(cfg);
                                    });

                                break;
                            }
                            catch (OperationCanceledException) { throw; }
                            catch (Exception ex)
                            {
                                // Make it a clean start next time.
                                Properties.Settings.Default[_instance_directory_id[source_index] + "Cache"] = null;

                                // Notify the sender the instance source loading failed. However, continue with other instance sources.
                                // This will overwrite all previous error messages.
                                Error = new AggregateException(String.Format(Resources.Strings.ErrorInstanceSourceInfoLoad, _instance_directory_id[source_index]), ex);
                            }

                            // Sleep for 3s, then retry.
                            if (Abort.Token.WaitHandle.WaitOne(TimeSpan.FromSeconds(3)))
                                break;
                        }
                    }));
                threads[i].Start(i);
            }

            // Spawn monitor thread to wait for initialization to complete.
            new Thread(new ThreadStart(
                () =>
                {
                    Dispatcher.Invoke(DispatcherPriority.Normal, (Action)(() => ChangeTaskCount(+1)));
                    try
                    {
                        // Wait for all threads.
                        foreach (var thread in threads)
                            thread.Join();

                        // Proceed to the "first" page.
                        Dispatcher.Invoke(DispatcherPriority.Normal, (Action)(() => Error = null));
                        CurrentPage = InstanceSourceSelectPage;
                    }
                    finally { Dispatcher.Invoke(DispatcherPriority.Normal, (Action)(() => ChangeTaskCount(-1))); }
                })).Start();
        }

        #endregion

        #region Methods

        /// <summary>
        /// Starts VPN session
        /// </summary>
        public void StartSession()
        {
            Session = new Models.OpenVPNSession(Configuration);

            // Save session configuration to history.
            var source_index = Array.IndexOf(_instance_sources, InstanceSource);
            if (source_index >= 0)
            {
                var hist = (Models.VPNConfigurationSettingsList)Properties.Settings.Default[_instance_directory_id[source_index] + "ConfigHistory"];
                Models.VPNConfigurationSettings el = null;
                if (_instance_sources[source_index] is Models.LocalInstanceSourceInfo)
                {
                    // Local authenticating instance source
                    el = new Models.LocalVPNConfigurationSettings()
                    {
                        Instance = Configuration.ConnectingInstance,
                        Profile = Configuration.ConnectingProfile,
                    };
                }
                else if (_instance_sources[source_index] is Models.DistributedInstanceSourceInfo)
                {
                    // Distributed authenticating instance source
                    el = new Models.DistributedVPNConfigurationSettings()
                    {
                        AuthenticatingInstance = Configuration.AuthenticatingInstance.Base.AbsoluteUri,
                        LastInstance = Configuration.ConnectingInstance.Base.AbsoluteUri,
                    };
                }
                else if (_instance_sources[source_index] is Models.FederatedInstanceSourceInfo)
                {
                    // Federated authenticating instance source.
                    el = new Models.FederatedVPNConfigurationSettings()
                    {
                        LastInstance = Configuration.ConnectingInstance.Base.AbsoluteUri,
                    };
                }

                if (el != null)
                {
                    // Check for duplicates and update popularity.
                    int found = -1;
                    for (var i = hist.Count; i-- > 0;)
                    {
                        if (hist[i].Equals(el))
                        {
                            if (found < 0)
                            {
                                // Upvote popularity.
                                hist[i].Popularity *= 1.1f;
                                found = i;
                            }
                            else
                            {
                                // We found a match second time. This happened early in the Alpha stage when duplicate checking didn't work right.
                                // Clean the list. The victim is less popular entry.
                                if (hist[i].Popularity < hist[found].Popularity)
                                {
                                    hist.RemoveAt(i);
                                    found--;
                                }
                                else
                                {
                                    hist.RemoveAt(found);
                                    found = i;
                                }
                            }
                        }
                        else
                        {
                            // Downvote popularity.
                            hist[i].Popularity /= 1.1f;
                        }
                    }
                    if (found < 0)
                        hist.Add(el);
                }
            }

            // Launch VPN session in the background.
            new Thread(new ThreadStart(
                () =>
                {
                    Dispatcher.Invoke(DispatcherPriority.Normal, (Action)(() => Error = null));
                    try
                    {
                        Session.Run(Abort.Token);
                    }
                    catch (OperationCanceledException) { }
                    catch (Exception ex) { Dispatcher.Invoke(DispatcherPriority.Normal, (Action)(() => { Error = ex; })); }
                })).Start();
        }

        /// <summary>
        /// Called when an instance requests user authorization
        /// </summary>
        /// <param name="sender">Instance of type <c>eduVPN.Models.InstanceInfo</c> requiring authorization</param>
        /// <param name="e">Authorization request event arguments</param>
        public void Instance_RequestAuthorization(object sender, Models.RequestAuthorizationEventArgs e)
        {
            // Re-raise this event as ConnectWizard event, to simplify view.
            // This way the view can listen ConnectWizard for authentication events only.
            if (Dispatcher.CurrentDispatcher == Dispatcher)
            {
                // We're in the GUI thread.
                var e_instance = new RequestInstanceAuthorizationEventArgs((Models.InstanceInfo)sender);
                RequestInstanceAuthorization?.Invoke(this, e_instance);
                e.AccessToken = e_instance.AccessToken;
            }
            else
            {
                // We're in the background thread - raise event via dispatcher.
                Dispatcher.Invoke(DispatcherPriority.Normal,
                    (Action)(() =>
                    {
                        var e_instance = new RequestInstanceAuthorizationEventArgs((Models.InstanceInfo)sender);
                        RequestInstanceAuthorization?.Invoke(this, e_instance);
                        e.AccessToken = e_instance.AccessToken;
                    }));
            }
        }

        #endregion

        #region IDisposable Support
        private bool disposedValue = false; // To detect redundant calls

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    if (_session != null)
                    {
                        _session.Dispose();
                        _session = null;
                    }
                }

                disposedValue = true;
            }
        }

        // This code added to correctly implement the disposable pattern.
        public void Dispose()
        {
            // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
            Dispose(true);
        }
        #endregion
    }
}
