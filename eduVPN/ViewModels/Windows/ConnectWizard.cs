/*
    eduVPN - VPN for education and research

    Copyright: 2017-2022 The Commons Conservancy
    SPDX-License-Identifier: GPL-3.0+
*/

using eduVPN.Models;
using eduVPN.ViewModels.Pages;
using Prism.Commands;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Threading;
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

        #endregion

        #region Properties

        /// <summary>
        /// Navigate to a pop-up page command
        /// </summary>
        public DelegateCommand<ConnectWizardPopupPage> NavigateTo
        {
            get
            {
                if (_NavigateTo == null)
                    _NavigateTo = new DelegateCommand<ConnectWizardPopupPage>(page => CurrentPopupPage = page);
                return _NavigateTo;
            }
        }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private DelegateCommand<ConnectWizardPopupPage> _NavigateTo;

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
        public ConnectWizardPage DisplayPage => (ConnectWizardPage)_CurrentPopupPage ?? _CurrentPage;

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
                    if (_CurrentPopupPage == null)
                        RaisePropertyChanged(nameof(DisplayPage));
                }
            }
        }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private ConnectWizardStandardPage _CurrentPage;

        /// <summary>
        /// The pop-up page the wizard is currently displaying
        /// </summary>
        public ConnectWizardPopupPage CurrentPopupPage
        {
            get => _CurrentPopupPage;
            set
            {
                if (SetProperty(ref _CurrentPopupPage, value))
                {
                    _CurrentPopupPage?.OnActivate();
                    RaisePropertyChanged(nameof(DisplayPage));
                }
            }
        }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private ConnectWizardPopupPage _CurrentPopupPage;

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
                if (Properties.Settings.Default.InstituteAccessServers.Count == 0 &&
                    string.IsNullOrEmpty(Properties.Settings.Default.SecureInternetOrganization) &&
                    Properties.Settings.Default.OwnServers.Count == 0)
                    return AddAnotherPage;

                return HomePage;
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
                InitOrganizations();
            else
            {
                Properties.Settings.Default.SecureInternetConnectingServer = null;
                Properties.Settings.Default.SecureInternetOrganization = null;
            }

            if (Properties.SettingsEx.Default.SelfUpdateDiscovery?.Uri != null)
                actions.Add(new KeyValuePair<Action, int>(
                    SelfUpdatePromptPage.CheckForUpdates,
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
                if (Properties.Settings.Default.CleanupInstituteAccessAndOwnServers)
                {
                    // Migrate non-discovered institute access servers to own servers.
                    // Migrate discovered own servers to institute access servers.
                    var instituteAccessServers = new Xml.UriList();
                    var ownServers = new Xml.UriList();
                    foreach (var baseUri in Properties.Settings.Default.InstituteAccessServers)
                        if (GetDiscoveredServer<InstituteAccessServer>(baseUri) == null)
                        {
                            if (!ownServers.Contains(baseUri))
                                ownServers.Add(baseUri);
                        }
                        else if (!instituteAccessServers.Contains(baseUri))
                            instituteAccessServers.Add(baseUri);
                    foreach (var baseUri in Properties.Settings.Default.OwnServers)
                        if (GetDiscoveredServer<InstituteAccessServer>(baseUri) != null)
                        {
                            if (!instituteAccessServers.Contains(baseUri))
                                instituteAccessServers.Add(baseUri);
                        }
                        else if (!ownServers.Contains(baseUri))
                            ownServers.Add(baseUri);
                    Properties.Settings.Default.InstituteAccessServers = instituteAccessServers;
                    Properties.Settings.Default.OwnServers = ownServers;
                    Properties.Settings.Default.CleanupInstituteAccessAndOwnServers = false;
                }

                DiscoveredServersChanged?.Invoke(this, EventArgs.Empty);
                AutoReconnect();
            }));
        }

        private void InitServers()
        {
            var response = Properties.Settings.Default.ResponseCache.GetSeqFromCache(Properties.SettingsEx.Default.ServersDiscovery);
            if (response != null)
            {
                Trace.TraceInformation("Populating servers from cache");
                UpdateServers((Dictionary<string, object>)eduJSON.Parser.Parse(response.Value, Abort.Token));
            }
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

        private void InitOrganizations()
        {
            var response = Properties.Settings.Default.ResponseCache.GetSeqFromCache(Properties.SettingsEx.Default.OrganizationsDiscovery);
            if (response != null)
            {
                Trace.TraceInformation("Populating organizations from cache");
                UpdateOrganizations((Dictionary<string, object>)eduJSON.Parser.Parse(response.Value, Abort.Token));
            }
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
                var org = GetDiscoveredOrganization(Properties.Settings.Default.SecureInternetOrganization);
                if (org != null)
                {
                    var srv = GetDiscoveredServer<SecureInternetServer>(org.SecureInternetBase);
                    if (srv != null)
                        srv.OrganizationId = Properties.Settings.Default.SecureInternetOrganization;
                    return srv;
                }
            }
            return connectingServer;
        }

        /// <summary>
        /// Auto-reconnects last connected server/profile.
        /// </summary>
        private async void AutoReconnect()
        {
            // Requires DiscoveredServers and DiscoveredOrganizations to be available.
            // Also, don't auto-reconnect if already connected.
            if (DiscoveredServers == null || DiscoveredOrganizations == null ||
                ConnectionPage.ActiveSession != null || Properties.Settings.Default.LastSelectedServer == null)
                return;

            var connectingServer = GetDiscoveredServer<SecureInternetServer>(Properties.Settings.Default.LastSelectedServer);
            if (connectingServer != null)
            {
                var authenticatingServer = HomePage.AuthenticatingSecureInternetServer;
                if (authenticatingServer != null)
                {
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
            }
            else
            {
                var srv = new Server(Properties.Settings.Default.LastSelectedServer);
                srv.RequestAuthorization += AuthorizationPage.OnRequestAuthorization;
                srv.ForgetAuthorization += AuthorizationPage.OnForgetAuthorization;
                if (await AuthorizationPage.TriggerAuthorizationAsync(srv, false) != null)
                {
                    ConnectionPage.ConnectingServer = srv;
                    CurrentPage = ConnectionPage;
                }
                else
                {
                    Properties.Settings.Default.LastSelectedServer = null;
                    AutoReconnectFailed?.Invoke(this, new AutoReconnectFailedEventArgs(srv, srv));
                }
            }
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
