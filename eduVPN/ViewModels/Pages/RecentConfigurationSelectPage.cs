/*
    eduVPN - VPN for education and research

    Copyright: 2017, The Commons Conservancy eduVPN Programme
    SPDX-License-Identifier: GPL-3.0+
*/

using eduVPN.Models;
using eduVPN.ViewModels.Panels;
using eduVPN.ViewModels.Windows;
using Prism.Commands;
using System;

namespace eduVPN.ViewModels.Pages
{
    /// <summary>
    /// Recent profile selection wizard page
    /// </summary>
    public class RecentConfigurationSelectPage : ConnectWizardPage
    {
        #region Properties

        /// <inheritdoc/>
        public override string Title
        {
            get { return Resources.Strings.ConnectingProfileSelectPageTitle; }
        }

        /// <summary>
        /// Configuration history panels
        /// </summary>
        public ConnectingSelectPanel[] Panels
        {
            get { return _panels; }
        }
        private ConnectingSelectPanel[] _panels;

        /// <summary>
        /// Add another instance or profile
        /// </summary>
        public DelegateCommand AddConnection
        {
            get
            {
                if (_add_connection == null)
                    _add_connection = new DelegateCommand(
                        //execute
                        () =>
                        {
                            Wizard.ChangeTaskCount(+1);
                            try {
                                if (Wizard.HasInstanceSources)
                                    Wizard.CurrentPage = Wizard.InstanceSourceSelectPage;
                                else
                                    Wizard.CurrentPage = Wizard.CustomInstancePage;
                            }
                            catch (Exception ex) { Wizard.Error = ex; }
                            finally { Wizard.ChangeTaskCount(-1); }
                        });

                return _add_connection;
            }
        }
        private DelegateCommand _add_connection;

        #endregion

        #region Constructors

        /// <summary>
        /// Constructs a recent profile selection wizard page.
        /// </summary>
        /// <param name="wizard">The connecting wizard</param>
        public RecentConfigurationSelectPage(ConnectWizard wizard) :
            base(wizard)
        {
            // Create history panels.
            _panels = new ConnectingSelectPanel[Wizard.InstanceSources.Length];
            for (var source_index = (int)InstanceSourceType._start; source_index < (int)InstanceSourceType._end; source_index++)
            {
                if (Wizard.InstanceSources[source_index] is LocalInstanceSource)
                {
                    switch (Properties.Settings.Default.ConnectingProfileSelectMode)
                    {
                        case 0: _panels[source_index] = new ConnectingProfileSelectPanel(Wizard, (InstanceSourceType)source_index); break;
                        case 1: _panels[source_index] = new ConnectingInstanceSelectPanel(Wizard, (InstanceSourceType)source_index); break;
                        case 2: _panels[source_index] = new ConnectingProfileSelectPanel(Wizard, (InstanceSourceType)source_index); break;
                        case 3: _panels[source_index] = new ConnectingInstanceAndProfileSelectPanel(Wizard, (InstanceSourceType)source_index); break;
                    }
                }
                else if (
                    Wizard.InstanceSources[source_index] is DistributedInstanceSource ||
                    Wizard.InstanceSources[source_index] is FederatedInstanceSource)
                    _panels[source_index] = new ConnectingInstanceAndProfileSelectPanel(Wizard, (InstanceSourceType)source_index);
            }
        }

        #endregion

        #region Methods

        /// <inheritdoc/>
        public override void OnActivate()
        {
            base.OnActivate();

            // Synchronize selected instance => triggers profile list refresh.
            for (var source_index = (int)InstanceSourceType._start; source_index < (int)InstanceSourceType._end; source_index++)
                if (Panels[source_index] != null)
                    Panels[source_index].SelectedInstance = Wizard.InstanceSources[source_index].ConnectingInstance;
        }

        #endregion
    }
}
