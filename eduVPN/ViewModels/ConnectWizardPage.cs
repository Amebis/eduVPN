/*
    eduVPN - End-user friendly VPN

    Copyright: 2017, The Commons Conservancy eduVPN Programme
    SPDX-License-Identifier: GPL-3.0+
*/

using Prism.Commands;
using Prism.Mvvm;
using System.Windows.Input;

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
                lock (_navigate_back_lock)
                {
                    if (_navigate_back == null)
                        _navigate_back = new DelegateCommand(DoNavigateBack, CanNavigateBack);

                    return _navigate_back;
                }
            }
        }
        private ICommand _navigate_back;
        private object _navigate_back_lock = new object();

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
            // Reset error condition on every backward navigation.
            Parent.Error = null;
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
