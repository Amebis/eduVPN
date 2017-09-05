/*
    eduVPN - End-user friendly VPN

    Copyright: 2017, The Commons Conservancy eduVPN Programme
    SPDX-License-Identifier: GPL-3.0+
*/

using Prism.Commands;
using System;

namespace eduVPN.ViewModels
{
    /// <summary>
    /// Recent profile selection wizard page
    /// </summary>
    public class RecentProfileSelectPage : ConnectWizardPage
    {
        #region Properties

        /// <summary>
        /// Configuration history panels
        /// </summary>
        public ConfigurationHistoryPanel[] ConfigurationHistoryPanels
        {
            get { return _configuration_history_panels; }
        }
        private ConfigurationHistoryPanel[] _configuration_history_panels;

        /// <summary>
        /// Add another profile
        /// </summary>
        public DelegateCommand AddAnotherProfile
        {
            get
            {
                if (_add_another_profile == null)
                    _add_another_profile = new DelegateCommand(
                        //execute
                        () =>
                        {
                            Parent.ChangeTaskCount(+1);
                            try { Parent.CurrentPage = Parent.InstanceSourceSelectPage; }
                            catch (Exception ex) { Parent.Error = ex; }
                            finally { Parent.ChangeTaskCount(-1); }
                        });

                return _add_another_profile;
            }
        }
        private DelegateCommand _add_another_profile;

        #endregion

        #region Constructors

        /// <summary>
        /// Constructs a recent profile selection wizard page.
        /// </summary>
        /// <param name="parent">The page parent</param>
        public RecentProfileSelectPage(ConnectWizard parent) :
            base(parent)
        {
            // Create history panels.
            var source_type_length = (int)Models.InstanceSourceType._end;
            _configuration_history_panels = new ConfigurationHistoryPanel[Parent.InstanceSources.Length];
            for (var i = (int)Models.InstanceSourceType._start; i < source_type_length; i++)
            {
                if (Parent.InstanceSources[i] is Models.LocalInstanceSourceInfo)
                    _configuration_history_panels[i] = new LocalConfigurationHistoryPanel(Parent, (Models.InstanceSourceType)i);
                else if (
                    Parent.InstanceSources[i] is Models.DistributedInstanceSourceInfo ||
                    Parent.InstanceSources[i] is Models.FederatedInstanceSourceInfo)
                    _configuration_history_panels[i] = new ConnectingInstanceAndProfileSelectPanel(Parent, (Models.InstanceSourceType)i);
            }
        }

        #endregion

        #region Methods

        public override void OnActivate()
        {
            base.OnActivate();

            for (var i = (int)Models.InstanceSourceType._start; i < (int)Models.InstanceSourceType._end; i++)
            {
                if (_configuration_history_panels[i] != null)
                    _configuration_history_panels[i].OnActivate();
            }
        }

        #endregion
    }
}
