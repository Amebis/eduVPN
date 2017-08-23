/*
    eduVPN - End-user friendly VPN

    Copyright: 2017, The Commons Conservancy eduVPN Programme
    SPDX-License-Identifier: GPL-3.0+
*/

using Prism.Mvvm;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Windows.Threading;

namespace eduVPN.ViewModels
{
    /// <summary>
    /// Connect wizard
    /// </summary>
    public class ConnectWizard : BindableBase, IDisposable
    {
        #region Fields

        /// <summary>
        /// Instance directory URI IDs as used in <c>Properties.Settings.Default</c> collection
        /// </summary>
        private static readonly string[] _instance_directory_id = new string[]
        {
            "SecureInternetDirectory",
            "InstituteAccessDirectory",
        };

        #endregion

        #region Properties

        /// <summary>
        /// UI thread's dispatcher
        /// </summary>
        /// <remarks>
        /// Background threads must raise property change events in the UI thread.
        /// </remarks>
        public Dispatcher Dispatcher { get; }

        /// <summary>
        /// Token used to abort unfinished background processes in case of application shutdown.
        /// </summary>
        public static CancellationTokenSource Abort { get => _abort; }
        private static CancellationTokenSource _abort = new CancellationTokenSource();

        /// <summary>
        /// The page error; <c>null</c> when no error condition.
        /// </summary>
        public Exception Error
        {
            get { return _error; }
            set { _error = value; RaisePropertyChanged(); }
        }
        private Exception _error;

        /// <summary>
        /// Is wizard performing background tasks?
        /// </summary>
        public bool IsBusy
        {
            get { return _task_count > 0; }
        }

        /// <summary>
        /// Number of background tasks the wizard is performing
        /// </summary>
        public int TaskCount
        {
            get { lock (_task_count_lock) return _task_count; }
        }
        private int _task_count;
        private object _task_count_lock = new object();

        /// <summary>
        /// Available instance groups
        /// </summary>
        public Models.InstanceGroupInfo[] InstanceGroups
        {
            get { return _instance_groups; }
        }
        private Models.InstanceGroupInfo[] _instance_groups;

        /// <summary>
        /// Selected instance group
        /// </summary>
        public Models.InstanceGroupInfo InstanceGroup
        {
            get { return _instance_group; }
            set { _instance_group = value; RaisePropertyChanged(); }
        }
        private Models.InstanceGroupInfo _instance_group;

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
        /// Instance group page
        /// </summary>
        public InstanceGroupSelectPage InstanceGroupSelectPage
        {
            get
            {
                if (_instance_group_page == null)
                    _instance_group_page = new InstanceGroupSelectPage(this);
                return _instance_group_page;
            }
        }
        private InstanceGroupSelectPage _instance_group_page;

        /// <summary>
        /// Instance selection page
        /// </summary>
        public InstanceSelectPage InstanceSelectPage
        {
            get
            {
                if (InstanceGroup is Models.DistributedInstanceGroupInfo)
                {
                    if (_country_select_page == null)
                        _country_select_page = new CountrySelectPage(this);
                    return _country_select_page;
                }
                else
                {
                    if (_institute_select_page == null)
                        _institute_select_page = new InstituteSelectPage(this);
                    return _institute_select_page;
                }
            }
        }
        private CountrySelectPage _country_select_page;
        private InstituteSelectPage _institute_select_page;

        /// <summary>
        /// Custom instance group page
        /// </summary>
        public CustomInstanceGroupPage CustomInstanceGroupPage
        {
            get
            {
                if (_custom_instance_group_page == null)
                    _custom_instance_group_page = new CustomInstanceGroupPage(this);
                return _custom_instance_group_page;
            }
        }
        private CustomInstanceGroupPage _custom_instance_group_page;

        /// <summary>
        /// Authorization wizard page
        /// </summary>
        public AuthorizationPage AuthorizationPage
        {
            get
            {
                if (_authorization_page == null)
                    _authorization_page = new AuthorizationPage(this);
                return _authorization_page;
            }
        }
        private AuthorizationPage _authorization_page;

