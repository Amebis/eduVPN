/*
    eduVPN - End-user friendly VPN

    Copyright: 2017, The Commons Conservancy eduVPN Programme
    SPDX-License-Identifier: GPL-3.0+
*/

using eduVPN.Models;
using eduVPN.ViewModels.Windows;
using Prism.Commands;
using System;

namespace eduVPN.ViewModels.Pages
{
    /// <summary>
    /// Base class for all wizard pages
    /// </summary>
    public class ConnectWizardPage : ValidatableBindableBase
    {
        #region Properties

        /// <summary>
        /// The page title
        /// </summary>
        public virtual string Title { get; }

        /// <summary>
        /// The page description
        /// </summary>
        public virtual string Description { get; }

        /// <summary>
        /// The connecting wizard
        /// </summary>
        public ConnectWizard Wizard { get; }

        /// <summary>
        /// Navigate back command
        /// </summary>
        public DelegateCommand NavigateBack
        {
            get
            {
                if (_navigate_back == null)
                    _navigate_back = new DelegateCommand(
                        // execute
                        () =>
                        {
                            Wizard.ChangeTaskCount(+1);
                            try { DoNavigateBack(); }
                            catch (Exception ex) { Wizard.Error = ex; }
                            finally { Wizard.ChangeTaskCount(-1); }
                        },

                        // canExecute
                        CanNavigateBack);

                return _navigate_back;
            }
        }
        private DelegateCommand _navigate_back;

        #endregion

        #region Constructors

        /// <summary>
        /// Constructs a wizard page.
        /// </summary>
        /// <param name="wizard">The connecting wizard</param>
        public ConnectWizardPage(ConnectWizard wizard)
        {
            Wizard = wizard;
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
            // Reset error condition on every page activation.
            Wizard.Error = null;
        }

        #endregion
    }
}
