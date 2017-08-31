/*
    eduVPN - End-user friendly VPN

    Copyright: 2017, The Commons Conservancy eduVPN Programme
    SPDX-License-Identifier: GPL-3.0+
*/

using Prism.Mvvm;
using System.Collections.ObjectModel;

namespace eduVPN.ViewModels
{
    /// <summary>
    /// Configuration history panel base class
    /// </summary>
    public class ConfigurationHistoryPanel : BindableBase
    {
        #region Properties

        /// <summary>
        /// The page parent
        /// </summary>
        public ConnectWizard Parent { get; }

        /// <summary>
        /// Selected instance source
        /// </summary>
        public Models.InstanceSourceType InstanceSourceType
        {
            get { return _instance_source_type; }
            set
            {
                if (value != _instance_source_type)
                {
                    _instance_source_type = value;
                    RaisePropertyChanged();
                    RaisePropertyChanged("InstanceSource");
                    RaisePropertyChanged("ConfigurationHistory");
                }
            }
        }
        private Models.InstanceSourceType _instance_source_type;

        /// <summary>
        /// Selected instance source
        /// </summary>
        public Models.InstanceSourceInfo InstanceSource
        {
            get { return Parent.InstanceSources[(int)_instance_source_type]; }
        }

        /// <summary>
        /// Configuration history list
        /// </summary>
        public ObservableCollection<Models.VPNConfiguration> ConfigurationHistory
        {
            get { return Parent.ConfigurationHistories[(int)_instance_source_type]; }
        }

        #endregion

        #region Constructors

        /// <summary>
        /// Constructs history panel
        /// </summary>
        /// <param name="parent">The page parent</param>
        /// <param name="instance_source_type">Instance source type</param>
        public ConfigurationHistoryPanel(ConnectWizard parent, Models.InstanceSourceType instance_source_type)
        {
            Parent = parent;
            InstanceSourceType = instance_source_type;
        }

        #endregion

        #region Methods

        /// <summary>
        /// Called when the panel is activated.
        /// </summary>
        public virtual void OnActivate()
        {
        }

        #endregion
    }
}
