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
    /// Instance selection base wizard page
    /// </summary>
    public class AuthenticatingInstanceSelectPage : ConnectWizardPage
    {
        #region Properties

        /// <summary>
        /// Selected instance
        /// </summary>
        /// <remarks><c>null</c> if none selected.</remarks>
        public Models.InstanceInfo SelectedInstance
        {
            get { return _selected_instance; }
            set { if (value != _selected_instance) { _selected_instance = value; RaisePropertyChanged(); } }
        }
        private Models.InstanceInfo _selected_instance;

        /// <summary>
        /// Authorize selected instance command
        /// </summary>
        public DelegateCommand<Models.InstanceInfo> AuthorizeInstance
        {
            get
            {
                if (_authorize_instance == null)
                {
                    _authorize_instance = new DelegateCommand<Models.InstanceInfo>(
                        // execute
                        async selected_instance =>
                        {
                            Parent.ChangeTaskCount(+1);
                            try
                            {
                                // Trigger initial authorization request.
                                var authorization_task = new Task(() => selected_instance.GetAccessToken(Window.Abort.Token), Window.Abort.Token, TaskCreationOptions.LongRunning);
                                authorization_task.Start();
                                await authorization_task;

                                // Save selected instance.
                                Parent.Configuration.AuthenticatingInstance = selected_instance;

                                // Assume the same connecting instance.
                                Parent.Configuration.ConnectingInstance = Parent.Configuration.AuthenticatingInstance;

                                // Go to (instance and) profile selection page.
                                Parent.CurrentPage = Parent.ProfileSelectPage;
                            }
                            catch (Exception ex) { Parent.Error = ex; }
                            finally { Parent.ChangeTaskCount(-1); }
                        },

                        // canExecute
                        selected_instance => selected_instance != null);
                }

                return _authorize_instance;
            }
        }
        private DelegateCommand<Models.InstanceInfo> _authorize_instance;

        #endregion

        #region Constructors

        /// <summary>
        /// Constructs an instance selection wizard page.
        /// </summary>
        /// <param name="parent">The page parent</param>
        public AuthenticatingInstanceSelectPage(ConnectWizard parent) :
            base(parent)
        {
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
