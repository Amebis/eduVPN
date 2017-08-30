/*
    eduVPN - End-user friendly VPN

    Copyright: 2017, The Commons Conservancy eduVPN Programme
    SPDX-License-Identifier: GPL-3.0+
*/

using Prism.Commands;
using System;
using System.Windows.Input;

namespace eduVPN.ViewModels
{
    /// <summary>
    /// Instance source selection wizard page
    /// </summary>
    public class InstanceSourceSelectPage : ConnectWizardPage
    {
        #region Properties

        /// <summary>
        /// Select instance source
        /// </summary>
        public ICommand SelectInstanceSource
        {
            get
            {
                lock (_select_instance_source_lock)
                {
                    if (_select_instance_source == null)
                    {
                        _select_instance_source = new DelegateCommand<Models.InstanceSourceType?>(
                            // execute
                            param =>
                            {
                                Parent.Error = null;
                                Parent.ChangeTaskCount(+1);
                                try
                                {
                                    Parent.InstanceSourceType = param.Value;

                                    if (Parent.InstanceSource is Models.LocalInstanceSourceInfo)
                                    {
                                        Parent.CurrentPage = Parent.AuthenticatingInstanceSelectPage;
                                    }
                                    else if (Parent.InstanceSource is Models.DistributedInstanceSourceInfo instance_source_distributed)
                                    {
                                        // TODO: Check for any available token and skip authentication instance select page when any usable token found. (issue #11)

                                        Parent.CurrentPage = Parent.AuthenticatingInstanceSelectPage;
                                    }
                                    else if(Parent.InstanceSource is Models.FederatedInstanceSourceInfo instance_source_federated)
                                    {
                                        // Set authenticating instance.
                                        Parent.Configuration.AuthenticatingInstance = new Models.InstanceInfo(instance_source_federated);
                                        Parent.Configuration.AuthenticatingInstance.RequestAuthorization += Parent.Instance_RequestAuthorization;

                                        // TODO: Add initial authorization request. (issue #15)

                                        // Reset connecting instance.
                                        Parent.Configuration.ConnectingInstance = null;

                                        Parent.CurrentPage = Parent.ProfileSelectPage;
                                    }
                                }
                                catch (Exception ex) { Parent.Error = ex; }
                                finally { Parent.ChangeTaskCount(-1); }
                            },

                            // canExecute
                            param =>
                                param != null &&
                                Parent.InstanceSources != null &&
                                Parent.InstanceSources[(int)param] != null);
                    }

                    return _select_instance_source;
                }
            }
        }
        private ICommand _select_instance_source;
        private object _select_instance_source_lock = new object();

        /// <summary>
        /// Select custom instance source
        /// </summary>
        public ICommand SelectCustomInstance
        {
            get
            {
                lock (_select_custom_instance_lock)
                {
                    if (_select_custom_instance == null)
                    {
                        _select_custom_instance = new DelegateCommand<Models.InstanceSourceInfo>(
                            // execute
                            param =>
                            {
                                Parent.Error = null;
                                Parent.ChangeTaskCount(+1);
                                try
                                {
                                    // Assume the custom instance would otherwise be a part of "Institute Access" source.
                                    Parent.InstanceSourceType = Models.InstanceSourceType.InstituteAccess;

                                    Parent.CurrentPage = Parent.CustomInstancePage;
                                }
                                catch (Exception ex) { Parent.Error = ex; }
                                finally { Parent.ChangeTaskCount(-1); }
                            },

                            // canExecute
                            param => true);
                    }

                    return _select_custom_instance;
                }
            }
        }
        private ICommand _select_custom_instance;
        private object _select_custom_instance_lock = new object();

        #endregion

        #region Constructors

        /// <summary>
        /// Constructs an instance source selection wizard page.
        /// </summary>
        /// <param name="parent">The page parent</param>
        public InstanceSourceSelectPage(ConnectWizard parent) :
            base(parent)
        {
        }

        #endregion
    }
}
