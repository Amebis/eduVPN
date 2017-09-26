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
                        async instance_source_type =>
                        {
                            Parent.ChangeTaskCount(+1);
                            try
                            {
                                Parent.InstanceSourceType = instance_source_type.Value;

                                if (Parent.InstanceSource is Models.LocalInstanceSourceInfo)
                                {
                                    // With local authentication, the authenticating instance is the connecting instance.
                                    // Therefore, select "authenticating" instance.
                                    Parent.CurrentPage = Parent.AuthenticatingInstanceSelectPage;
                                }
                                else if (Parent.InstanceSource is Models.DistributedInstanceSourceInfo instance_source_distributed)
                                {
                                    // Check if we have access token for any of the instances.
                                    object authenticating_instance_lock = new object();
                                    Models.InstanceInfo authenticating_instance = null;
                                    await Task.WhenAll(Parent.InstanceSource.Select(instance =>
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
                                        Parent.AuthenticatingInstance = authenticating_instance;

                                        // Go to (instance and) profile selection page.
                                        Parent.CurrentPage = Parent.ConnectingProfileSelectPage;
                                    }
                                    else
                                        Parent.CurrentPage = Parent.AuthenticatingInstanceSelectPage;
                                }
                                else if (Parent.InstanceSource is Models.FederatedInstanceSourceInfo instance_source_federated)
                                {
                                    // Create authenticating instance.
                                    var authenticating_instance = new Models.InstanceInfo(instance_source_federated);
                                    authenticating_instance.RequestAuthorization += Parent.Instance_RequestAuthorization;

                                    // Trigger initial authorization request.
                                    var authorization_task = new Task(() => authenticating_instance.GetAccessToken(Window.Abort.Token), Window.Abort.Token, TaskCreationOptions.LongRunning);
                                    authorization_task.Start();
                                    await authorization_task;

                                    // Set authenticating instance.
                                    Parent.AuthenticatingInstance = authenticating_instance;

                                    Parent.CurrentPage = Parent.ConnectingProfileSelectPage;
                                }
                            }
                            catch (Exception ex) { Parent.Error = ex; }
                            finally { Parent.ChangeTaskCount(-1); }
                        },

                        // canExecute
                        instance_source_type =>
                            instance_source_type is Models.InstanceSourceType &&
                            Parent.InstanceSources != null &&
                            Parent.InstanceSources[(int)instance_source_type] != null);

                    // Setup canExecute refreshing.
                    // Note: Parent.InstanceSources is pseudo-static. We don't need to monitor it for changes.
                }

                return _select_instance_source;
            }
        }
        private DelegateCommand<Models.InstanceSourceType?> _select_instance_source;

        /// <summary>
        /// Select custom instance source
        /// </summary>
        public DelegateCommand SelectCustomInstance
        {
            get
            {
                if (_select_custom_instance == null)
                {
                    _select_custom_instance = new DelegateCommand(
                        // execute
                        () =>
                        {
                            Parent.ChangeTaskCount(+1);
                            try
                            {
                                // Assume the custom instance would otherwise be a part of "Institute Access" source.
                                Parent.InstanceSourceType = Models.InstanceSourceType.InstituteAccess;

                                Parent.CurrentPage = Parent.CustomInstancePage;
                            }
                            catch (Exception ex) { Parent.Error = ex; }
                            finally { Parent.ChangeTaskCount(-1); }
                        });
                }

                return _select_custom_instance;
            }
        }
        private DelegateCommand _select_custom_instance;

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

        #region Members

        protected override void DoNavigateBack()
        {
            base.DoNavigateBack();

            Parent.CurrentPage = Parent.RecentConfigurationSelectPage;
        }

        protected override bool CanNavigateBack()
        {
            return Parent.StartingPage != this;
        }

        #endregion
    }
}
