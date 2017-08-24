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
                if (_select_instance_source == null)
                {
                    _select_instance_source = new DelegateCommand<Models.InstanceSourceType?>(
                        // execute
                        async param =>
                        {
                            Parent.Error = null;
                            Parent.ChangeTaskCount(+1);
                            try
                            {
                                Parent.InstanceSource = Parent.InstanceSources[(int)param];

                                if (Parent.InstanceSource is Models.FederatedInstanceSourceInfo instance_source_federated)
                                {
                                    // Set authenticating instance.
                                    Parent.Configuration.AuthenticatingInstance = new Models.InstanceInfo(instance_source_federated);

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
                                    Parent.CurrentPage = Parent.AuthenticatingInstanceSelectPage;
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
        private ICommand _select_instance_source;

        /// <summary>
        /// Select custom instance source
        /// </summary>
        public ICommand SelectCustomInstance
        {
            get
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
                                Parent.InstanceSource = Parent.InstanceSources[(int)Models.InstanceSourceType.InstituteAccess];

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
        private ICommand _select_custom_instance;

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
