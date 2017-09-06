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
    /// Custom instance source entry wizard page
    /// </summary>
    public class CustomInstancePage : ConnectWizardPage
    {
        #region Properties

        /// <summary>
        /// Instance base URI
        /// </summary>
        public string BaseURI
        {
            get { return _uri; }
            set
            {
                if (value != _uri)
                {
                    _uri = value;
                    RaisePropertyChanged();
                    SelectCustomInstance.RaiseCanExecuteChanged();
                }
            }
        }
        private string _uri;

        /// <summary>
        /// Authorize other instance command
        /// </summary>
        public DelegateCommand SelectCustomInstance
        {
            get
            {
                if (_select_custom_instance == null)
                {
                    _select_custom_instance = new DelegateCommand(
                        // execute
                        async () =>
                        {
                            Parent.ChangeTaskCount(+1);
                            try
                            {
                                var selected_instance = new Models.InstanceInfo(new Uri(BaseURI));
                                selected_instance.RequestAuthorization += Parent.Instance_RequestAuthorization;

                                // Trigger initial authorization request.
                                var authorization_task = new Task(() => selected_instance.GetAccessToken(Window.Abort.Token), Window.Abort.Token, TaskCreationOptions.LongRunning);
                                authorization_task.Start();
                                await authorization_task;

                                // Set authentication instance.
                                Parent.Configuration.AuthenticatingInstance = selected_instance;

                                // Connecting instance will be the same as authenticating.
                                Parent.Configuration.ConnectingInstance = Parent.Configuration.AuthenticatingInstance;

                                // Go to (instance and) profile selection page.
                                Parent.CurrentPage = Parent.ConnectingProfileSelectPage;
                            }
                            catch (Exception ex) { Parent.Error = ex; }
                            finally { Parent.ChangeTaskCount(-1); }
                        },

                        // canExecute
                        () =>
                        {
                            try { new Uri(BaseURI); }
                            catch (Exception) { return false; }
                            return true;
                        });
                }

                return _select_custom_instance;
            }
        }
        private DelegateCommand _select_custom_instance;

        #endregion

        #region Constructors

        /// <summary>
        /// Constructs a view model.
        /// </summary>
        public CustomInstancePage(ConnectWizard parent) :
            base(parent)
        {
            BaseURI = "https://";
        }

        #endregion

        #region Methods

        protected override void DoNavigateBack()
        {
            base.DoNavigateBack();

            Parent.CurrentPage = Parent.InstanceSourceSelectPage;
        }

        protected override bool CanNavigateBack()
        {
            return true;
        }

        #endregion
    }
}
