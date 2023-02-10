/*
    eduVPN - VPN for education and research

    Copyright: 2017-2023 The Commons Conservancy
    SPDX-License-Identifier: GPL-3.0+
*/

using eduEx.System;
using eduVPN.Models;
using eduVPN.ViewModels.Pages;
using Microsoft.Win32;
using Prism.Commands;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
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
        /// Stack of displayed popup pages
        /// </summary>
        private List<ConnectWizardPopupPage> PopupPages = new List<ConnectWizardPopupPage>();

        /// <summary>
        /// Dictionary of discovered servers
        /// </summary>
        private ServerDictionary DiscoveredServers;

        /// <summary>
        /// Dictionary of discovered organizations
        /// </summary>
        private OrganizationDictionary DiscoveredOrganizations;

        /// <summary>
        /// Search index of discovered institute access servers
        /// </summary>
        private Dictionary<string, HashSet<InstituteAccessServer>> DiscoveredInstituteServerIndex;

        /// <summary>
        /// Search index of discovered organizations
        /// </summary>
        private Dictionary<string, HashSet<Organization>> DiscoveredOrganizationIndex;

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly object DiscoveredListLock = new object();

        /// <summary>
        /// Is auto-reconnect in progress?
        /// </summary>
        private bool IsAutoReconnectInProgress = false;

        #endregion

        #region Properties

        /// <summary>
        /// Copyright notice
        /// </summary>
        public string Copyright => (Attribute.GetCustomAttribute(Assembly.GetExecutingAssembly(), typeof(AssemblyCopyrightAttribute)) as AssemblyCopyrightAttribute)?.Copyright;

        /// <summary>
        /// Executing assembly version
        /// </summary>
        public Version Version
        {
            get
            {
                var ver = Assembly.GetExecutingAssembly()?.GetName()?.Version;
                return
                    ver.Revision != 0 ? new Version(ver.Major, ver.Minor, ver.Build, ver.Revision) :
                    ver.Build != 0 ? new Version(ver.Major, ver.Minor, ver.Build) :
                        new Version(ver.Major, ver.Minor);
            }
        }

        /// <summary>
        /// Build timestamp
        /// </summary>
        public DateTimeOffset Build =>
                // The Builtin class is implemented in Builtin target in Default.targets.
                new DateTimeOffset(Builtin.CompileTime, TimeSpan.Zero);

        /// <summary>
        /// Installed product version
        /// </summary>
        public Version InstalledVersion
        {
            get => _InstalledVersion;
            private set
            {
                if (SetProperty(ref _InstalledVersion, value))
                    _StartUpdate?.RaiseCanExecuteChanged();
            }
        }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private Version _InstalledVersion;

        /// <summary>
        /// Available product version
        /// </summary>
        public Version AvailableVersion
        {
            get => _AvailableVersion;
            private set
            {
                if (SetProperty(ref _AvailableVersion, value))
                    _StartUpdate?.RaiseCanExecuteChanged();
            }
        }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private Version _AvailableVersion;

        /// <summary>
        /// Product changelog
        /// </summary>
        public Uri Changelog
        {
            get => _Changelog;
            private set
            {
                if (SetProperty(ref _Changelog, value))
                    _ShowChangelog?.RaiseCanExecuteChanged();
            }
        }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private Uri _Changelog;

        /// <summary>
        /// Show changelog command
        /// </summary>
        public DelegateCommand ShowChangelog
        {
            get
            {
                if (_ShowChangelog == null)
                    _ShowChangelog = new DelegateCommand(
                        () => Process.Start(Changelog.ToString()),
                        () => Changelog != null);
                return _ShowChangelog;
            }
        }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private DelegateCommand _ShowChangelog;

        /// <summary>
        /// Pass control to the self-update page
        /// </summary>
        public DelegateCommand StartUpdate
        {
            get
            {
                if (_StartUpdate == null)
                    _StartUpdate = new DelegateCommand(
                        () =>
                        {
                            if (NavigateTo.CanExecute(SelfUpdateProgressPage))
                                NavigateTo.Execute(SelfUpdateProgressPage);
                        },
                        () => AvailableVersion != null && InstalledVersion != null && AvailableVersion > InstalledVersion);
                return _StartUpdate;
            }
        }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private DelegateCommand _StartUpdate;

        /// <summary>
        /// Navigate to a pop-up page command
        /// </summary>
        public DelegateCommand<ConnectWizardPopupPage> NavigateTo
        {
            get
            {
                if (_NavigateTo == null)
                    _NavigateTo = new DelegateCommand<ConnectWizardPopupPage>(
                        page =>
                        {
                            var displayPagePrev = DisplayPage;
                            var removed = PopupPages.Remove(page);
                            PopupPages.Add(page);
                            if (!removed) page.OnActivate();
                            if (displayPagePrev != DisplayPage)
                                RaisePropertyChanged(nameof(DisplayPage));
                        });
                return _NavigateTo;
            }
        }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private DelegateCommand<ConnectWizardPopupPage> _NavigateTo;

        /// <summary>
        /// Navigate back from a pop-up page command
        /// </summary>
        public DelegateCommand<ConnectWizardPopupPage> NavigateBack
        {
            get
            {
                if (_NavigateBack == null)
                    _NavigateBack = new DelegateCommand<ConnectWizardPopupPage>(
                        page =>
                        {
                            var displayPagePrev = DisplayPage;
                            PopupPages.Remove(page);
                            if (displayPagePrev != DisplayPage)
                                RaisePropertyChanged(nameof(DisplayPage));
                        });
                return _NavigateBack;
            }
        }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private DelegateCommand<ConnectWizardPopupPage> _NavigateBack;

        /// <summary>
        /// Occurs when discovered server list is refreshed.
        /// </summary>
        /// <remarks>Sender is the connection wizard <see cref="ConnectWizard"/>.</remarks>
        public event EventHandler DiscoveredServersChanged;

        /// <summary>
        /// Occurs when discovered organization list is refreshed.
        /// </summary>
        /// <remarks>Sender is the connection wizard <see cref="ConnectWizard"/>.</remarks>
        public event EventHandler DiscoveredOrganizationsChanged;

        /// <summary>
        /// Occurs when auto-reconnection failed.
        /// </summary>
        /// <remarks>Sender is the connection wizard <see cref="ConnectWizard"/>.</remarks>
        public event EventHandler<AutoReconnectFailedEventArgs> AutoReconnectFailed;

        /// <summary>
        /// Occurs when application should quit.
        /// </summary>
        /// <remarks>Sender is the connection wizard <see cref="ConnectWizard"/>.</remarks>
        public event EventHandler QuitApplication;

        #region Pages

        /// <summary>
        /// The page the wizard is currently displaying
        /// </summary>
        public ConnectWizardPage DisplayPage => PopupPages.Count > 0 ? (ConnectWizardPage)PopupPages.Last() : _CurrentPage;

        /// <summary>
        /// The page the wizard should be displaying (if no pop-up page)
        /// </summary>
        public ConnectWizardStandardPage CurrentPage
        {
            get => _CurrentPage;
            set
            {
                if (SetProperty(ref _CurrentPage, value))
                {
                    _CurrentPage.OnActivate();
                    if (PopupPages.Count <= 0)
                        RaisePropertyChanged(nameof(DisplayPage));
                }
            }
        }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private ConnectWizardStandardPage _CurrentPage;

        /// <summary>
        /// Page to add another server
        /// </summary>
        public ConnectWizardStandardPage AddAnotherPage => Properties.SettingsEx.Default.ServersDiscovery?.Uri != null ? (ConnectWizardStandardPage)SearchPage : SelectOwnServerPage;

        /// <summary>
        /// The first page of the wizard
        /// </summary>
        public ConnectWizardStandardPage StartingPage
        {
            get
            {
                var precfgList = Properties.SettingsEx.Default.InstituteAccessServers;
                if (precfgList != null && precfgList.Count != 0)
                    return HomePage;

                var precfgOrgId = Properties.SettingsEx.Default.SecureInternetOrganization;
                if (!string.IsNullOrEmpty(precfgOrgId))
                    return HomePage;

                if (Properties.Settings.Default.InstituteAccessServers.Count != 0 ||
                    precfgOrgId == null && !string.IsNullOrEmpty(Properties.Settings.Default.SecureInternetOrganization) ||
                    Properties.Settings.Default.OwnServers.Count != 0)
                    return HomePage;

                return AddAnotherPage;
            }
        }

        /// <summary>
        /// Authorization wizard page
        /// </summary>
        public AuthorizationPage AuthorizationPage
        {
            get
            {
                if (_AuthorizationPage == null)
                    _AuthorizationPage = new AuthorizationPage(this);
                return _AuthorizationPage;
            }
        }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private AuthorizationPage _AuthorizationPage;

        /// <summary>
        /// Search wizard page
        /// </summary>
        public SearchPage SearchPage
        {
            get
            {
                if (_SearchPage == null)
                    _SearchPage = new SearchPage(this);
                return _SearchPage;
            }
        }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private SearchPage _SearchPage;

        /// <summary>
        /// Select own server page
        /// </summary>
        public SelectOwnServerPage SelectOwnServerPage
        {
            get
            {
                if (_SelectOwnServerPage == null)
                    _SelectOwnServerPage = new SelectOwnServerPage(this);
                return _SelectOwnServerPage;
            }
        }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private SelectOwnServerPage _SelectOwnServerPage;

        /// <summary>
        /// Home wizard page
        /// </summary>
        public HomePage HomePage
        {
            get
            {
                if (_HomePage == null)
                    _HomePage = new HomePage(this);
                return _HomePage;
            }
        }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private HomePage _HomePage;

        /// <summary>
        /// Select secure internet connecting server wizard page
        /// </summary>
        public SelectSecureInternetServerPage SelectSecureInternetServerPage
        {
            get
            {
                if (_SelectSecureInternetServerPage == null)
                    _SelectSecureInternetServerPage = new SelectSecureInternetServerPage(this);
                return _SelectSecureInternetServerPage;
            }
        }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private SelectSecureInternetServerPage _SelectSecureInternetServerPage;

        /// <summary>
        /// Status wizard page
        /// </summary>
        public ConnectionPage ConnectionPage
        {
            get
            {
                if (_ConnectionPage == null)
                    _ConnectionPage = new ConnectionPage(this);
                return _ConnectionPage;
            }
        }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private ConnectionPage _ConnectionPage;

        /// <summary>
        /// Settings wizard page
        /// </summary>
        public SettingsPage SettingsPage
        {
            get
            {
                if (_SettingsPage == null)
                    _SettingsPage = new SettingsPage(this);
                return _SettingsPage;
            }
        }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private SettingsPage _SettingsPage;

        /// <summary>
        /// About wizard page
        /// </summary>
        public AboutPage AboutPage
        {
            get
            {
                if (_AboutPage == null)
                    _AboutPage = new AboutPage(this);
                return _AboutPage;
            }
        }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private AboutPage _AboutPage;

        /// <summary>
        /// Self-update prompt wizard page
        /// </summary>
        public SelfUpdatePromptPage SelfUpdatePromptPage
        {
            get
            {
                if (_SelfUpdatePromptPage == null)
                    _SelfUpdatePromptPage = new SelfUpdatePromptPage(this);
                return _SelfUpdatePromptPage;
            }
        }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private SelfUpdatePromptPage _SelfUpdatePromptPage;

        /// <summary>
        /// Self-update progress wizard page
        /// </summary>
        public SelfUpdateProgressPage SelfUpdateProgressPage
        {
            get
            {
                if (_SelfUpdateProgressPage == null)
                    _SelfUpdateProgressPage = new SelfUpdateProgressPage(this);
                return _SelfUpdateProgressPage;
            }
        }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private SelfUpdateProgressPage _SelfUpdateProgressPage;

        #endregion

        #endregion

        #region Constructors

        /// <summary>
        /// Constructs the wizard
        /// </summary>
        public ConnectWizard()
        {
            var actions = new List<KeyValuePair<Action, int>>();

            actions.Add(new KeyValuePair<Action, int>(
                () =>
                {
                    try { Properties.Settings.Default.ResponseCache.PurgeOldCacheEntries(); } catch { }
                    try { VPN.OpenVPNSession.PurgeOldLogs(); } catch { }
                    try { VPN.WireGuardSession.PurgeOldLogs(); } catch { }
                },
                24 * 60 * 60 * 1000)); // Repeat every 24 hours

            if (Properties.SettingsEx.Default.ServersDiscovery?.Uri != null)
            {
                InitServers();
                actions.Add(new KeyValuePair<Action, int>(
                    DiscoverServers,
                    6 * 60 * 60 * 1000)); // Repeat every 6 hours
            }
            else
            {
                Properties.Settings.Default.InstituteAccessServers.Clear();
                Properties.Settings.Default.SecureInternetConnectingServer = null;
                Properties.Settings.Default.SecureInternetOrganization = null;
            }

            if (Properties.SettingsEx.Default.OrganizationsDiscovery?.Uri != null)
            {
                if (!InitOrganizations())
                    actions.Add(new KeyValuePair<Action, int>(DiscoverOrganizations, 0));
            }
            else
            {
                Properties.Settings.Default.SecureInternetConnectingServer = null;
                Properties.Settings.Default.SecureInternetOrganization = null;
            }

            if (Properties.SettingsEx.Default.SelfUpdateDiscovery?.Uri != null)
                actions.Add(new KeyValuePair<Action, int>(
                    DiscoverVersions,
                    24 * 60 * 60 * 1000)); // Repeat every 24 hours

            // Show Starting wizard page.
            CurrentPage = StartingPage;

            foreach (var action in actions)
            {
                var w = new BackgroundWorker();
                w.DoWork += (object sender, DoWorkEventArgs e) =>
                {
                    var random = new Random();
                    do
                    {
                        TryInvoke((Action)(() => TaskCount++));
                        try { action.Key(); }
                        catch (OperationCanceledException) { }
                        catch (Exception ex) { TryInvoke((Action)(() => throw ex)); }
                        finally { TryInvoke((Action)(() => TaskCount--)); }
                    }
                    // Sleep for given time±10%, then retry.
                    while (action.Value != 0 && !Abort.Token.WaitHandle.WaitOne(random.Next(action.Value * 9 / 10, action.Value * 11 / 10)));
                };
                w.RunWorkerCompleted += (object sender, RunWorkerCompletedEventArgs e) => (sender as BackgroundWorker)?.Dispose();
                w.RunWorkerAsync();
            }

            //Abort.Token.WaitHandle.WaitOne(5000); // Mock slow wizard initialization
        }

        #endregion

        #region Methods

        private void UpdateServers(Dictionary<string, object> obj)
        {
            var dict = new ServerDictionary();
            dict.Load(obj);
            //Abort.Token.WaitHandle.WaitOne(10000); // Mock a slow link for testing.
            //throw new Exception("Server list download failed"); // Mock download failure.
            var idx = new Dictionary<string, HashSet<InstituteAccessServer>>(StringComparer.InvariantCultureIgnoreCase);
            foreach (var el in dict)
            {
                Abort.Token.ThrowIfCancellationRequested();
                el.Value.RequestAuthorization += AuthorizationPage.OnRequestAuthorization;
                el.Value.ForgetAuthorization += AuthorizationPage.OnForgetAuthorization;
                if (el.Value is InstituteAccessServer srv)
                    idx.Index(srv);
            }
            lock (DiscoveredListLock)
            {
                DiscoveredServers = dict;
                DiscoveredInstituteServerIndex = idx;
            }
            TryInvoke((Action)(() =>
            {
                var instituteAccessServers = new Xml.UriList();
                var ownServers = new Xml.UriList();
                var precfgList = Properties.SettingsEx.Default.InstituteAccessServers;
                if (precfgList != null)
                {
                    foreach (var baseUri in precfgList)
                        if (!instituteAccessServers.Contains(baseUri))
                            instituteAccessServers.Add(baseUri);
                }
                // Migrate non-discovered institute access servers to own servers.
                foreach (var baseUri in Properties.Settings.Default.InstituteAccessServers)
                    if (!instituteAccessServers.Contains(baseUri))
                    {
                        if (GetDiscoveredServer<InstituteAccessServer>(baseUri) == null)
                        {
                            if (!ownServers.Contains(baseUri))
                                ownServers.Add(baseUri);
                        }
                        else
                            instituteAccessServers.Add(baseUri);
                    }
                // Migrate discovered own servers to institute access servers.
                foreach (var baseUri in Properties.Settings.Default.OwnServers)
                    if (!instituteAccessServers.Contains(baseUri))
                    {
                        if (GetDiscoveredServer<InstituteAccessServer>(baseUri) != null)
                            instituteAccessServers.Add(baseUri);
                        else if (!ownServers.Contains(baseUri))
                            ownServers.Add(baseUri);
                    }
                Properties.Settings.Default.InstituteAccessServers = instituteAccessServers;
                Properties.Settings.Default.OwnServers = ownServers;

                DiscoveredServersChanged?.Invoke(this, EventArgs.Empty);
                AutoReconnect();
            }));
        }

        private bool InitServers()
        {
            var response = Properties.Settings.Default.ResponseCache.GetSeqFromCache(Properties.SettingsEx.Default.ServersDiscovery);
            if (response != null)
            {
                Trace.TraceInformation("Populating servers from cache");
                UpdateServers((Dictionary<string, object>)eduJSON.Parser.Parse(response.Value, Abort.Token));
                return true;
            }
            return false;
        }

        private void DiscoverServers()
        {
            Trace.TraceInformation("Updating servers {0}", Properties.SettingsEx.Default.ServersDiscovery.Uri.AbsoluteUri);
            UpdateServers(Properties.Settings.Default.ResponseCache.GetSeq(
                Properties.SettingsEx.Default.ServersDiscovery,
                Abort.Token));
        }

        /// <summary>
        /// Returns available institute access server with given base URI
        /// </summary>
        /// <param name="baseUri">Server base URI</param>
        /// <returns>Server if found; null otherwise</returns>
        public T GetDiscoveredServer<T>(Uri baseUri) where T : Server
        {
            lock (DiscoveredListLock)
                return
                    baseUri != null &&
                    DiscoveredServers != null &&
                    DiscoveredServers.TryGetValue(baseUri, out var srv) &&
                    srv is T srvT ? srvT : null;
        }

        private void UpdateOrganizations(Dictionary<string, object> obj)
        {
            var dict = new OrganizationDictionary();
            dict.Load(obj);
            //Abort.Token.WaitHandle.WaitOne(10000); // Mock a slow link for testing.
            //throw new Exception("Organization list download failed"); // Mock download failure.
            var idx = new Dictionary<string, HashSet<Organization>>(StringComparer.InvariantCultureIgnoreCase);
            foreach (var el in dict)
            {
                Abort.Token.ThrowIfCancellationRequested();
                idx.Index(el.Value);
            }
            lock (DiscoveredListLock)
            {
                DiscoveredOrganizations = dict;
                DiscoveredOrganizationIndex = idx;
            }
            TryInvoke((Action)(() =>
            {
                DiscoveredOrganizationsChanged?.Invoke(this, EventArgs.Empty);
                AutoReconnect();
            }));
        }

        private bool InitOrganizations()
        {
            var response = Properties.Settings.Default.ResponseCache.GetSeqFromCache(Properties.SettingsEx.Default.OrganizationsDiscovery);
            if (response != null)
            {
                Trace.TraceInformation("Populating organizations from cache");
                UpdateOrganizations((Dictionary<string, object>)eduJSON.Parser.Parse(response.Value, Abort.Token));
                return true;
            }
            return false;
        }

        /// <summary>
        /// Triggers background organization discovery
        /// </summary>
        public void DiscoverOrganizations()
        {
            if (Properties.SettingsEx.Default.OrganizationsDiscovery?.Uri == null)
                return;

            var w = new BackgroundWorker();
            w.DoWork += (object sender, DoWorkEventArgs e) =>
            {
                TryInvoke((Action)(() => TaskCount++));
                try
                {
                    Trace.TraceInformation("Updating organizations {0}", Properties.SettingsEx.Default.OrganizationsDiscovery.Uri.AbsoluteUri);
                    UpdateOrganizations(Properties.Settings.Default.ResponseCache.GetSeq(
                        Properties.SettingsEx.Default.OrganizationsDiscovery,
                        Abort.Token));
                }
                catch (OperationCanceledException) { }
                catch (Exception ex) { TryInvoke((Action)(() => throw ex)); }
                finally { TryInvoke((Action)(() => TaskCount--)); }
            };
            w.RunWorkerCompleted += (object sender, RunWorkerCompletedEventArgs e) => (sender as BackgroundWorker)?.Dispose();
            w.RunWorkerAsync();
        }

        /// <summary>
        /// Returns discovered organization with given identifier
        /// </summary>
        /// <param name="id">Organization identifier</param>
        /// <returns>Organization if found; null otherwise</returns>
        public Organization GetDiscoveredOrganization(string id)
        {
            lock (DiscoveredListLock)
                return
                    !string.IsNullOrEmpty(id) &&
                    DiscoveredOrganizations != null &&
                    DiscoveredOrganizations.TryGetValue(id, out var org) ? org : null;
        }

        /// <summary>
        /// Returns discovered institute access servers matching keywords ordered by relevance
        /// </summary>
        /// <param name="keywords">List of keywords</param>
        /// <param name="ct">The token to monitor for cancellation requests</param>
        /// <returns>List of servers</returns>
        public ObservableCollection<InstituteAccessServer> GetDiscoveredInstituteAccessServers(IEnumerable<string> keywords, CancellationToken ct = default)
        {
            lock (DiscoveredListLock)
            {
                if (DiscoveredInstituteServerIndex == null)
                    return null;

                var serverHits = new Dictionary<InstituteAccessServer, int>();
                foreach (var keyword in keywords)
                {
                    ct.ThrowIfCancellationRequested();
                    if (DiscoveredInstituteServerIndex.TryGetValue(keyword, out var hits))
                        foreach (var hit in hits)
                        {
                            ct.ThrowIfCancellationRequested();
                            if (serverHits.ContainsKey(hit))
                                serverHits[hit]++;
                            else
                                serverHits.Add(hit, 1);
                        }
                }
                var coll = new ObservableCollection<InstituteAccessServer>();
                foreach (var srv in serverHits.OrderByDescending(el =>
                    {
                        ct.ThrowIfCancellationRequested();
                        return el.Value;
                    }))
                {
                    ct.ThrowIfCancellationRequested();
                    coll.Add(srv.Key);
                }
                return coll;
            }
        }

        /// <summary>
        /// Returns discovered secure internet servers
        /// </summary>
        /// <param name="ct">The token to monitor for cancellation requests</param>
        /// <returns>List of servers</returns>
        public ObservableCollection<SecureInternetServer> GetDiscoveredSecureInternetServers(CancellationToken ct = default)
        {
            lock (DiscoveredListLock)
            {
                if (DiscoveredServers == null)
                    return null;

                var serverHits = new ObservableCollection<SecureInternetServer>();
                foreach (var entry in DiscoveredServers)
                {
                    ct.ThrowIfCancellationRequested();
                    if (entry.Value is SecureInternetServer srv2)
                        serverHits.Add(srv2);
                }
                return serverHits;
            }
        }

        /// <summary>
        /// Returns discovered organizations matching keywords ordered by relevance
        /// </summary>
        /// <param name="ct">The token to monitor for cancellation requests</param>
        /// <returns>List of organizations</returns>
        public ObservableCollection<Organization> GetDiscoveredOrganizations(IEnumerable<string> keywords, CancellationToken ct = default)
        {
            lock (DiscoveredListLock)
            {
                if (DiscoveredOrganizationIndex == null)
                    return null;

                var organizationHits = new Dictionary<Organization, int>();
                foreach (var keyword in keywords)
                {
                    ct.ThrowIfCancellationRequested();
                    if (DiscoveredOrganizationIndex.TryGetValue(keyword, out var hits))
                        foreach (var hit in hits)
                        {
                            ct.ThrowIfCancellationRequested();
                            if (organizationHits.ContainsKey(hit))
                                organizationHits[hit]++;
                            else
                                organizationHits.Add(hit, 1);
                        }
                }
                var coll = new ObservableCollection<Organization>();
                foreach (var org in organizationHits.OrderByDescending(el =>
                    {
                        ct.ThrowIfCancellationRequested();
                        return el.Value;
                    }))
                {
                    ct.ThrowIfCancellationRequested();
                    coll.Add(org.Key);
                }
                return coll;
            }
        }

        /// <summary>
        /// Returns authenticating server for the given connecting server
        /// </summary>
        /// <param name="connectingServer">Connecting server</param>
        /// <returns>Authenticating server</returns>
        public Server GetAuthenticatingServer(Server connectingServer)
        {
            if (connectingServer is SecureInternetServer)
            {
                var orgId = Properties.SettingsEx.Default.SecureInternetOrganization;
                if (orgId != "")
                {
                    if (orgId == null)
                        orgId = Properties.Settings.Default.SecureInternetOrganization;
                    var org = GetDiscoveredOrganization(orgId);
                    if (org != null)
                    {
                        var srv = GetDiscoveredServer<SecureInternetServer>(org.SecureInternetBase);
                        if (srv != null)
                            srv.OrganizationId = orgId;
                        return srv;
                    }
                }
            }
            return connectingServer;
        }

        /// <summary>
        /// Auto-reconnects last connected server/profile.
        /// </summary>
        private async void AutoReconnect()
        {
            if (Properties.Settings.Default.LastSelectedServer == null ||
                IsAutoReconnectInProgress)
                return;

            // Select connecting and authenticating server. Or make one.
            Server connectingServer = null, authenticatingServer = null;
            if (DiscoveredServers != null)
            {
                connectingServer = GetDiscoveredServer<SecureInternetServer>(Properties.Settings.Default.LastSelectedServer);
                if (connectingServer != null)
                {
                    if (DiscoveredOrganizations == null)
                        return;
                    authenticatingServer = HomePage.AuthenticatingSecureInternetServer;
                    if (authenticatingServer == null)
                        return;
                }
                else
                    connectingServer = authenticatingServer = GetDiscoveredServer<InstituteAccessServer>(Properties.Settings.Default.LastSelectedServer);
            }
            if (connectingServer == null)
            {
                connectingServer = authenticatingServer = new Server(Properties.Settings.Default.LastSelectedServer);
                connectingServer.RequestAuthorization += AuthorizationPage.OnRequestAuthorization;
                connectingServer.ForgetAuthorization += AuthorizationPage.OnForgetAuthorization;
            }

            // Authorize and start connecting.
            IsAutoReconnectInProgress = true;
            if (await AuthorizationPage.TriggerAuthorizationAsync(authenticatingServer, false) != null)
            {
                ConnectionPage.ConnectingServer = connectingServer;
                CurrentPage = ConnectionPage;
            }
            else
            {
                Properties.Settings.Default.LastSelectedServer = null;
                AutoReconnectFailed?.Invoke(this, new AutoReconnectFailedEventArgs(authenticatingServer, connectingServer));
            }
        }

        private void DiscoverVersions()
        {
            try
            {
                Parallel.ForEach(new List<Action>()
                    {
                        () =>
                        {
                            // Get self-update.
                            var res = Properties.SettingsEx.Default.SelfUpdateDiscovery;
                            Trace.TraceInformation("Downloading self-update JSON discovery {0}", res.Uri.AbsoluteUri);
                            var obj = Properties.Settings.Default.ResponseCache.GetSeq(res, Abort.Token);

                            var repoVersion = new Version((string)obj["version"]);
                            Trace.TraceInformation("Online version: {0}", repoVersion.ToString());
                            TryInvoke((Action)(() => {
                                AvailableVersion = repoVersion;
                                Changelog = eduJSON.Parser.GetValue(obj, "changelog_uri", out string changelogUri) ? new Uri(changelogUri) : null;
                                SelfUpdateProgressPage.DownloadUris = new List<Uri>(((List<object>)obj["uri"]).Select(uri => new Uri(res.Uri, (string)uri)));
                                SelfUpdateProgressPage.Hash = ((string)obj["hash-sha256"]).FromHexToBin();
                                SelfUpdateProgressPage.Arguments = eduJSON.Parser.GetValue(obj, "arguments", out string installerArguments) ? installerArguments : null;
                            }));
                        },

                        () =>
                        {
                            // Evaluate installed products.
                            Version productVersion = null;
                            var productId = Properties.Settings.Default.SelfUpdateBundleId.ToUpperInvariant();
                            Trace.TraceInformation("Evaluating installed products");
                            using (var hklmKey = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry32))
                            using (var uninstallKey = hklmKey.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Uninstall", false))
                            {
                                foreach (var productKeyName in uninstallKey.GetSubKeyNames())
                                {
                                    Abort.Token.ThrowIfCancellationRequested();
                                    using (var productKey = uninstallKey.OpenSubKey(productKeyName))
                                    {
                                        var bundleUpgradeCode = productKey.GetValue("BundleUpgradeCode");
                                        if ((bundleUpgradeCode is string   bundleUpgradeCodeString && bundleUpgradeCodeString.ToUpperInvariant() == productId ||
                                                bundleUpgradeCode is string[] bundleUpgradeCodeArray  && bundleUpgradeCodeArray.FirstOrDefault(code => code.ToUpperInvariant() == productId) != null) &&
                                            productKey.GetValue("BundleVersion") is string bundleVersionString)
                                        {
                                            // Our product entry found.
                                            productVersion = new Version(productKey.GetValue("DisplayVersion") is string displayVersionString ? displayVersionString : bundleVersionString);
                                            Trace.TraceInformation("Installed version: {0}", productVersion.ToString());
                                            break;
                                        }
                                    }
                                }
                            }
                            TryInvoke((Action)(() => { InstalledVersion = productVersion; }));
                        },
                    },
                action =>
                {
                    TryInvoke((Action)(() => TaskCount++));
                    try { action(); }
                    finally { TryInvoke((Action)(() => TaskCount--)); }
                });
            }
            catch (AggregateException ex)
            {
                var nonCancelledException = ex.InnerExceptions.Where(innerException => !(innerException is OperationCanceledException));
                if (nonCancelledException.Any())
                    throw new AggregateException(Resources.Strings.ErrorSelfUpdateDetection, nonCancelledException.ToArray());
                throw new OperationCanceledException();
            }

            //// Mock the values for testing.
            //InstalledVersion = new Version(1, 0);
            //Properties.Settings.Default.SelfUpdateLastReminder = DateTimeOffset.MinValue;

            try
            {
                if (new Version(Properties.Settings.Default.SelfUpdateLastVersion) == AvailableVersion &&
                    (Properties.Settings.Default.SelfUpdateLastReminder == DateTimeOffset.MaxValue ||
                    (DateTimeOffset.Now - Properties.Settings.Default.SelfUpdateLastReminder).TotalDays < 3))
                {
                    // We already prompted user for this version.
                    // Either user opted not to be reminded of this version update again,
                    // or it has been less than three days since the last prompt.
                    Trace.TraceInformation("Update deferred by user choice");
                    return;
                }
            }
            catch { }

            if (InstalledVersion == null)
            {
                // Nothing to update.
                Trace.TraceInformation("Product not installed or version could not be determined");
                return; // Quit self-updating.
            }

            if (AvailableVersion <= InstalledVersion)
            {
                // Product already up-to-date.
                Trace.TraceInformation("Update not required");
                return;
            }

            // We're in the background thread - raise the prompt event via dispatcher.
            TryInvoke((Action)(() =>
            {
                if (NavigateTo.CanExecute(SelfUpdatePromptPage))
                {
                    Properties.Settings.Default.SelfUpdateLastVersion = AvailableVersion.ToString();
                    Properties.Settings.Default.SelfUpdateLastReminder = DateTimeOffset.Now;
                    NavigateTo.Execute(SelfUpdatePromptPage);
                }
            }));
        }

        /// <summary>
        /// Ask view to quit.
        /// </summary>
        /// <param name="sender">Event sender</param>
        public void OnQuitApplication(object sender)
        {
            Trace.TraceInformation("Quitting client");
            QuitApplication?.Invoke(sender, EventArgs.Empty);
        }

        /// <summary>
        /// Invoke method on GUI thread if it's not terminating.
        /// </summary>
        /// <param name="method">Method to execute</param>
        /// <returns>The return value from the delegate being invoked or <c>null</c> if the delegate has no return value or dispatcher is shutting down.</returns>
        public object TryInvoke(Delegate method)
        {
            if (Dispatcher.HasShutdownStarted)
                return null;
            return Dispatcher.Invoke(DispatcherPriority.Normal, method);
        }

        #endregion
    }
}
