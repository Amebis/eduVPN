/*
    eduVPN - VPN for education and research

    Copyright: 2017-2024 The Commons Conservancy
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

        /// <summary>
        /// Trigger search
        /// </summary>
        void Search()
        {
            SearchInProgress?.Cancel();
            if (string.IsNullOrEmpty(Query))
            {
                InstituteAccessServers = null;
                SelectedInstituteAccessServer = null;
                Organizations = null;
                SelectedOrganization = null;
                SelectedOwnServer = null;
                OwnServers = null;
                return;
            }

            SearchInProgress = new CancellationTokenSource();
            var ct = CancellationTokenSource.CreateLinkedTokenSource(SearchInProgress.Token, Window.Abort.Token).Token;
            new Thread(() =>
            {
                Wizard.TryInvoke((Action)(() => Wizard.TaskCount++));
                try
                {
                    ServerDictionary dict;
                    using (var cookie = new Engine.CancellationTokenCookie(ct))
                        dict = Engine.DiscoServers(cookie, Query);
                    var orderedServerHits = new ObservableCollection<InstituteAccessServer>(dict.Select(el => el.Value as InstituteAccessServer).Where(srv => srv != null));
                    ct.ThrowIfCancellationRequested();
                    Wizard.TryInvoke((Action)(() =>
                    {
                        if (ct.IsCancellationRequested) return;
                        var selected = SelectedInstituteAccessServer?.Id;
                        InstituteAccessServers = orderedServerHits.Count >= 0 ? orderedServerHits : null;
                        SelectedInstituteAccessServer = selected != null && dict.TryGetValue(selected, out var srv) ? srv as InstituteAccessServer : null;
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
                        OrganizationDictionary dict;
                        using (var cookie = new Engine.CancellationTokenCookie(ct))
                            dict = Engine.DiscoOrganizations(cookie, Query);
                        var orderedOrganizationHits = new ObservableCollection<Organization>(dict.Select(el => el.Value));
                        ct.ThrowIfCancellationRequested();
                        Wizard.TryInvoke((Action)(() =>
                        {
                            if (ct.IsCancellationRequested) return;
                            var selected = SelectedOrganization?.Id;
                            Organizations = orderedOrganizationHits.Count >= 0 ? orderedOrganizationHits : null;
                            SelectedOrganization = selected != null && dict.TryGetValue(selected, out var org) ? org : null;
                        }));
                    }
                    catch (OperationCanceledException) { }
                    catch (Exception ex) { Wizard.TryInvoke((Action)(() => Wizard.Error = ex)); }
                    finally { Wizard.TryInvoke((Action)(() => Wizard.TaskCount--)); }
                }).Start();
            }

            var keywords = Query.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
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

        #endregion
    }
}
