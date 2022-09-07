/*
    eduVPN - VPN for education and research

    Copyright: 2017-2022 The Commons Conservancy
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
            get => _SelectedInstituteAccessServer;
            set
            {
                if (SetProperty(ref _SelectedInstituteAccessServer, value))
                {
                    _ConfirmInstituteAccessServerSelection?.RaiseCanExecuteChanged();
                    _ForgetInstituteAccessServer?.RaiseCanExecuteChanged();
                }
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
                            Wizard.ConnectionPage.ConnectingServer = SelectedInstituteAccessServer;
                            Wizard.CurrentPage = Wizard.ConnectionPage;
                        },
                        () => SelectedInstituteAccessServer != null);
                return _ConfirmInstituteAccessServerSelection;
            }
        }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private DelegateCommand _ConfirmInstituteAccessServerSelection;

        /// <summary>
        /// Forgets selected institute access server
        /// </summary>
        public DelegateCommand ForgetInstituteAccessServer
        {
            get
            {
                if (_ForgetInstituteAccessServer == null)
                    _ForgetInstituteAccessServer = new DelegateCommand(
                        () =>
                        {
                            Properties.Settings.Default.InstituteAccessServers.Remove(SelectedInstituteAccessServer.Base);
                            SelectedInstituteAccessServer.Forget();
                            InstituteAccessServers.Remove(SelectedInstituteAccessServer);
                            SelectedInstituteAccessServer = null;

                            // Return to starting page. Should the abscence of configurations from history resolve in different starting page of course.
                            if (Wizard.StartingPage != Wizard.CurrentPage)
                                Wizard.CurrentPage = Wizard.StartingPage;
                        },
                        () =>
                        {
                            if (SelectedInstituteAccessServer == null)
                                return false;
                            var precfgList = Properties.SettingsEx.Default.InstituteAccessServers;
                            if (precfgList != null && precfgList.Contains(SelectedInstituteAccessServer.Base))
                                return false;
                            return true;
                        });
                return _ForgetInstituteAccessServer;
            }
        }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private DelegateCommand _ForgetInstituteAccessServer;

        /// <summary>
        /// Secure internet authenticating server
        /// </summary>
        public SecureInternetServer AuthenticatingSecureInternetServer
        {
            get
            {
                var orgId = Properties.SettingsEx.Default.SecureInternetOrganization;
                if (orgId == "")
                    return null;
                if (orgId == null)
                    orgId = Properties.Settings.Default.SecureInternetOrganization;
                var org = Wizard.GetDiscoveredOrganization(orgId);
                if (org == null)
                    return null;

                var srv = Wizard.GetDiscoveredServer<SecureInternetServer>(org.SecureInternetBase);
                if (srv == null)
                    return null;

                srv.OrganizationId = orgId;
                return srv;
            }
        }

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
            get => _SelectedSecureInternetServer;
            set
            {
                if (SetProperty(ref _SelectedSecureInternetServer, value))
                    _ConfirmSecureInternetServerSelection?.RaiseCanExecuteChanged();
            }
        }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private SecureInternetServer _SelectedSecureInternetServer;

        /// <summary>
        /// Confirms secure internet server selection
        /// </summary>
        public DelegateCommand ConfirmSecureInternetServerSelection
        {
            get
            {
                if (_ConfirmSecureInternetServerSelection == null)
                    _ConfirmSecureInternetServerSelection = new DelegateCommand(
                        async () =>
                        {
                            await Wizard.AuthorizationPage.TriggerAuthorizationAsync(AuthenticatingSecureInternetServer);
                            Wizard.ConnectionPage.ConnectingServer = SelectedSecureInternetServer;
                            Wizard.CurrentPage = Wizard.ConnectionPage;
                        },
                        () => AuthenticatingSecureInternetServer != null && SelectedSecureInternetServer != null);
                return _ConfirmSecureInternetServerSelection;
            }
        }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private DelegateCommand _ConfirmSecureInternetServerSelection;

        /// <summary>
        /// Forgets secure internet
        /// </summary>
        public DelegateCommand ForgetSecureInternet
        {
            get
            {
                if (_ForgetSecureInternet == null)
                    _ForgetSecureInternet = new DelegateCommand(
                        () =>
                        {
                            var authenticatingServer = AuthenticatingSecureInternetServer;
                            authenticatingServer?.Forget();
                            Properties.Settings.Default.SecureInternetOrganization = null;
                            Properties.Settings.Default.SecureInternetConnectingServer = null;
                            SecureInternetServers.Clear();
                            SelectedSecureInternetServer = null;
                            RaisePropertyChanged(nameof(AuthenticatingSecureInternetServer));
                            _ConfirmSecureInternetServerSelection?.RaiseCanExecuteChanged();
                            _ForgetSecureInternet?.RaiseCanExecuteChanged();
                            _ChangeSecureInternetServer?.RaiseCanExecuteChanged();

                            // Return to starting page. Should the abscence of configurations from history resolve in different starting page of course.
                            if (Wizard.StartingPage != Wizard.CurrentPage)
                                Wizard.CurrentPage = Wizard.StartingPage;
                        },
                        () => !string.IsNullOrEmpty(Properties.Settings.Default.SecureInternetOrganization) && Properties.SettingsEx.Default.SecureInternetOrganization == null);
                return _ForgetSecureInternet;
            }
        }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private DelegateCommand _ForgetSecureInternet;

        /// <summary>
        /// Changes secure internet server
        /// </summary>
        public DelegateCommand ChangeSecureInternetServer
        {
            get
            {
                if (_ChangeSecureInternetServer == null)
                    _ChangeSecureInternetServer = new DelegateCommand(
                        () => Wizard.CurrentPage = Wizard.SelectSecureInternetServerPage,
                        () =>
                        {
                            var orgId = Properties.SettingsEx.Default.SecureInternetOrganization;
                            if (orgId == "")
                                return false;
                            if (orgId == null)
                                orgId = Properties.Settings.Default.SecureInternetOrganization;
                            return !string.IsNullOrEmpty(orgId);
                        });
                return _ChangeSecureInternetServer;
            }
        }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private DelegateCommand _ChangeSecureInternetServer;

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
            get => _SelectedOwnServer;
            set
            {
                if (SetProperty(ref _SelectedOwnServer, value))
                {
                    _ConfirmOwnServerSelection?.RaiseCanExecuteChanged();
                    _ForgetOwnServer?.RaiseCanExecuteChanged();
                }
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
                            Wizard.ConnectionPage.ConnectingServer = SelectedOwnServer;
                            Wizard.CurrentPage = Wizard.ConnectionPage;
                        },
                        () => SelectedOwnServer != null);
                return _ConfirmOwnServerSelection;
            }
        }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private DelegateCommand _ConfirmOwnServerSelection;

        /// <summary>
        /// Forgets selected own server
        /// </summary>
        public DelegateCommand ForgetOwnServer
        {
            get
            {
                if (_ForgetOwnServer == null)
                    _ForgetOwnServer = new DelegateCommand(
                        () =>
                        {
                            Properties.Settings.Default.OwnServers.Remove(SelectedOwnServer.Base);
                            SelectedOwnServer.Forget();
                            OwnServers.Remove(SelectedOwnServer);
                            SelectedOwnServer = null;

                            // Return to starting page. Should the abscence of configurations from history resolve in different starting page of course.
                            if (Wizard.StartingPage != Wizard.CurrentPage)
                                Wizard.CurrentPage = Wizard.StartingPage;
                        },
                        () => SelectedOwnServer != null);
                return _ForgetOwnServer;
            }
        }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private DelegateCommand _ForgetOwnServer;

        /// <summary>
        /// Adds another server
        /// </summary>
        public DelegateCommand AddAnother
        {
            get
            {
                if (_AddAnother == null)
                    _AddAnother = new DelegateCommand(() => Wizard.CurrentPage = Wizard.AddAnotherPage);
                return _AddAnother;
            }
        }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private DelegateCommand _AddAnother;

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
                var precfgList = Properties.SettingsEx.Default.InstituteAccessServers;
                if (precfgList != null)
                {
                    Trace.TraceInformation("Adding preconfigured Institute Access servers {0}", string.Join(", ", precfgList));
                    foreach (var baseUri in precfgList)
                    {
                        Window.Abort.Token.ThrowIfCancellationRequested();
                        var srv = Wizard.GetDiscoveredServer<InstituteAccessServer>(baseUri);
                        if (srv == null)
                        {
                            srv = new InstituteAccessServer(baseUri);
                            srv.RequestAuthorization += Wizard.AuthorizationPage.OnRequestAuthorization;
                            srv.ForgetAuthorization += Wizard.AuthorizationPage.OnForgetAuthorization;
                        }
                        list.Add(srv);
                    }
                }
                foreach (var baseUri in Properties.Settings.Default.InstituteAccessServers)
                {
                    Window.Abort.Token.ThrowIfCancellationRequested();
                    if (precfgList != null && precfgList.Contains(baseUri))
                        continue;
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
            var precfgList = Properties.SettingsEx.Default.InstituteAccessServers;
            if (precfgList != null && precfgList.Contains(srv.Base) ||
                Properties.Settings.Default.InstituteAccessServers.Contains(srv.Base))
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
                var orgId = Properties.SettingsEx.Default.SecureInternetOrganization;
                if (orgId != null)
                    Trace.TraceInformation("Using preconfigured Secure Internet organization {0}", orgId);
                if (orgId != "")
                {
                    if (orgId == null)
                        orgId = Properties.Settings.Default.SecureInternetOrganization;
                    var org = Wizard.GetDiscoveredOrganization(orgId);
                    if (org != null)
                    {
                        SecureInternetServer srv;
                        if ((srv = Wizard.GetDiscoveredServer<SecureInternetServer>(Properties.Settings.Default.SecureInternetConnectingServer)) != null ||
                            (srv = AuthenticatingSecureInternetServer) != null)
                            list.Add(srv);
                    }
                }
            }
            finally { SecureInternetServers.EndUpdate(); }
            SelectedSecureInternetServer = Wizard.GetDiscoveredServer<SecureInternetServer>(selected);
            RaisePropertyChanged(nameof(AuthenticatingSecureInternetServer));
            _ConfirmSecureInternetServerSelection?.RaiseCanExecuteChanged();
        }

        /// <summary>
        /// Sets secure internet organization
        /// </summary>
        /// <param name="org">Organization</param>
        public void SetSecureInternetOrganization(Organization org)
        {
            if (Properties.SettingsEx.Default.SecureInternetOrganization != null)
                return;
            Properties.Settings.Default.SecureInternetOrganization = org.Id;
            Properties.Settings.Default.SecureInternetConnectingServer = org.SecureInternetBase;
            RebuildSecureInternetServers(this, null);
            _ForgetSecureInternet?.RaiseCanExecuteChanged();
            _ChangeSecureInternetServer?.RaiseCanExecuteChanged();
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
