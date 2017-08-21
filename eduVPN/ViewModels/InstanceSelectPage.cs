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
    /// Instance selection base wizard page
    /// </summary>
    public class InstanceSelectPage : ConnectWizardPage
    {
        #region Properties

        /// <summary>
        /// Selected instance
        /// </summary>
        /// <remarks><c>null</c> if none selected.</remarks>
        public Models.InstanceInfo SelectedInstance
        {
            get { return _selected_instance; }
            set
            {
                _selected_instance = value;
                RaisePropertyChanged();
                ((DelegateCommandBase)AuthorizeSelectedInstance).RaiseCanExecuteChanged();
            }
        }
        private Models.InstanceInfo _selected_instance;

        /// <summary>
        /// Authorize selected instance command
        /// </summary>
        public ICommand AuthorizeSelectedInstance
        {
            get
            {
                if (_authorize_instance == null)
                {
                    _authorize_instance = new DelegateCommand(
                        // execute
                        async () =>
                        {
                            Error = null;
                            TaskCount++;
                            try
                            {
                                // Save selected instance.
                                Parent.AuthenticatingInstance = SelectedInstance;

                                if (SelectedInstance.IsCustom)
                                    Parent.CurrentPage = Parent.CustomInstancePage;
                                else
                                {
                                    // Restore the access token from the settings.
                                    Parent.AccessToken = await Parent.AuthenticatingInstance.GetAccessTokenAsync(ConnectWizard.Abort.Token);

                                    if (Parent.InstanceGroup is Models.LocalInstanceGroupInfo)
                                    {
                                        // Connecting instance will be the same as authenticating.
                                        Parent.ConnectingInstance = Parent.AuthenticatingInstance;
                                    }
                                    else if (Parent.InstanceGroup is Models.DistributedInstanceGroupInfo)
                                    {
                                        // Connecting instance will not (necessarry) be the same as authenticating.
                                        Parent.ConnectingInstance = null;
                                    } else
                                        throw new NotImplementedException();

                                    if (Parent.AccessToken == null)
                                        Parent.CurrentPage = Parent.AuthorizationPage;
                                    else if (Parent.ConnectingInstance == null)
                                        Parent.CurrentPage = Parent.InstanceAndProfileSelectPage;
                                    else
                                        Parent.CurrentPage = Parent.ProfileSelectPage;
                                }
                            }
                            catch (Exception ex) { Error = ex; }
                            finally { TaskCount--; }
                        },

                        // canExecute
                        () => SelectedInstance != null);
                }
                return _authorize_instance;
            }
        }
        private ICommand _authorize_instance;

        #endregion

        #region Constructors

        /// <summary>
        /// Constructs an instance selection wizard page.
        /// </summary>
        /// <param name="parent">The page parent</param>
        public InstanceSelectPage(ConnectWizard parent) :
            base(parent)
        {
        }

        #endregion

        #region Methods

        public override void OnActivate()
        {
            base.OnActivate();

            // Reset selected instance, to prevent automatic continuation to
            // CustomInstance/Authorization page.
            SelectedInstance = null;
        }

        protected override void DoNavigateBack()
        {
            Parent.CurrentPage = Parent.AccessTypePage;
        }

        protected override bool CanNavigateBack()
        {
            return true;
        }

        #endregion
    }
}
