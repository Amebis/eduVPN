/*
    eduVPN - VPN for education and research

    Copyright: 2017-2024 The Commons Conservancy
    SPDX-License-Identifier: GPL-3.0+
*/

using eduVPN.Models;
using eduVPN.ViewModels.Windows;
using Prism.Commands;
using Prism.Common;
using System;
using System.Collections.Generic;
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
                        () => Wizard.Connect(SelectedInstituteAccessServer),
                        () => SelectedInstituteAccessServer != null && !SelectedInstituteAccessServer.Delisted);
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
                            Engine.RemoveServer(ServerType.InstituteAccess, SelectedInstituteAccessServer.Id);
                            SelectedInstituteAccessServer.Forget();

                            // eduvpn-common does not do callback after servers are removed. Do the bookkeeping manually.
                            InstituteAccessServers.Remove(SelectedInstituteAccessServer);
                            SelectedInstituteAccessServer = null;
                            Wizard.CurrentPage = Wizard.StartingPage;
                        },
                        () =>
                        {
                            if (SelectedInstituteAccessServer == null)
                                return false;
                            var precfgList = Properties.SettingsEx.Default.InstituteAccessServers;
                            if (precfgList != null && precfgList.Contains(new Uri(SelectedInstituteAccessServer.Id)))
                                return false;
                            return true;
                        });
                return _ForgetInstituteAccessServer;
            }
        }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private DelegateCommand _ForgetInstituteAccessServer;

        /// <summary>
        /// Secure internet server list
        /// Actually, this is a list to ensure same GUI experience as Institute Access and Own Server.
        /// However, there may only be 0 or 1 items in this list - the chosen server to connect to.
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
        /// Confirms secure internet country selection
        /// </summary>
        public DelegateCommand ConfirmSecureInternetServerSelection
        {
            get
            {
                if (_ConfirmSecureInternetServerSelection == null)
                    _ConfirmSecureInternetServerSelection = new DelegateCommand(
                        () => Wizard.Connect(SelectedSecureInternetServer),
                        () => SelectedSecureInternetServer != null && !SelectedSecureInternetServer.Delisted);
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
                            foreach (var srv in SecureInternetServers)
                            {
                                Engine.RemoveServer(ServerType.SecureInternet, srv.Id);
                                srv.Forget();
                            }

                            // eduvpn-common does not do callback after servers are removed. Do the bookkeeping manually.
                            SecureInternetServers.Clear();
                            SelectedSecureInternetServer = null;
                            Wizard.CurrentPage = Wizard.StartingPage;
                        },
                        () => SecureInternetServers.Count != 0 && Properties.SettingsEx.Default.SecureInternetOrganization == null);
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
                        () =>
                        {
                            foreach (var srv in SecureInternetServers)
                            {
                                Wizard.SelectSecureInternetCountryPage.SetSecureInternetCountries(
                                    srv.Locations,
                                    srv.Country);
                                Wizard.CurrentPage = Wizard.SelectSecureInternetCountryPage;
                                break;
                            }
                        },
                        () => SecureInternetServers.Count != 0);
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
                        () => Wizard.Connect(SelectedOwnServer),
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
                            Engine.RemoveServer(ServerType.Own, SelectedOwnServer.Id);
                            SelectedOwnServer.Forget();

                            // eduvpn-common does not do callback after servers are removed. Do the bookkeeping manually.
                            OwnServers.Remove(SelectedOwnServer);
                            SelectedOwnServer = null;
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
        }

        #endregion

        #region Methods

        /// <summary>
        /// Populates lists of servers from eduvpn-common
        /// </summary>
        /// <param name="obj">eduvpn-common provided server list</param>
        public void LoadServers(Dictionary<string, object> obj)
        {
            LoadInstituteAccessServers(obj);
            LoadSecureInternetServer(obj);
            LoadOwnServers(obj);
        }

        /// <summary>
        /// Populates list of institute access servers
        /// </summary>
        /// <param name="obj">eduvpn-common provided server list</param>
        void LoadInstituteAccessServers(Dictionary<string, object> obj)
        {
            var selected = SelectedInstituteAccessServer?.Id;
            var list = InstituteAccessServers.BeginUpdate();
            try
            {
                list.Clear();
                if (obj.TryGetValue("institute_access_servers", out List<object> instituteAccessServers))
                    foreach (var s in instituteAccessServers)
                    {
                        Window.Abort.Token.ThrowIfCancellationRequested();
                        if (!(s is Dictionary<string, object> srvObj))
                            continue;
                        list.Add(new InstituteAccessServer(srvObj));
                    }
            }
            finally { InstituteAccessServers.EndUpdate(); }
            SelectedInstituteAccessServer = selected != null ? InstituteAccessServers.FirstOrDefault(s => s.Id == selected) : null;
        }

        /// <summary>
        /// Populates secure internet server
        /// </summary>
        /// <param name="obj">eduvpn-common provided server list</param>
        void LoadSecureInternetServer(Dictionary<string, object> obj)
        {
            var selected = SelectedSecureInternetServer?.Id;
            var list = SecureInternetServers.BeginUpdate();
            try
            {
                list.Clear();
                if (obj.TryGetValue("secure_internet_server", out Dictionary<string, object> srvObj))
                    list.Add(new SecureInternetServer(srvObj));
            }
            finally { SecureInternetServers.EndUpdate(); }
            _ForgetSecureInternet?.RaiseCanExecuteChanged();
            _ChangeSecureInternetServer?.RaiseCanExecuteChanged();
            SelectedSecureInternetServer = selected != null ? SecureInternetServers.FirstOrDefault(s => s.Id == selected) : null;
        }

        /// <summary>
        /// Populates list of own servers
        /// </summary>
        /// <param name="obj">eduvpn-common provided server list</param>
        void LoadOwnServers(Dictionary<string, object> obj)
        {
            var selected = SelectedOwnServer?.Id;
            var list = OwnServers.BeginUpdate();
            try
            {
                list.Clear();
                if (obj.TryGetValue("custom_servers", out List<object> ownServers))
                    foreach (var s in ownServers)
                    {
                        Window.Abort.Token.ThrowIfCancellationRequested();
                        if (!(s is Dictionary<string, object> srvObj))
                            continue;
                        list.Add(new Server(srvObj));
                    }
            }
            finally { OwnServers.EndUpdate(); }
            SelectedOwnServer = selected != null ? OwnServers.FirstOrDefault(srv => srv.Id == selected) : null;
        }

        #endregion
    }
}
