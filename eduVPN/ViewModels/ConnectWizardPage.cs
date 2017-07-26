/*
    eduVPN - End-user friendly VPN

    Copyright: 2017, The Commons Conservancy eduVPN Programme
    SPDX-License-Identifier: GPL-3.0+
*/

using Prism.Commands;
using Prism.Mvvm;
using System;
using System.Threading;
using System.Windows.Input;
using System.Windows.Threading;

namespace eduVPN.ViewModels
{
    /// <summary>
    /// Base class for all wizard pages
    /// </summary>
    public class ConnectWizardPage : BindableBase
    {
        #region Properties

        /// <summary>
        /// The page parent
        /// </summary>
        public ConnectWizard Parent { get; }

        /// <summary>
        /// Navigate back
        /// </summary>
        public ICommand NavigateBack
        {
            get
            {
                if (_navigate_back == null)
                    _navigate_back = new DelegateCommand(DoNavigateBack, CanNavigateBack);
                return _navigate_back;
            }
        }
        private ICommand _navigate_back;

        /// <summary>
        /// The page error message; <c>null</c> when no error condition.
        /// </summary>
        public string ErrorMessage
        {
            get { return _error_message; }
            set { _error_message = value; RaisePropertyChanged(); }
        }
        private string _error_message;

        /// <summary>
        /// Is wizard page performing background tasks?
        /// </summary>
        public bool IsBusy
        {
            get { return _task_count > 0; }
        }

        /// <summary>
        /// Number of background tasks the wizard is performing
        /// </summary>
        public uint TaskCount
        {
            get { return _task_count; }
            set {
                if (value != _task_count)
                {
                    _task_count = value;
                    RaisePropertyChanged();
                    RaisePropertyChanged("IsBusy");
                }
            }
        }
        private uint _task_count;

        #endregion

        #region Constructors

        /// <summary>
        /// Constructs a wizard page.
        /// </summary>
        /// <param name="parent">The page parent</param>
        public ConnectWizardPage(ConnectWizard parent)
        {
            Parent = parent;
        }

        #endregion

        #region Methods

        /// <summary>
        /// Called when NavigateBack command is invoked.
        /// </summary>
        protected virtual void DoNavigateBack()
        {
        }

        /// <summary>
        /// Called to test if NavigateBack command is enabled.
        /// </summary>
        /// <returns><c>true</c> if enabled; <c>false</c> otherwise</returns>
        protected virtual bool CanNavigateBack()
        {
            return false;
        }

        /// <summary>
        /// Called when the page is activated.
        /// </summary>
        public virtual void OnActivate()
        {
        }

        #endregion
    }
}
