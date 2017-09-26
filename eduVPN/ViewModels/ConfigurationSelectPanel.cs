/*
    eduVPN - End-user friendly VPN

    Copyright: 2017, The Commons Conservancy eduVPN Programme
    SPDX-License-Identifier: GPL-3.0+
*/

using Prism.Commands;
using System;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
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
        /// Currently selected configuration
        /// </summary>
        public Models.VPNConfiguration SelectedConfiguration
        {
            get { return _selected_configuration; }
            set { if (value != _selected_configuration) { _selected_configuration = value; RaisePropertyChanged(); } }
        }
        private Models.VPNConfiguration _selected_configuration;

        /// <summary>
        /// Connect selected configuration command
        /// </summary>
        public DelegateCommand ConnectSelectedConfiguration
        {
            get
            {
                if (_connect_selected_configuration == null)
                {
                    _connect_selected_configuration = new DelegateCommand(
                        // execute
                        async () =>
                        {
                            Parent.ChangeTaskCount(+1);
                            try
                            {
                                // Trigger initial authorization request.
                                var authorization_task = new Task(() => SelectedConfiguration.AuthenticatingInstance.GetAccessToken(Window.Abort.Token), Window.Abort.Token, TaskCreationOptions.LongRunning);
                                authorization_task.Start();
                                await authorization_task;

                                // Start VPN session.
                                var param = new ConnectWizard.StartSessionParams(
                                    InstanceSourceType,
                                    (Models.VPNConfiguration)SelectedConfiguration.Clone());
                                if (Parent.StartSession.CanExecute(param))
                                    Parent.StartSession.Execute(param);
                            }
                            catch (Exception ex) { Parent.Error = ex; }
                            finally { Parent.ChangeTaskCount(-1); }
                        },

                        // canExecute
                        () => SelectedConfiguration != null);

                    // Setup canExecute refreshing.
                    PropertyChanged += (object sender, PropertyChangedEventArgs e) => { if (e.PropertyName == "SelectedConfiguration") _connect_selected_configuration.RaiseCanExecuteChanged(); };
                }

                return _connect_selected_configuration;
            }
        }
        private DelegateCommand _connect_selected_configuration;

        /// <summary>
        /// Forget selected configuration command
        /// </summary>
        public DelegateCommand ForgetSelectedConfiguration
        {
            get
            {
                if (_forget_selected_configuration == null)
                {
                    _forget_selected_configuration = new DelegateCommand(
                        // execute
                        () =>
                        {
                            Parent.ChangeTaskCount(+1);
                            try { ConfigurationHistory.Remove(SelectedConfiguration); }
                            catch (Exception ex) { Parent.Error = ex; }
                            finally { Parent.ChangeTaskCount(-1); }
                        },

                        // canExecute
                        () =>
                            SelectedConfiguration != null &&
                            ConfigurationHistory.IndexOf(SelectedConfiguration) >= 0 &&
                            !Parent.Sessions.Any(session => session.Configuration.Equals(SelectedConfiguration)));

                    // Setup canExecute refreshing.
                    PropertyChanged += (object sender, PropertyChangedEventArgs e) => { if (e.PropertyName == "SelectedConfiguration") _forget_selected_configuration.RaiseCanExecuteChanged(); };
                    ConfigurationHistory.CollectionChanged += (object sender, NotifyCollectionChangedEventArgs e) => _forget_selected_configuration.RaiseCanExecuteChanged();
                    Parent.Sessions.CollectionChanged += (object sender, NotifyCollectionChangedEventArgs e) => _forget_selected_configuration.RaiseCanExecuteChanged();
                }

                return _forget_selected_configuration;
            }
        }
        private DelegateCommand _forget_selected_configuration;

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
