/*
    eduVPN - End-user friendly VPN

    Copyright: 2017, The Commons Conservancy eduVPN Programme
    SPDX-License-Identifier: GPL-3.0+
*/

using eduVPN.Models;
using eduVPN.ViewModels.Windows;
using Prism.Commands;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace eduVPN.ViewModels.Pages
{
    /// <summary>
    /// Instance source selection wizard page
    /// </summary>
    public class InstanceSourceSelectPage : ConnectWizardPage
    {
        #region Properties

        /// <summary>
        /// The page title
        /// </summary>
        public override string Title
        {
            get { return Resources.Strings.InstanceSourceSelectPageTitle; }
        }

        /// <summary>
        /// The page description
        /// </summary>
        public override string Description
        {
            get { return Resources.Strings.InstanceSourceSelectPageDescription; }
        }

        /// <summary>
        /// Select instance source command
        /// </summary>
        public DelegateCommand<InstanceSourceType?> SelectInstanceSource
        {
            get
            {
                if (_select_instance_source == null)
                {
                    _select_instance_source = new DelegateCommand<InstanceSourceType?>(
                        // execute
                        async instance_source_type =>
                        {
                            Wizard.ChangeTaskCount(+1);
                            try
                            {
                                Wizard.InstanceSourceType = instance_source_type.Value;

                                if (Wizard.InstanceSource is LocalInstanceSource)
                                {
                                    // With local authentication, the authenticating instance is the connecting instance.
                                    // Therefore, select "authenticating" instance.
                                    Wizard.CurrentPage = Wizard.AuthenticatingInstanceSelectPage;
                                }
                                else if (Wizard.InstanceSource is DistributedInstanceSource instance_source_distributed)
                                {
                                    // Check if we have saved access token for any of the instances.
                                    object authenticating_instance_lock = new object();
                                    Instance authenticating_instance = null;
                                    await Task.WhenAll(Wizard.InstanceSource.InstanceList.Select(instance =>
                                    {
                                        var authorization_task = new Task(
                                            () =>
                                            {
                                                var e = new RequestAuthorizationEventArgs("config") { SourcePolicy = RequestAuthorizationEventArgs.SourcePolicyType.SavedOnly };
                                                Wizard.Instance_RequestAuthorization(instance, e);
                                                if (e.AccessToken != null)
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
                                        instance_source_distributed.AuthenticatingInstance = authenticating_instance;
                                        instance_source_distributed.ConnectingInstance = authenticating_instance;

                                        // Go to (instance and) profile selection page.
                                        Wizard.CurrentPage = Wizard.RecentConfigurationSelectPage;
                                    }
                                    else
                                        Wizard.CurrentPage = Wizard.AuthenticatingInstanceSelectPage;
                                }
                                else if (Wizard.InstanceSource is FederatedInstanceSource instance_source_federated)
                                {
                                    // Trigger initial authorization request.
                                    await Wizard.TriggerAuthorizationAsync(instance_source_federated.AuthenticatingInstance);

                                    Wizard.CurrentPage = Wizard.RecentConfigurationSelectPage;
                                }
                            }
                            catch (Exception ex) { Wizard.Error = ex; }
                            finally { Wizard.ChangeTaskCount(-1); }
                        },

                        // canExecute
                        instance_source_type =>
                            instance_source_type is InstanceSourceType &&
                            Wizard.InstanceSources != null &&
                            Wizard.InstanceSources[(int)instance_source_type] != null);

                    // Setup canExecute refreshing.
                    // Note: Wizard.InstanceSources is pseudo-static. We don't need to monitor it for changes.
                }

                return _select_instance_source;
            }
        }
        private DelegateCommand<InstanceSourceType?> _select_instance_source;

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
                            Wizard.ChangeTaskCount(+1);
                            try
                            {
                                // Assume the custom instance would otherwise be a part of "Institute Access" source.
                                Wizard.InstanceSourceType = InstanceSourceType.InstituteAccess;

                                Wizard.CurrentPage = Wizard.CustomInstancePage;
                            }
                            catch (Exception ex) { Wizard.Error = ex; }
                            finally { Wizard.ChangeTaskCount(-1); }
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
        /// <param name="wizard">The connecting wizard</param>
        public InstanceSourceSelectPage(ConnectWizard wizard) :
            base(wizard)
        {
        }

        #endregion

        #region Members

        /// <inheritdoc/>
        protected override void DoNavigateBack()
        {
            base.DoNavigateBack();

            Wizard.CurrentPage = Wizard.RecentConfigurationSelectPage;
        }

        /// <inheritdoc/>
        protected override bool CanNavigateBack()
        {
            return Wizard.StartingPage != this;
        }

        #endregion
    }
}
