/*
    eduVPN - End-user friendly VPN

    Copyright: 2017, The Commons Conservancy eduVPN Programme
    SPDX-License-Identifier: GPL-3.0+
*/

using Prism.Commands;
using System;
using System.ComponentModel;
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
        public Models.InstanceInfo SelectedInstance
        {
            get { return _selected_instance; }
            set { SetProperty(ref _selected_instance, value); }
        }
        private Models.InstanceInfo _selected_instance;

        /// <summary>
        /// Authorize selected instance command
        /// </summary>
        public DelegateCommand AuthorizeSelectedInstance
        {
            get
            {
                if (_authorize_selected_instance == null)
                {
                    _authorize_selected_instance = new DelegateCommand(
                        // execute
                        async () =>
                        {
                            Parent.ChangeTaskCount(+1);
                            try
                            {
                                // Trigger initial authorization request.
                                var authorization_task = new Task(() => SelectedInstance.GetAccessToken(Window.Abort.Token), Window.Abort.Token, TaskCreationOptions.LongRunning);
                                authorization_task.Start();
                                await authorization_task;

                                // Save selected instance.
                                Parent.AuthenticatingInstance = SelectedInstance;

                                // Go to (instance and) profile selection page.
                                Parent.CurrentPage = Parent.ConnectingProfileSelectPage;
                            }
                            catch (Exception ex) { Parent.Error = ex; }
                            finally { Parent.ChangeTaskCount(-1); }
                        },

                        // canExecute
                        () => SelectedInstance != null);

                    // Setup canExecute refreshing.
                    PropertyChanged += (object sender, PropertyChangedEventArgs e) => { if (e.PropertyName == nameof(SelectedInstance)) _authorize_selected_instance.RaiseCanExecuteChanged(); };
                }

                return _authorize_selected_instance;
            }
        }
        private DelegateCommand _authorize_selected_instance;

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

        /// <inheritdoc/>
        protected override void DoNavigateBack()
        {
            base.DoNavigateBack();

            Parent.CurrentPage = Parent.InstanceSourceSelectPage;
        }

        /// <inheritdoc/>
        protected override bool CanNavigateBack()
        {
            return true;
        }

        #endregion
    }
}
