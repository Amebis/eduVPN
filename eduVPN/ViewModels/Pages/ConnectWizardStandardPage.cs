/*
    eduVPN - VPN for education and research

    Copyright: 2017-2023 The Commons Conservancy
    SPDX-License-Identifier: GPL-3.0+
*/

using eduVPN.ViewModels.Windows;

namespace eduVPN.ViewModels.Pages
{
    /// <summary>
    /// Base class for standard wizard pages
    /// </summary>
    public class ConnectWizardStandardPage : ConnectWizardPage
    {
        #region Constructors

        /// <summary>
        /// Constructs a wizard page.
        /// </summary>
        /// <param name="wizard">The connecting wizard</param>
        public ConnectWizardStandardPage(ConnectWizard wizard) :
            base(wizard)
        {
        }

        #endregion
    }
}
