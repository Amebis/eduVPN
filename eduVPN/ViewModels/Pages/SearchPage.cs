/*
    eduVPN - VPN for education and research

    Copyright: 2017-2023 The Commons Conservancy
    SPDX-License-Identifier: GPL-3.0+
*/

using eduVPN.Models;
using eduVPN.ViewModels.Windows;
using Prism.Commands;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace eduVPN.ViewModels.Pages
{
    /// <summary>
    /// Server/organization selection wizard page
    /// </summary>
    public class SearchPage : ConnectWizardStandardPage
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

        /// <summary>
        /// Search cancellation token
        /// </summary>
        private CancellationTokenSource SearchInProgress;

        #endregion

        #region Properties

        /// <summary>
        /// Search query
        /// </summary>
        public string Query
        {
            get => _Query;
            set
            {
                if (SetProperty(ref _Query, value))
                    Search();
            }
        }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private string _Query = "";

        /// <summary>
        /// Institute access server search results
        /// </summary>
        public ObservableCollection<InstituteAccessServer> InstituteAccessServers
        {
            get => _InstituteAccessServers;
            private set => SetProperty(ref _InstituteAccessServers, value);
        }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private ObservableCollection<InstituteAccessServer> _InstituteAccessServers;

        /// <summary>
        /// Selected institute access server
        /// </summary>
        /// <remarks><c>null</c> if none selected.</remarks>
        public InstituteAccessServer SelectedInstituteAccessServer
        {
            get => _SelectedInstituteAccessServer;
            set
            {
                if (SetProperty(ref _SelectedInstituteAccessServer, value))
                    _ConfirmInstituteAccessServerSelection?.RaiseCanExecuteChanged();
            }
        }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private InstituteAccessServer _SelectedInstituteAccessServer;

        /// <summary>
        /// Confirms institute access server selection
        /// </summary>
        public DelegateCommand ConfirmInstituteAccessServerSelection
        {
            get
            {
                if (_ConfirmInstituteAccessServerSelection == null)
                    _ConfirmInstituteAccessServerSelection = new DelegateCommand(
                        () =>
                        {
                            try
                            {
                                Wizard.AddAndConnect(SelectedInstituteAccessServer);
                                Query = "";
                            }
                            catch (OperationCanceledException) { Wizard.CurrentPage = this; }
                        },
                        () => SelectedInstituteAccessServer != null);
                return _ConfirmInstituteAccessServerSelection;
            }
        }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private DelegateCommand _ConfirmInstituteAccessServerSelection;

        /// <summary>
        /// Organization search results
        /// </summary>
        public ObservableCollection<Organization> Organizations
        {
            get => _Organizations;
            private set => SetProperty(ref _Organizations, value);
        }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private ObservableCollection<Organization> _Organizations;

        /// <summary>
        /// Selected organization
        /// </summary>
        /// <remarks><c>null</c> if none selected.</remarks>
        public Organization SelectedOrganization
        {
            get => _SelectedOrganization;
            set
            {
                if (SetProperty(ref _SelectedOrganization, value))
                    _ConfirmOrganizationSelection?.RaiseCanExecuteChanged();
            }
        }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private Organization _SelectedOrganization;

        /// <summary>
        /// Confirms organization selection
        /// </summary>
        public DelegateCommand ConfirmOrganizationSelection
        {
            get
            {
                if (_ConfirmOrganizationSelection == null)
                    _ConfirmOrganizationSelection = new DelegateCommand(
                        () =>
                        {
                            try
                            {
                                Wizard.AddAndConnect(SelectedOrganization);
                                Query = "";
                            }
                            catch (OperationCanceledException) { Wizard.CurrentPage = this; }
                        },
                        () => SelectedOrganization != null);
                return _ConfirmOrganizationSelection;
            }
        }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private DelegateCommand _ConfirmOrganizationSelection;

        /// <summary>
        /// Own server list
        /// </summary>
        public ObservableCollection<Server> OwnServers
        {
            get => _OwnServers;
            private set => SetProperty(ref _OwnServers, value);
        }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private ObservableCollection<Server> _OwnServers;

        /// <summary>
        /// Selected own server
        /// </summary>
        /// <remarks><c>null</c> if none selected.</remarks>
        public Server SelectedOwnServer
        {
            get => _SelectedOwnServer;
            set
            {
                if (SetProperty(ref _SelectedOwnServer, value))
                    _ConfirmOwnServerSelection?.RaiseCanExecuteChanged();
            }
        }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private Server _SelectedOwnServer;

        /// <summary>
        /// Confirms own server selection
        /// </summary>
        public DelegateCommand ConfirmOwnServerSelection
        {
            get
            {
                if (_ConfirmOwnServerSelection == null)
                    _ConfirmOwnServerSelection = new DelegateCommand(
                        () =>
                        {
                            try
                            {
                                Wizard.AddAndConnect(SelectedOwnServer);
                                Query = "";
                            }
                            catch (OperationCanceledException) { Wizard.CurrentPage = this; }
                        },
                        () => SelectedOwnServer != null);
                return _ConfirmOwnServerSelection;
            }
        }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private DelegateCommand _ConfirmOwnServerSelection;

        /// <inheritdoc/>
        public override DelegateCommand NavigateBack
        {
            get
            {
                if (_NavigateBack == null)
                    _NavigateBack = new DelegateCommand(
                        () => Wizard.CurrentPage = Wizard.HomePage,
                        () => Wizard.StartingPage != this);
                return _NavigateBack;
            }
        }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private DelegateCommand _NavigateBack;

        #endregion

        #region Constructors

        /// <summary>
        /// Constructs a wizard page.
        /// </summary>
        /// <param name="wizard">The connecting wizard</param>
        public SearchPage(ConnectWizard wizard) :
            base(wizard)
        {
        }

        #endregion

        #region Methods

        /// <inheritdoc/>
        public override void OnActivate()
        {
            base.OnActivate();
            DiscoverServers();
            DiscoverOrganizations();
        }

        /// <summary>
        /// Trigger search
        /// </summary>
        void Search()
        {
            SearchInProgress?.Cancel();
            SearchInProgress = new CancellationTokenSource();
            var ct = CancellationTokenSource.CreateLinkedTokenSource(SearchInProgress.Token, Window.Abort.Token).Token;
            var keywords = Query.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            new Thread(() =>
            {
                Wizard.TryInvoke((Action)(() => Wizard.TaskCount++));
                try
                {
                    var orderedServerHits = GetDiscoveredInstituteAccessServers(keywords, ct);
                    ct.ThrowIfCancellationRequested();
                    Wizard.TryInvoke((Action)(() =>
                    {
                        if (ct.IsCancellationRequested) return;
                        var selected = SelectedInstituteAccessServer?.Id;
                        InstituteAccessServers = orderedServerHits;
                        SelectedInstituteAccessServer = GetDiscoveredServer<InstituteAccessServer>(selected);
                    }));
                }
                catch (OperationCanceledException) { }
                catch (Exception ex) { Wizard.TryInvoke((Action)(() => Wizard.Error = ex)); }
                finally { Wizard.TryInvoke((Action)(() => Wizard.TaskCount--)); }
            }).Start();
            if (Properties.SettingsEx.Default.SecureInternetOrganization == null)
            {
                new Thread(() =>
                {
                    Wizard.TryInvoke((Action)(() => Wizard.TaskCount++));
                    try
                    {
                        var orderedOrganizationHits = GetDiscoveredOrganizations(keywords, ct);
                        ct.ThrowIfCancellationRequested();
                        Wizard.TryInvoke((Action)(() =>
                        {
                            if (ct.IsCancellationRequested) return;
                            var selected = SelectedOrganization?.Id;
                            Organizations = orderedOrganizationHits;
                            SelectedOrganization = GetDiscoveredOrganization(selected);
                        }));
                    }
                    catch (OperationCanceledException) { }
                    catch (Exception ex) { Wizard.TryInvoke((Action)(() => Wizard.Error = ex)); }
                    finally { Wizard.TryInvoke((Action)(() => Wizard.TaskCount--)); }
                }).Start();
            }

            if (keywords.Length == 1 && keywords[0].Split('.').Length >= 3)
            {
                var ownServers = new ObservableCollection<Server>();
                try
                {
                    var srv = new Server(new UriBuilder("https", keywords[0]).Uri.AbsoluteUri);
                    ownServers.Add(srv);
                }
                catch { }
                var selected = SelectedOwnServer?.Id;
                OwnServers = ownServers;
                SelectedOwnServer = ownServers.FirstOrDefault(srv => selected == srv.Id);
            }
            else
            {
                SelectedOwnServer = null;
                OwnServers = null;
            }
        }

        async void DiscoverServers()
        {
            Wizard.TryInvoke((Action)(() => Wizard.TaskCount++));
            try
            {
                Trace.TraceInformation("Discovering servers");
                ServerDictionary dict;
                using (var cookie = new Engine.CancellationTokenCookie(Window.Abort.Token))
                    dict = new ServerDictionary(
                        eduJSON.Parser.Parse(
                            await Task.Run(() => Engine.DiscoServers(cookie)),
                            Window.Abort.Token) as Dictionary<string, object>);
                //await Task.Run(() => Abort.Token.WaitHandle.WaitOne(10000)); // Mock a slow link for testing.
                //await Task.Run(() => throw new Exception("Server list download failed")); // Mock download failure.
                var idx = new Dictionary<string, HashSet<InstituteAccessServer>>(StringComparer.InvariantCultureIgnoreCase);
                foreach (var el in dict)
                {
                    Window.Abort.Token.ThrowIfCancellationRequested();
                    if (el.Value is InstituteAccessServer srv)
                    {
                        idx.IndexName(srv);
                        idx.IndexKeywords(srv);
                    }
                }
                lock (DiscoveredListLock)
                {
                    DiscoveredServers = dict;
                    DiscoveredInstituteServerIndex = idx;
                }
                Wizard.TryInvoke((Action)(() => Search()));
            }
            catch (Exception ex) { Wizard.TryInvoke((Action)(() => Wizard.Error = ex)); }
            finally { Wizard.TryInvoke((Action)(() => Wizard.TaskCount--)); }
        }

        /// <summary>
        /// Returns available server with given base URI
        /// </summary>
        /// <param name="id">Server identifier</param>
        /// <returns>Server if found; null otherwise</returns>
        T GetDiscoveredServer<T>(string id) where T : Server
        {
            lock (DiscoveredListLock)
                return
                    id != null &&
                    DiscoveredServers != null &&
                    DiscoveredServers.TryGetValue(id, out var srv) &&
                    srv is T srvT ? srvT : null;
        }

        /// <summary>
        /// Returns discovered institute access servers matching keywords ordered by relevance
        /// </summary>
        /// <param name="keywords">List of keywords</param>
        /// <param name="ct">The token to monitor for cancellation requests</param>
        /// <returns>List of servers</returns>
        ObservableCollection<InstituteAccessServer> GetDiscoveredInstituteAccessServers(IEnumerable<string> keywords, CancellationToken ct = default)
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
        /// Triggers background organization discovery
        /// </summary>
        async void DiscoverOrganizations()
        {
            Wizard.TryInvoke((Action)(() => Wizard.TaskCount++));
            try
            {
                Trace.TraceInformation("Discovering organizations");
                OrganizationDictionary dict;
                using (var cookie = new Engine.CancellationTokenCookie(Window.Abort.Token))
                    dict = new OrganizationDictionary(
                        eduJSON.Parser.Parse(
                            await Task.Run(() => Engine.DiscoOrganizations(cookie)),
                            Window.Abort.Token) as Dictionary<string, object>);
                //await Task.Run(() => Abort.Token.WaitHandle.WaitOne(10000)); // Mock a slow link for testing.
                //await Task.Run(() => throw new Exception("Organization list download failed")); // Mock download failure.
                var idx = new Dictionary<string, HashSet<Organization>>(StringComparer.InvariantCultureIgnoreCase);
                foreach (var el in dict)
                {
                    Window.Abort.Token.ThrowIfCancellationRequested();
                    idx.IndexName(el.Value);
                    idx.IndexKeywords(el.Value);
                }
                lock (DiscoveredListLock)
                {
                    DiscoveredOrganizations = dict;
                    DiscoveredOrganizationIndex = idx;
                }
                Wizard.TryInvoke((Action)(() => Search()));
            }
            catch (Exception ex) { Wizard.TryInvoke((Action)(() => Wizard.Error = ex)); }
            finally { Wizard.TryInvoke((Action)(() => Wizard.TaskCount--)); }
        }

        /// <summary>
        /// Returns discovered organization with given identifier
        /// </summary>
        /// <param name="id">Organization identifier</param>
        /// <returns>Organization if found; null otherwise</returns>
        Organization GetDiscoveredOrganization(string id)
        {
            lock (DiscoveredListLock)
                return
                    !string.IsNullOrEmpty(id) &&
                    DiscoveredOrganizations != null &&
                    DiscoveredOrganizations.TryGetValue(id, out var org) ? org : null;
        }

        /// <summary>
        /// Returns discovered organizations matching keywords ordered by relevance
        /// </summary>
        /// <param name="ct">The token to monitor for cancellation requests</param>
        /// <returns>List of organizations</returns>
        ObservableCollection<Organization> GetDiscoveredOrganizations(IEnumerable<string> keywords, CancellationToken ct = default)
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

        #endregion
    }
}
