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
    /// Custom instance entry wizard page
    /// </summary>
    public class CustomInstancePageViewModel : ConnectWizardPageViewModel
    {
        #region Properties

        /// <summary>
        /// Instance URI
        /// </summary>
        public string InstanceURI
        {
            get { return _instance_uri; }
            set
            {
                if (value != _instance_uri)
                {
                    _instance_uri = value;
                    RaisePropertyChanged();
                    ((DelegateCommandBase)AuthorizeCustomInstance).RaiseCanExecuteChanged();
                }
            }
        }
        private string _instance_uri;

        /// <summary>
        /// Authorize Other Instance Command
        /// </summary>
        public ICommand AuthorizeCustomInstance
        {
            get
            {
                if (_authorize_instance == null)
                {
                    _authorize_instance = new DelegateCommand(
                        // execute
                        () => {
                            Parent.InstanceURI = new Uri(InstanceURI);
                            Parent.IsCustomInstance = true;
                            Parent.CurrentPage = Parent.AuthorizationPage;
                        },

                        // canExecute
                        () => {
                            try { new Uri(InstanceURI); }
                            catch (Exception) { return false; }
                            return true;
                        });
                }
                return _authorize_instance;
            }
        }
        private ICommand _authorize_instance;

        #endregion

        #region Constructors

        /// <summary>
        /// Constructs a view model.
        /// </summary>
        public CustomInstancePageViewModel(ConnectWizardViewModel parent) :
            base(parent)
        {
            InstanceURI = "https://";
        }

        #endregion

        #region Methods

        protected override void DoNavigateBack()
        {
            Parent.CurrentPage = Parent.InstanceSelectPage;
        }

        protected override bool CanNavigateBack()
        {
            return true;
        }

        #endregion
    }
}
