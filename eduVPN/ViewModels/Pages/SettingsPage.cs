/*
    eduVPN - VPN for education and research

    Copyright: 2017-2024 The Commons Conservancy
    SPDX-License-Identifier: GPL-3.0+
*/

using eduVPN.ViewModels.Windows;

namespace eduVPN.ViewModels.Pages
{
    /// <summary>
    /// Settings wizard page
    /// </summary>
    public class SettingsPage : ConnectWizardPopupPage
    {
        #region Constructors

        /// <summary>
        /// Constructs a settings wizard page.
        /// </summary>
        /// <param name="wizard">The connecting wizard</param>
        public SettingsPage(ConnectWizard wizard) :
            base(wizard)
        {
        }

        #endregion
    }
}
