/*
    eduVPN - VPN for education and research

    Copyright: 2017-2022 The Commons Conservancy
    SPDX-License-Identifier: GPL-3.0+
*/

using eduVPN.Models;
using eduVPN.ViewModels.Windows;
using Prism.Commands;
using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Windows.Threading;

namespace eduVPN.ViewModels.Pages
{

    /// <summary>
    /// Server/organization selection wizard page
    /// </summary>
    public class SearchPage : ConnectWizardStandardPage
    {
        #region Fields

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
                        async () =>
                        {
                            await Wizard.AuthorizationPage.TriggerAuthorizationAsync(SelectedInstituteAccessServer);
                            Wizard.HomePage.AddInstituteAccessServer(SelectedInstituteAccessServer);
                            Wizard.ConnectionPage.ConnectingServer = SelectedInstituteAccessServer;
                            Wizard.CurrentPage = Wizard.ConnectionPage;
                            Query = "";
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
                        async () =>
                        {
                            var authenticatingServer = Wizard.GetDiscoveredServer<SecureInternetServer>(SelectedOrganization.SecureInternetBase);
                            authenticatingServer.OrganizationId = SelectedOrganization.Id;
                            await Wizard.AuthorizationPage.TriggerAuthorizationAsync(authenticatingServer);
                            Wizard.HomePage.SetSecureInternetOrganization(SelectedOrganization);
                            Wizard.ConnectionPage.ConnectingServer = authenticatingServer;
                            Wizard.CurrentPage = Wizard.ConnectionPage;
                            Query = "";
                        },
                        () =>
                            SelectedOrganization != null &&
                            Wizard.GetDiscoveredServer<SecureInternetServer>(SelectedOrganization.SecureInternetBase) != null);
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
                        async () =>
                        {
                            await Wizard.AuthorizationPage.TriggerAuthorizationAsync(SelectedOwnServer);
                            Wizard.HomePage.AddOwnServer(SelectedOwnServer);
                            Wizard.ConnectionPage.ConnectingServer = SelectedOwnServer;
                            Wizard.CurrentPage = Wizard.ConnectionPage;
                            Query = "";
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
            Wizard.DiscoveredServersChanged += (object sender, EventArgs e) =>
            {
                _ConfirmOrganizationSelection?.RaiseCanExecuteChanged();
                Search();
            };

            Wizard.DiscoveredOrganizationsChanged += (object sender, EventArgs e) => Search();
        }

        #endregion

        #region Methods

        /// <inheritdoc/>
        public override void OnActivate()
        {
            base.OnActivate();
            Wizard.DiscoverOrganizations();
        }

        /// <summary>
        /// Trigger search
        /// </summary>
        private void Search()
        {
            SearchInProgress?.Cancel();
            SearchInProgress = new CancellationTokenSource();
            var ct = CancellationTokenSource.CreateLinkedTokenSource(SearchInProgress.Token, Window.Abort.Token).Token;
            var keywords = Query.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            new Thread(new ThreadStart(
                () =>
                {
                    Wizard.Dispatcher.Invoke(DispatcherPriority.Normal, (Action)(() => Wizard.TaskCount++));
                    try
                    {
                        var orderedServerHits = Wizard.GetDiscoveredInstituteAccessServers(keywords, ct);
                        ct.ThrowIfCancellationRequested();
                        Wizard.Dispatcher.Invoke(DispatcherPriority.Normal, (Action)(() =>
                        {
                            if (ct.IsCancellationRequested) return;
                            var selected = SelectedInstituteAccessServer?.Base;
                            InstituteAccessServers = orderedServerHits;
                            SelectedInstituteAccessServer = Wizard.GetDiscoveredServer<InstituteAccessServer>(selected);
                        }));
                    }
                    catch (OperationCanceledException) { }
                    catch (Exception ex) { Wizard.Dispatcher.Invoke(DispatcherPriority.Normal, (Action)(() => throw ex)); }
                    finally { Wizard.Dispatcher.Invoke(DispatcherPriority.Normal, (Action)(() => Wizard.TaskCount--)); }
                })).Start();
            new Thread(new ThreadStart(
                () =>
                {
                    Wizard.Dispatcher.Invoke(DispatcherPriority.Normal, (Action)(() => Wizard.TaskCount++));
                    try
                    {
                        var orderedOrganizationHits = Wizard.GetDiscoveredOrganizations(keywords, ct);
                        ct.ThrowIfCancellationRequested();
                        Wizard.Dispatcher.Invoke(DispatcherPriority.Normal, (Action)(() =>
                        {
                            if (ct.IsCancellationRequested) return;
                            var selected = SelectedOrganization?.Id;
                            Organizations = orderedOrganizationHits;
                            SelectedOrganization = Wizard.GetDiscoveredOrganization(selected);
                        }));
                    }
                    catch (OperationCanceledException) { }
                    catch (Exception ex) { Wizard.Dispatcher.Invoke(DispatcherPriority.Normal, (Action)(() => throw ex)); }
                    finally { Wizard.Dispatcher.Invoke(DispatcherPriority.Normal, (Action)(() => Wizard.TaskCount--)); }
                })).Start();

            if (keywords.Length == 1 && keywords[0].Split('.').Length >= 3)
            {
                var ownServers = new ObservableCollection<Server>();
                try
                {
                    var srv = new Server(new UriBuilder("https", keywords[0]).Uri);
                    srv.RequestAuthorization += Wizard.AuthorizationPage.OnRequestAuthorization;
                    srv.ForgetAuthorization += Wizard.AuthorizationPage.OnForgetAuthorization;
                    ownServers.Add(srv);
                }
                catch { }
                var selected = SelectedOwnServer?.Base;
                OwnServers = ownServers;
                SelectedOwnServer = ownServers.FirstOrDefault(srv => selected == srv.Base);
            }
            else
            {
                SelectedOwnServer = null;
                OwnServers = null;
            }
        }

        #endregion
    }
}