        /// <summary>
        /// (Instance and) profile selection wizard page
        /// </summary>
        public ProfileSelectBasePage ProfileSelectPage
        {
            get
            {
                if (InstanceGroup is Models.FederatedInstanceGroupInfo ||
                    InstanceGroup is Models.DistributedInstanceGroupInfo)
                {
                    if (_instance_and_profile_select_page == null)
                        _instance_and_profile_select_page = new InstanceAndProfileSelectPage(this);
                    return _instance_and_profile_select_page;
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
        private InstanceAndProfileSelectPage _instance_and_profile_select_page;

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
            // Save UI thread's dispatcher.
            Dispatcher = Dispatcher.CurrentDispatcher;

            if (Properties.Settings.Default.SettingsVersion == 0)
            {
                // Migrate settings from previous version.
                Properties.Settings.Default.Upgrade();
                Properties.Settings.Default.SettingsVersion = 1;
            }

            Dispatcher.ShutdownStarted += (object sender, EventArgs e) => {
                // Raise the abort flag to gracefully shutdown all background threads.
                Abort.Cancel();

                // Persist settings to disk.
                Properties.Settings.Default.Save();
            };

            Configuration = new Models.VPNConfiguration();

            // Show initializing wizard page.
            CurrentPage = InitializingPage;

            // Spawn instance group loading threads.
            _instance_groups = new Models.InstanceGroupInfo[_instance_directory_id.Length];
            var threads = new Thread[_instance_directory_id.Length];
            for (var i = 0; i < _instance_directory_id.Length; i++)
            {
                // Launch instance group load in the background.
                threads[i] = new Thread(new ParameterizedThreadStart(
                    param =>
                    {
                        var group_index = (int)param;
                        try
                        {
                            // Get instance group.
                            var response_cache = JSON.Response.Get(
                                uri: new Uri((string)Properties.Settings.Default[_instance_directory_id[group_index]]),
                                pub_key: Convert.FromBase64String((string)Properties.Settings.Default[_instance_directory_id[group_index] + "PubKey"]),
                                ct: Abort.Token,
                                previous: (JSON.Response)Properties.Settings.Default[_instance_directory_id[group_index] + "Cache"]);

                            // Parse instance group JSON.
                            var obj = (Dictionary<string, object>)eduJSON.Parser.Parse(
                                response_cache.Value,
                                Abort.Token);

                            // Load instance group.
                            _instance_groups[group_index] = Models.InstanceGroupInfo.FromJSON(obj);

                            if (response_cache.IsFresh)
                            {
                                // If we got here, the loaded instance group is (probably) OK. Update cache.
                                Properties.Settings.Default[_instance_directory_id[group_index] + "Cache"] = response_cache;
                            }
                        }
                        catch (OperationCanceledException) { throw; }
                        catch (Exception ex)
                        {
                            // Make it a clean start next time.
                            Properties.Settings.Default[_instance_directory_id[group_index] + "Cache"] = null;

                            // Notify the sender the instance group loading failed. However, continue with other groups.
                            // This will overwrite all previous error messages.
                            Error = new AggregateException(String.Format(Resources.Strings.ErrorInstanceGroupInfoLoad, _instance_directory_id[group_index]), ex);
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

                        // Of everything is OK, proceed to the "first" page.
                        if (Error == null)
                            CurrentPage = InstanceGroupSelectPage;
                    }
                    finally { Dispatcher.Invoke(DispatcherPriority.Normal, (Action)(() => ChangeTaskCount(-1))); }
                })).Start();
        }

        #endregion

        #region Methods

        /// <summary>
        /// Increments or decrements number of background tasks the wizard is performing
        /// </summary>
        /// <param name="increment">Positive to increment, negative to decrement</param>
        public void ChangeTaskCount(int increment)
        {
            lock (_task_count_lock)
                _task_count += increment;

            RaisePropertyChanged("TaskCount");
            RaisePropertyChanged("IsBusy");
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
