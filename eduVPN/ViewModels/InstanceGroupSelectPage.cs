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
    /// Instance group selection wizard page
    /// </summary>
    public class InstanceGroupSelectPage : ConnectWizardPage
    {
        #region Properties

        /// <summary>
        /// Select instance group
        /// </summary>
        public ICommand SelectInstanceGroup
        {
            get
            {
                if (_select_instance_group == null)
                {
                    _select_instance_group = new DelegateCommand<Models.InstanceGroupInfo>(
                        // execute
                        async param =>
                        {
                            Parent.Error = null;
                            Parent.ChangeTaskCount(+1);
                            try
                            {
                                Parent.InstanceGroup = param;

                                if (Parent.InstanceGroup is Models.FederatedInstanceGroupInfo instance_group)
                                {
                                    // Set authenticating instance.
                                    Parent.Configuration.AuthenticatingInstance = new Models.InstanceInfo(instance_group);

                                    // Restore the access token from the settings.
                                    Parent.Configuration.AccessToken = await Parent.Configuration.AuthenticatingInstance.GetAccessTokenAsync(ConnectWizard.Abort.Token);

                                    // Reset connecting instance.
                                    Parent.Configuration.ConnectingInstance = null;

                                    if (Parent.Configuration.AccessToken == null)
                                        Parent.CurrentPage = Parent.AuthorizationPage;
                                    else
                                        Parent.CurrentPage = Parent.ProfileSelectPage;
                                }
                                else
                                    Parent.CurrentPage = Parent.InstanceSelectPage;
                            }
                            catch (Exception ex) { Parent.Error = ex; }
                            finally { Parent.ChangeTaskCount(-1); }
                        },

                        // canExecute
                        param => param != null);
                }
                return _select_instance_group;
            }
        }
        private ICommand _select_instance_group;

        /// <summary>
        /// Select custom instance group
        /// </summary>
        public ICommand SelectCustomInstanceGroup
        {
            get
            {
                if (_select_custom_instance_group == null)
                {
                    _select_custom_instance_group = new DelegateCommand<Models.InstanceGroupInfo>(
                        // execute
                        param =>
                        {
                            Parent.Error = null;
                            Parent.ChangeTaskCount(+1);
                            try
                            {
                                Parent.InstanceGroup = null;
                                Parent.CurrentPage = Parent.CustomInstanceGroupPage;
                            }
                            catch (Exception ex) { Parent.Error = ex; }
                            finally { Parent.ChangeTaskCount(-1); }
                        },

                        // canExecute
                        param => true);
                }
                return _select_custom_instance_group;
            }
        }
        private ICommand _select_custom_instance_group;

        #endregion

        #region Constructors

        /// <summary>
        /// Constructs an instance group selection wizard page.
        /// </summary>
        /// <param name="parent">The page parent</param>
        public InstanceGroupSelectPage(ConnectWizard parent) :
            base(parent)
        {
        }

        #endregion
    }
}
