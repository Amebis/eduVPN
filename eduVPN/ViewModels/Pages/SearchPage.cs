/*
    eduVPN - VPN for education and research

    Copyright: 2017-2020 The Commons Conservancy eduVPN Programme
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

        /// <summary>
        /// Search thread
        /// </summary>
        private Thread SearchThread;

        #endregion

        #region Properties

        /// <summary>
        /// Search query
        /// </summary>
        public string Query
        {
            get { return _Query; }
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
            get { return _InstituteAccessServers; }
            private set { SetProperty(ref _InstituteAccessServers, value); }
        }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private ObservableCollection<InstituteAccessServer> _InstituteAccessServers;

        /// <summary>
        /// Selected institute access server
        /// </summary>
        /// <remarks><c>null</c> if none selected.</remarks>
        public InstituteAccessServer SelectedInstituteAccessServer
        {
            get { return _SelectedInstituteAccessServer; }
            set
            {
                if (SetProperty(ref _SelectedInstituteAccessServer, value))
                    ConfirmInstituteAccessServerSelection.RaiseCanExecuteChanged();
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
                            try
                            {
                                await Wizard.AuthorizationPage.TriggerAuthorizationAsync(SelectedInstituteAccessServer);
                                Wizard.HomePage.AddInstituteAccessServer(SelectedInstituteAccessServer);
                                Wizard.CurrentPage = Wizard.HomePage;
                                Query = "";
                            }
                            catch (OperationCanceledException) { }
                            catch (Exception ex) { Wizard.Error = ex; }
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
            get { return _Organizations; }
            private set { SetProperty(ref _Organizations, value); }
        }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private ObservableCollection<Organization> _Organizations;

        /// <summary>
        /// Selected organization
        /// </summary>
        /// <remarks><c>null</c> if none selected.</remarks>
        public Organization SelectedOrganization
        {
            get { return _SelectedOrganization; }
            set
            {
                if (SetProperty(ref _SelectedOrganization, value))
                    ConfirmOrganizationSelection.RaiseCanExecuteChanged();
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
                            try
                            {
                                var authenticatingServer = Wizard.GetDiscoveredServer<SecureInternetServer>(SelectedOrganization.SecureInternetBase);
                                authenticatingServer.OrganizationId = SelectedOrganization.Id;
                                await Wizard.AuthorizationPage.TriggerAuthorizationAsync(authenticatingServer);
                                Wizard.HomePage.SetSecureInternetOrganization(SelectedOrganization);
                                Wizard.CurrentPage = Wizard.HomePage;
                                Query = "";
                            }
                            catch (OperationCanceledException) { }
                            catch (Exception ex) { Wizard.Error = ex; }
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
            get { return _OwnServers; }
            private set { SetProperty(ref _OwnServers, value); }
        }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private ObservableCollection<Server> _OwnServers;

        /// <summary>
        /// Selected own server
        /// </summary>
        /// <remarks><c>null</c> if none selected.</remarks>
        public Server SelectedOwnServer
        {
            get { return _SelectedOwnServer; }
            set
            {
                if (SetProperty(ref _SelectedOwnServer, value))
                    ConfirmOwnServerSelection.RaiseCanExecuteChanged();
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
                            try
                            {
                                await Wizard.AuthorizationPage.TriggerAuthorizationAsync(SelectedOwnServer);
                                Wizard.HomePage.AddOwnServer(SelectedOwnServer);
                                Wizard.CurrentPage = Wizard.HomePage;
                                Query = "";
                            }
                            catch (OperationCanceledException) { }
                            catch (Exception ex) { Wizard.Error = ex; }
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
                        () =>
                        {
                            try { Wizard.CurrentPage = Wizard.HomePage; }
                            catch (Exception ex) { Wizard.Error = ex; }
                        },
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
                ConfirmOrganizationSelection.RaiseCanExecuteChanged();
                Search();
            };

            Wizard.DiscoveredOrganizationsChanged += (object sender, EventArgs e) => Search();
        }

        #endregion

        #region Methods

        /// <summary>
        /// Trigger search
        /// </summary>
        private void Search()
        {
            SearchInProgress?.Cancel();
            SearchThread?.Join();
            SearchInProgress = new CancellationTokenSource();
            SearchThread = new Thread(new ThreadStart(
                () =>
                {
                    var ct = CancellationTokenSource.CreateLinkedTokenSource(SearchInProgress.Token, Window.Abort.Token).Token;
                    Wizard.Dispatcher.Invoke(DispatcherPriority.Normal, (Action)(() => Wizard.TaskCount++));
                    try
                    {
                        var keywords = Query.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                        var orderedServerHits = Wizard.GetDiscoveredInstituteAccessServers(keywords, ct);
                        var orderedOrganizationHits = Wizard.GetDiscoveredOrganizations(keywords, ct);
                        var ownServers = new ObservableCollection<Server>();

                        if (keywords.Length == 1 && keywords[0].Split('.').Length >= 3)
                        {
                            try
                            {
                                var srv = new Server(new UriBuilder("https", keywords[0]).Uri);
                                srv.RequestAuthorization += Wizard.AuthorizationPage.OnRequestAuthorization;
                                srv.ForgetAuthorization += Wizard.AuthorizationPage.OnForgetAuthorization;
                                ownServers.Add(srv);
                            }
                            catch { }
                        }

                        ct.ThrowIfCancellationRequested();
                        Wizard.Dispatcher.Invoke(DispatcherPriority.Normal, (Action)(() =>
                        {
                            {
                                var selected = SelectedInstituteAccessServer?.Base;
                                InstituteAccessServers = orderedServerHits;
                                SelectedInstituteAccessServer = Wizard.GetDiscoveredServer<InstituteAccessServer>(selected);
                            }

                            {
                                var selected = SelectedOrganization?.Id;
                                Organizations = orderedOrganizationHits;
                                SelectedOrganization = Wizard.GetDiscoveredOrganization(selected);
                            }

                            {
                                var selected = SelectedOwnServer?.Base;
                                OwnServers = ownServers;
                                SelectedOwnServer = ownServers.FirstOrDefault(srv => selected == srv.Base);
                            }
                        }));
                    }
                    catch (OperationCanceledException) { }
                    catch (Exception ex) { Wizard.Dispatcher.Invoke(DispatcherPriority.Normal, (Action)(() => Wizard.Error = ex)); }
                    finally { Wizard.Dispatcher.Invoke(DispatcherPriority.Normal, (Action)(() => Wizard.TaskCount--)); }
                }));
            SearchThread.Start();
        }

        #endregion
    }
}
