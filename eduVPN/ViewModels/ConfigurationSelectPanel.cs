﻿/*
    eduVPN - End-user friendly VPN

    Copyright: 2017, The Commons Conservancy eduVPN Programme
    SPDX-License-Identifier: GPL-3.0+
*/

using Prism.Commands;
using System;
using System.Collections.Specialized;
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
                                var param = new ConnectWizard.StartSessionParams(
                                    InstanceSourceType,
                                    (Models.VPNConfiguration)configuration.Clone());
                                if (Parent.StartSession.CanExecute(param))
                                    Parent.StartSession.Execute(param);
                            }
                            catch (Exception ex) { Parent.Error = ex; }
                            finally { Parent.ChangeTaskCount(-1); }
                        },

                        // canExecute
                        configuration => configuration is Models.VPNConfiguration);

                return _connect_configuration;
            }
        }
        private DelegateCommand<Models.VPNConfiguration> _connect_configuration;

        /// <summary>
        /// Forget configuration command
        /// </summary>
        public DelegateCommand<Models.VPNConfiguration> ForgetConfiguration
        {
            get
            {
                if (_forget_configuration == null)
                {
                    _forget_configuration = new DelegateCommand<Models.VPNConfiguration>(
                        // execute
                        configuration =>
                        {
                            Parent.ChangeTaskCount(+1);
                            try { ConfigurationHistory.Remove(configuration); }
                            catch (Exception ex) { Parent.Error = ex; }
                            finally { Parent.ChangeTaskCount(-1); }
                        },

                        // canExecute
                        configuration =>
                            configuration is Models.VPNConfiguration &&
                            ConfigurationHistory.IndexOf(configuration) >= 0 &&
                            !Parent.Sessions.Any(session => session.Configuration.Equals(configuration)));

                    // Setup canExecute refreshing.
                    ConfigurationHistory.CollectionChanged += (object sender, NotifyCollectionChangedEventArgs e) => _forget_configuration.RaiseCanExecuteChanged();
                    Parent.Sessions.CollectionChanged += (object sender, NotifyCollectionChangedEventArgs e) => _forget_configuration.RaiseCanExecuteChanged();
                }

                return _forget_configuration;
            }
        }
        private DelegateCommand<Models.VPNConfiguration> _forget_configuration;

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
