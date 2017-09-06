/*
    eduVPN - End-user friendly VPN

    Copyright: 2017, The Commons Conservancy eduVPN Programme
    SPDX-License-Identifier: GPL-3.0+
*/

using Prism.Commands;
using System;
using System.Threading.Tasks;

namespace eduVPN.ViewModels
{
    /// <summary>
    /// Locally authenticated configuration history panel
    /// </summary>
    public class ConfigurationSelectPanel : ConfigurationSelectBasePanel
    {
        #region Properties

        /// <summary>
        /// Selected VPN configuration
        /// </summary>
        /// <remarks><c>null</c> if none selected.</remarks>
        public Models.VPNConfiguration SelectedConfiguration
        {
            get { return _selected_configuration; }
            set { if (value != _selected_configuration) { _selected_configuration = value; RaisePropertyChanged(); } }
        }
        private Models.VPNConfiguration _selected_configuration;

        /// <summary>
        /// Connect configuration command
        /// </summary>
        public DelegateCommand<Models.VPNConfiguration> ConnectConfiguration
        {
            get
            {
                if (_connect_configuration == null)
                    _connect_configuration = new DelegateCommand<Models.VPNConfiguration>(
                        // execute
                        async configuration =>
                        {
                            Parent.ChangeTaskCount(+1);
                            try
                            {
                                // Trigger initial authorization request.
                                var authorization_task = new Task(() => configuration.AuthenticatingInstance.GetAccessToken(Window.Abort.Token), Window.Abort.Token, TaskCreationOptions.LongRunning);
                                authorization_task.Start();
                                await authorization_task;

                                // Start VPN session.
                                if (Parent.StartSession.CanExecute(configuration))
                                    Parent.StartSession.Execute(configuration);
                            }
                            catch (Exception ex) { Parent.Error = ex; }
                            finally { Parent.ChangeTaskCount(-1); }
                        },

                        // canExecute
                        configuration => configuration != null);

                return _connect_configuration;
            }
        }
        private DelegateCommand<Models.VPNConfiguration> _connect_configuration;

        #endregion

        #region Constructors

        /// <summary>
        /// Constructs history panel
        /// </summary>
        /// <param name="parent">The page parent</param>
        /// <param name="instance_source_type">Instance source type</param>
        public ConfigurationSelectPanel(ConnectWizard parent, Models.InstanceSourceType instance_source_type) :
            base(parent, instance_source_type)
        {
        }

        #endregion
    }
}
