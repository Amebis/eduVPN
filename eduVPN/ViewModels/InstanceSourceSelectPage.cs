/*
    eduVPN - End-user friendly VPN

    Copyright: 2017, The Commons Conservancy eduVPN Programme
    SPDX-License-Identifier: GPL-3.0+
*/

using Prism.Commands;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace eduVPN.ViewModels
{
    /// <summary>
    /// Instance source selection wizard page
    /// </summary>
    public class InstanceSourceSelectPage : ConnectWizardPage
    {
        #region Properties

        /// <summary>
        /// Select instance source command
        /// </summary>
        public DelegateCommand<Models.InstanceSourceType?> SelectInstanceSource
        {
            get
            {
                if (_select_instance_source == null)
                {
                    _select_instance_source = new DelegateCommand<Models.InstanceSourceType?>(
                        // execute
                        async param =>
                        {
                            Parent.ChangeTaskCount(+1);
                            try
                            {
                                Parent.Configuration = new Models.VPNConfiguration()
                                {
                                    InstanceSourceType = param.Value,
                                    InstanceSource = Parent.InstanceSources[(int)param.Value]
                                };

                                if (Parent.Configuration.InstanceSource is Models.LocalInstanceSourceInfo)
                                {
                                    // With local authentication, the authenticating instance is the connecting instance.
                                    // Therefore, select "authenticating" instance.
                                    Parent.CurrentPage = Parent.AuthenticatingInstanceSelectPage;
                                }
                                else if (Parent.Configuration.InstanceSource is Models.DistributedInstanceSourceInfo instance_source_distributed)
                                {
                                    // Check if we have access token for any of the instances.
                                    object authenticating_instance_lock = new object();
                                    Models.InstanceInfo authenticating_instance = null;
                                    await Task.WhenAll(Parent.Configuration.InstanceSource.Select(instance =>
                                    {
                                        var authorization_task = new Task(
                                            () =>
                                            {
                                                if (instance.PeekAccessToken(Window.Abort.Token) != null)
                                                    lock (authenticating_instance_lock)
                                                        authenticating_instance = instance;
                                            },
                                            Window.Abort.Token,
                                            TaskCreationOptions.LongRunning);
                                        authorization_task.Start();
                                        return authorization_task;
                                    }));

                                    if (authenticating_instance != null)
                                    {
                                        // Save found instance.
                                        Parent.Configuration.AuthenticatingInstance = authenticating_instance;

                                        // Assume the same connecting instance.
                                        Parent.Configuration.ConnectingInstance = Parent.Configuration.AuthenticatingInstance;

                                        // Go to (instance and) profile selection page.
                                        Parent.CurrentPage = Parent.ConnectingProfileSelectPage;
                                    }
                                    else
                                        Parent.CurrentPage = Parent.AuthenticatingInstanceSelectPage;
                                }
                                else if(Parent.Configuration.InstanceSource is Models.FederatedInstanceSourceInfo instance_source_federated)
                                {
                                    // Create authenticating instance.
                                    var authenticating_instance = new Models.InstanceInfo(instance_source_federated);
                                    authenticating_instance.RequestAuthorization += Parent.Instance_RequestAuthorization;

                                    // Trigger initial authorization request.
                                    var authorization_task = new Task(() => authenticating_instance.GetAccessToken(Window.Abort.Token), Window.Abort.Token, TaskCreationOptions.LongRunning);
                                    authorization_task.Start();
                                    await authorization_task;

                                    // Set authenticating instance.
                                    Parent.Configuration.AuthenticatingInstance = authenticating_instance;

                                    // Reset connecting instance.
                                    Parent.Configuration.ConnectingInstance = null;

                                    Parent.CurrentPage = Parent.ConnectingProfileSelectPage;
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
        private DelegateCommand<Models.InstanceSourceType?> _select_instance_source;

        /// <summary>
        /// Select custom instance source
        /// </summary>
        public DelegateCommand<Models.InstanceSourceInfo> SelectCustomInstance
        {
            get
            {
                if (_select_custom_instance == null)
                {
                    _select_custom_instance = new DelegateCommand<Models.InstanceSourceInfo>(
                        // execute
                        param =>
                        {
                            Parent.ChangeTaskCount(+1);
                            try
                            {
                                // Assume the custom instance would otherwise be a part of "Institute Access" source.
                                Parent.Configuration = new Models.VPNConfiguration()
                                {
                                    InstanceSourceType = Models.InstanceSourceType.InstituteAccess,
                                    InstanceSource = Parent.InstanceSources[(int)Models.InstanceSourceType.InstituteAccess]
                                };

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
        private DelegateCommand<Models.InstanceSourceInfo> _select_custom_instance;

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
