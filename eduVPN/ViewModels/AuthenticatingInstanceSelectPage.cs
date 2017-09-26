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
                        async instance =>
                        {
                            Parent.ChangeTaskCount(+1);
                            try
                            {
                                // Trigger initial authorization request.
                                var authorization_task = new Task(() => instance.GetAccessToken(Window.Abort.Token), Window.Abort.Token, TaskCreationOptions.LongRunning);
                                authorization_task.Start();
                                await authorization_task;

                                // Save selected instance.
                                Parent.AuthenticatingInstance = instance;

                                // Go to (instance and) profile selection page.
                                Parent.CurrentPage = Parent.ConnectingProfileSelectPage;
                            }
                            catch (Exception ex) { Parent.Error = ex; }
                            finally { Parent.ChangeTaskCount(-1); }
                        },

                        // canExecute
                        instance => instance is Models.InstanceInfo);
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
