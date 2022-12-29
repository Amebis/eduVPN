/*
    eduVPN - VPN for education and research

    Copyright: 2017-2023 The Commons Conservancy
    SPDX-License-Identifier: GPL-3.0+
*/

using eduVPN.Models;
using eduVPN.ViewModels.Windows;
using Prism.Commands;

namespace eduVPN.ViewModels.Pages
{
    /// <summary>
    /// Base class for all wizard pages
    /// </summary>
    public class ConnectWizardPage : ValidatableBindableBase
    {
        #region Properties

        /// <summary>
        /// The connecting wizard
        /// </summary>
        public ConnectWizard Wizard { get; }

        /// <summary>
        /// Navigate back command
        /// </summary>
        public virtual DelegateCommand NavigateBack { get; } = new DelegateCommand(() => { }, () => false);

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
