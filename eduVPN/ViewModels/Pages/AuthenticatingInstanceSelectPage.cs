﻿/*
    eduVPN - VPN for education and research

    Copyright: 2017-2021 The Commons Conservancy eduVPN Programme
    SPDX-License-Identifier: GPL-3.0+
*/

using eduVPN.Models;
using eduVPN.ViewModels.Windows;
using Prism.Commands;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;

namespace eduVPN.ViewModels.Pages
{
    /// <summary>
    /// Instance selection base wizard page
    /// </summary>
    public class AuthenticatingInstanceSelectPage : ConnectWizardPage
    {
        #region Properties

        /// <summary>
        /// Selected instance
        /// </summary>
        /// <remarks><c>null</c> if none selected.</remarks>
        public Instance SelectedInstance
        {
            get { return _selected_instance; }
            set { SetProperty(ref _selected_instance, value); }
        }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private Instance _selected_instance;

        /// <summary>
        /// Authorize selected instance command
        /// </summary>
        public DelegateCommand AuthorizeSelectedInstance
        {
            get
            {
                if (_authorize_selected_instance == null)
                {
                    _authorize_selected_instance = new DelegateCommand(
                        // execute
                        async () =>
                        {
                            Wizard.ChangeTaskCount(+1);
                            try
                            {
                                // Trigger initial authorization request.
                                var authenticating_instance = SelectedInstance;
                                await Wizard.TriggerAuthorizationAsync(authenticating_instance);
                                Wizard.InstanceSource.AuthenticatingInstance = authenticating_instance;

                                // Assume the same connecting instance.
                                var connecting_instance = Wizard.InstanceSource.ConnectingInstanceList.FirstOrDefault(inst => inst.Base.AbsoluteUri == authenticating_instance.Base.AbsoluteUri);
                                if (connecting_instance == null)
                                    Wizard.InstanceSource.ConnectingInstanceList.Add(authenticating_instance);
                                Wizard.InstanceSource.ConnectingInstance = authenticating_instance;

                                switch (Properties.Settings.Default.ConnectingProfileSelectMode)
                                {
                                    case 0:
                                    case 2:
                                        // Go to profile selection page.
                                        Wizard.CurrentPage = Wizard.ConnectingProfileSelectPage;
                                        break;

                                    case 1:
                                    case 3:
                                        // Update settings.
                                        var source_index = (int)Wizard.InstanceSourceType;
                                        Properties.Settings.Default[Properties.Settings.InstanceDirectoryId[source_index] + "InstanceSourceSettings"] = new Xml.InstanceSourceSettings() { InstanceSource = Wizard.InstanceSources[source_index].ToSettings() };

                                        // Go to recent configuration selection page.
                                        Wizard.CurrentPage = Wizard.RecentConfigurationSelectPage;
                                        break;
                                }
                            }
                            catch (Exception ex) { Wizard.Error = ex; }
                            finally { Wizard.ChangeTaskCount(-1); }
                        },

                        // canExecute
                        () => SelectedInstance != null);

                    // Setup canExecute refreshing.
                    PropertyChanged += (object sender, PropertyChangedEventArgs e) => { if (e.PropertyName == nameof(SelectedInstance)) _authorize_selected_instance.RaiseCanExecuteChanged(); };
                }

                return _authorize_selected_instance;
            }
        }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private DelegateCommand _authorize_selected_instance;

        /// <inheritdoc/>
        public override DelegateCommand NavigateBack
        {
            get
            {
                if (_navigate_back == null)
                    _navigate_back = new DelegateCommand(
                        // execute
                        () =>
                        {
                            Wizard.ChangeTaskCount(+1);
                            try { Wizard.CurrentPage = Wizard.InstanceSourceSelectPage; }
                            catch (Exception ex) { Wizard.Error = ex; }
                            finally { Wizard.ChangeTaskCount(-1); }
                        });

                return _navigate_back;
            }
        }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private DelegateCommand _navigate_back;

        #endregion

        #region Constructors

        /// <summary>
        /// Constructs an instance selection wizard page.
        /// </summary>
        /// <param name="wizard">The connecting wizard</param>
        public AuthenticatingInstanceSelectPage(ConnectWizard wizard) :
            base(wizard)
        {
        }

        #endregion
    }
}
