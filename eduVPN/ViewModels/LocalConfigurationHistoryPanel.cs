/*
    eduVPN - End-user friendly VPN

    Copyright: 2017, The Commons Conservancy eduVPN Programme
    SPDX-License-Identifier: GPL-3.0+
*/

using Prism.Commands;
using System.Threading.Tasks;

namespace eduVPN.ViewModels
{
    /// <summary>
    /// Locally authenticated configuration history panel
    /// </summary>
    public class LocalConfigurationHistoryPanel : ConfigurationHistoryPanel
    {
        #region Properties

        /// <summary>
        /// Selected VPN configuration
        /// </summary>
        /// <remarks><c>null</c> if none selected.</remarks>
        public Models.VPNConfiguration SelectedConfiguration
        {
            get { return _selected_configuration; }
            set
            {
                if (value != _selected_configuration)
                {
                    _selected_configuration = value;
                    RaisePropertyChanged();
                    ConnectSelectedConfiguration.RaiseCanExecuteChanged();
                }
            }
        }
        private Models.VPNConfiguration _selected_configuration;

        /// <summary>
        /// Connect selected configuration command
        /// </summary>
        public DelegateCommand ConnectSelectedConfiguration
        {
            get
            {
                lock (_connect_selected_configuration_lock)
                {
                    if (_connect_selected_configuration == null)
                        _connect_selected_configuration = new DelegateCommand(
                            // execute
                            async () =>
                            {
                                // Trigger initial authorization request.
                                var authorization_task = new Task(() => SelectedConfiguration.AuthenticatingInstance.GetAccessToken(Window.Abort.Token), Window.Abort.Token, TaskCreationOptions.LongRunning);
                                authorization_task.Start();
                                await authorization_task;

                                // Set selected configuration.
                                Parent.Configuration = SelectedConfiguration;

                                // Reset selected configuration, to prevent repetitive triggering.
                                SelectedConfiguration = null;

                                // Go to status page.
                                Parent.CurrentPage = Parent.StatusPage;
                            },

                            // canExecute
                            () => SelectedConfiguration != null);

                    return _connect_selected_configuration;
                }
            }
        }
        private DelegateCommand _connect_selected_configuration;
        private object _connect_selected_configuration_lock = new object();

        #endregion

        #region Constructors

        /// <summary>
        /// Constructs history panel
        /// </summary>
        /// <param name="parent">The page parent</param>
        /// <param name="instance_source_type">Instance source type</param>
        public LocalConfigurationHistoryPanel(ConnectWizard parent, Models.InstanceSourceType instance_source_type) :
            base(parent, instance_source_type)
        {
        }

        #endregion
    }
}
