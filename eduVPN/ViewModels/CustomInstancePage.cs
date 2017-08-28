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
                    ((DelegateCommandBase)SelectCustomInstance).RaiseCanExecuteChanged();
                }
            }
        }
        private string _uri;

        /// <summary>
        /// Authorize Other Instance Command
        /// </summary>
        public ICommand SelectCustomInstance
        {
            get
            {
                lock (_select_custom_instance_lock)
                {
                    if (_select_custom_instance == null)
                    {
                        _select_custom_instance = new DelegateCommand(
                            // execute
                            () =>
                            {
                                Parent.Error = null;
                                Parent.ChangeTaskCount(+1);
                                try
                                {
                                    // Set authentication instance.
                                    Parent.Configuration.AuthenticatingInstance = new Models.InstanceInfo(new Uri(BaseURI));
                                    Parent.Configuration.AuthenticatingInstance.RequestAuthorization += Parent.Instance_RequestAuthorization;

                                    // Connecting instance will be the same as authenticating.
                                    Parent.Configuration.ConnectingInstance = Parent.Configuration.AuthenticatingInstance;

                                    Parent.CurrentPage = Parent.ProfileSelectPage;
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
        }
        private ICommand _select_custom_instance;
        private object _select_custom_instance_lock = new object();

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
