/*
    eduVPN - End-user friendly VPN

    Copyright: 2017, The Commons Conservancy eduVPN Programme
    SPDX-License-Identifier: GPL-3.0+
*/

using eduVPN.Models;
using eduVPN.ViewModels.Windows;
using Prism.Commands;
using System;
using System.ComponentModel;
using System.Linq;

namespace eduVPN.ViewModels.Pages
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
        public Instance SelectedInstance
        {
            get { return _selected_instance; }
            set { SetProperty(ref _selected_instance, value); }
        }
        private Instance _selected_instance;

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
                                var authenticating_instance = SelectedInstance;
                                await Parent.TriggerAuthorizationAsync(authenticating_instance);
                                Parent.InstanceSource.AuthenticatingInstance = authenticating_instance;

                                // Assume the same connecting instance.
                                var connecting_instance = Parent.InstanceSource.ConnectingInstanceList.FirstOrDefault(inst => inst.Base.AbsoluteUri == authenticating_instance.Base.AbsoluteUri);
                                if (connecting_instance == null)
                                    Parent.InstanceSource.ConnectingInstanceList.Add(authenticating_instance);
                                Parent.InstanceSource.ConnectingInstance = authenticating_instance;

                                // Go to (instance and) profile selection page.
                                switch (Properties.Settings.Default.ConnectingProfileSelectMode)
                                {
                                    case 0: Parent.CurrentPage = Parent.ConnectingProfileSelectPage; break;
                                    case 1: Parent.CurrentPage = Parent.RecentConfigurationSelectPage; break;
                                    case 2: Parent.CurrentPage = Parent.ConnectingProfileSelectPage; break;
                                    case 3: Parent.CurrentPage = Parent.RecentConfigurationSelectPage; break;
                                }
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
