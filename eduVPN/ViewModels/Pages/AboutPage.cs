/*
    eduVPN - VPN for education and research

    Copyright: 2017-2023 The Commons Conservancy
    SPDX-License-Identifier: GPL-3.0+
*/

using eduVPN.ViewModels.Windows;

namespace eduVPN.ViewModels.Pages
{
    /// <summary>
    /// About wizard page
    /// </summary>
    public class AboutPage : ConnectWizardPopupPage
    {
        #region Constructors

        /// <summary>
        /// Constructs a view model.
        /// </summary>
        /// <param name="wizard">The connecting wizard</param>
        public AboutPage(ConnectWizard wizard) :
            base(wizard)
        {
        }

        #endregion
    }
}
