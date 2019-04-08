/*
    eduVPN - VPN for education and research

    Copyright: 2017-2019 The Commons Conservancy eduVPN Programme
    SPDX-License-Identifier: GPL-3.0+
*/

using eduVPN.Models;
using eduVPN.ViewModels.Panels;
using eduVPN.ViewModels.Windows;
using Prism.Commands;
using System;
using System.ComponentModel;
using System.Diagnostics;

namespace eduVPN.ViewModels.Pages
{
    /// <summary>
    /// Profile selection wizard page
    /// </summary>
    public class ConnectingProfileSelectPage : ConnectWizardPage
    {
        #region Properties

        /// <inheritdoc/>
        public override string Title
        {
            get { return Resources.Strings.ConnectingProfileSelectPageTitle; }
        }

        /// <summary>
        /// Profile select panel
        /// </summary>
        public ConnectingRefreshableProfileSelectPanel Panel
        {
            get
            {
                var source_index = (int)Wizard.InstanceSourceType;
                if (_panels[source_index] == null)
                    _panels[source_index] = new ConnectingRefreshableProfileSelectPanel(Wizard, Wizard.InstanceSourceType);

                return _panels[source_index];
            }
        }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private ConnectingRefreshableProfileSelectPanel[] _panels = new ConnectingRefreshableProfileSelectPanel[(int)InstanceSourceType._end];

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
                            try
                            {
                                if (Wizard.InstanceSource is LocalInstanceSource)
                                {
                                    switch (Properties.Settings.Default.ConnectingProfileSelectMode)
                                    {
                                        case 0:
                                        case 2:
                                            if (Wizard.InstanceSource.InstanceList.IndexOf(Wizard.InstanceSource.AuthenticatingInstance) >= 0)
                                                Wizard.CurrentPage = Wizard.AuthenticatingInstanceSelectPage;
                                            else
                                                Wizard.CurrentPage = Wizard.CustomInstancePage;
                                            break;

                                        case 1:
                                            Wizard.CurrentPage = Wizard.RecentConfigurationSelectPage;
                                            break;
                                    }
                                }
                                else
                                    Wizard.CurrentPage = Wizard.InstanceSourceSelectPage;
                            }
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
        /// Constructs an profile selection wizard page.
        /// </summary>
        /// <param name="wizard">The connecting wizard</param>
        public ConnectingProfileSelectPage(ConnectWizard wizard) :
            base(wizard)
        {
            Wizard.PropertyChanged += (object sender, PropertyChangedEventArgs e) =>
            {
                if (e.PropertyName == nameof(Wizard.InstanceSourceType))
                    RaisePropertyChanged(nameof(Panel));
            };
        }

        #endregion

        #region Methods

        /// <inheritdoc/>
        public override void OnActivate()
        {
            base.OnActivate();

            // Synchronize selected instance => triggers profile list refresh.
            // But reset it first: when the previous attempt to load the profile list failed,
            // setting the Panel.SelectedInstance to the very same connecting instance again
            // will not trigger a reload. This is not a performance regression, since the
            // sucessfully retrieved profile lists are cached.
            Panel.SelectedInstance = null;
            Panel.SelectedInstance = Wizard.InstanceSource.ConnectingInstance;
        }

        #endregion
    }
}
