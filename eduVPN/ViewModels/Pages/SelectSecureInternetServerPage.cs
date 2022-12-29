﻿/*
    eduVPN - VPN for education and research

    Copyright: 2017-2023 The Commons Conservancy
    SPDX-License-Identifier: GPL-3.0+
*/

using eduVPN.Models;
using eduVPN.ViewModels.Windows;
using Prism.Commands;
using System;
using System.Collections.ObjectModel;
using System.Diagnostics;

namespace eduVPN.ViewModels.Pages
{
    /// <summary>
    /// Secure internet connecting server selection wizard page
    /// </summary>
    public class SelectSecureInternetServerPage : ConnectWizardStandardPage
    {
        #region Properties

        /// <summary>
        /// Secure internet server list
        /// </summary>
        public ObservableCollection<SecureInternetServer> SecureInternetServers
        {
            get => _SecureInternetServers;
            private set => SetProperty(ref _SecureInternetServers, value);
        }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private ObservableCollection<SecureInternetServer> _SecureInternetServers;

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
                        () =>
                        {
                            Wizard.HomePage.SetSecureInternetConnectingServer(SelectedSecureInternetServer);
                            Wizard.CurrentPage = Wizard.HomePage;
                        },
                        () => SelectedSecureInternetServer != null);
                return _ConfirmSecureInternetServerSelection;
            }
        }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private DelegateCommand _ConfirmSecureInternetServerSelection;

        /// <inheritdoc/>
        public override DelegateCommand NavigateBack
        {
            get
            {
                if (_NavigateBack == null)
                    _NavigateBack = new DelegateCommand(() => Wizard.CurrentPage = Wizard.HomePage);
                return _NavigateBack;
            }
        }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private DelegateCommand _NavigateBack;

        #endregion

        #region Constructors

        /// <summary>
        /// Constructs a country selection wizard page.
        /// </summary>
        /// <param name="wizard">The connecting wizard</param>
        public SelectSecureInternetServerPage(ConnectWizard wizard) :
            base(wizard)
        {
            RebuildSecureInternetServers(this, null);
            Wizard.DiscoveredServersChanged += (object sender, EventArgs e) => RebuildSecureInternetServers(sender, e);
        }

        #endregion

        #region Methods

        /// <summary>
        /// Populates the secure internet server list
        /// </summary>
        /// <param name="sender">The source of the event</param>
        /// <param name="e">An object that contains no event data</param>
        private void RebuildSecureInternetServers(object sender, EventArgs e)
        {
            var selected = SelectedSecureInternetServer?.Base;
            SecureInternetServers = Wizard.GetDiscoveredSecureInternetServers(Window.Abort.Token);
            SelectedSecureInternetServer = Wizard.GetDiscoveredServer<SecureInternetServer>(selected);
        }

        #endregion
    }
}
