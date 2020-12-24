/*
    eduVPN - VPN for education and research

    Copyright: 2017-2020 The Commons Conservancy eduVPN Programme
    SPDX-License-Identifier: GPL-3.0+
*/

using eduVPN.Models;
using eduVPN.ViewModels.Windows;
using Prism.Commands;
using System;
using System.Diagnostics;
using System.Linq;

namespace eduVPN.ViewModels.Pages
{
    /// <summary>
    /// Home wizard page
    /// </summary>
    public class HomePage : ConnectWizardStandardPage
    {
        #region Properties

        /// <summary>
        /// Institute access server list
        /// </summary>
        public ObservableCollectionEx<InstituteAccessServer> InstituteAccessServers { get; } = new ObservableCollectionEx<InstituteAccessServer>();

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
                {
                    ConfirmInstituteAccessServerSelection.RaiseCanExecuteChanged();
                    ForgetInstituteAccessServer.RaiseCanExecuteChanged();
                }
            }
        }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private InstituteAccessServer _SelectedInstituteAccessServer;

        /// <summary>
        /// Confirms institute access server selection
        /// </summary>
        public DelegateCommand ConfirmInstituteAccessServerSelection { get; }

        /// <summary>
        /// Forgets selected institute access server
        /// </summary>
        public DelegateCommand ForgetInstituteAccessServer { get; }

        /// <summary>
        /// Secure internet server list
        /// </summary>
        public ObservableCollectionEx<SecureInternetServer> SecureInternetServers { get; } = new ObservableCollectionEx<SecureInternetServer>();

        /// <summary>
        /// Selected secure internet server
        /// </summary>
        /// <remarks><c>null</c> if none selected.</remarks>
        public SecureInternetServer SelectedSecureInternetServer
        {
            get { return _SelectedSecureInternetServer; }
            set
            {
                if (SetProperty(ref _SelectedSecureInternetServer, value))
                    ConfirmSecureInternetServerSelection.RaiseCanExecuteChanged();
            }
        }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private SecureInternetServer _SelectedSecureInternetServer;

        /// <summary>
        /// Confirms secure internet server selection
        /// </summary>
        public DelegateCommand ConfirmSecureInternetServerSelection { get; }

        /// <summary>
        /// Forgets secure internet
        /// </summary>
        public DelegateCommand ForgetSecureInternet { get; }

        /// <summary>
        /// Changes secure internet server
        /// </summary>
        public DelegateCommand ChangeSecureInternetServer { get; }

        /// <summary>
        /// Own server list
        /// </summary>
        public ObservableCollectionEx<Server> OwnServers { get; } = new ObservableCollectionEx<Server>();

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
                {
                    ConfirmOwnServerSelection.RaiseCanExecuteChanged();
                    ForgetOwnServer.RaiseCanExecuteChanged();
                }
            }
        }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private Server _SelectedOwnServer;

        /// <summary>
        /// Confirms own server selection
        /// </summary>
        public DelegateCommand ConfirmOwnServerSelection { get; }

        /// <summary>
        /// Forgets selected own server
        /// </summary>
        public DelegateCommand ForgetOwnServer { get; }

        /// <summary>
        /// Adds another server
        /// </summary>
        public DelegateCommand AddAnother { get; }

        #endregion

        #region Constructors

        /// <summary>
        /// Constructs a wizard page.
        /// </summary>
        /// <param name="wizard">The connecting wizard</param>
        public HomePage(ConnectWizard wizard) :
            base(wizard)
        {
            RebuildInstituteAccessServers(this, null);
            RebuildSecureInternetServers(this, null);
            RebuildOwnServers(this, null);

            Wizard.DiscoveredServersChanged += (object sender, EventArgs e) =>
            {
                RebuildInstituteAccessServers(sender, e);
                RebuildSecureInternetServers(sender, e);
                RebuildOwnServers(sender, e);
            };
            Wizard.DiscoveredOrganizationsChanged += RebuildSecureInternetServers;

            ConfirmInstituteAccessServerSelection = new DelegateCommand(
                async () =>
                {
                    try
                    {
                        await Wizard.AuthorizationPage.TriggerAuthorizationAsync(SelectedInstituteAccessServer);
                        Wizard.ConnectionPage.ConnectingServer = Wizard.ConnectionPage.AuthenticatingServer = SelectedInstituteAccessServer;
                        Wizard.CurrentPage = Wizard.ConnectionPage;
                    }
                    catch (OperationCanceledException) { }
                    catch (Exception ex) { Wizard.Error = ex; }
                },
                () => SelectedInstituteAccessServer != null);

            ForgetInstituteAccessServer = new DelegateCommand(
                () =>
                {
                    try
                    {
                        Properties.Settings.Default.InstituteAccessServers.Remove(SelectedInstituteAccessServer.Base);
                        SelectedInstituteAccessServer.Forget();
                        InstituteAccessServers.Remove(SelectedInstituteAccessServer);
                        SelectedInstituteAccessServer = null;

                        // Return to starting page. Should the abscence of configurations from history resolve in different starting page of course.
                        if (Wizard.StartingPage != Wizard.CurrentPage)
                            Wizard.CurrentPage = Wizard.StartingPage;
                    }
                    catch (Exception ex) { Wizard.Error = ex; }
                },
                () => SelectedInstituteAccessServer != null);

            ConfirmSecureInternetServerSelection = new DelegateCommand(
                async () =>
                {
                    try
                    {
                        var org = Wizard.GetDiscoveredOrganization(Properties.Settings.Default.SecureInternetOrganization);
                        var authenticatingServer = Wizard.GetDiscoveredServer<SecureInternetServer>(org.SecureInternetBase);
                        authenticatingServer.OrganizationId = Properties.Settings.Default.SecureInternetOrganization;
                        await Wizard.AuthorizationPage.TriggerAuthorizationAsync(authenticatingServer);
                        Wizard.ConnectionPage.AuthenticatingServer = authenticatingServer;
                        Wizard.ConnectionPage.ConnectingServer = SelectedSecureInternetServer;
                        Wizard.CurrentPage = Wizard.ConnectionPage;
                    }
                    catch (OperationCanceledException) { }
                    catch (Exception ex) { Wizard.Error = ex; }
                },
                () => !string.IsNullOrEmpty(Properties.Settings.Default.SecureInternetOrganization) &&
                    SelectedSecureInternetServer != null);

            ForgetSecureInternet = new DelegateCommand(
                () =>
                {
                    try
                    {
                        var org = Wizard.GetDiscoveredOrganization(Properties.Settings.Default.SecureInternetOrganization);
                        var authenticatingServer = Wizard.GetDiscoveredServer<SecureInternetServer>(org.SecureInternetBase);
                        authenticatingServer.OrganizationId = Properties.Settings.Default.SecureInternetOrganization;
                        authenticatingServer.Forget();
                        Properties.Settings.Default.SecureInternetOrganization = null;
                        Properties.Settings.Default.SecureInternetConnectingServer = null;
                        SecureInternetServers.Clear();
                        SelectedSecureInternetServer = null;
                        ForgetSecureInternet.RaiseCanExecuteChanged();
                        ChangeSecureInternetServer.RaiseCanExecuteChanged();

                        // Return to starting page. Should the abscence of configurations from history resolve in different starting page of course.
                        if (Wizard.StartingPage != Wizard.CurrentPage)
                            Wizard.CurrentPage = Wizard.StartingPage;
                    }
                    catch (Exception ex) { Wizard.Error = ex; }
                },
                () => !string.IsNullOrEmpty(Properties.Settings.Default.SecureInternetOrganization));

            ChangeSecureInternetServer = new DelegateCommand(
                () =>
                {
                    try { Wizard.CurrentPage = Wizard.SelectSecureInternetServerPage; }
                    catch (Exception ex) { Wizard.Error = ex; }
                },
                () => !string.IsNullOrEmpty(Properties.Settings.Default.SecureInternetOrganization));

            ConfirmOwnServerSelection = new DelegateCommand(
                async () =>
                {
                    try
                    {
                        await Wizard.AuthorizationPage.TriggerAuthorizationAsync(SelectedOwnServer);
                        Wizard.ConnectionPage.ConnectingServer = Wizard.ConnectionPage.AuthenticatingServer = SelectedOwnServer;
                        Wizard.CurrentPage = Wizard.ConnectionPage;
                    }
                    catch (OperationCanceledException) { }
                    catch (Exception ex) { Wizard.Error = ex; }
                },
                () => SelectedOwnServer != null);

            ForgetOwnServer = new DelegateCommand(
                () =>
                {
                    try
                    {
                        Properties.Settings.Default.OwnServers.Remove(SelectedOwnServer.Base);
                        SelectedOwnServer.Forget();
                        OwnServers.Remove(SelectedOwnServer);
                        SelectedOwnServer = null;

                        // Return to starting page. Should the abscence of configurations from history resolve in different starting page of course.
                        if (Wizard.StartingPage != Wizard.CurrentPage)
                            Wizard.CurrentPage = Wizard.StartingPage;
                    }
                    catch (Exception ex) { Wizard.Error = ex; }
                },
                () => SelectedOwnServer != null);

            AddAnother = new DelegateCommand(
                () =>
                {
                    try { Wizard.CurrentPage = Wizard.AddAnotherPage; }
                    catch (Exception ex) { Wizard.Error = ex; }
                });
        }

        #endregion

        #region Methods

        /// <summary>
        /// Populates the server list
        /// </summary>
        /// <param name="sender">The source of the event</param>
        /// <param name="e">An object that contains no event data</param>
        private void RebuildInstituteAccessServers(object sender, EventArgs e)
        {
            var selected = SelectedInstituteAccessServer?.Base;
            var list = InstituteAccessServers.BeginUpdate();
            try
            {
                list.Clear();
                foreach (var baseUri in Properties.Settings.Default.InstituteAccessServers)
                {
                    Window.Abort.Token.ThrowIfCancellationRequested();
                    var srv = Wizard.GetDiscoveredServer<InstituteAccessServer>(baseUri);
                    if (srv != null)
                        list.Add(srv);
                }
            }
            finally { InstituteAccessServers.EndUpdate(); }
            SelectedInstituteAccessServer = Wizard.GetDiscoveredServer<InstituteAccessServer>(selected);
        }

        /// <summary>
        /// Adds institute access server to the list
        /// </summary>
        /// <param name="srv">Server</param>
        public void AddInstituteAccessServer(InstituteAccessServer srv)
        {
            if (Properties.Settings.Default.InstituteAccessServers.Contains(srv.Base))
                return;
            Properties.Settings.Default.InstituteAccessServers.Add(srv.Base);
            srv = Wizard.GetDiscoveredServer<InstituteAccessServer>(srv.Base);
            if (srv != null)
                InstituteAccessServers.Add(srv);
        }

        /// <summary>
        /// Populates the secure internet server list
        /// </summary>
        /// <param name="sender">The source of the event</param>
        /// <param name="e">An object that contains no event data</param>
        private void RebuildSecureInternetServers(object sender, EventArgs e)
        {
            var selected = SelectedSecureInternetServer?.Base;
            var list = SecureInternetServers.BeginUpdate();
            try
            {
                list.Clear();
                SecureInternetServer srv;
                Organization org;
                if ((srv = Wizard.GetDiscoveredServer<SecureInternetServer>(Properties.Settings.Default.SecureInternetConnectingServer)) != null ||
                    (org = Wizard.GetDiscoveredOrganization(Properties.Settings.Default.SecureInternetOrganization)) != null &&
                    (srv = Wizard.GetDiscoveredServer<SecureInternetServer>(org.SecureInternetBase)) != null)
                    list.Add(srv);
            }
            finally { SecureInternetServers.EndUpdate(); }
            SelectedSecureInternetServer = Wizard.GetDiscoveredServer<SecureInternetServer>(selected);
        }

        /// <summary>
        /// Sets secure internet organization
        /// </summary>
        /// <param name="org">Organization</param>
        public void SetSecureInternetOrganization(Organization org)
        {
            Properties.Settings.Default.SecureInternetOrganization = org.Id;
            Properties.Settings.Default.SecureInternetConnectingServer = org.SecureInternetBase;
            RebuildSecureInternetServers(this, null);
            ForgetSecureInternet.RaiseCanExecuteChanged();
            ChangeSecureInternetServer.RaiseCanExecuteChanged();
        }

        /// <summary>
        /// Sets secure internet connecting server
        /// </summary>
        /// <param name="srv">Server</param>
        public void SetSecureInternetConnectingServer(SecureInternetServer srv)
        {
            Properties.Settings.Default.SecureInternetConnectingServer = srv.Base;
            RebuildSecureInternetServers(this, null);
        }

        /// <summary>
        /// Populates the own server list
        /// </summary>
        /// <param name="sender">The source of the event</param>
        /// <param name="e">An object that contains no event data</param>
        private void RebuildOwnServers(object sender, EventArgs e)
        {
            var selected = SelectedOwnServer?.Base;
            var list = OwnServers.BeginUpdate();
            try
            {
                list.Clear();
                foreach (var baseUri in Properties.Settings.Default.OwnServers)
                {
                    Window.Abort.Token.ThrowIfCancellationRequested();
                    var srv = new Server(baseUri);
                    srv.RequestAuthorization += Wizard.AuthorizationPage.OnRequestAuthorization;
                    srv.ForgetAuthorization += Wizard.AuthorizationPage.OnForgetAuthorization;
                    list.Add(srv);
                }
            }
            finally { OwnServers.EndUpdate(); }
            SelectedOwnServer = selected != null ? OwnServers.FirstOrDefault(srv => srv.Base.AbsoluteUri == selected.AbsoluteUri) : null;
        }

        /// <summary>
        /// Adds own server to the list
        /// </summary>
        /// <param name="srv">Own server</param>
        public void AddOwnServer(Server srv)
        {
            if (Properties.Settings.Default.OwnServers.Contains(srv.Base))
                return;
            Properties.Settings.Default.OwnServers.Add(srv.Base);
            OwnServers.Add(srv);
        }

        #endregion
    }
}
